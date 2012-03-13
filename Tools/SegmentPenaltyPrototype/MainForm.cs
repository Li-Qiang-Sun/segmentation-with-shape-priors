using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AForge.Genetic;
using AForge.Math.Random;

namespace Research.GraphBasedShapePrior.Tools.SegmentPenaltyPrototype
{
    public partial class MainForm : Form
    {
        private const float PointRadius = 3;
        private const float CoordScale = 300;

        private readonly ProblemProperties properties = new ProblemProperties();

        public MainForm()
        {
            InitializeComponent();

            this.problemPropertiesGrid.SelectedObject = properties;
        }

        private static Vector MapCoords(Vector original)
        {
            return original * CoordScale;
        }

        private static void DrawRectange(Graphics graphics, Pen pen, Vector min, Vector max)
        {
            min = MapCoords(min);
            max = MapCoords(max);
            graphics.DrawRectangle(pen, (float)min.X, (float)min.Y, (float)(max.X - min.X), (float)(max.Y - min.Y));
        }

        private static void DrawPoint(Graphics graphics, Brush brush, Vector point)
        {
            point = MapCoords(point);
            graphics.FillEllipse(brush, (float)point.X - PointRadius, (float)point.Y - PointRadius, PointRadius * 2, PointRadius * 2);
        }

        private static void DrawSegment(Graphics graphics, Pen pen, Vector point1, Vector point2)
        {
            point1 = MapCoords(point1);
            point2 = MapCoords(point2);
            graphics.DrawLine(pen, (float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y);
        }


        private static void DrawText(Graphics graphics, Brush brush, Vector point, string text)
        {
            point = MapCoords(point);
            graphics.DrawString(text, SystemFonts.DefaultFont, brush, (float) point.X, (float) point.Y);
        }

        private void OnCalculateButtonClick(object sender, EventArgs e)
        {
            try
            {
                this.ValidateProperties();
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
                return;
            }

            if (this.solutionPictureBox.Image != null)
                this.solutionPictureBox.Image.Dispose();
            this.solutionPictureBox.Image = new Bitmap(1024, 1024);

            // Solve with brute force
            Vector segmentPoint1, segmentPoint2, point;
            double objective;
            SolveProblemWithStochasticSearch(out segmentPoint1, out segmentPoint2, out point, out objective);

            using (Graphics graphics = Graphics.FromImage(this.solutionPictureBox.Image))
            {
                graphics.Clear(Color.White);

                // Boundaries
                Pen rectPen = new Pen(Color.Green, 3);
                DrawRectange(graphics, rectPen, properties.Box1Min, properties.Box1Max);
                DrawRectange(graphics, rectPen, properties.Box2Min, properties.Box2Max);

                // Solution
                Pen solutionPen = new Pen(Color.Blue, 4);
                DrawSegment(graphics, solutionPen, segmentPoint1, segmentPoint2);
                DrawPoint(graphics, Brushes.Red, point);

                // Print some values
                DrawText(graphics, Brushes.Red, point, String.Format("{0:0.000}", objective));
            }
        }

        private void ValidateProperties()
        {
            if (properties.Box1Min.X > properties.Box1Max.X || properties.Box1Min.Y > properties.Box1Max.Y)
                throw new ApplicationException("Invalid bounding box corners for box 1");
            if (properties.Box2Min.X > properties.Box2Max.X || properties.Box2Min.Y > properties.Box2Max.Y)
                throw new ApplicationException("Invalid bounding box corners for box 2");
        }

        private static IEnumerable<Vector> EnumerateCorners(Vector boxMin, Vector boxMax)
        {
            yield return boxMin;
            yield return new Vector(boxMax.X, boxMin.Y);
            yield return boxMax;
            yield return new Vector(boxMin.X, boxMax.Y);
        }

        class OptimizationObjective : IFitnessFunction
        {
            private readonly ProblemProperties properties;
            private Polygon segmentPoint1Constraints;
            private Polygon segmentPoint2Constraints;
            private Polygon pointConstraints;

            public OptimizationObjective(ProblemProperties properties)
            {
                this.properties = properties;

                IEnumerable<Vector> segment1Corners = EnumerateCorners(this.properties.Box1Min, this.properties.Box1Max);
                IEnumerable<Vector> segment2Corners = EnumerateCorners(this.properties.Box2Min, this.properties.Box2Max);
                this.segmentPoint1Constraints = Polygon.FromPoints(segment1Corners);
                this.segmentPoint2Constraints = Polygon.FromPoints(segment2Corners);
                this.pointConstraints = Polygon.ConvexHull(segment1Corners.Concat(segment2Corners).ToList());
            }

            public void ExtractData(IChromosome chromosome, out Vector segmentPoint1, out Vector segmentPoint2, out Vector point)
            {
                DoubleArrayChromosome castedChromosome = (DoubleArrayChromosome)chromosome;
                segmentPoint1 = new Vector(castedChromosome.Value[0], castedChromosome.Value[1]);
                segmentPoint2 = new Vector(castedChromosome.Value[2], castedChromosome.Value[3]);
                point = new Vector(castedChromosome.Value[4], castedChromosome.Value[5]);
            }

            public double Evaluate(IChromosome chromosome)
            {
                Vector segmentPoint1, segmentPoint2, point;
                ExtractData(chromosome, out segmentPoint1, out segmentPoint2, out point);

                if (!segmentPoint1Constraints.IsPointInside(segmentPoint1))
                    return Double.NegativeInfinity;
                if (!segmentPoint2Constraints.IsPointInside(segmentPoint2))
                    return Double.NegativeInfinity;
                if (!pointConstraints.IsPointInside(point))
                    return Double.NegativeInfinity;

                double distanceSqr = point.DistanceToSegmentSquared(segmentPoint1, segmentPoint2);
                double penalty =
                    Vector.DotProduct(point, properties.Lambda) +
                    Vector.DotProduct(segmentPoint1, properties.Lambda1) +
                    Vector.DotProduct(segmentPoint2, properties.Lambda2);
                return -(distanceSqr * properties.DistanceWeight + penalty);
            }
        }

        void SolveProblemWithStochasticSearch(out Vector bestSegmentPoint1, out Vector bestSegmentPoint2, out Vector bestPoint, out double bestObjective)
        {
            OptimizationObjective objective = new OptimizationObjective(this.properties);
            DoubleArrayChromosome startChromosome = new DoubleArrayChromosome(
                new UniformGenerator(new AForge.Range(0, 1.5f)), new UniformOneGenerator(), new GaussianGenerator(0, 0.1f), 6);
            Population population = new Population(500, startChromosome, objective, new EliteSelection());

            int maxUpdateTime = 0;
            double lastMax = Double.NegativeInfinity;
            int iteration = 0;
            while (true)
            {
                population.RunEpoch();
                if (iteration - maxUpdateTime > 500 && population.FitnessMax - lastMax < 1e-8)
                {
                    Trace.WriteLine("Convergence detected. Breaking...");
                    break;
                }
                if (population.FitnessMax > lastMax)
                {
                    maxUpdateTime = iteration;
                    lastMax = population.FitnessMax;
                }
                if (iteration % 20 == 0)
                    Trace.WriteLine(String.Format("On iteration {0} best={1} avg={2}", iteration + 1, -population.FitnessMax, -population.FitnessAvg));
                ++iteration;
            }

            bestObjective = -population.BestChromosome.Fitness;
            objective.ExtractData(population.BestChromosome, out bestSegmentPoint1, out bestSegmentPoint2, out bestPoint);
        }
    }
}
