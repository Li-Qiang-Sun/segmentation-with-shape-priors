using System;
using System.Collections.Generic;
using System.Linq;

namespace Research.GraphBasedShapePrior.Tests
{
    static class TestHelper
    {
        public static ShapeModel CreateTestShapeModelWith1Edge()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));

            List<ShapeEdgeParams> edgeParams = new List<ShapeEdgeParams>();
            edgeParams.Add(new ShapeEdgeParams(0.15, 0.05));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();

            return ShapeModel.Create(edges, edgeParams, edgePairParams);
        }
        
        public static ShapeModel CreateTestShapeModelWith2Edges(double meanAngle, double lengthRatio)
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));
            edges.Add(new ShapeEdge(1, 2));

            List<ShapeEdgeParams> edgeParams = new List<ShapeEdgeParams>();
            edgeParams.Add(new ShapeEdgeParams(0.15, 0.05));
            edgeParams.Add(new ShapeEdgeParams(0.15, 0.05));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(meanAngle, lengthRatio, 0.1, 10));

            return ShapeModel.Create(edges, edgeParams, edgePairParams);
        }

        public static ShapeModel CreateTestShapeModel5Edges()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));
            edges.Add(new ShapeEdge(1, 2));
            edges.Add(new ShapeEdge(2, 3));
            edges.Add(new ShapeEdge(2, 4));
            edges.Add(new ShapeEdge(0, 5));

            List<ShapeEdgeParams> edgeParams = new List<ShapeEdgeParams>();
            edgeParams.Add(new ShapeEdgeParams(0.15, 0.05));
            edgeParams.Add(new ShapeEdgeParams(0.15, 0.05));
            edgeParams.Add(new ShapeEdgeParams(0.15, 0.05));
            edgeParams.Add(new ShapeEdgeParams(0.15, 0.05));
            edgeParams.Add(new ShapeEdgeParams(0.15, 0.05));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(Math.PI * 0.4, 1.1, 0.1, 10));
            edgePairParams.Add(new Tuple<int, int>(1, 2), new ShapeEdgePairParams(-Math.PI * 0.5, 0.8, 0.1, 10));
            edgePairParams.Add(new Tuple<int, int>(1, 3), new ShapeEdgePairParams(Math.PI * 0.5, 0.8, 0.1, 10));
            edgePairParams.Add(new Tuple<int, int>(0, 4), new ShapeEdgePairParams(-Math.PI * 0.5, 1.2, 0.05, 5));

            return ShapeModel.Create(edges, edgeParams, edgePairParams);
        }

        public static ShapeModel CreateLetterShapeModel()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));
            edges.Add(new ShapeEdge(0, 2));
            edges.Add(new ShapeEdge(2, 3));
            edges.Add(new ShapeEdge(2, 4));
            edges.Add(new ShapeEdge(4, 5));

            List<ShapeEdgeParams> vertexParams = new List<ShapeEdgeParams>();
            vertexParams.Add(new ShapeEdgeParams(0.07, 0.05));
            vertexParams.Add(new ShapeEdgeParams(0.07, 0.05));
            vertexParams.Add(new ShapeEdgeParams(0.07, 0.05));
            vertexParams.Add(new ShapeEdgeParams(0.07, 0.05));
            vertexParams.Add(new ShapeEdgeParams(0.07, 0.05));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams = new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(-Math.PI * 0.5, 1.3, Math.PI * 0.02, 2));
            edgePairParams.Add(new Tuple<int, int>(1, 2), new ShapeEdgePairParams(Math.PI * 0.5, 1, Math.PI * 0.02, 2));
            edgePairParams.Add(new Tuple<int, int>(2, 3), new ShapeEdgePairParams(-Math.PI * 0.5, 1, Math.PI * 0.02, 2));
            edgePairParams.Add(new Tuple<int, int>(3, 4), new ShapeEdgePairParams(Math.PI * 0.5, 0.77, Math.PI * 0.02, 2));

            return ShapeModel.Create(edges, vertexParams, edgePairParams);
        }

        public static IEnumerable<VertexConstraints> VerticesToConstraints(IEnumerable<Vector> vertices)
        {
            return from vertex in vertices select new VertexConstraints(vertex);
        }

        public static IEnumerable<EdgeConstraints> EdgeWidthsToConstraints(IEnumerable<double> edgeWidths)
        {
            return from width in edgeWidths select new EdgeConstraints(width);
        }
    }
}
