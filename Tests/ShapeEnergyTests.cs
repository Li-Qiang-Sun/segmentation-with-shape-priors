using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Research.GraphBasedShapePrior.Tests
{
    [TestClass]
    public class ShapeEnergyTests
    {
        private static void TestShapeEnergyCalculationApproachesImpl(
            ShapeModel model, IEnumerable<Circle> vertices, Size objectSize, int lengthGridSize, int angleGridSize, double eps)
        {
            double sizeEstimate = SegmentatorBase.ImageSizeToObjectSizeEstimate(objectSize);

            // Create shape model and calculate energy in normal way
            Shape shape = new Shape(model, vertices);
            double energy1 = shape.CalculateEnergy(sizeEstimate);

            // Calculate energy via generalized distance transforms
            ShapeConstraintsSet constraints = ShapeConstraintsSet.Create(
                model, TestHelper.VerticesToConstraints(vertices));
            BranchAndBoundSegmentatorBase segmentator = new BranchAndBoundSegmentatorCpu();
            segmentator.ShapeModel = model;
            segmentator.LengthGridSize = lengthGridSize;
            segmentator.AngleGridSize = angleGridSize;
            double energy2 = segmentator.CalculateMinShapeEnergy(constraints, objectSize);

            Assert.AreEqual(energy1, energy2, eps);
        }

        private static void TestMeanShapeImpl(ShapeModel shapeModel)
        {
            Size imageSize = new Size(100, 160);
            Shape meanShape = shapeModel.BuildMeanShape(imageSize);
            Assert.AreEqual(0, meanShape.CalculateEnergy(SegmentatorBase.ImageSizeToObjectSizeEstimate(imageSize)), 1e-6);
        }
        
        [TestMethod]
        public void TestShapeEnergyCalculationApproaches1()
        {
            List<Circle> vertices = new List<Circle>();
            vertices.Add(new Circle(0, 0, 10));
            vertices.Add(new Circle(80, 0, 15));
            vertices.Add(new Circle(80, 100, 13));

            TestShapeEnergyCalculationApproachesImpl(
                TestHelper.CreateTestShapeModelWith2Edges(Math.PI * 0.5, 1.1), vertices, new Size(100, 100), 3000, 3000, 0.1);
        }

        [TestMethod]
        public void TestShapeEnergyCalculationApproaches2()
        {
            List<Circle> vertices = new List<Circle>();
            vertices.Add(new Circle(0, 0, 10));
            vertices.Add(new Circle(40, 0, 15));
            vertices.Add(new Circle(0, 42, 13));

            TestShapeEnergyCalculationApproachesImpl(
                TestHelper.CreateTestShapeModelWith2Edges(Math.PI * 0.5, 1.1), vertices, new Size(100, 100), 3000, 3000, 0.25);
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

            TestShapeEnergyCalculationApproachesImpl(TestHelper.CreateTestShapeModel4Edges(), vertices, new Size(100, 100), 3000, 3000, 0.1);
        }

        [TestMethod]
        public void TestMeanShape1()
        {
            TestMeanShapeImpl(TestHelper.CreateTestShapeModelWith2Edges(Math.PI * 0.5, 1.1));
        }

        [TestMethod]
        public void TestMeanShape2()
        {
            TestMeanShapeImpl(TestHelper.CreateTestShapeModel4Edges());
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
            
            double objectSizeEstimate = SegmentatorBase.ImageSizeToObjectSizeEstimate(objectSize);

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

                // Test if approximated energy is ok, use linearly increasing eps
                double smartEps = 0.1 + 2 * (i <= iterationCount / 2 ? i : iterationCount - i);
                TestShapeEnergyCalculationApproachesImpl(shapeModel, vertices, objectSize, 2000, 2000, smartEps);

                lastShape = shape;
            }
        }
    }
}
