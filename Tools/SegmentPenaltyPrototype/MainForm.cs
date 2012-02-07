using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Research.GraphBasedShapePrior.Tools.SegmentPenaltyPrototype
{
    public partial class MainForm : Form
    {
        private const float PointRadius = 3;
        private const float CoordScale = 300;
        private const float SolutionStep = 0.01f;

        private readonly ProblemProperties properties = new ProblemProperties();

        public MainForm()
        {
            InitializeComponent();

            this.problemPropertiesGrid.SelectedObject = properties;
        }

        private Vector MapCoords(Vector original)
        {
            return original * CoordScale;
        }

        private void DrawRectange(Graphics graphics, Pen pen, Vector min, Vector max)
        {
            min = MapCoords(min);
            max = MapCoords(max);
            graphics.DrawRectangle(pen, (float)min.X, (float)min.Y, (float)(max.X - min.X), (float)(max.Y - min.Y));
        }

        private void DrawPoint(Graphics graphics, Brush brush, Vector point)
        {
            point = MapCoords(point);
            graphics.FillEllipse(brush, (float)point.X - PointRadius, (float)point.Y - PointRadius, PointRadius * 2, PointRadius * 2);
        }

        private void DrawSegment(Graphics graphics, Pen pen, Vector point1, Vector point2)
        {
            point1 = MapCoords(point1);
            point2 = MapCoords(point2);
            graphics.DrawLine(pen, (float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y);
        }


        private void DrawText(Graphics graphics, Brush brush, Vector point, string text)
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
            Vector solutionPoint1, solutionPoint2, distanceFromPoint;
            double distanceSqr, penalty;
            SolveProblemWithBruteForce(out solutionPoint1, out solutionPoint2, out distanceFromPoint, out distanceSqr, out penalty);
            Debug.Assert(Math.Abs((properties.Point - distanceFromPoint).LengthSquared - distanceSqr) < 1e-6);

            // Solve with GDT
            Vector gdtSolutionPoint1, gdtSolutionPoint2, gdtBestPoint;
            double gdtObjective;
            SolveProblemWithDistanceTransform(out gdtSolutionPoint1, out gdtSolutionPoint2, out gdtBestPoint, out gdtObjective);

            using (Graphics graphics = Graphics.FromImage(this.solutionPictureBox.Image))
            {
                graphics.Clear(Color.White);

                // Boundaries
                Pen rectPen = new Pen(Color.Green, 3);
                DrawRectange(graphics, rectPen, properties.Box1Min, properties.Box1Max);
                DrawRectange(graphics, rectPen, properties.Box2Min, properties.Box2Max);

                // Solution
                Pen solutionPen = new Pen(Color.Blue, 4);
                DrawSegment(graphics, solutionPen, solutionPoint1, solutionPoint2);
                Pen gdtSolutionPen = new Pen(Color.Red, 2);
                DrawSegment(graphics, gdtSolutionPen, gdtSolutionPoint1, gdtSolutionPoint2);
                DrawPoint(graphics, Brushes.Yellow, gdtBestPoint);

                // Point itself
                DrawPoint(graphics, Brushes.Black, properties.Point);

                // Distance to solution
                DrawSegment(graphics, Pens.Pink, distanceFromPoint, properties.Point);

                // Print some values
                DrawText(graphics, Brushes.Red, properties.Point, String.Format("D={0:0.000} P={1:0.000} T={2:0.000} GT={3:0.000}", distanceSqr, penalty, distanceSqr + penalty, gdtObjective));
            }
        }

        private void ValidateProperties()
        {
            if (properties.Box1Min.X > properties.Box1Max.X || properties.Box1Min.Y > properties.Box1Max.Y)
                throw new ApplicationException("Invalid bounding box corners for box 1");
            if (properties.Box2Min.X > properties.Box2Max.X || properties.Box2Min.Y > properties.Box2Max.Y)
                throw new ApplicationException("Invalid bounding box corners for box 2");
        }

        void SolveProblemWithDistanceTransform(out Vector point1, out Vector point2, out Vector bestPoint, out double objective)
        {
            DistanceTransformBasedSolver solver = new DistanceTransformBasedSolver(new Size(400, 400), new Vector(2, 2));
            solver.Solve(
                new VertexConstraint(this.properties.Box1Min, this.properties.Box1Max, 1, 100),
                this.properties.Lambda1,
                new VertexConstraint(this.properties.Box2Min, this.properties.Box2Max, 1, 100),
                this.properties.Lambda2);

            objective = solver.GetObjective(this.properties.Point);
            solver.GetBestEdge(this.properties.Point, out point1, out point2);
            bestPoint = solver.GetBestPoint(this.properties.Point);
        }

        void SolveProblemWithBruteForce(out Vector point1, out Vector point2, out Vector distanceFromPoint, out double distanceSqr, out double penalty)
        {
            double bestObjective = Double.PositiveInfinity;
            point1 = Vector.Zero;
            point2 = Vector.Zero;
            distanceFromPoint = Vector.Zero;
            distanceSqr = 0;
            penalty = 0;

            for (double x1 = this.properties.Box1Min.X; x1 <= properties.Box1Max.X; x1 += SolutionStep)
                for (double y1 = this.properties.Box1Min.Y; y1 <= properties.Box1Max.Y; y1 += SolutionStep)
                    for (double x2 = this.properties.Box2Min.X; x2 <= properties.Box2Max.X; x2 += SolutionStep)
                        for (double y2 = this.properties.Box2Min.Y; y2 <= properties.Box2Max.Y; y2 += SolutionStep)
                        {
                            Vector segmentStart = new Vector(x1, y1);
                            Vector segmentEnd = new Vector(x2, y2);
                            double curDistanceSqr, curAlpha;
                            this.properties.Point.DistanceToSegmentSquared(segmentStart, segmentEnd, out curDistanceSqr, out curAlpha);
                            double curPenalty =
                                Vector.DotProduct(segmentStart, properties.Lambda1) +
                                Vector.DotProduct(segmentEnd, properties.Lambda2);
                            double objective = curDistanceSqr + curPenalty;
                            if (objective < bestObjective)
                            {
                                bestObjective = objective;
                                point1 = segmentStart;
                                point2 = segmentEnd;
                                distanceFromPoint = segmentStart + (segmentEnd - segmentStart) * MathHelper.Trunc(curAlpha, 0, 1);
                                penalty = curPenalty;
                                distanceSqr = curDistanceSqr;
                            }
                        }
        }
    }
}
