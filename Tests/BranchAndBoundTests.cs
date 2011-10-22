using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Research.GraphBasedShapePrior;
using TidePowerd.DeviceMethods.Vectors;

namespace Tests
{
    [TestClass]
    public class BranchAndBoundTests
    {
        private ShapeModel CreateTestShapeModel()
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
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(Math.PI * 0.4, 1.2, 0.1, 10));

            return ShapeModel.Create(edges, vertexParams, edgePairParams);
        }

        private IEnumerable<VertexConstraints> VerticesToConstraints(IEnumerable<Circle> vertices)
        {
            return from v in vertices
                   select new VertexConstraints(
                       new Point((int)v.Center.X, (int)v.Center.Y),
                       new Point((int)v.Center.X + 1, (int)v.Center.Y + 1),
                       (int)v.Radius,
                       (int)v.Radius + 1);
        }
        
        [TestMethod]
        public void TestShapeEnergyCalculationApproaches()
        {
            ShapeModel model = CreateTestShapeModel();
            
            // Create shape vertices (integer length and angles for fair comparison)
            List<Circle> vertices = new List<Circle>();
            vertices.Add(new Circle(0, 0, 10));
            vertices.Add(new Circle(80, 0, 15));
            vertices.Add(new Circle(80, 100, 13));
            const double objectSize = 100;

            // Create shape model and calculate energy in normal way
            Shape shape = new Shape(model, vertices);
            double energy1 = shape.CalculateEnergy(objectSize);

            // Calculate energy via generalized distance transforms
            ShapeConstraintsSet constraints = ShapeConstraintsSet.Create(
                model, VerticesToConstraints(vertices));
            BranchAndBoundSegmentator segmentator = new BranchAndBoundSegmentator();
            segmentator.ShapeModel = model;
            double energy2 = segmentator.CalculateMinShapeEnergy(constraints, objectSize);

            Assert.AreEqual(energy1, energy2, 1e-6);
        }

        [TestMethod]
        public void TestGpuConvexHull()
        {
            Int16Vector2[] convexHull = new Int16Vector2[5];
            convexHull[0] = new Int16Vector2(0, 0);
            convexHull[1] = new Int16Vector2(-1, 2);
            convexHull[2] = new Int16Vector2(2, 3);
            convexHull[3] = new Int16Vector2(2, 1);
            convexHull[4] = new Int16Vector2(1, 0);
            
            Assert.IsTrue(BranchAndBoundGPU.PointInConvexHull(new Int16Vector2(0, 1), convexHull));
            Assert.IsTrue(BranchAndBoundGPU.PointInConvexHull(new Int16Vector2(0, 2), convexHull));
            Assert.IsTrue(BranchAndBoundGPU.PointInConvexHull(new Int16Vector2(1, 1), convexHull));
            Assert.IsTrue(BranchAndBoundGPU.PointInConvexHull(new Int16Vector2(1, 2), convexHull));

            Assert.IsFalse(BranchAndBoundGPU.PointInConvexHull(new Int16Vector2(-1, 0), convexHull));
            Assert.IsFalse(BranchAndBoundGPU.PointInConvexHull(new Int16Vector2(-1, 1), convexHull));
            Assert.IsFalse(BranchAndBoundGPU.PointInConvexHull(new Int16Vector2(2, 0), convexHull));
            Assert.IsFalse(BranchAndBoundGPU.PointInConvexHull(new Int16Vector2(3, 3), convexHull));
            Assert.IsFalse(BranchAndBoundGPU.PointInConvexHull(new Int16Vector2(0, 3), convexHull));
        }

        [TestMethod]
        public void TestGpuShapeTerms()
        {
            ShapeModel model = CreateTestShapeModel();
            
            List<VertexConstraints> vertexConstraints = new List<VertexConstraints>();
            vertexConstraints.Add(new VertexConstraints(
                new Point(10, 10), new Point(20, 20), 5, 20));
            vertexConstraints.Add(new VertexConstraints(
                new Point(80, 20), new Point(90, 30), 1, 10));
            vertexConstraints.Add(new VertexConstraints(
                new Point(70, 100), new Point(85, 115), 1, 20));
            ShapeConstraintsSet constraintSet = ShapeConstraintsSet.Create(model, vertexConstraints);

            // Get GPU results
            Image2D<Tuple<double, double>> shapeTerms = new Image2D<Tuple<double, double>>(100, 150);
            BranchAndBoundGPU.CalculateShapeUnaryTerms(constraintSet, shapeTerms);

            // Compare with CPU results
            for (int x = 0; x < shapeTerms.Width; ++x)
                for (int y = 0; y < shapeTerms.Height; ++y)
                {
                    Tuple<double, double> cpuResults = BranchAndBoundSegmentator.CalculateShapeTerm(
                        constraintSet, new Point(x, y));
                    Assert.AreEqual(cpuResults.Item1, shapeTerms[x, y].Item1, 1e-6);
                    //Assert.AreEqual(cpuResults.Item2, shapeTerms[x, y].Item2, 1e-6);
                }
        }
    }
}
