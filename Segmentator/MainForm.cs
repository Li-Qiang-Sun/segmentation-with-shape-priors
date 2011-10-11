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
        private static ShapeModel CreateSimpleShapeModel1()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));

            List<ShapeVertexParams> vertexParams = new List<ShapeVertexParams>();
            vertexParams.Add(new ShapeVertexParams(0.15, 0.1));
            vertexParams.Add(new ShapeVertexParams(0.15, 0.1));

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
            vertexParams.Add(new ShapeVertexParams(0.15, 0.1));
            vertexParams.Add(new ShapeVertexParams(0.15, 0.1));
            vertexParams.Add(new ShapeVertexParams(0.15, 0.1));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(Math.PI * 0.5, 1, 0.1, 10)); // TODO: we need edge length deviations to be relative

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
            InitializeComponent();
            RunSegmentation();
        }

        private void RunSegmentation()
        {
            Console.SetOut(new ConsoleCapture(this.consoleContents));

            BackgroundWorker segmentationWorker = new BackgroundWorker();
            segmentationWorker.DoWork += DoSegmentation;
            segmentationWorker.RunWorkerCompleted += OnSegmentationCompleted;
            segmentationWorker.RunWorkerAsync();
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

            BranchAndBoundSegmentator segmentator = new BranchAndBoundSegmentator();
            //segmentator.ShapeModel = CreateSimpleShapeModel1();
            segmentator.ShapeModel = CreateSimpleShapeModel2();
            //segmentator.ShapeModel = CreateLetterShapeModel();
            segmentator.BranchAndBoundType = BranchAndBoundType.Combined;
            segmentator.MaxBfsIterationsInCombinedMode = 30000;
            segmentator.BreadthFirstBranchAndBoundStatus += OnBfsStatusUpdate;
            segmentator.DepthFirstBranchAndBoundStatus += OnDfsStatusUpdate;
            segmentator.StatusReportRate = 100;
            segmentator.ShapeUnaryTermWeight = 3;
            segmentator.ShapeEnergyWeight = 10;

            DebugConfiguration.VerbosityLevel = VerbosityLevel.Everything;

            const double scale = 0.15;
            //Image2D<Color> image = Image2D.LoadFromFile("../../../Images/simple_1.png", scale); // Simple model 1
            //Image2D<Color> image = Image2D.LoadFromFile("../../../Images/simple_2.png", scale); // Simple model 1
            Image2D<Color> image = Image2D.LoadFromFile("../../../Images/simple_3.png", scale); // Simple model 2
            //Image2D<Color> image = Image2D.LoadFromFile("../../../Images/letter_1.jpg", scale); // Letter model
            //Rectangle bigLocation = new Rectangle(153, 124, 796, 480); // simple_1.png
            //Rectangle bigLocation = new Rectangle(334, 37, 272, 547); // simple_2.png
            Rectangle bigLocation = new Rectangle(249, 22, 391, 495); // simple_3.png
            //Rectangle bigLocation = new Rectangle(68, 70, 203, 359); // letter_1.jpg
            Rectangle location = new Rectangle(
                (int)(bigLocation.X * scale),
                (int)(bigLocation.Y * scale),
                (int)(bigLocation.Width * scale),
                (int)(bigLocation.Height * scale));

            Image2D<bool> mask = segmentator.SegmentImage(image, location);
            e.Result = mask;
        }

        void OnBfsStatusUpdate(object sender, BreadthFirstBranchAndBoundStatusEventArgs e)
        {
            this.Invoke(new MethodInvoker(
                delegate
                {
                    if (this.currentImage.Image != null)
                        this.currentImage.Image.Dispose();
                    this.currentImage.Image = e.StatusImage;
                    this.currentEnergyLabel.Text = String.Format("Lower bound: {0:0.000}", e.LowerBound);
                    this.frontSizeLabel.Text = String.Format("Front size: {0:0}", e.FrontSize);
                    this.processingSpeedLabel.Text = String.Format("Processing speed: {0:0.0} items/sec", e.FrontItemsPerSecond);
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
                    this.currentEnergyLabel.Text = String.Format("Upper bound: {0:0.000}", e.UpperBound);
                    this.resultImage.Image = Image2D.ToRegularImage(e.UpperBoundSegmentationMask);
                }));
        }

        private void OnSegmentationCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Image2D<bool> mask = (Image2D<bool>)e.Result;
            this.resultImage.Image = Image2D.ToRegularImage(mask);
        }
    }
}
