using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using MicrosoftResearch.Infer.Maths;
using Research.GraphBasedShapePrior;

namespace Segmentator
{
    public partial class MainForm : Form
    {
        private BranchAndBoundSegmentationAlgorithm segmentator;

        private readonly BackgroundWorker segmentationWorker = new BackgroundWorker();

        private readonly SegmentationProperties segmentationProperties = new SegmentationProperties();

        private bool stopReporting;

        private bool switchedToDfs;

        private bool regularSegmentation;

        private ShapeConstraints bestConstraints;

        private Image segmentedImage;

        public MainForm()
        {
            this.InitializeComponent();

            this.segmentationPropertiesGrid.SelectedObject = this.segmentationProperties;
            DebugConfiguration.VerbosityLevel = VerbosityLevel.Everything;
            Console.SetOut(new ConsoleCapture(this.consoleContents));

            this.segmentationWorker.DoWork += DoSegmentation;
            this.segmentationWorker.RunWorkerCompleted += OnSegmentationCompleted;
        }

        private void RunSegmentation(bool regular)
        {
            File.Delete("./lower_bound.txt");

            this.regularSegmentation = regular;

            this.justSegmentButton.Enabled = false;
            this.startGpuButton.Enabled = false;
            this.startCpuButton.Enabled = false;
            this.segmentationPropertiesGrid.Enabled = false;
            this.pauseContinueButton.Enabled = true;

            this.consoleContents.Clear();

            this.segmentationWorker.RunWorkerAsync();
        }

        private class ConsoleCapture : TextWriter
        {
            private readonly TextBox resultHolder;
            private readonly StringBuilder line = new StringBuilder();
            private readonly object syncRoot = new object();

            public ConsoleCapture(TextBox result)
            {
                this.resultHolder = result;
            }

            public override Encoding Encoding
            {
                get { return Encoding.UTF8; }
            }

            public override void Write(char value)
            {
                lock (syncRoot)
                {
                    line.Append(value);
                    if (value == '\n')
                    {
                        string result = line.ToString();
                        this.resultHolder.Invoke(new MethodInvoker(
                            delegate
                            {
                                int oldSelectionStart = resultHolder.SelectionStart;
                                bool scrollToEnd = oldSelectionStart == resultHolder.Text.Length;
                                resultHolder.Text += result;
                                resultHolder.SelectionStart = scrollToEnd ? resultHolder.Text.Length : oldSelectionStart;
                                resultHolder.ScrollToCaret();
                            }));
                        line.Length = 0;
                    }
                }
            }
        }

        private static Model CreateModel(ModelType modelType)
        {
            switch (modelType)
            {
                case ModelType.OneEdge:
                    return Model.CreateOneEdge();
                case ModelType.TwoEdges:
                    return Model.CreateTwoEdges();
                case ModelType.Letter1:
                    return Model.CreateLetter1();
                case ModelType.Letter2:
                    return Model.CreateLetter2();
                case ModelType.Letter3:
                    return Model.CreateLetter3();
                default:
                    throw new NotSupportedException("Model type is not supported.");
            }
        }

        private void DoSegmentation(object sender, DoWorkEventArgs e)
        {
            Rand.Restart(666);

            // Register handlers
            segmentator.BreadthFirstBranchAndBoundProgress += OnBfsStatusUpdate;
            segmentator.DepthFirstBranchAndBoundProgress += OnDfsStatusUpdate;
            segmentator.BranchAndBoundStarted += OnBranchAndBoundStarted;
            segmentator.BranchAndBoundCompleted += OnBranchAndBoundCompleted;
            segmentator.SwitchToDfsBranchAndBound += OnSwitchToDfsBranchAndBound;

            // Setup params
            segmentator.BranchAndBoundType = BranchAndBoundType.Combined;
            segmentator.MaxBfsIterationsInCombinedMode = this.segmentationProperties.BfsIterations;
            segmentator.ProgressReportRate = this.segmentationProperties.ReportRate;
            segmentator.MaxBfsUpperBoundEstimateProbability = this.segmentationProperties.MaxBfsUpperBoundEstimateProbability;
            segmentator.UnaryTermWeight = this.segmentationProperties.UnaryTermWeight;
            segmentator.ShapeUnaryTermWeight = regularSegmentation ? 0 : this.segmentationProperties.ShapeTermWeight;
            segmentator.ShapeEnergyWeight = this.segmentationProperties.ShapeEnergyWeight;
            segmentator.BrightnessBinaryTermCutoff = this.segmentationProperties.BrightnessBinaryTermCutoff;
            segmentator.ConstantBinaryTermWeight = this.segmentationProperties.ConstantBinaryTermWeight;
            segmentator.MinEdgeWidth = this.segmentationProperties.MinEdgeWidth;
            segmentator.MaxEdgeWidth = this.segmentationProperties.MaxEdgeWidth;


            if (this.segmentationProperties.UseTwoStepApproach)
            {
                segmentator.MaxCoordFreedom = this.segmentationProperties.MaxCoordFreedomPre;
                segmentator.MaxWidthFreedom = this.segmentationProperties.MaxWidthFreedomPre;
            }
            else
            {
                segmentator.MaxCoordFreedom = this.segmentationProperties.MaxCoordFreedom;
                segmentator.MaxWidthFreedom = this.segmentationProperties.MaxWidthFreedom;
            }

            // Customize lower bound calculators
            ShapeEnergyLowerBoundCalculator shapeEnergyCalculator = new ShapeEnergyLowerBoundCalculator(201, 201);
            segmentator.ShapeEnergyLowerBoundCalculator = shapeEnergyCalculator;

            // Load what has to be segmented
            Model model = CreateModel(this.segmentationProperties.ModelType);

            // Setup shape model
            this.segmentator.ShapeModel = model.ShapeModel;
            this.segmentator.ShapeModel.BackgroundDistanceCoeff = this.segmentationProperties.BackgroundDistanceCoeff;

            // Learn color models
            GaussianMixtureModel objectColorModel, backgroundColorModel;
            segmentator.LearnColorModels(
                model.ImageToLearnColors, model.ObjectRectangle, segmentationProperties.MixtureComponents, out objectColorModel, out backgroundColorModel);
            Image2D<Color> shrinkedImage = model.ImageToSegment.Shrink(model.ObjectRectangle);
            this.segmentedImage = Image2D.ToRegularImage(shrinkedImage);

            // Show original image in status window)
            this.currentImage.Image = (Image)this.segmentedImage.Clone();

            // Run segmentation
            Image2D<bool> mask = segmentator.SegmentImage(shrinkedImage, objectColorModel, backgroundColorModel);

            // Re-run segmentation with reduced constraints in two-step mode
            if (!this.regularSegmentation && mask != null && this.segmentationProperties.UseTwoStepApproach)
            {
                segmentator.MaxCoordFreedom = this.segmentationProperties.MaxCoordFreedom;
                segmentator.MaxWidthFreedom = this.segmentationProperties.MaxWidthFreedom;
                segmentator.StartConstraints = this.bestConstraints;

                Console.WriteLine("Performing second pass...");
                mask = segmentator.SegmentImage(shrinkedImage, objectColorModel, backgroundColorModel);
            }

            // Save mask as worker result
            e.Result = mask;
        }

        private void OnSwitchToDfsBranchAndBound(object sender, EventArgs e)
        {
            this.Invoke(
                (MethodInvoker)delegate
                {
                    this.switchToDfsButton.Enabled = false;
                });
        }

        private void OnBranchAndBoundStarted(object sender, EventArgs e)
        {
            this.Invoke(
                (MethodInvoker)delegate
                {
                    this.switchToDfsButton.Enabled = true;
                    this.stopButton.Enabled = true;
                });
        }

        private void OnBranchAndBoundCompleted(object sender, BranchAndBoundCompletedEventArgs e)
        {
            this.bestConstraints = e.ResultConstraints;

            this.Invoke(new MethodInvoker(
                delegate
                {
                    this.DisposeStatusImages();

                    Image statusImage = (Image)this.segmentedImage.Clone();
                    using (Graphics g = Graphics.FromImage(statusImage))
                        e.ResultConstraints.Draw(g);

                    this.currentImage.Image = statusImage;
                    this.segmentationMaskImage.Image = e.CollapsedSolutionSegmentationMask;
                    this.unaryTermsImage.Image = e.CollapsedSolutionUnaryTermsImage;
                    this.shapeTermsImage.Image = e.CollapsedSolutionShapeTermsImage;
                }));
        }

        void OnBfsStatusUpdate(object sender, BreadthFirstBranchAndBoundProgressEventArgs e)
        {
            File.AppendAllText("./lower_bound.txt", e.LowerBound + Environment.NewLine);

            this.UpdateBranchAndBoundStatusImages(e);
        }

        void OnDfsStatusUpdate(object sender, DepthFirstBranchAndBoundProgressEventArgs e)
        {
            this.UpdateBranchAndBoundStatusImages(e);
        }

        private void UpdateBranchAndBoundStatusImages(BranchAndBoundProgressEventArgs e)
        {
            if (this.stopReporting)
                return;

            this.Invoke(new MethodInvoker(
                delegate
                {
                    this.DisposeStatusImages();

                    Image statusImage = (Image)this.segmentedImage.Clone();
                    using (Graphics g = Graphics.FromImage(statusImage))
                        e.Constraints.Draw(g);

                    this.currentImage.Image = statusImage;
                    this.segmentationMaskImage.Image = e.SegmentationMask;
                    this.unaryTermsImage.Image = e.UnaryTermsImage;
                    this.shapeTermsImage.Image = e.ShapeTermsImage;
                    this.bestSegmentationMaskImage.Image = e.BestMaskEstimate;
                }));
        }

        private void DisposeStatusImages()
        {
            if (this.currentImage.Image != null)
            {
                this.currentImage.Image.Dispose();
                this.currentImage.Image = null;
            }

            if (this.segmentationMaskImage.Image != null)
            {
                this.segmentationMaskImage.Image.Dispose();
                this.segmentationMaskImage.Image = null;
            }

            if (this.unaryTermsImage.Image != null)
            {
                this.unaryTermsImage.Image.Dispose();
                this.unaryTermsImage.Image = null;
            }

            if (this.shapeTermsImage.Image != null)
            {
                this.shapeTermsImage.Image.Dispose();
                this.shapeTermsImage.Image = null;
            }

            if (this.bestSegmentationMaskImage.Image != null)
            {
                this.bestSegmentationMaskImage.Image.Dispose();
                this.bestSegmentationMaskImage.Image.Dispose();
            }
        }

        private void OnSegmentationCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (this.regularSegmentation)
            {
                // No BranchAndBoundFinished event in this case
                this.DisposeStatusImages();
                this.currentImage.Image = this.segmentedImage;
                Image2D<bool> mask = (Image2D<bool>) e.Result;
                this.segmentationMaskImage.Image = Image2D.ToRegularImage(mask);
            }
            
            this.justSegmentButton.Enabled = true;
            this.startGpuButton.Enabled = true;
            this.startCpuButton.Enabled = true;
            this.switchToDfsButton.Enabled = false;
            this.stopButton.Enabled = false;
            this.pauseContinueButton.Enabled = false;
            this.segmentationPropertiesGrid.Enabled = true;
        }

        private void OnStartGpuButtonClick(object sender, EventArgs e)
        {
            this.segmentator = new BranchAndBoundSegmentationAlgorithm();
            this.segmentator.ShapeTermCalculator = new GpuShapeTermsLowerBoundCalculator();
            this.RunSegmentation(false);
        }

        private void OnStartCpuButtonClick(object sender, EventArgs e)
        {
            this.segmentator = new BranchAndBoundSegmentationAlgorithm();
            this.RunSegmentation(false);
        }

        private void OnSwitchToDfsButtonClick(object sender, EventArgs e)
        {
            this.switchToDfsButton.Enabled = false;
            this.switchedToDfs = true;
            this.segmentator.ForceSwitchToDfsBranchAndBound();
        }

        private void OnStopButtonClick(object sender, EventArgs e)
        {
            this.stopButton.Enabled = false;
            this.pauseContinueButton.Enabled = false;
            this.switchToDfsButton.Enabled = false;
            this.segmentator.ForceStop();
        }

        private void OnJustSegmentButtonClick(object sender, EventArgs e)
        {
            this.segmentator = new BranchAndBoundSegmentationAlgorithm();
            this.RunSegmentation(true);
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            this.stopReporting = true;
        }

        private void OnPauseContinueButtonClick(object sender, EventArgs e)
        {
            if (this.segmentator.IsPaused)
            {
                this.pauseContinueButton.Text = "Pause";
                this.stopButton.Enabled = true;
                if (!this.switchedToDfs)
                    this.switchToDfsButton.Enabled = true;
                this.segmentator.Continue();
            }
            else
            {
                this.pauseContinueButton.Text = "Continue";
                this.stopButton.Enabled = false;
                this.switchToDfsButton.Enabled = false;
                this.segmentator.Pause();
            }
        }
    }
}
