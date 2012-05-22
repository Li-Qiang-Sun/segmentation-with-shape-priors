using System;
using System.Collections.Generic;
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

        private static ShapeModel CreateSimpleShapeModel1()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));

            List<ShapeEdgeParams> edgeParams = new List<ShapeEdgeParams>();
            edgeParams.Add(new ShapeEdgeParams(0.1, 0.1));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();

            return ShapeModel.Create(edges, edgeParams, edgePairParams);
        }

        private static ShapeModel CreateSimpleShapeModel2()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));
            edges.Add(new ShapeEdge(1, 2));

            List<ShapeEdgeParams> edgeParams = new List<ShapeEdgeParams>();
            edgeParams.Add(new ShapeEdgeParams(0.1, 0.1));
            edgeParams.Add(new ShapeEdgeParams(0.1, 0.1));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(Math.PI * 0.5, 1, Math.PI * 0.1, 5)); // TODO: we need edge length deviations to be relative

            return ShapeModel.Create(edges, edgeParams, edgePairParams);
        }

        private static ShapeModel CreateLetterShapeModel()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));
            edges.Add(new ShapeEdge(0, 2));
            edges.Add(new ShapeEdge(2, 3));
            edges.Add(new ShapeEdge(2, 4));
            edges.Add(new ShapeEdge(4, 5));

            List<ShapeEdgeParams> edgeParams = new List<ShapeEdgeParams>();
            edgeParams.Add(new ShapeEdgeParams(0.2, 0.05));
            edgeParams.Add(new ShapeEdgeParams(0.2, 0.05));
            edgeParams.Add(new ShapeEdgeParams(0.2, 0.05));
            edgeParams.Add(new ShapeEdgeParams(0.2, 0.05));
            edgeParams.Add(new ShapeEdgeParams(0.2, 0.05));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams = new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(-Math.PI * 0.5, 1.3, Math.PI * 0.01, 1)); // TODO: we need edge length deviations to be relative
            edgePairParams.Add(new Tuple<int, int>(1, 2), new ShapeEdgePairParams(Math.PI * 0.5, 1, Math.PI * 0.01, 1));
            edgePairParams.Add(new Tuple<int, int>(2, 3), new ShapeEdgePairParams(-Math.PI * 0.5, 1, Math.PI * 0.01, 1));
            edgePairParams.Add(new Tuple<int, int>(3, 4), new ShapeEdgePairParams(Math.PI * 0.5, 0.77, Math.PI * 0.01, 1));

            return ShapeModel.Create(edges, edgeParams, edgePairParams);
        }

        public MainForm()
        {
            this.InitializeComponent();

            this.segmentationPropertiesGrid.SelectedObject = this.segmentationProperties;
            DebugConfiguration.VerbosityLevel = VerbosityLevel.Everything;
            Console.SetOut(new ConsoleCapture(this.consoleContents));

            this.segmentationWorker.DoWork += DoSegmentation;
            this.segmentationWorker.RunWorkerCompleted += OnSegmentationCompleted;
        }

        private void RunSegmentation(bool regularSegmentation)
        {
            File.Delete("./lower_bound.txt");
            
            this.justSegmentButton.Enabled = false;
            this.startGpuButton.Enabled = false;
            this.startCpuButton.Enabled = false;
            this.segmentationPropertiesGrid.Enabled = false;
            this.pauseContinueButton.Enabled = true;

            this.consoleContents.Clear();

            this.segmentationWorker.RunWorkerAsync(regularSegmentation);
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

        private void LoadModel(out ShapeModel model, out Image2D<Color> image, out Rectangle rectangle)
        {
            double scale;
            Rectangle bigLocation;

            if (this.segmentationProperties.Model == Model.OneEdge)
            {
                model = CreateSimpleShapeModel1();
                scale = 0.2;
                image = Image2D.LoadFromFile("./simple_1.png", scale);
                bigLocation = new Rectangle(153, 124, 796, 480);
            }
            else if (this.segmentationProperties.Model == Model.TwoEdges)
            {
                model = CreateSimpleShapeModel2();
                scale = 0.2;
                image = Image2D.LoadFromFile("./simple_3.png", scale);
                bigLocation = new Rectangle(249, 22, 391, 495);
            }
            else if (this.segmentationProperties.Model == Model.Letter1)
            {
                model = CreateLetterShapeModel();
                scale = 0.2;
                image = Image2D.LoadFromFile("./letter_1.jpg", scale);
                bigLocation = new Rectangle(68, 70, 203, 359);
            }
            else /*if (this.segmentationProperties.Model == Model.Letter2)*/
            {
                model = CreateLetterShapeModel();
                scale = 0.5;
                image = Image2D.LoadFromFile("./letter_2.jpg", scale);
                bigLocation = new Rectangle(126, 35, 148, 188);
            }

            rectangle = new Rectangle(
                (int)(bigLocation.X * scale),
                (int)(bigLocation.Y * scale),
                (int)(bigLocation.Width * scale),
                (int)(bigLocation.Height * scale));
        }

        private void DoSegmentation(object sender, DoWorkEventArgs e)
        {
            Rand.Restart(666);

            bool regularSegmentation = (bool) e.Argument;

            // Register handlers
            segmentator.BreadthFirstBranchAndBoundStatus += OnBfsStatusUpdate;
            segmentator.DepthFirstBranchAndBoundStatus += OnDfsStatusUpdate;
            segmentator.BranchAndBoundStarted += OnBranchAndBoundStarted;
            segmentator.SwitchToDfsBranchAndBound += OnSwitchToDfsBranchAndBound;

            // Setup params
            segmentator.BranchAndBoundType = BranchAndBoundType.Combined;
            segmentator.MaxBfsIterationsInCombinedMode = this.segmentationProperties.BfsIterations;
            segmentator.StatusReportRate = this.segmentationProperties.ReportRate;
            segmentator.BfsFrontSaveRate = this.segmentationProperties.FrontSaveRate;
            segmentator.UnaryTermWeight = this.segmentationProperties.UnaryTermWeight;
            segmentator.ShapeUnaryTermWeight = regularSegmentation ? 0 : this.segmentationProperties.ShapeTermWeight;
            segmentator.ShapeEnergyWeight = this.segmentationProperties.ShapeEnergyWeight;
            segmentator.BrightnessBinaryTermCutoff = this.segmentationProperties.BrightnessBinaryTermCutoff;
            segmentator.ConstantBinaryTermWeight = this.segmentationProperties.ConstantBinaryTermWeight;
            segmentator.MinEdgeWidth = this.segmentationProperties.MinEdgeWidth;
            segmentator.MaxEdgeWidth = this.segmentationProperties.MaxEdgeWidth;
            segmentator.MaxCoordFreedom = this.segmentationProperties.MaxCoordFreedom;
            segmentator.MaxWidthFreedom = this.segmentationProperties.MaxWidthFreedom;

            // Customize lower bound calculators
            ShapeEnergyLowerBoundCalculator shapeEnergyCalculator = new ShapeEnergyLowerBoundCalculator(201, 201);
            segmentator.ShapeEnergyLowerBoundCalculator = shapeEnergyCalculator;

            // Load what has to be segmented
            ShapeModel model;
            Image2D<Color> image;
            Rectangle objectRect;
            this.LoadModel(out model, out image, out objectRect);
            model.BackgroundDistanceCoeff = this.segmentationProperties.BackgroundDistanceCoeff;
            
            // Setup shape model
            this.segmentator.ShapeModel = model;

            // Show original image in status window);
            this.currentImage.Image = Image2D.ToRegularImage(image);

            // Run segmentation
            Image2D<bool> mask = segmentator.SegmentImage(image, objectRect);
            
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

        void OnBfsStatusUpdate(object sender, BreadthFirstBranchAndBoundStatusEventArgs e)
        {
            File.AppendAllText("./lower_bound.txt", e.LowerBound + Environment.NewLine);
            
            this.UpdateBranchAndBoundStatusImages(e);
        }

        void OnDfsStatusUpdate(object sender, DepthFirstBranchAndBoundStatusEventArgs e)
        {
            this.UpdateBranchAndBoundStatusImages(e);
        }

        private void UpdateBranchAndBoundStatusImages(BranchAndBoundStatusEventArgs e)
        {
            if (this.stopReporting)
                return;

            this.Invoke(new MethodInvoker(
                delegate
                {
                    if (this.currentImage.Image != null)
                        this.currentImage.Image.Dispose();
                    this.currentImage.Image = e.StatusImage;

                    if (this.segmentationMaskImage.Image != null)
                        this.segmentationMaskImage.Image.Dispose();
                    this.segmentationMaskImage.Image = e.SegmentationMask;

                    if (this.unaryTermsImage.Image != null)
                        this.unaryTermsImage.Image.Dispose();
                    this.unaryTermsImage.Image = e.UnaryTermsImage;

                    if (this.shapeTermsImage.Image != null)
                        this.shapeTermsImage.Image.Dispose();
                    this.shapeTermsImage.Image = e.ShapeTermsImage;
                }));
        }

        private void OnSegmentationCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Image2D<bool> mask = (Image2D<bool>)e.Result;
            if (mask != null)
                this.segmentationMaskImage.Image = Image2D.ToRegularImage(mask);

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
