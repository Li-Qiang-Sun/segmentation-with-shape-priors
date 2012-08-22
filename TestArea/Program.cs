using System;
using System.Collections.Generic;
using System.Drawing;
using MicrosoftResearch.Infer.Maths;
using Research.GraphBasedShapePrior;
using Research.GraphBasedShapePrior.Util;
using Vector = Research.GraphBasedShapePrior.Util.Vector;

namespace TestArea
{
    class Program
    {
        static ShapeModel CreateSimpleShapeModel1()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));

            List<ShapeEdgeParams> edgeParams = new List<ShapeEdgeParams>();
            edgeParams.Add(new ShapeEdgeParams(0.2, 0.1));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();

            return ShapeModel.Create(new ShapeStructure(edges), edgeParams, edgePairParams);
        }

        private static ShapeModel CreateSimpleShapeModel2()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));
            edges.Add(new ShapeEdge(1, 2));

            List<ShapeEdgeParams> edgeParams = new List<ShapeEdgeParams>();
            edgeParams.Add(new ShapeEdgeParams(0.15, 0.1));
            edgeParams.Add(new ShapeEdgeParams(0.15, 0.1));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(Math.PI * 0.5, 1, 0.1, 10));

            return ShapeModel.Create(new ShapeStructure(edges), edgeParams, edgePairParams);
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
            edgeParams.Add(new ShapeEdgeParams(0.07, 0.05));
            edgeParams.Add(new ShapeEdgeParams(0.07, 0.05));
            edgeParams.Add(new ShapeEdgeParams(0.07, 0.05));
            edgeParams.Add(new ShapeEdgeParams(0.07, 0.05));
            edgeParams.Add(new ShapeEdgeParams(0.07, 0.05));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams = new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(-Math.PI * 0.5, 1.3, Math.PI * 0.1, 5));
            edgePairParams.Add(new Tuple<int, int>(1, 2), new ShapeEdgePairParams(Math.PI * 0.5, 1, Math.PI * 0.1, 5));
            edgePairParams.Add(new Tuple<int, int>(2, 3), new ShapeEdgePairParams(-Math.PI * 0.5, 1, Math.PI * 0.1, 5));
            edgePairParams.Add(new Tuple<int, int>(3, 4), new ShapeEdgePairParams(Math.PI * 0.5, 0.77, Math.PI * 0.1, 5));

            return ShapeModel.Create(new ShapeStructure(edges), edgeParams, edgePairParams);
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

        static void PointIsClosestExperiment(
            Vector point,
            VertexConstraints point1Constraints,
            VertexConstraints point2Constraints,
            string fileName)
        {
            Console.WriteLine(string.Format("Doing experiment for {0}", fileName));

            Bitmap image = new Bitmap(320, 240);
            const int iterations = 200000;
            using (Graphics graphics = Graphics.FromImage(image))
            {
                Random random = new Random();
                for (int i = 0; i < iterations; ++i)
                {
                    Vector point1 = new Vector(
                        point1Constraints.MinCoord.X + (point1Constraints.MaxCoord.X - point1Constraints.MinCoord.X) * random.NextDouble(),
                        point1Constraints.MinCoord.Y + (point1Constraints.MaxCoord.Y - point1Constraints.MinCoord.Y) * random.NextDouble());
                    Vector point2 = new Vector(
                        point2Constraints.MinCoord.X + (point2Constraints.MaxCoord.X - point2Constraints.MinCoord.X) * random.NextDouble(),
                        point2Constraints.MinCoord.Y + (point2Constraints.MaxCoord.Y - point2Constraints.MinCoord.Y) * random.NextDouble());

                    double distanceSqr, alpha;
                    point.DistanceToSegmentSquared(point1, point2, out distanceSqr, out alpha);
                    alpha = MathHelper.Trunc(alpha, 0, 1);
                    Vector closestPoint = point1 + (point2 - point1) * alpha;
                    const float radius = 2;
                    graphics.FillEllipse(
                        Brushes.Green,
                        (float)closestPoint.X - radius,
                        (float)closestPoint.Y - radius,
                        radius * 2,
                        radius * 2);
                }

                graphics.DrawRectangle(
                    Pens.Blue,
                    point1Constraints.CoordRectangle.Left,
                    point1Constraints.CoordRectangle.Top,
                    point1Constraints.CoordRectangle.Width,
                    point1Constraints.CoordRectangle.Height);
                graphics.DrawRectangle(
                    Pens.Blue,
                    point2Constraints.CoordRectangle.Left,
                    point2Constraints.CoordRectangle.Top,
                    point2Constraints.CoordRectangle.Width,
                    point2Constraints.CoordRectangle.Height);
                graphics.FillEllipse(Brushes.Red, (float)point.X - 2, (float)point.Y - 2, 4, 4);
            }

            image.Save(fileName);
        }

        static void MainForPointIsClosestExperiment()
        {
            PointIsClosestExperiment(
                new Vector(20, 100),
                new VertexConstraints(new Vector(40, 10), new Vector(180, 120)),
                new VertexConstraints(new Vector(80, 100), new Vector(230, 170)),
                "cp_experiment1.png");
            PointIsClosestExperiment(
                new Vector(200, 10),
                new VertexConstraints(new Vector(40, 10), new Vector(180, 120)),
                new VertexConstraints(new Vector(80, 100), new Vector(230, 170)),
                "cp_experiment2.png");
            PointIsClosestExperiment(
                new Vector(120, 160),
                new VertexConstraints(new Vector(40, 10), new Vector(180, 120)),
                new VertexConstraints(new Vector(80, 100), new Vector(230, 170)),
                "cp_experiment3.png");
            PointIsClosestExperiment(
                new Vector(10, 130),
                new VertexConstraints(new Vector(40, 10), new Vector(120, 120)),
                new VertexConstraints(new Vector(140, 140), new Vector(230, 170)),
                "cp_experiment4.png");
            PointIsClosestExperiment(
                new Vector(130, 190),
                new VertexConstraints(new Vector(40, 10), new Vector(120, 120)),
                new VertexConstraints(new Vector(140, 140), new Vector(230, 170)),
                "cp_experiment5.png");
            PointIsClosestExperiment(
                new Vector(100, 80),
                new VertexConstraints(new Vector(40, 10), new Vector(120, 120)),
                new VertexConstraints(new Vector(140, 140), new Vector(230, 170)),
                "cp_experiment6.png");
        }

        static void MainForLengthAngleDependenceExperiment()
        {
            LengthAngleDependenceExperiment(
                new VertexConstraints(new Vector(0, -5), new Vector(5, 5)),
                new VertexConstraints(new Vector(10, -10), new Vector(15, 10)),
                "experiment1.png");
            LengthAngleDependenceExperiment(
                new VertexConstraints(new Vector(10, -10), new Vector(15, 10)),
                new VertexConstraints(new Vector(0, -5), new Vector(5, 5)),
                "experiment1_revert.png");
            LengthAngleDependenceExperiment(
                new VertexConstraints(new Vector(-10, 8), new Vector(10, 10)),
                new VertexConstraints(new Vector(5, 0), new Vector(6, 7)),
                "experiment2.png");
            LengthAngleDependenceExperiment(
                new VertexConstraints(new Vector(5, 0), new Vector(6, 7)),
                new VertexConstraints(new Vector(-10, 8), new Vector(10, 10)),
                "experiment2_revert.png");
            LengthAngleDependenceExperiment(
                new VertexConstraints(new Vector(0, 0), new Vector(10, 10)),
                new VertexConstraints(new Vector(15, 15), new Vector(20, 20)),
                "experiment3.png");
            LengthAngleDependenceExperiment(
                new VertexConstraints(new Vector(15, 15), new Vector(20, 20)),
                new VertexConstraints(new Vector(0, 0), new Vector(10, 10)),
                "experiment3_revert.png");
            LengthAngleDependenceExperiment(
                new VertexConstraints(new Vector(0, 0), new Vector(10, 10)),
                new VertexConstraints(new Vector(-6, 11), new Vector(-1, 16)),
                "experiment4.png");
            LengthAngleDependenceExperiment(
                new VertexConstraints(new Vector(-6, 11), new Vector(-1, 16)),
                new VertexConstraints(new Vector(0, 0), new Vector(10, 10)),
                "experiment4_revert.png");
            LengthAngleDependenceExperiment(
                new VertexConstraints(new Vector(0, 0), new Vector(10, 10)),
                new VertexConstraints(new Vector(9, 9), new Vector(19, 19)),
                "experiment5.png");
            LengthAngleDependenceExperiment(
                new VertexConstraints(new Vector(9, 9), new Vector(19, 19)),
                new VertexConstraints(new Vector(0, 0), new Vector(10, 10)),
                "experiment5_revert.png");
            LengthAngleDependenceExperiment(
                new VertexConstraints(new Vector(0, 0), new Vector(10, 10)),
                new VertexConstraints(new Vector(13, 0), new Vector(23, 10)),
                "experiment6.png");
            LengthAngleDependenceExperiment(
                new VertexConstraints(new Vector(13, 0), new Vector(23, 10)),
                new VertexConstraints(new Vector(0, 0), new Vector(10, 10)),
                "experiment6_revert.png");
            LengthAngleDependenceExperiment(
                new VertexConstraints(new Vector(0, 0), new Vector(10, 10)),
                new VertexConstraints(new Vector(0, 15), new Vector(10, 45)),
                "experiment7.png");
            LengthAngleDependenceExperiment(
                new VertexConstraints(new Vector(0, 15), new Vector(10, 45)),
                new VertexConstraints(new Vector(0, 0), new Vector(10, 10)),
                "experiment7_revert.png");
            LengthAngleDependenceExperiment(
                new VertexConstraints(new Vector(0, 0), new Vector(10, 10)),
                new VertexConstraints(new Vector(5, 5), new Vector(8, 8)),
                "experiment8.png");
            LengthAngleDependenceExperiment(
                new VertexConstraints(new Vector(5, 5), new Vector(8, 8)),
                new VertexConstraints(new Vector(0, 0), new Vector(10, 10)),
                "experiment8_revert.png");
        }

        static void LengthAngleDependenceExperiment(
            VertexConstraints constraint1, VertexConstraints constraint2, string fileName)
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

            //ShapeConstraints constraintSet = ShapeConstraints.CreateFromConstraints(
            //    CreateSimpleShapeModel1(),
            //    new[] { constraint1, constraint2 },
            //    new[] { new EdgeConstraints(1) },
            //    1,
            //    1);
            BoxSetLengthAngleConstraints lengthAngleConstraints =
                BoxSetLengthAngleConstraints.FromVertexConstraints(constraint1, constraint2, 2, 0);

            const int lengthImageSize = 360;
            const int angleImageSize = 360;
            double lengthScale = (lengthImageSize - 20) / maxLength;

            //Image2D<bool> myAwesomeMask = new Image2D<bool>(lengthImageSize, angleImageSize);
            //LengthAngleSpaceSeparatorSet myAwesomeSeparator = new LengthAngleSpaceSeparatorSet(constraint1, constraint2);
            //for (int i = 0; i < lengthImageSize; ++i)
            //    for (int j = 0; j < angleImageSize; ++j)
            //    {
            //        double length = i / lengthScale;
            //        double angle = MathHelper.ToRadians(j - 180.0);
            //        if (myAwesomeSeparator.IsInside(length, angle))
            //            myAwesomeMask[i, j] = true;
            //    }

            using (Bitmap image = new Bitmap(lengthImageSize, angleImageSize))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.Clear(Color.White);

                // Draw generated points
                for (int i = 0; i < lengthAnglePoints.Count; ++i)
                    DrawLengthAngle(graphics, Pens.Black, lengthAnglePoints[i].X, lengthAnglePoints[i].Y, lengthScale, 1);

                // Draw estimated ranges
                foreach (BoxLengthAngleConstraints child in lengthAngleConstraints.ChildConstraints)
                    DrawLengthAngleConstraintBox(graphics, Pens.Green, child, lengthScale);
                DrawLengthAngleConstraintBox(graphics, new Pen(Color.Red, 2), lengthAngleConstraints.OverallRange, lengthScale);

                // Draw constraint corners
                //for (int i = 0; i < 4; ++i)
                //{
                //    for (int j = 0; j < 4; ++j)
                //    {
                //        Vector diff = constraint2.Corners[j] - constraint1.Corners[i];
                //        DrawLengthAngle(diff.Length, Vector.AngleBetween(Vector.UnitX, diff), lengthScale, 5, graphics, Pens.Blue);
                //    }
                //}

                // Draw my awesome separation lines
                //for (int i = 0; i < lengthImageSize - 1; ++i)
                //    for (int j = 0; j < lengthImageSize - 1; ++j)
                //    {
                //        bool border = false;
                //        border |= myAwesomeMask[i, j] != myAwesomeMask[i + 1, j];
                //        border |= myAwesomeMask[i, j] != myAwesomeMask[i, j + 1];
                //        border |= myAwesomeMask[i, j] != myAwesomeMask[i + 1, j + 1];
                //        if (border)
                //            image.SetPixel(i, j, Color.Orange);
                //    }

                //graphics.DrawString(
                //    String.Format("Max length is {0:0.0}", maxLength), SystemFonts.DefaultFont, Brushes.Green, 5, 5);

                image.Save(fileName);
            }
        }

        private static void DrawLengthAngleConstraintBox(Graphics graphics, Pen pen, BoxLengthAngleConstraints constraints, double lengthScale)
        {
            if (constraints.AngleBoundary.Outside)
            {
                graphics.DrawRectangle(
                    pen,
                    (float)(constraints.LengthBoundary.Left * lengthScale),
                    0,
                    (float)(constraints.LengthBoundary.Length * lengthScale),
                    (float)(MathHelper.ToDegrees(constraints.AngleBoundary.Left) + 180));
                graphics.DrawRectangle(
                    pen,
                    (float)(constraints.LengthBoundary.Left * lengthScale),
                    (float)(MathHelper.ToDegrees(constraints.AngleBoundary.Right) + 180),
                    (float)(constraints.LengthBoundary.Length * lengthScale),
                    (float)(180 - MathHelper.ToDegrees(constraints.AngleBoundary.Right)));
            }
            else
            {
                graphics.DrawRectangle(
                    pen,
                    (float)(constraints.LengthBoundary.Left * lengthScale),
                    (float)(MathHelper.ToDegrees(constraints.AngleBoundary.Left) + 180),
                    (float)(constraints.LengthBoundary.Length * lengthScale),
                    (float)(MathHelper.ToDegrees(constraints.AngleBoundary.Right) - MathHelper.ToDegrees(constraints.AngleBoundary.Left)));
            }
        }

        private static void DrawLengthAngle( Graphics graphics, Pen pen, double length, double angle, double lengthScale, double radius)
        {
            float x = (float)(length * lengthScale);
            float y = (float)MathHelper.ToDegrees(angle) + 180;
            graphics.DrawEllipse(pen, x - (float)radius, y - (float)radius, (float)radius * 2, (float)radius * 2);
        }

        private static void GenerateMeanShape()
        {
            ShapeModel shapeModel = CreateLetterShapeModel();
            Shape shape = shapeModel.FitMeanShape(100, 200);
        }

        //private static void MainForDualDecomposition()
        //{
        //    ShapeModel shapeModel = CreateSimpleShapeModel1();
        //    ShapeConstraints constraints = ShapeConstraints.CreateFromConstraints(
        //        shapeModel,
        //        new[] { new VertexConstraints(new Vector(3, 0), new Vector(40, 40)), new VertexConstraints(new Vector(60, 10), new Vector(100, 50)) },
        //        new[] { new EdgeConstraints(10, 20) },
        //        1,
        //        1);
        //    Image2D<Color> image = Image2D.LoadFromFile("../../../images/simple_1.png", 0.2);
        //    Rectangle objectLocation = new Rectangle(30, 24, 159, 96);
        //    ImageSegmentator segmentator = new ImageSegmentator(image, objectLocation, 0.01, 0, 1, 0.05, 3);
        //    LowerBoundCalculator lowerBoundCalculator = new LowerBoundCalculator(segmentator);
        //    lowerBoundCalculator.CalculateLowerBound(constraints);
        //}

        static void Main()
        {
            Rand.Restart(666);

            //MainForDualDecomposition();
            //MainForPointIsClosestExperiment();
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
