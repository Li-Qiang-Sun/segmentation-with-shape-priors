using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Research.GraphBasedShapePrior.Tests
{
    static class TestHelper
    {
        public static ShapeModel CreateTestShapeModelWith2Edges(double meanAngle, double lengthRatio)
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
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(meanAngle, lengthRatio, 0.1, 10));

            return ShapeModel.Create(edges, vertexParams, edgePairParams);
        }

        public static ShapeModel CreateTestShapeModel4Edges()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));
            edges.Add(new ShapeEdge(1, 2));
            edges.Add(new ShapeEdge(2, 3));
            edges.Add(new ShapeEdge(2, 4));
            edges.Add(new ShapeEdge(0, 5));

            List<ShapeVertexParams> vertexParams = new List<ShapeVertexParams>();
            vertexParams.Add(new ShapeVertexParams(0.15, 0.1));
            vertexParams.Add(new ShapeVertexParams(0.15, 0.1));
            vertexParams.Add(new ShapeVertexParams(0.15, 0.1));
            vertexParams.Add(new ShapeVertexParams(0.15, 0.1));
            vertexParams.Add(new ShapeVertexParams(0.15, 0.1));
            vertexParams.Add(new ShapeVertexParams(0.15, 0.1));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(Math.PI * 0.4, 1.1, 0.1, 10));
            edgePairParams.Add(new Tuple<int, int>(1, 2), new ShapeEdgePairParams(-Math.PI * 0.5, 0.8, 0.1, 10));
            edgePairParams.Add(new Tuple<int, int>(1, 3), new ShapeEdgePairParams(Math.PI * 0.5, 0.8, 0.1, 10));
            edgePairParams.Add(new Tuple<int, int>(0, 4), new ShapeEdgePairParams(-Math.PI * 0.5, 1.2, 0.05, 5));

            return ShapeModel.Create(edges, vertexParams, edgePairParams);
        }

        public static IEnumerable<VertexConstraints> VerticesToConstraints(IEnumerable<Circle> vertices)
        {
            return from v in vertices select new VertexConstraints(
                new Point((int)v.Center.X, (int)v.Center.Y),
                new Point((int)v.Center.X + 1, (int)v.Center.Y + 1),
                (int)v.Radius,
                (int)v.Radius + 1);
        }
    }
}
