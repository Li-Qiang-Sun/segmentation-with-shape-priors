using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using MicrosoftResearch.Infer.Maths;
using Research.GraphBasedShapePrior;
using Vector = Research.GraphBasedShapePrior.Vector;

namespace TestArea
{
    class Program
    {
        static ShapeModel CreateSimpleShapeModel1()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));

            List<ShapeVertexParams> vertexParams = new List<ShapeVertexParams>();
            vertexParams.Add(new ShapeVertexParams(0.3, 0.1));
            vertexParams.Add(new ShapeVertexParams(0.3, 0.1));

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
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(Math.PI * 0.5, 1, 0.1, 10)); // TODO: we need deviations to be relative

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
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(-Math.PI * 0.5, 1.3, Math.PI * 0.1, 5)); // TODO: we need edge length deviations to be relative
            edgePairParams.Add(new Tuple<int, int>(1, 2), new ShapeEdgePairParams(Math.PI * 0.5, 1, Math.PI * 0.1, 5));
            edgePairParams.Add(new Tuple<int, int>(2, 3), new ShapeEdgePairParams(-Math.PI * 0.5, 1, Math.PI * 0.1, 5));
            edgePairParams.Add(new Tuple<int, int>(3, 4), new ShapeEdgePairParams(Math.PI * 0.5, 0.77, Math.PI * 0.1, 5));

            return ShapeModel.Create(edges, vertexParams, edgePairParams);
        }

        static void MainForConvexHull()
        {
            List<Vector> points = new List<Vector>();
            points.Add(new Vector(0, 0));
            points.Add(new Vector(1, 2));
            points.Add(new Vector(2, 1));
            points.Add(new Vector(3, 2));
            points.Add(new Vector(4, 2));
            points.Add(new Vector(3, -1));

            Polygon p = Polygon.ConvexHull(points);
            Console.WriteLine(p.IsPointInside(new Vector(2, 1)));
            Console.WriteLine(p.IsPointInside(new Vector(2, -1)));
            Console.WriteLine(p.IsPointInside(new Vector(0, -1)));
            Console.WriteLine(p.IsPointInside(new Vector(-1, 0)));
            Console.WriteLine(p.IsPointInside(new Vector(2, 0)));
            Console.WriteLine(p.IsPointInside(new Vector(3, 1)));
        }

        static void MainForLengthAngleDependenceExperiment()
        {
            LengthAngleDependenceExperiment(
                new VertexConstraint(new Vector(0, -5), new Vector(5, 5), 1, 1),
                new VertexConstraint(new Vector(10, -10), new Vector(15, 10), 1, 1),
                "experiment1.png");
            LengthAngleDependenceExperiment(
                new VertexConstraint(new Vector(10, -10), new Vector(15, 10), 1, 1),
                new VertexConstraint(new Vector(0, -5), new Vector(5, 5), 1, 1),
                "experiment1_revert.png");
            LengthAngleDependenceExperiment(
                new VertexConstraint(new Vector(-10, 8), new Vector(10, 10), 1, 1),
                new VertexConstraint(new Vector(5, 0), new Vector(6, 7), 1, 1),
                "experiment2.png");
            LengthAngleDependenceExperiment(
                new VertexConstraint(new Vector(5, 0), new Vector(6, 7), 1, 1),
                new VertexConstraint(new Vector(-10, 8), new Vector(10, 10), 1, 1),
                "experiment2_revert.png");
            LengthAngleDependenceExperiment(
                new VertexConstraint(new Vector(0, 0), new Vector(10, 10), 1, 1),
                new VertexConstraint(new Vector(15, 15), new Vector(20, 20), 1, 1),
                "experiment3.png");
            LengthAngleDependenceExperiment(
                new VertexConstraint(new Vector(15, 15), new Vector(20, 20), 1, 1),
                new VertexConstraint(new Vector(0, 0), new Vector(10, 10), 1, 1),
                "experiment3_revert.png");
            LengthAngleDependenceExperiment(
                new VertexConstraint(new Vector(0, 0), new Vector(10, 10), 1, 1),
                new VertexConstraint(new Vector(-6, 11), new Vector(-1, 16), 1, 1),
                "experiment4.png");
            LengthAngleDependenceExperiment(
                new VertexConstraint(new Vector(-6, 11), new Vector(-1, 16), 1, 1),
                new VertexConstraint(new Vector(0, 0), new Vector(10, 10), 1, 1),
                "experiment4_revert.png");
            LengthAngleDependenceExperiment(
                new VertexConstraint(new Vector(0, 0), new Vector(10, 10), 1, 1),
                new VertexConstraint(new Vector(9, 9), new Vector(19, 19), 1, 1),
                "experiment5.png");
            LengthAngleDependenceExperiment(
                new VertexConstraint(new Vector(9, 9), new Vector(19, 19), 1, 1),
                new VertexConstraint(new Vector(0, 0), new Vector(10, 10), 1, 1),
                "experiment5_revert.png");
            LengthAngleDependenceExperiment(
                new VertexConstraint(new Vector(0, 0), new Vector(10, 10), 1, 1),
                new VertexConstraint(new Vector(13, 0), new Vector(23, 10), 1, 1),
                "experiment6.png");
            LengthAngleDependenceExperiment(
                new VertexConstraint(new Vector(13, 0), new Vector(23, 10), 1, 1),
                new VertexConstraint(new Vector(0, 0), new Vector(10, 10), 1, 1),
                "experiment6_revert.png");
            LengthAngleDependenceExperiment(
                new VertexConstraint(new Vector(0, 0), new Vector(10, 10), 1, 1),
                new VertexConstraint(new Vector(0, 15), new Vector(10, 45), 1, 1),
                "experiment7.png");
            LengthAngleDependenceExperiment(
                new VertexConstraint(new Vector(0, 15), new Vector(10, 45), 1, 1),
                new VertexConstraint(new Vector(0, 0), new Vector(10, 10), 1, 1),
                "experiment7_revert.png");
            LengthAngleDependenceExperiment(
                new VertexConstraint(new Vector(0, 0), new Vector(10, 10), 1, 1),
                new VertexConstraint(new Vector(5, 5), new Vector(8, 8), 1, 1),
                "experiment8.png");
            LengthAngleDependenceExperiment(
                new VertexConstraint(new Vector(5, 5), new Vector(8, 8), 1, 1),
                new VertexConstraint(new Vector(0, 0), new Vector(10, 10), 1, 1),
                "experiment8_revert.png");
        }

        static void LengthAngleDependenceExperiment(
            VertexConstraint constraint1, VertexConstraint constraint2, string fileName)
        {
            const int GeneratedPointCount = 100000;

            Random random = new Random();
            List<Vector> lengthAnglePoints = new List<Vector>();
            double maxLength = 0;
            for (int i = 0; i < GeneratedPointCount; ++i)
            {
                double randomX1 = constraint1.MinCoord.X + random.NextDouble() * (constraint1.MaxCoord.X - constraint1.MinCoord.X);
                double randomY1 = constraint1.MinCoord.Y + random.NextDouble() * (constraint1.MaxCoord.Y - constraint1.MinCoord.Y);
                double randomX2 = constraint2.MinCoord.X + random.NextDouble() * (constraint2.MaxCoord.X - constraint2.MinCoord.X);
                double randomY2 = constraint2.MinCoord.Y + random.NextDouble() * (constraint2.MaxCoord.Y - constraint2.MinCoord.Y);
                Vector vector1 = new Vector(randomX1, randomY1);
                Vector vector2 = new Vector(randomX2, randomY2);
                if (vector1 == vector2)
                    continue;

                Vector diff = vector2 - vector1;
                double length = diff.Length;
                double angle = Vector.AngleBetween(Vector.UnitX, diff);
                lengthAnglePoints.Add(new Vector(length, angle));

                maxLength = Math.Max(maxLength, length);
            }

            VertexConstraintSet constraintSet = VertexConstraintSet.CreateFromConstraints(
                CreateSimpleShapeModel1(), new[] { constraint1, constraint2 });
            Range lengthRange, angleRange;
            constraintSet.DetermineEdgeLimits(0, out lengthRange, out angleRange);

            const int lengthImageSize = 360;
            const int angleImageSize = 360;
            double lengthScale = (lengthImageSize - 20) / maxLength;

            Image2D<bool> myAwesomeMask = new Image2D<bool>(lengthImageSize, angleImageSize);
            LengthAngleSpaceSeparatorSet myAwesomeSeparator = new LengthAngleSpaceSeparatorSet(constraint1, constraint2);
            for (int i = 0; i < lengthImageSize; ++i)
                for (int j = 0; j < angleImageSize; ++j)
                {
                    double length = i / lengthScale;
                    double angle = MathHelper.ToRadians(j - 180.0);
                    if (myAwesomeSeparator.IsInside(length, angle))
                        myAwesomeMask[i, j] = true;
                }

            using (Bitmap image = new Bitmap(lengthImageSize, angleImageSize))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.Clear(Color.White);

                // Draw generated points);
                for (int i = 0; i < lengthAnglePoints.Count; ++i)
                    DrawLengthAngle(lengthAnglePoints[i].X, lengthAnglePoints[i].Y, lengthScale, 1, graphics, Pens.Black);

                // Draw estimated ranges
                if (angleRange.Outside)
                {
                    graphics.DrawRectangle(
                        Pens.Red,
                        (float)(lengthRange.Left * lengthScale),
                        0,
                        (float)((lengthRange.Right - lengthRange.Left) * lengthScale),
                        (float)(MathHelper.ToDegrees(angleRange.Left) + 180));
                    graphics.DrawRectangle(
                        Pens.Red,
                        (float)(lengthRange.Left * lengthScale),
                        (float)(MathHelper.ToDegrees(angleRange.Right) + 180),
                        (float)((lengthRange.Right - lengthRange.Left) * lengthScale),
                        (float)(180 - MathHelper.ToDegrees(angleRange.Right)));
                }
                else
                {
                    graphics.DrawRectangle(
                        Pens.Red,
                        (float)(lengthRange.Left * lengthScale),
                        (float)(MathHelper.ToDegrees(angleRange.Left) + 180),
                        (float)((lengthRange.Right - lengthRange.Left) * lengthScale),
                        (float)(MathHelper.ToDegrees(angleRange.Right) - MathHelper.ToDegrees(angleRange.Left)));
                }

                // Draw constraint corners
                for (int i = 0; i < 4; ++i)
                {
                    for (int j = 0; j < 4; ++j)
                    {
                        Vector diff = constraint2.Corners[j] - constraint1.Corners[i];
                        DrawLengthAngle(diff.Length, Vector.AngleBetween(Vector.UnitX, diff), lengthScale, 5, graphics, Pens.Blue);
                    }
                }

                // Draw my awesome separation lines
                for (int i = 0; i < lengthImageSize - 1; ++i)
                    for (int j = 0; j < lengthImageSize - 1; ++j)
                    {
                        bool border = false;
                        border |= myAwesomeMask[i, j] != myAwesomeMask[i + 1, j];
                        border |= myAwesomeMask[i, j] != myAwesomeMask[i, j + 1];
                        border |= myAwesomeMask[i, j] != myAwesomeMask[i + 1, j + 1];
                        if (border)
                            image.SetPixel(i, j, Color.Orange);
                    }

                    graphics.DrawString(
                        String.Format("Max length is {0:0.0}", maxLength), SystemFonts.DefaultFont, Brushes.Green, 5, 5);

                image.Save(fileName);
            }
        }

        private static void DrawLengthAngle(double length, double angle, double lengthScale, double radius, Graphics graphics, Pen pen)
        {
            float x = (float)(length * lengthScale);
            float y = (float)MathHelper.ToDegrees(angle) + 180;
            graphics.DrawEllipse(pen, x - (float)radius, y - (float)radius, (float)radius * 2, (float)radius * 2);
        }

        private static void GenerateMeanShape()
        {
            ShapeModel shapeModel = CreateLetterShapeModel();
            Shape shape = shapeModel.FitMeanShape(new Size(100, 200));
        }

        static void Main()
        {
            Rand.Restart(666);

            MainForLengthAngleDependenceExperiment();
            //GenerateMeanShape();
            //DrawLengthAngleDependence();
            //MainForUnaryPotentialsCheck();
            //MainForSegmentation();
            //MainForConvexHull();
            //MainForShapeEnergyCheck();
        }
    }
}
