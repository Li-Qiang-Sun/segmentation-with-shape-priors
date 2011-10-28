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
        private BranchAndBoundSegmentatorBase segmentator;

        private static ShapeModel CreateSimpleShapeModel1()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));

            List<ShapeVertexParams> vertexParams = new List<ShapeVertexParams>();
            vertexParams.Add(new ShapeVertexParams(0.1, 0.1));
            vertexParams.Add(new ShapeVertexParams(0.1, 0.1));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();

            return ShapeModel.Create(edges, vertexParams, edgePairParams);
        }

        private static ShapeModel CreateSimpleShapeModel2()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));
            edges.Add(new ShapeEdge(1, 2));

            List<ShapeVertexParams> vertexParams = new List<ShapeVertexParams>();
            vertexParams.Add(new ShapeVertexParams(0.1, 0.1));
            vertexParams.Add(new ShapeVertexParams(0.1, 0.1));
            vertexParams.Add(new ShapeVertexParams(0.1, 0.1));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(Math.PI * 0.5, 1, Math.PI * 0.1, 10)); // TODO: we need edge length deviations to be relative

            return ShapeModel.Create(edges, vertexParams, edgePairParams);
        }

        private static ShapeModel CreateLetterShapeModel()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));
            edges.Add(new ShapeEdge(0, 2));
            edges.Add(new ShapeEdge(2, 3));
            edges.Add(new ShapeEdge(2, 4));
            edges.Add(new ShapeEdge(4, 5));

            List<ShapeVertexParams> vertexParams = new List<ShapeVertexParams>();
            vertexParams.Add(new ShapeVertexParams(0.07, 0.05));
            vertexParams.Add(new ShapeVertexParams(0.07, 0.05));
            vertexParams.Add(new ShapeVertexParams(0.07, 0.05));
            vertexParams.Add(new ShapeVertexParams(0.07, 0.05));
            vertexParams.Add(new ShapeVertexParams(0.07, 0.05));
            vertexParams.Add(new ShapeVertexParams(0.07, 0.05));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams = new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(-Math.PI * 0.5, 1.3, 0.1, 10)); // TODO: we need edge length deviations to be relative
            edgePairParams.Add(new Tuple<int, int>(1, 2), new ShapeEdgePairParams(Math.PI * 0.5, 1, 0.1, 10));
            edgePairParams.Add(new Tuple<int, int>(2, 3), new ShapeEdgePairParams(-Math.PI * 0.5, 1, 0.1, 10));
            edgePairParams.Add(new Tuple<int, int>(3, 4), new ShapeEdgePairParams(Math.PI * 0.5, 0.77, 0.1, 10));

            return ShapeModel.Create(edges, vertexParams, edgePairParams);
        }

        private static ShapeModel CreateGiraffeShapeModel()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1)); // Body
            edges.Add(new ShapeEdge(0, 2)); // Neck
            edges.Add(new ShapeEdge(2, 3)); // Head
            edges.Add(new ShapeEdge(0, 4)); // Front leg (top)
            edges.Add(new ShapeEdge(4, 6)); // Front Leg (bottom)
            edges.Add(new ShapeEdge(1, 5)); // Back leg (top)
            edges.Add(new ShapeEdge(5, 7)); // Back leg (bottom)

            List<ShapeVertexParams> vertexParams = new List<ShapeVertexParams>();
            vertexParams.Add(new ShapeVertexParams(0.6, 0.05));
            vertexParams.Add(new ShapeVertexParams(0.5, 0.05));
            vertexParams.Add(new ShapeVertexParams(0.15, 0.02));
            vertexParams.Add(new ShapeVertexParams(0.1, 0.01));
            vertexParams.Add(new ShapeVertexParams(0.1, 0.01));
            vertexParams.Add(new ShapeVertexParams(0.1, 0.01));
            vertexParams.Add(new ShapeVertexParams(0.1, 0.01));
            vertexParams.Add(new ShapeVertexParams(0.1, 0.01));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(-Math.PI * 0.666, 2, 0.1, 0.1));
            edgePairParams.Add(new Tuple<int, int>(1, 2), new ShapeEdgePairParams(-Math.PI * 0.5, 0.1, 0.1, 0.1));
            edgePairParams.Add(new Tuple<int, int>(0, 3), new ShapeEdgePairParams(Math.PI * 0.5, 0.5, 0.1, 0.1));
            edgePairParams.Add(new Tuple<int, int>(3, 4), new ShapeEdgePairParams(0, 1, 0.1, 0.1));
            edgePairParams.Add(new Tuple<int, int>(0, 5), new ShapeEdgePairParams(Math.PI * 0.5, 0.5, 0.1, 0.1));
            edgePairParams.Add(new Tuple<int, int>(5, 6), new ShapeEdgePairParams(0, 1, 0.1, 0.1));

            return ShapeModel.Create(edges, vertexParams, edgePairParams);
        }

        public MainForm()
        {
            this.InitializeComponent();

            DebugConfiguration.VerbosityLevel = VerbosityLevel.Everything;
            this.modelComboBox.SelectedIndex = 0;
        }

        private void RunSegmentation(bool regularSegmentation)
        {
            Console.SetOut(new ConsoleCapture(this.consoleContents));

            this.justSegmentButton.Enabled = false;
            this.startGpuButton.Enabled = false;
            this.startCpuButton.Enabled = false;

            BackgroundWorker segmentationWorker = new BackgroundWorker();
            segmentationWorker.DoWork += DoSegmentation;
            segmentationWorker.RunWorkerCompleted += OnSegmentationCompleted;
            segmentationWorker.RunWorkerAsync(regularSegmentation);
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
            string modelName = null;
            this.Invoke(
                (MethodInvoker) delegate
                {
                    modelName = this.modelComboBox.Items[this.modelComboBox.SelectedIndex].ToString();
                });
            modelName = modelName.ToLowerInvariant();
            
            const double scale = 0.2;
            Rectangle bigLocation;

            if (modelName == "1 edge")
            {
                model = CreateSimpleShapeModel1();
                image = Image2D.LoadFromFile("./simple_1.png", scale);
                bigLocation = new Rectangle(153, 124, 796, 480);
            }
            else if (modelName == "2 edges")
            {
                model = CreateSimpleShapeModel2();
                image = Image2D.LoadFromFile("./simple_3.png", scale);
                bigLocation = new Rectangle(249, 22, 391, 495);
            }
            else /*if (name == "e letter")*/
            {
                model = CreateLetterShapeModel();
                image = Image2D.LoadFromFile("./letter_1.jpg", scale);
                bigLocation = new Rectangle(68, 70, 203, 359);
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
            segmentator.MaxBfsIterationsInCombinedMode = (int)this.bfsIterationsInput.Value;
            segmentator.StatusReportRate = (int)this.reportRateInput.Value;
            segmentator.BfsFrontSaveRate = (int)this.frontSaveRateInput.Value;
            segmentator.ShapeUnaryTermWeight = regularSegmentation ? 0 : (double)this.shapeTermWeightInput.Value;
            segmentator.ShapeEnergyWeight = (double)this.shapeEnergyWeightInput.Value;

            // Load what has to be segmented
            ShapeModel model;
            Image2D<Color> image;
            Rectangle objectRect;
            this.LoadModel(out model, out image, out objectRect);
            
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
            this.Invoke(new MethodInvoker(
                delegate
                {
                    if (this.currentImage.Image != null)
                        this.currentImage.Image.Dispose();
                    this.currentImage.Image = e.StatusImage;
                }));
        }

        void OnDfsStatusUpdate(object sender, DepthFirstBranchAndBoundStatusEventArgs e)
        {
            this.Invoke(new MethodInvoker(
                delegate
                {
                    if (this.currentImage.Image != null)
                        this.currentImage.Image.Dispose();
                    this.currentImage.Image = e.StatusImage;
                    this.resultImage.Image = Image2D.ToRegularImage(e.UpperBoundSegmentationMask);
                }));
        }

        private void OnSegmentationCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Image2D<bool> mask = (Image2D<bool>)e.Result;
            if (mask != null)
                this.resultImage.Image = Image2D.ToRegularImage(mask);

            this.justSegmentButton.Enabled = true;
            this.startGpuButton.Enabled = true;
            this.startCpuButton.Enabled = true;
            this.switchToDfsButton.Enabled = false;
            this.stopButton.Enabled = false;
        }

        private void OnStartGpuButtonClick(object sender, EventArgs e)
        {
            this.segmentator = new BranchAndBoundSegmentatorGpu2();
            this.RunSegmentation(false);
        }

        private void OnStartCpuButtonClick(object sender, EventArgs e)
        {
            this.segmentator = new BranchAndBoundSegmentatorCpu();
            this.RunSegmentation(false);
        }

        private void OnSwitchToDfsButtonClick(object sender, EventArgs e)
        {
            this.switchToDfsButton.Enabled = false;
            this.segmentator.ForceSwitchToDfsBranchAndBound();
        }

        private void OnStopButtonClick(object sender, EventArgs e)
        {
            this.stopButton.Enabled = false;
            this.switchToDfsButton.Enabled = false;
            this.segmentator.ForceStop();
        }

        private void OnJustSegmentButtonClick(object sender, EventArgs e)
        {
            this.segmentator = new BranchAndBoundSegmentatorCpu();
            this.RunSegmentation(true);
        }
    }
}
