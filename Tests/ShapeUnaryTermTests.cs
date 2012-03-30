using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Research.GraphBasedShapePrior.Tests
{
    [TestClass]
    public class ShapeUnaryTermTests
    {
        private static void TestShapeTermsImpl(ShapeModel shapeModel, IEnumerable<VertexConstraints> vertexConstraints, IEnumerable<EdgeConstraints> edgeConstraints, Size imageSize)
        {
            ShapeConstraints constraintSet = ShapeConstraints.CreateFromConstraints(shapeModel, vertexConstraints, edgeConstraints);

            // Get CPU results
            Image2D<ObjectBackgroundTerm> shapeTermsCpu = new Image2D<ObjectBackgroundTerm>(imageSize.Width, imageSize.Height);
            CpuBranchAndBoundShapeTermsCalculator calculatorCpu = new CpuBranchAndBoundShapeTermsCalculator();
            calculatorCpu.CalculateShapeTerms(constraintSet, shapeTermsCpu);
            Image2D.SaveToFile(shapeTermsCpu, -1000, 1000, "./cpu.png");

            // Get GPU results
            Image2D<ObjectBackgroundTerm> shapeTermsGpu = new Image2D<ObjectBackgroundTerm>(imageSize.Width, imageSize.Height);
            GpuBranchAndBoundShapeTermsCalculator calculatorGpu = new GpuBranchAndBoundShapeTermsCalculator();
            calculatorGpu.CalculateShapeTerms(constraintSet, shapeTermsGpu);
            Image2D.SaveToFile(shapeTermsGpu, -1000, 1000, "./gpu.png");

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
            List<VertexConstraints> vertexConstraints = new List<VertexConstraints>();
            vertexConstraints.Add(new VertexConstraints(new Vector(30, 30), new Vector(70, 40)));
            vertexConstraints.Add(new VertexConstraints(new Vector(280, 180), new Vector(281, 181)));
            vertexConstraints.Add(new VertexConstraints(new Vector(30, 160), new Vector(50, 200)));

            List<EdgeConstraints> edgeConstraints = new List<EdgeConstraints>();
            edgeConstraints.Add(new EdgeConstraints(1, 8));
            edgeConstraints.Add(new EdgeConstraints(5, 21));
            
            TestShapeTermsImpl(TestHelper.CreateTestShapeModelWith2Edges(Math.PI * 0.5, 1.1), vertexConstraints, edgeConstraints, new Size(320, 240));
        }

        [TestMethod]
        public void TestGpuShapeTerms2()
        {
            List<VertexConstraints> vertexConstraints = new List<VertexConstraints>();
            vertexConstraints.Add(new VertexConstraints(new Vector(100, 100), new Vector(105, 107)));
            vertexConstraints.Add(new VertexConstraints(new Vector(110, 130), new Vector(113, 135)));
            vertexConstraints.Add(new VertexConstraints(new Vector(310, 230), new Vector(320, 240)));

            List<EdgeConstraints> edgeConstraints = new List<EdgeConstraints>();
            edgeConstraints.Add(new EdgeConstraints(30, 31));
            edgeConstraints.Add(new EdgeConstraints(2, 41));

            TestShapeTermsImpl(TestHelper.CreateTestShapeModelWith2Edges(Math.PI * 0.5, 1.1), vertexConstraints, edgeConstraints, new Size(320, 240));
        }

        [TestMethod]
        public void TestGpuShapeTerms3()
        {
            List<VertexConstraints> vertexConstraints = new List<VertexConstraints>();
            vertexConstraints.Add(new VertexConstraints(new Vector(100, 100), new Vector(105, 107)));
            vertexConstraints.Add(new VertexConstraints(new Vector(120, 140), new Vector(153, 176)));
            vertexConstraints.Add(new VertexConstraints(new Vector(10, 10), new Vector(130, 12)));
            
            List<EdgeConstraints> edgeConstraints = new List<EdgeConstraints>();
            edgeConstraints.Add(new EdgeConstraints(10));
            edgeConstraints.Add(new EdgeConstraints(20));

            TestShapeTermsImpl(TestHelper.CreateTestShapeModelWith2Edges(Math.PI * 0.5, 1.1), vertexConstraints, edgeConstraints, new Size(320, 240));
        }

        [TestMethod]
        public void TestGpuShapeTerms4()
        {
            List<VertexConstraints> vertexConstraints = new List<VertexConstraints>();
            vertexConstraints.Add(new VertexConstraints(new Vector(100, 200), new Vector(101, 201)));
            vertexConstraints.Add(new VertexConstraints(new Vector(300, 0), new Vector(301, 1)));
            vertexConstraints.Add(new VertexConstraints(new Vector(10, 10), new Vector(11, 11)));

            List<EdgeConstraints> edgeConstraints = new List<EdgeConstraints>();
            edgeConstraints.Add(new EdgeConstraints(20));
            edgeConstraints.Add(new EdgeConstraints(30));

            TestShapeTermsImpl(TestHelper.CreateTestShapeModelWith2Edges(Math.PI * 0.5, 1.1), vertexConstraints, edgeConstraints, new Size(320, 240));
        }

        [TestMethod]
        public void TestGpuShapeTerms5()
        {
            List<VertexConstraints> vertexConstraints = new List<VertexConstraints>();
            vertexConstraints.Add(new VertexConstraints(new Vector(100, 100), new Vector(101, 101)));
            vertexConstraints.Add(new VertexConstraints(new Vector(280, 200), new Vector(281, 201)));

            List<EdgeConstraints> edgeConstraints = new List<EdgeConstraints>();
            edgeConstraints.Add(new EdgeConstraints(1, 40));

            TestShapeTermsImpl(TestHelper.CreateTestShapeModelWith1Edge(), vertexConstraints, edgeConstraints, new Size(320, 240));
        }

        [TestMethod]
        public void TestGpuShapeTerms6()
        {
            List<VertexConstraints> vertexConstraints = new List<VertexConstraints>();
            vertexConstraints.Add(new VertexConstraints(new Vector(100, 100), new Vector(101, 101)));
            vertexConstraints.Add(new VertexConstraints(new Vector(280, 200), new Vector(301, 221)));

            List<EdgeConstraints> edgeConstraints = new List<EdgeConstraints>();
            edgeConstraints.Add(new EdgeConstraints(20, 20));

            TestShapeTermsImpl(TestHelper.CreateTestShapeModelWith1Edge(), vertexConstraints, edgeConstraints, new Size(320, 240));
        }
    }
}
