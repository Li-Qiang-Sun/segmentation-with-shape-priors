using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using MicrosoftResearch.Infer.Maths;
using Research.GraphBasedShapePrior;
using Research.GraphBasedShapePrior.Util;
using Vector = Research.GraphBasedShapePrior.Util.Vector;

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

        private bool TryValidateProperties()
        {
            try
            {
                this.segmentationProperties.Validate();
                return true;
            }
            catch (PropertyValidationException e)
            {
                MessageBox.Show(e.Message, "Invalid settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void RunSegmentation(bool regular)
        {
            if (!this.TryValidateProperties())
                return;

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

            ShapeEnergyLowerBoundCalculator shapeEnergyCalculator;
            if (this.segmentationProperties.UseTwoStepApproach)
            {
                segmentator.MaxCoordFreedom = this.segmentationProperties.MaxCoordFreedomPre;
                segmentator.MaxWidthFreedom = this.segmentationProperties.MaxWidthFreedomPre;
                shapeEnergyCalculator = new ShapeEnergyLowerBoundCalculator(
                    this.segmentationProperties.LengthGridSizePre, this.segmentationProperties.AngleGridSizePre);
            }
            else
            {
                segmentator.MaxCoordFreedom = this.segmentationProperties.MaxCoordFreedom;
                segmentator.MaxWidthFreedom = this.segmentationProperties.MaxWidthFreedom;
                shapeEnergyCalculator = new ShapeEnergyLowerBoundCalculator(
                    this.segmentationProperties.LengthGridSize, this.segmentationProperties.AngleGridSize);
            }

            segmentator.ShapeEnergyLowerBoundCalculator = shapeEnergyCalculator;

            // Load model
            ShapeModel model = ShapeModel.LoadFromFile(this.segmentationProperties.ShapeModel);
            ObjectBackgroundColorModels colorModels = ObjectBackgroundColorModels.LoadFromFile(this.segmentationProperties.ColorModel);
            
            // Load and downscale image
            Image2D<Color> originalImage = Image2D.LoadFromFile(this.segmentationProperties.ImageToSegment);
            double scale = this.segmentationProperties.DownscaledImageSize / (double)Math.Max(originalImage.Width, originalImage.Height);
            Image2D<Color> downscaledImage = Image2D.LoadFromFile(this.segmentationProperties.ImageToSegment, scale);
            this.segmentedImage = Image2D.ToRegularImage(downscaledImage);

            // Setup shape model)))
            this.segmentator.ShapeModel = model;
            this.segmentator.ShapeModel.BackgroundDistanceCoeff = this.segmentationProperties.BackgroundDistanceCoeff;

            // Show original image in status window)
            this.currentImage.Image = (Image)this.segmentedImage.Clone();

            // Run segmentation
            Image2D<bool> mask = segmentator.SegmentImage(downscaledImage, colorModels);

            // Re-run segmentation with reduced constraints in two-step mode
            if (!this.regularSegmentation && mask != null && this.segmentationProperties.UseTwoStepApproach)
            {
                segmentator.MaxCoordFreedom = this.segmentationProperties.MaxCoordFreedom;
                segmentator.MaxWidthFreedom = this.segmentationProperties.MaxWidthFreedom;
                segmentator.StartConstraints = this.bestConstraints;
                segmentator.ShapeEnergyLowerBoundCalculator = new ShapeEnergyLowerBoundCalculator(
                    this.segmentationProperties.LengthGridSize, this.segmentationProperties.AngleGridSize);

                Console.WriteLine("Performing second pass...");
                mask = segmentator.SegmentImage(downscaledImage, colorModels);
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

                    this.currentImage.Image = CreateStatusImage(e.ResultConstraints, false, false, false, true);
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

                    this.currentImage.Image = CreateStatusImage(e.Constraints, true, true, true, false);
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
                Image2D<bool> mask = (Image2D<bool>)e.Result;
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

        private Image DrawConstraints(
            Size imageSize, float constraintsScale, ShapeConstraints shapeConstraints, bool drawVertexConstraints, bool drawMinEdgeWidth, bool drawMaxEdgeWidth, bool drawAverageEdgeWidth)
        {
            Bitmap backgroundImage = new Bitmap(imageSize.Width, imageSize.Height);
            using (Graphics graphics = Graphics.FromImage(backgroundImage))
                graphics.Clear(Color.Black);
            return DrawConstraintsOnTopOfImage(
                backgroundImage, constraintsScale, shapeConstraints, drawVertexConstraints, drawMinEdgeWidth, drawMaxEdgeWidth, drawAverageEdgeWidth);
        }

        private Image DrawConstraintsOnTopOfImage(
            Image backgroundImage, float drawSizeRatio, ShapeConstraints shapeConstraints, bool drawVertexConstraints, bool drawMinEdgeWidth, bool drawMaxEdgeWidth, bool drawAverageEdgeWidth)
        {
            const float pointRadius = 4;
            const float lineWidth = 2;

            Bitmap statusImage = new Bitmap((int)(backgroundImage.Width * drawSizeRatio), (int)(backgroundImage.Height * drawSizeRatio));
            using (Graphics graphics = Graphics.FromImage(statusImage))
            {
                graphics.DrawImage(backgroundImage, 0, 0, statusImage.Width, statusImage.Height);

                if (drawVertexConstraints)
                {
                    foreach (VertexConstraints vertexConstraint in shapeConstraints.VertexConstraints)
                    {
                        graphics.DrawRectangle(
                            new Pen(Color.Orange, lineWidth),
                            (float)vertexConstraint.MinCoord.X * drawSizeRatio,
                            (float)vertexConstraint.MinCoord.Y * drawSizeRatio,
                            (float)(vertexConstraint.MaxCoord.X - vertexConstraint.MinCoord.X) * drawSizeRatio,
                            (float)(vertexConstraint.MaxCoord.Y - vertexConstraint.MinCoord.Y) * drawSizeRatio);
                    }
                }

                foreach (VertexConstraints vertexConstraint in shapeConstraints.VertexConstraints)
                {
                    graphics.FillEllipse(
                        Brushes.Black,
                        (float)vertexConstraint.MiddleCoord.X * drawSizeRatio - pointRadius,
                        (float)vertexConstraint.MiddleCoord.Y * drawSizeRatio - pointRadius,
                        2 * pointRadius,
                        2 * pointRadius);
                }

                for (int i = 0; i < shapeConstraints.ShapeStructure.Edges.Count; ++i)
                {
                    ShapeEdge edge = shapeConstraints.ShapeStructure.Edges[i];
                    Vector point1 = shapeConstraints.VertexConstraints[edge.Index1].MiddleCoord * drawSizeRatio;
                    Vector point2 = shapeConstraints.VertexConstraints[edge.Index2].MiddleCoord * drawSizeRatio;
                    graphics.DrawLine(new Pen(Color.Black, lineWidth), MathHelper.VecToPointF(point1), MathHelper.VecToPointF(point2));

                    if (drawMinEdgeWidth || drawMaxEdgeWidth || drawAverageEdgeWidth)
                    {
                        EdgeConstraints edgeConstraint = shapeConstraints.EdgeConstraints[i];
                        Vector diff = point2 - point1;
                        Vector edgeNormal = (new Vector(diff.Y, -diff.X)).GetNormalized();

                        if (drawMaxEdgeWidth)
                            DrawOrientedRectange(graphics, point1, point2, edgeNormal, (float)edgeConstraint.MaxWidth * drawSizeRatio, new Pen(Color.Cyan, lineWidth));
                        if (drawMinEdgeWidth)
                            DrawOrientedRectange(graphics, point1, point2, edgeNormal, (float)edgeConstraint.MinWidth * drawSizeRatio, new Pen(Color.Red, lineWidth));
                        if (drawAverageEdgeWidth)
                            DrawOrientedRectange(graphics, point1, point2, edgeNormal, (float)(edgeConstraint.MinWidth + edgeConstraint.MaxWidth) * drawSizeRatio * 0.5f, new Pen(Color.Blue, lineWidth));
                    }
                }
            }

            return statusImage;
        }

        private Image CreateStatusImage(
            ShapeConstraints shapeConstraints, bool drawVertexConstraints, bool drawMinEdgeWidth, bool drawMaxEdgeWidth, bool drawAverageEdgeWidth)
        {
            return this.DrawConstraintsOnTopOfImage(
                this.segmentedImage, 2, shapeConstraints, drawVertexConstraints, drawMinEdgeWidth, drawMaxEdgeWidth, drawAverageEdgeWidth);
        }

        private void DrawOrientedRectange(Graphics graphics, Vector point1, Vector point2, Vector sideDirection, float sideWidth, Pen pen)
        {
            graphics.DrawPolygon(
                pen,
                new[]
                    {
                        MathHelper.VecToPointF(point1 - sideDirection * sideWidth * 0.5),
                        MathHelper.VecToPointF(point1 + sideDirection * sideWidth * 0.5),
                        MathHelper.VecToPointF(point2 + sideDirection * sideWidth * 0.5),
                        MathHelper.VecToPointF(point2 - sideDirection * sideWidth * 0.5)
                    });
        }
    }
}
