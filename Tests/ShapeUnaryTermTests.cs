using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Research.GraphBasedShapePrior.Tests
{
    [TestClass]
    public class ShapeUnaryTermTests
    {
        private static void TestGpuShapeTermsImpl(IEnumerable<VertexConstraints> vertexConstraints, Size imageSize)
        {
            ShapeModel model = TestHelper.CreateTestShapeModelWith2Edges(Math.PI * 0.5, 1.1);
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
