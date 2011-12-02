using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Research.GraphBasedShapePrior.Tests
{
    [TestClass]
    public class ShapeUnaryTermTests
    {
        private static void TestGpuShapeTermsImpl(IEnumerable<VertexConstraint> vertexConstraints, Size imageSize)
        {
            ShapeModel model = TestHelper.CreateTestShapeModelWith2Edges(Math.PI * 0.5, 1.1);
            model.Cutoff = 1;
            VertexConstraintSet constraintSet = VertexConstraintSet.CreateFromConstraints(model, vertexConstraints);

            // Get CPU results
            Image2D<ObjectBackgroundTerm> shapeTermsCpu = new Image2D<ObjectBackgroundTerm>(imageSize.Width, imageSize.Height);
            CpuBranchAndBoundShapeTermsCalculator calculatorCpu = new CpuBranchAndBoundShapeTermsCalculator();
            calculatorCpu.CalculateShapeTerms(constraintSet, shapeTermsCpu);
            Image2D.SaveToFile(shapeTermsCpu, -10, 10, "./cpu.png");

            // Get GPU results
            Image2D<ObjectBackgroundTerm> shapeTermsGpu = new Image2D<ObjectBackgroundTerm>(imageSize.Width, imageSize.Height);
            GpuBranchAndBoundShapeTermsCalculator calculatorGpu = new GpuBranchAndBoundShapeTermsCalculator();
            calculatorGpu.CalculateShapeTerms(constraintSet, shapeTermsGpu);
            Image2D.SaveToFile(shapeTermsGpu, -10, 10, "./gpu.png");

            // Compare with CPU results
            for (int x = 0; x < imageSize.Width; ++x)
                for (int y = 0; y < imageSize.Height; ++y)
                {
                    Assert.AreEqual(shapeTermsCpu[x, y].ObjectTerm, shapeTermsGpu[x, y].ObjectTerm, 1e-2f);
                    Assert.AreEqual(shapeTermsCpu[x, y].BackgroundTerm, shapeTermsGpu[x, y].BackgroundTerm, 1e-2f);
                }
        }

        [TestMethod]
        public void TestGpuShapeTerms1()
        {
            List<VertexConstraint> vertexConstraints = new List<VertexConstraint>();
            vertexConstraints.Add(new VertexConstraint(
                new Vector(30, 30), new Vector(70, 40), 5, 15));
            vertexConstraints.Add(new VertexConstraint(
                new Vector(280, 180), new Vector(281, 181), 1, 10));
            vertexConstraints.Add(new VertexConstraint(
                new Vector(30, 160), new Vector(50, 200), 1, 20));
            
            TestGpuShapeTermsImpl(vertexConstraints, new Size(320, 240));
        }

        [TestMethod]
        public void TestGpuShapeTerms2()
        {
            List<VertexConstraint> vertexConstraints = new List<VertexConstraint>();
            vertexConstraints.Add(new VertexConstraint(
                new Vector(100, 100), new Vector(105, 107), 5, 70));
            vertexConstraints.Add(new VertexConstraint(
                new Vector(110, 130), new Vector(113, 135), 1, 6));
            vertexConstraints.Add(new VertexConstraint(
                new Vector(310, 230), new Vector(320, 240), 3, 20));

            TestGpuShapeTermsImpl(vertexConstraints, new Size(320, 240));
        }

        [TestMethod]
        public void TestGpuShapeTerms3()
        {
            List<VertexConstraint> vertexConstraints = new List<VertexConstraint>();
            vertexConstraints.Add(new VertexConstraint(
                new Vector(100, 100), new Vector(105, 107), 5, 70));
            vertexConstraints.Add(new VertexConstraint(
                new Vector(120, 140), new Vector(153, 176), 25, 45));
            vertexConstraints.Add(new VertexConstraint(
                new Vector(10, 10), new Vector(130, 12), 1, 57));

            TestGpuShapeTermsImpl(vertexConstraints, new Size(320, 240));
        }

        [TestMethod]
        public void TestGpuShapeTerms4()
        {
            List<VertexConstraint> vertexConstraints = new List<VertexConstraint>();
            vertexConstraints.Add(new VertexConstraint(
                new Vector(100, 200), new Vector(101, 201), 5, 6));
            vertexConstraints.Add(new VertexConstraint(
                new Vector(300, 0), new Vector(301, 1), 25, 26));
            vertexConstraints.Add(new VertexConstraint(
                new Vector(10, 10), new Vector(11, 11), 57, 57));

            TestGpuShapeTermsImpl(vertexConstraints, new Size(320, 240));
        }
    }
}
