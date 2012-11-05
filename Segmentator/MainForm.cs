using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Research.GraphBasedShapePrior;
using Research.GraphBasedShapePrior.Util;
using Random = Research.GraphBasedShapePrior.Util.Random;
using Vector = Research.GraphBasedShapePrior.Util.Vector;

namespace Segmentator
{
    public partial class MainForm : Form
    {
        private SegmentationAlgorithmBase segmentator;

        private readonly BackgroundWorker segmentationWorker = new BackgroundWorker();

        private readonly SegmentationProperties segmentationProperties = new SegmentationProperties();

        private bool stopReporting;

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

        private void RunSegmentation()
        {
            if (!this.TryValidateProperties())
                return;

            this.startGpuButton.Enabled = false;
            this.startCpuButton.Enabled = false;
            this.segmentationPropertiesGrid.Enabled = false;
            this.pauseContinueButton.Enabled = true;
            this.stopButton.Enabled = true;

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

        private void SetupBranchAndBoundSegmentationAlgorithm(BranchAndBoundSegmentationAlgorithm algorithm)
        {
            algorithm.BreadthFirstBranchAndBoundProgress += OnBfsStatusUpdate;
            algorithm.BranchAndBoundCompleted += OnBranchAndBoundCompleted;

            ShapeEnergyLowerBoundCalculator shapeEnergyCalculator;
            algorithm.ProgressReportRate = this.segmentationProperties.BranchAndBoundReportRate;
            algorithm.MinEdgeWidth = this.segmentationProperties.MinEdgeWidth;
            algorithm.MaxEdgeWidth = this.segmentationProperties.MaxEdgeWidth;
            if (this.segmentationProperties.UseTwoStepApproach)
            {
                algorithm.MaxCoordFreedom = this.segmentationProperties.MaxCoordFreedomPre;
                algorithm.MaxWidthFreedom = this.segmentationProperties.MaxWidthFreedomPre;
                shapeEnergyCalculator = new ShapeEnergyLowerBoundCalculator(
                    this.segmentationProperties.LengthGridSizePre, this.segmentationProperties.AngleGridSizePre);
            }
            else
            {
                algorithm.MaxCoordFreedom = this.segmentationProperties.MaxCoordFreedom;
                algorithm.MaxWidthFreedom = this.segmentationProperties.MaxWidthFreedom;
                shapeEnergyCalculator = new ShapeEnergyLowerBoundCalculator(
                    this.segmentationProperties.LengthGridSize, this.segmentationProperties.AngleGridSize);
            }
            algorithm.ShapeEnergyLowerBoundCalculator = shapeEnergyCalculator;
        }

        private void SetupCoordinateDescentSegmentationAlgorithm(CoordinateDescentSegmentationAlgorithm algorithm)
        {
            algorithm.MinIterationCount = this.segmentationProperties.MinDescentIterations;
            algorithm.MaxIterationCount = this.segmentationProperties.MaxDescentIterations;
            algorithm.MinChangeRate = this.segmentationProperties.MinDescentPixelChangeRate;

            algorithm.IterationFinished += OnCoordinateDescentIterationFinished;
            algorithm.ShapeFitter.AnnealingProgress += OnCoordinateDescentAnnealingProgress;

            this.SetupShapeMutator(algorithm.ShapeMutator);
            this.SetupAnnealing(algorithm.ShapeFitter);
        }

        private void SetupAnnealingSegmentationAlgorithm(AnnealingSegmentationAlgorithm algorithm)
        {
            algorithm.SolutionFitter.AnnealingProgress += OnAnnealingBasedSegmentatorProgress;

            if (!String.IsNullOrEmpty(this.segmentationProperties.InitialShape))
                algorithm.StartShape = Shape.LoadFromFile(this.segmentationProperties.InitialShape);

            this.SetupShapeMutator(algorithm.ShapeMutator);
            this.SetupAnnealing(algorithm.SolutionFitter);
        }

        private void SetupSimpleSegmentationAlgorithm(SimpleSegmentationAlgorithm algorithm)
        {
            if (!String.IsNullOrEmpty(this.segmentationProperties.InitialShape))
                algorithm.Shape = Shape.LoadFromFile(this.segmentationProperties.InitialShape);
        }

        private void SetupShapeMutator(ShapeMutator mutator)
        {
            mutator.EdgeWidthMutationWeight = this.segmentationProperties.EdgeWidthMutationWeight;
            mutator.EdgeWidthMutationPower = this.segmentationProperties.EdgeWidthMutationPower;
            mutator.EdgeLengthMutationWeight = this.segmentationProperties.EdgeLengthMutationWeight;
            mutator.EdgeLengthMutationPower = this.segmentationProperties.EdgeLengthMutationPower;
            mutator.EdgeAngleMutationWeight = this.segmentationProperties.EdgeAngleMutationWeight;
            mutator.EdgeAngleMutationPower = this.segmentationProperties.EdgeAngleMutationPower;
            mutator.ShapeTranslationWeight = this.segmentationProperties.ShapeTranslationWeight;
            mutator.ShapeTranslationPower = this.segmentationProperties.ShapeTranslationPower;
            mutator.ShapeScaleWeight = this.segmentationProperties.ShapeScaleWeight;
            mutator.ShapeScalePower = this.segmentationProperties.ShapeScalePower;
        }

        private void SetupAnnealing<T>(SimulatedAnnealingMinimizer<T> minimizer)
        {
            minimizer.MaxIterations = this.segmentationProperties.MaxAnnealingIterations;
            minimizer.MaxStallingIterations = this.segmentationProperties.MaxAnnealingStallingIterations;
            minimizer.ReannealingInterval = this.segmentationProperties.ReannealingInterval;
            minimizer.ReportRate = this.segmentationProperties.AnnealingReportRate;
            minimizer.StartTemperature = this.segmentationProperties.AnnealingStartTemperature;
        }

        private void DoSegmentation(object sender, DoWorkEventArgs e)
        {
            Random.SetSeed(666);
            
            // Common settings
            segmentator.ColorUnaryTermWeight = this.segmentationProperties.ColorTermWeight;
            segmentator.ShapeUnaryTermWeight = this.segmentationProperties.ShapeTermWeight;
            segmentator.ColorDifferencePairwiseTermWeight = this.segmentationProperties.ColorDifferencePairwiseTermWeight;
            segmentator.ColorDifferencePairwiseTermCutoff = this.segmentationProperties.ColorDifferencePairwiseTermCutoff;
            segmentator.ConstantPairwiseTermWeight = this.segmentationProperties.ConstantPairwiseTermWeight;
            segmentator.ShapeEnergyWeight = this.segmentationProperties.ShapeEnergyWeight;

            // Custom setup
            if (this.segmentator is BranchAndBoundSegmentationAlgorithm)
                this.SetupBranchAndBoundSegmentationAlgorithm((BranchAndBoundSegmentationAlgorithm)this.segmentator);
            else if (this.segmentator is CoordinateDescentSegmentationAlgorithm)
                this.SetupCoordinateDescentSegmentationAlgorithm((CoordinateDescentSegmentationAlgorithm)this.segmentator);
            else if (this.segmentator is AnnealingSegmentationAlgorithm)
                this.SetupAnnealingSegmentationAlgorithm((AnnealingSegmentationAlgorithm)this.segmentator);
            else if (this.segmentator is SimpleSegmentationAlgorithm)
                this.SetupSimpleSegmentationAlgorithm((SimpleSegmentationAlgorithm)this.segmentator);

            // Load color models
            ObjectBackgroundColorModels colorModels = ObjectBackgroundColorModels.LoadFromFile(this.segmentationProperties.ColorModel);

            // Load and downscale image
            Image2D<Color> originalImage = Image2D.LoadFromFile(this.segmentationProperties.ImageToSegment);
            double scale = this.segmentationProperties.DownscaledImageSize / (double)Math.Max(originalImage.Width, originalImage.Height);
            Image2D<Color> downscaledImage = Image2D.LoadFromFile(this.segmentationProperties.ImageToSegment, scale);
            this.segmentedImage = Image2D.ToRegularImage(downscaledImage);

            // Setup shape model
            ShapeModel model = ShapeModel.LoadFromFile(this.segmentationProperties.ShapeModel);
            this.segmentator.ShapeModel = model;

            // Show original image in status window)
            this.currentImage.Image = (Image)this.segmentedImage.Clone();

            // Run segmentation
            SegmentationSolution solution = segmentator.SegmentImage(downscaledImage, colorModels);

            // Re-run B&B segmentation with reduced constraints in two-step mode
            if (this.segmentator is BranchAndBoundSegmentationAlgorithm && this.segmentationProperties.UseTwoStepApproach && !this.segmentator.WasStopped)
            {
                BranchAndBoundSegmentationAlgorithm branchAndBoundSegmentator =
                    (BranchAndBoundSegmentationAlgorithm)this.segmentator;

                branchAndBoundSegmentator.MaxCoordFreedom = this.segmentationProperties.MaxCoordFreedom;
                branchAndBoundSegmentator.MaxWidthFreedom = this.segmentationProperties.MaxWidthFreedom;
                branchAndBoundSegmentator.StartConstraints = this.bestConstraints;
                branchAndBoundSegmentator.ShapeEnergyLowerBoundCalculator = new ShapeEnergyLowerBoundCalculator(
                    this.segmentationProperties.LengthGridSize, this.segmentationProperties.AngleGridSize);

                Console.WriteLine("Performing second pass...");
                solution = segmentator.SegmentImage(downscaledImage, colorModels);
            }

            // Save mask as worker result
            e.Result = solution;
        }

        private void OnCoordinateDescentAnnealingProgress(object sender, SimulatedAnnealingProgressEventArgs<Shape> e)
        {
            this.Invoke(new MethodInvoker(
                delegate
                {
                    this.currentImage.Image.Dispose();
                    this.currentImage.Image = CreateStatusImage(e.CurrentSolution);
                }));
        }

        private void OnAnnealingBasedSegmentatorProgress(object sender, SimulatedAnnealingProgressEventArgs<Shape> e)
        {
            this.Invoke(new MethodInvoker(
                delegate
                {
                    this.currentImage.Image.Dispose();
                    this.currentImage.Image = CreateStatusImage(e.CurrentSolution);
                    // TODO: show current mask somehow
                }));
        }

        void OnCoordinateDescentIterationFinished(object sender, SegmentationIterationFinishedEventArgs e)
        {
            this.Invoke(new MethodInvoker(
                delegate
                {
                    this.DisposeStatusImages();

                    this.currentImage.Image = CreateStatusImage(e.Shape);
                    this.segmentationMaskImage.Image = Image2D.ToRegularImage(e.SegmentationMask);
                    this.unaryTermsImage.Image = Image2D.ToRegularImage(e.UnaryTermsImage, -5, 5);
                    this.shapeTermsImage.Image = Image2D.ToRegularImage(e.ShapeTermsImage, -5, 5);
                }));
        }

        private void OnBranchAndBoundCompleted(object sender, BranchAndBoundCompletedEventArgs e)
        {
            this.bestConstraints = e.ResultConstraints;
        }

        private void OnBfsStatusUpdate(object sender, BranchAndBoundProgressEventArgs e)
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
                    this.segmentationMaskImage.Image = Image2D.ToRegularImage(e.SegmentationMask);
                    this.unaryTermsImage.Image = Image2D.ToRegularImage(e.UnaryTermsImage, -5, 5);
                    this.shapeTermsImage.Image = Image2D.ToRegularImage(e.ShapeTermsImage, -5, 5);
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
        }

        private void OnSegmentationCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.DisposeStatusImages();
            SegmentationSolution result = (SegmentationSolution)e.Result;
            if (result.Shape != null)
                this.currentImage.Image = CreateStatusImage(result.Shape);
            else
                this.currentImage.Image = (Image)this.segmentedImage.Clone();
            if (result.Mask != null)
                this.segmentationMaskImage.Image = Image2D.ToRegularImage(result.Mask);

            this.startGpuButton.Enabled = true;
            this.startCpuButton.Enabled = true;
            this.stopButton.Enabled = false;
            this.pauseContinueButton.Enabled = false;
            this.segmentationPropertiesGrid.Enabled = true;
        }

        private void OnStartGpuButtonClick(object sender, EventArgs e)
        {
            if (this.segmentationProperties.Algorithm == SegmentationAlgorithm.BranchAndBound)
            {
                BranchAndBoundSegmentationAlgorithm branchAndBoundSegmentator = new BranchAndBoundSegmentationAlgorithm();
                branchAndBoundSegmentator.ShapeTermCalculator = new GpuShapeTermsLowerBoundCalculator();
                this.segmentator = branchAndBoundSegmentator;
                this.RunSegmentation();
            }
            else
            {
                MessageBox.Show(
                    "No GPU version available for this kind of algorithm.",
                    "Information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void OnStartCpuButtonClick(object sender, EventArgs e)
        {
            if (this.segmentationProperties.Algorithm == SegmentationAlgorithm.BranchAndBound)
                this.segmentator = new BranchAndBoundSegmentationAlgorithm();
            else if (this.segmentationProperties.Algorithm == SegmentationAlgorithm.CoordinateDescent)
                this.segmentator = new CoordinateDescentSegmentationAlgorithm();
            else if (this.segmentationProperties.Algorithm == SegmentationAlgorithm.Annealing)
                this.segmentator = new AnnealingSegmentationAlgorithm();
            else
                this.segmentator = new SimpleSegmentationAlgorithm();

            this.RunSegmentation();
        }

        private void OnStopButtonClick(object sender, EventArgs e)
        {
            this.stopButton.Enabled = false;
            this.pauseContinueButton.Enabled = false;
            this.segmentator.Stop();
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
                this.segmentator.Continue();
            }
            else
            {
                this.pauseContinueButton.Text = "Continue";
                this.stopButton.Enabled = false;
                this.segmentator.Pause();
            }
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

        private Image CreateStatusImage(Shape shape)
        {
            return this.DrawConstraintsOnTopOfImage(
                this.segmentedImage, 2, ShapeConstraints.CreateFromShape(shape), false, false, false, true);
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
