using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Research.GraphBasedShapePrior.Tests
{
    [TestClass]
    public class ShapeEnergyTests
    {
        private static double TestShapeEnergyCalculationApproachesImpl(
            ShapeModel model, IEnumerable<Circle> vertices, Size objectSize, int lengthGridSize, int angleGridSize, double eps)
        {
            double sizeEstimate = SegmentationAlgorithmBase.ImageSizeToObjectSizeEstimate(objectSize);

            // Create shape model and calculate energy in normal way
            Shape shape = new Shape(model, vertices);
            double energy1 = shape.CalculateEnergy(sizeEstimate);

            // Calculate energy via generalized distance transforms
            VertexConstraintSet constraints = VertexConstraintSet.CreateFromConstraints(
                model, TestHelper.VerticesToConstraints(vertices));
            BranchAndBoundSegmentationAlgorithm segmentator = new BranchAndBoundSegmentationAlgorithm();
            segmentator.ShapeModel = model;
            segmentator.LengthGridSize = lengthGridSize;
            segmentator.AngleGridSize = angleGridSize;
            double energy2 = segmentator.CalculateMinShapeEnergy(constraints, objectSize);

            Assert.AreEqual(energy1, energy2, eps);

            return energy1;
        }

        private static void TestMeanShapeImpl(ShapeModel shapeModel, double eps)
        {
            Size imageSize = new Size(100, 160);
            Shape meanShape = shapeModel.FitMeanShape(imageSize);

            // Check GDT vs usual approach
            double energy = TestShapeEnergyCalculationApproachesImpl(shapeModel, meanShape.Vertices, imageSize, 1001, 1001, eps);

            // Check if energy is zero
            Assert.AreEqual(0, energy, 1e-6);

            // Check if shape is inside given rect);
            foreach (Circle vertex in meanShape.Vertices)
                Assert.IsTrue(new RectangleF(0, 0, imageSize.Width, imageSize.Height).Contains((float)vertex.Center.X, (float)vertex.Center.Y));
        }

        private static void TestEdgeLimitsCommonImpl(
            VertexConstraint constraint1, VertexConstraint constraint2, out Range lengthRange, out Range angleRange)
        {
            VertexConstraintSet constraintSet = VertexConstraintSet.CreateFromConstraints(TestHelper.CreateTestShapeModelWith1Edge(), new[] { constraint1, constraint2 });
            constraintSet.DetermineEdgeLimits(0, out lengthRange, out angleRange);

            GeneralizedDistanceTransform2D transform = new GeneralizedDistanceTransform2D(
                new Vector(0, -Math.PI * 2), new Vector(35, Math.PI * 2), new Size(2000, 2000), 1, 1, delegate { return 0; });
            AllowedLengthAngleChecker allowedLengthAngleChecker = new AllowedLengthAngleChecker(constraint1, constraint2, transform);

            Random random = new Random(666);
            
            const int insideCheckCount = 1000;
            for (int i = 0; i < insideCheckCount; ++i)
            {
                Vector edgePoint1 =
                    constraint1.MinCoord +
                    new Vector(
                        random.NextDouble() * (constraint1.MaxCoord.X - constraint1.MinCoord.X),
                        random.NextDouble() * (constraint1.MaxCoord.Y - constraint1.MinCoord.Y));
                Vector edgePoint2 =
                    constraint2.MinCoord +
                    new Vector(
                        random.NextDouble() * (constraint2.MaxCoord.X - constraint2.MinCoord.X),
                        random.NextDouble() * (constraint2.MaxCoord.Y - constraint2.MinCoord.Y));

                Vector vec = edgePoint2 - edgePoint1;
                double length = vec.Length;
                double angle = Vector.AngleBetween(Vector.UnitX, vec);

                Assert.IsTrue(lengthRange.Contains(length));
                Assert.IsTrue(angleRange.Contains(angle));
                Assert.IsTrue(allowedLengthAngleChecker.IsAllowed(transform.CoordToGridIndexX(length), transform.CoordToGridIndexY(angle)));
            }

            const int outsideCheckCount = 1000;
            for (int i = 0; i < outsideCheckCount; ++i)
            {
                Vector edgePoint1 =
                    constraint1.MinCoord +
                    new Vector(
                        (random.NextDouble() * 2 - 0.5) * (constraint1.MaxCoord.X - constraint1.MinCoord.X),
                        (random.NextDouble() * 2 - 0.5) * (constraint1.MaxCoord.Y - constraint1.MinCoord.Y));
                Vector edgePoint2 =
                    constraint2.MinCoord +
                    new Vector(
                        (random.NextDouble() * 2 - 0.5) * (constraint2.MaxCoord.X - constraint2.MinCoord.X),
                        (random.NextDouble() * 2 - 0.5) * (constraint2.MaxCoord.Y - constraint2.MinCoord.Y));

                Vector vec = edgePoint2 - edgePoint1;
                double length = vec.Length;
                double angle = Vector.AngleBetween(Vector.UnitX, vec);

                // We've generated too large edge
                if (length > transform.GridMax.X)
                    continue;

                bool definitelyOutside = !lengthRange.Contains(length) || !angleRange.Contains(angle);
                bool outside = !allowedLengthAngleChecker.IsAllowed(
                    transform.CoordToGridIndexX(length), transform.CoordToGridIndexY(angle));
                Assert.IsTrue(!definitelyOutside || outside);
            }
        }

        [TestMethod]
        public void TestShapeEnergyCalculationApproaches1()
        {
            List<Circle> vertices = new List<Circle>();
            vertices.Add(new Circle(0, 0, 10));
            vertices.Add(new Circle(80, 0, 15));
            vertices.Add(new Circle(80, 100, 13));

            TestShapeEnergyCalculationApproachesImpl(
                TestHelper.CreateTestShapeModelWith2Edges(Math.PI * 0.5, 1.1), vertices, new Size(100, 100), 2001, 2001, 0.1);
        }

        [TestMethod]
        public void TestShapeEnergyCalculationApproaches2()
        {
            List<Circle> vertices = new List<Circle>();
            vertices.Add(new Circle(0, 0, 10));
            vertices.Add(new Circle(40, 0, 15));
            vertices.Add(new Circle(0, 42, 13));

            TestShapeEnergyCalculationApproachesImpl(
                TestHelper.CreateTestShapeModelWith2Edges(Math.PI * 0.5, 1.1), vertices, new Size(100, 100), 2001, 2001, 0.2);
        }

        [TestMethod]
        public void TestShapeEnergyCalculationApproaches3()
        {
            List<Circle> vertices = new List<Circle>();
            vertices.Add(new Circle(0, 0, 10));
            vertices.Add(new Circle(40, 0, 15));
            vertices.Add(new Circle(40, 50, 13));
            vertices.Add(new Circle(80, 70, 7));
            vertices.Add(new Circle(30, 55, 20));
            vertices.Add(new Circle(10, -50, 10));

            TestShapeEnergyCalculationApproachesImpl(TestHelper.CreateTestShapeModel5Edges(), vertices, new Size(100, 100), 2001, 2001, 0.5);
        }

        [TestMethod]
        public void TestShapeEnergyCalculationApproaches4()
        {
            List<Circle> vertices = new List<Circle>();
            vertices.Add(new Circle(0, 0, 10));
            vertices.Add(new Circle(40, 0, 15));
            vertices.Add(new Circle(3, -40, 13));
            vertices.Add(new Circle(37, -43, 7));
            vertices.Add(new Circle(2, -90, 20));
            vertices.Add(new Circle(-35, -95, 10));

            TestShapeEnergyCalculationApproachesImpl(TestHelper.CreateTestShapeModel5Edges(), vertices, new Size(100, 100), 3001, 3001, 2);
        }

        [TestMethod]
        public void TestShapeEnergyCalculationApproaches5()
        {
            List<Circle> vertices = new List<Circle>();
            vertices.Add(new Circle(0, 0, 10));
            vertices.Add(new Circle(-40, -1, 15));
            vertices.Add(new Circle(3, -40, 13));
            vertices.Add(new Circle(37, -43, 7));
            vertices.Add(new Circle(2, -90, 20));
            vertices.Add(new Circle(-35, -95, 10));

            TestShapeEnergyCalculationApproachesImpl(TestHelper.CreateLetterShapeModel(), vertices, new Size(100, 100), 3001, 3001, 2);
        }

        [TestMethod]
        public void TestMeanShape1()
        {
            TestMeanShapeImpl(TestHelper.CreateTestShapeModelWith2Edges(Math.PI * 0.5, 1.1), 1e-6);
        }

        [TestMethod]
        public void TestMeanShape2()
        {
            TestMeanShapeImpl(TestHelper.CreateTestShapeModel5Edges(), 1e-6);
        }

        [TestMethod]
        public void TestMeanShape3()
        {
            TestMeanShapeImpl(TestHelper.CreateLetterShapeModel(), 1e-6);
        }

        [TestMethod]
        public void TestShapeTwist()
        {
            Size objectSize = new Size(100, 100);
            const double edgeLength = 100;
            const double startAngle = Math.PI * 0.5;

            ShapeModel shapeModel = TestHelper.CreateTestShapeModelWith2Edges(Math.PI * 0.5, 1);
            List<Circle> vertices = new List<Circle>();
            vertices.Add(new Circle(0, 0, 10));
            vertices.Add(new Circle(Math.Cos(startAngle) * edgeLength, Math.Sin(startAngle) * edgeLength, 10));
            vertices.Add(new Circle());

            double objectSizeEstimate = SegmentationAlgorithmBase.ImageSizeToObjectSizeEstimate(objectSize);

            const int iterationCount = 10;
            const double angleStep = 2 * Math.PI / iterationCount;
            Shape lastShape = null;
            for (int i = 0; i < iterationCount; ++i)
            {
                double angle = startAngle + Math.PI * 0.5 + angleStep * i;
                vertices[2] = new Circle(vertices[1].Center.X + edgeLength * Math.Cos(angle), vertices[1].Center.Y + edgeLength * Math.Sin(angle), 10);
                Shape shape = new Shape(shapeModel, vertices);

                // Test if energy is increasing/decreasing properly
                if (i <= iterationCount / 2)
                    Assert.IsTrue(lastShape == null || lastShape.CalculateEnergy(objectSizeEstimate) < shape.CalculateEnergy(objectSizeEstimate));
                else
                    Assert.IsTrue(lastShape.CalculateEnergy(objectSizeEstimate) > shape.CalculateEnergy(objectSizeEstimate));

                TestShapeEnergyCalculationApproachesImpl(shapeModel, vertices, objectSize, 2001, 2001, 1e-6);

                lastShape = shape;
            }
        }

        [TestMethod]
        public void TestEdgeLimits1()
        {
            VertexConstraint constraint1 = new VertexConstraint(new Vector(-10, -10), new Vector(10, 10), 3, 6);
            VertexConstraint constraint2 = new VertexConstraint(new Vector(11, -7), new Vector(13, 15), 2, 4);

            Range lengthRange, angleRange;
            TestEdgeLimitsCommonImpl(constraint1, constraint2, out lengthRange, out angleRange);

            Assert.IsFalse(angleRange.Outside);
            Assert.IsTrue(angleRange.Contains(0));
            Assert.IsFalse(angleRange.Contains(Math.PI));
            Assert.IsFalse(angleRange.Contains(Math.PI * 0.5));
            Assert.IsFalse(angleRange.Contains(-Math.PI * 0.5));
            Assert.IsFalse(angleRange.Contains(-Math.PI));
        }

        [TestMethod]
        public void TestEdgeLimits2()
        {
            VertexConstraint constraint1 = new VertexConstraint(new Vector(11, -7), new Vector(13, 15), 2, 4);
            VertexConstraint constraint2 = new VertexConstraint(new Vector(-10, -10), new Vector(10, 10), 3, 6);

            Range lengthRange, angleRange;
            TestEdgeLimitsCommonImpl(constraint1, constraint2, out lengthRange, out angleRange);

            Assert.IsTrue(angleRange.Outside);
            Assert.IsFalse(angleRange.Contains(0));
            Assert.IsTrue(angleRange.Contains(Math.PI));
            Assert.IsTrue(angleRange.Contains(-Math.PI));
            Assert.IsFalse(angleRange.Contains(Math.PI * 0.5));
            Assert.IsFalse(angleRange.Contains(-Math.PI * 0.5));
        }

        [TestMethod]
        public void TestEdgeLimits3()
        {
            VertexConstraint constraint1 = new VertexConstraint(new Vector(0, 0), new Vector(10, 10), 2, 4);
            VertexConstraint constraint2 = new VertexConstraint(new Vector(11, 11), new Vector(12, 16), 3, 6);

            Range lengthRange, angleRange;
            TestEdgeLimitsCommonImpl(constraint1, constraint2, out lengthRange, out angleRange);

            Assert.IsFalse(angleRange.Outside);
            Assert.IsTrue(angleRange.Contains(Math.PI * 0.25));
            Assert.IsFalse(angleRange.Contains(0));
            Assert.IsFalse(angleRange.Contains(Math.PI * 0.5));

            Assert.IsFalse(lengthRange.Contains(0));
            Assert.IsFalse(lengthRange.Contains(1));
        }

        [TestMethod]
        public void TestEdgeLimits4()
        {
            const double eps = 0.01;
            VertexConstraint constraint1 = new VertexConstraint(new Vector(0, 0), new Vector(1 - eps, 1 - eps), 2, 4);
            VertexConstraint constraint2 = new VertexConstraint(new Vector(1 + eps, 1 + eps), new Vector(2, 2), 3, 6);

            Range lengthRange, angleRange;
            TestEdgeLimitsCommonImpl(constraint1, constraint2, out lengthRange, out angleRange);

            Assert.IsFalse(angleRange.Outside);
            Assert.IsTrue(angleRange.Contains(0.01));
            Assert.IsFalse(angleRange.Contains(-Math.PI * 0.501));
            Assert.IsFalse(angleRange.Contains(-Math.PI));
            Assert.IsFalse(angleRange.Contains(Math.PI * 0.501));
            Assert.IsFalse(angleRange.Contains(Math.PI));

            Assert.IsFalse(lengthRange.Contains(3));
            Assert.IsTrue(lengthRange.Contains(0.05));
            Assert.IsTrue(lengthRange.Contains(2.8));
        }

        [TestMethod]
        public void TestEdgeLimits5()
        {
            VertexConstraint constraint1 = new VertexConstraint(new Vector(-10, 8), new Vector(10, 10), 1, 1);
            VertexConstraint constraint2 = new VertexConstraint(new Vector(5, 0), new Vector(6, 7), 1, 1);

            Range lengthRange, angleRange;
            TestEdgeLimitsCommonImpl(constraint1, constraint2, out lengthRange, out angleRange);
        }

        [TestMethod]
        public void TestEdgeLimits6()
        {
            VertexConstraint constraint1 = new VertexConstraint(new Vector(0, 0), new Vector(5, 5), 1, 1);
            VertexConstraint constraint2 = new VertexConstraint(new Vector(4, 4), new Vector(10, 9), 1, 1);

            Range lengthRange, angleRange;
            TestEdgeLimitsCommonImpl(constraint1, constraint2, out lengthRange, out angleRange);
        }

        [TestMethod]
        public void TestEdgeLimits7()
        {
            VertexConstraint constraint1 = new VertexConstraint(new Vector(0, 0), new Vector(10, 10), 1, 1);
            VertexConstraint constraint2 = new VertexConstraint(new Vector(5, 5), new Vector(8, 8), 1, 1);

            Range lengthRange, angleRange;
            TestEdgeLimitsCommonImpl(constraint1, constraint2, out lengthRange, out angleRange);
        }

        [TestMethod]
        public void TestSplitsNonIntersection()
        {
            VertexConstraint constraint = new VertexConstraint(new Vector(0, 0), new Vector(1, 1), 0, 1);
            
            List<VertexConstraint> coordSplit = constraint.SplitByCoords();
            Assert.IsTrue(coordSplit.Count == 4);
            for (int i = 0; i < coordSplit.Count; ++i)
            {
                for (int j = i + 1; j < coordSplit.Count; ++j)
                {
                    Range xRange1 = new Range(coordSplit[i].MinCoord.X, coordSplit[i].MaxCoord.X, false);
                    Range yRange1 = new Range(coordSplit[i].MinCoord.Y, coordSplit[i].MaxCoord.Y, false);
                    Range xRange2 = new Range(coordSplit[j].MinCoord.X, coordSplit[j].MaxCoord.X, false);
                    Range yRange2 = new Range(coordSplit[j].MinCoord.Y, coordSplit[j].MaxCoord.Y, false);
                    
                    Assert.IsFalse(xRange1.IntersectsWith(xRange2) && yRange1.IntersectsWith(yRange2));
                }
            }

            List<VertexConstraint> radiusSplit = constraint.SplitByRadius();
            Assert.IsTrue(radiusSplit.Count == 2);
            Range rRange1 = new Range(radiusSplit[0].MinRadius, radiusSplit[0].MaxRadius, false);
            Range rRange2 = new Range(radiusSplit[1].MinRadius, radiusSplit[1].MaxRadius, false);
            Assert.IsFalse(rRange1.IntersectsWith(rRange2));
        }
    }
}
