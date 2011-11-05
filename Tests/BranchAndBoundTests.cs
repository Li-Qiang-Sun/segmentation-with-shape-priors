using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Research.GraphBasedShapePrior.Tests
{
    [TestClass]
    public class BranchAndBoundTests
    {
        private static ShapeModel CreateTestShapeModel()
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
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(Math.PI * 0.4, 1.1, 0.1, 10));

            return ShapeModel.Create(edges, vertexParams, edgePairParams);
        }

        private static IEnumerable<VertexConstraints> VerticesToConstraints(IEnumerable<Circle> vertices)
        {
            return from v in vertices
                select new VertexConstraints(
                    new Point((int)v.Center.X, (int)v.Center.Y),
                    new Point((int)v.Center.X + 1, (int)v.Center.Y + 1),
                    (int)v.Radius,
                    (int)v.Radius + 1);
        }

        private static void TestShapeEnergyCalculationApproachesImpl(IEnumerable<Circle> vertices, Size objectSize)
        {
            ShapeModel model = CreateTestShapeModel();
            
            double sizeEstimate = SegmentatorBase.ImageSizeToObjectSizeEstimate(objectSize);

            // Create shape model and calculate energy in normal way
            Shape shape = new Shape(model, vertices);
            double energy1 = shape.CalculateEnergy(sizeEstimate);

            // Calculate energy via generalized distance transforms
            ShapeConstraintsSet constraints = ShapeConstraintsSet.Create(
                model, VerticesToConstraints(vertices));
            BranchAndBoundSegmentatorBase segmentator = new BranchAndBoundSegmentatorCpu();
            segmentator.ShapeModel = model;
            segmentator.AngleGridSize = 4000;
            segmentator.LengthGridSize = 3200;
            double energy2 = segmentator.CalculateMinShapeEnergy(constraints, objectSize);

            Assert.AreEqual(energy1, energy2, 0.2);
        }

        private static void TestGpuShapeTermsImpl(IEnumerable<VertexConstraints> vertexConstraints, Size imageSize)
        {
            ShapeModel model = CreateTestShapeModel();
            ShapeConstraintsSet constraintSet = ShapeConstraintsSet.Create(model, vertexConstraints);

            // Get CPU results
            Image2D<Tuple<double, double>> shapeTermsCpu = new Image2D<Tuple<double, double>>(imageSize.Width, imageSize.Height);
            BranchAndBoundSegmentatorCpu segmentatorCpu = new BranchAndBoundSegmentatorCpu();
            segmentatorCpu.PrepareShapeUnaryPotentials(constraintSet, shapeTermsCpu);

            // Get GPU results
            Image2D<Tuple<double, double>> shapeTermsGpu = new Image2D<Tuple<double, double>>(imageSize.Width, imageSize.Height);
            BranchAndBoundSegmentatorGpu2 segmentatorGpu = new BranchAndBoundSegmentatorGpu2();
            segmentatorGpu.PrepareShapeUnaryPotentials(constraintSet, shapeTermsGpu);

            // Compare with CPU results
            for (int x = 0; x < imageSize.Width; ++x)
                for (int y = 0; y < imageSize.Height; ++y)
                {
                    Assert.AreEqual(shapeTermsCpu[x, y].Item1, shapeTermsGpu[x, y].Item1, 1e-2f);
                    Assert.AreEqual(shapeTermsCpu[x, y].Item2, shapeTermsGpu[x, y].Item2, 1e-2f);
                }
        }
        
        [TestMethod]
        public void TestShapeEnergyCalculationApproaches1()
        {
            List<Circle> vertices = new List<Circle>();
            vertices.Add(new Circle(0, 0, 10));
            vertices.Add(new Circle(80, 0, 15));
            vertices.Add(new Circle(80, 100, 13));

            TestShapeEnergyCalculationApproachesImpl(vertices, new Size(100, 100));
        }

        [TestMethod]
        public void TestShapeEnergyCalculationApproaches2()
        {
            List<Circle> vertices = new List<Circle>();
            vertices.Add(new Circle(0, 0, 10));
            vertices.Add(new Circle(40, 0, 15));
            vertices.Add(new Circle(0, 42, 13));

            TestShapeEnergyCalculationApproachesImpl(vertices, new Size(100, 100));
        }

        [TestMethod]
        public void TestGpuShapeTerms1()
        {
            List<VertexConstraints> vertexConstraints = new List<VertexConstraints>();
            vertexConstraints.Add(new VertexConstraints(
                new Point(30, 30), new Point(70, 40), 5, 15));
            vertexConstraints.Add(new VertexConstraints(
                new Point(280, 180), new Point(281, 181), 1, 10));
            vertexConstraints.Add(new VertexConstraints(
                new Point(30, 160), new Point(50, 200), 1, 20));
            
            TestGpuShapeTermsImpl(vertexConstraints, new Size(320, 240));
        }

        [TestMethod]
        public void TestGpuShapeTerms2()
        {
            List<VertexConstraints> vertexConstraints = new List<VertexConstraints>();
            vertexConstraints.Add(new VertexConstraints(
                new Point(100, 100), new Point(105, 107), 5, 70));
            vertexConstraints.Add(new VertexConstraints(
                new Point(110, 130), new Point(113, 135), 1, 6));
            vertexConstraints.Add(new VertexConstraints(
                new Point(310, 230), new Point(320, 240), 3, 20));

            TestGpuShapeTermsImpl(vertexConstraints, new Size(320, 240));
        }

        [TestMethod]
        public void TestGpuShapeTerms3()
        {
            List<VertexConstraints> vertexConstraints = new List<VertexConstraints>();
            vertexConstraints.Add(new VertexConstraints(
                new Point(100, 100), new Point(105, 107), 5, 70));
            vertexConstraints.Add(new VertexConstraints(
                new Point(120, 140), new Point(153, 176), 25, 45));
            vertexConstraints.Add(new VertexConstraints(
                new Point(10, 10), new Point(130, 12), 1, 57));

            TestGpuShapeTermsImpl(vertexConstraints, new Size(320, 240));
        }
    }
}
