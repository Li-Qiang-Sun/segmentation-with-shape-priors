using System;
using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class CpuBranchAndBoundShapeTermsCalculator : IBranchAndBoundShapeTermsCalculator
    {
        public void CalculateShapeTerms(ShapeConstraints constraintsSet, Image2D<ObjectBackgroundTerm> result)
        {
            for (int x = 0; x < result.Width; ++x)
                for (int y = 0; y < result.Height; ++y)
                    result[x, y] = CalculateShapeTerm(constraintsSet, new Point(x, y));
        }

        public static ObjectBackgroundTerm CalculateShapeTerm(ShapeConstraints constraintsSet, Point point)
        {
            Vector pointAsVec = new Vector(point.X, point.Y);

            double minObjectPenalty = Double.PositiveInfinity;
            double maxOfMinBackgroundPenalty = Double.NegativeInfinity;
            for (int edgeIndex = 0; edgeIndex < constraintsSet.ShapeModel.Edges.Count; ++edgeIndex)
            {
                ShapeEdge edge = constraintsSet.ShapeModel.Edges[edgeIndex];
                VertexConstraints constraints1 = constraintsSet.VertexConstraints[edge.Index1];
                VertexConstraints constraints2 = constraintsSet.VertexConstraints[edge.Index2];
                EdgeConstraints edgeConstraint = constraintsSet.EdgeConstraints[edgeIndex];

                double minDistanceSqr, maxDistanceSqr;
                MinDistanceForEdge(pointAsVec, constraints1, constraints2, out minDistanceSqr, out maxDistanceSqr);

                // If point is inside convex hull than min distance is 0
                Polygon convexHull = constraintsSet.GetConvexHullForVertexPair(edge.Index1, edge.Index2);
                if (convexHull.IsPointInside(pointAsVec))
                    minDistanceSqr = 0;

                // Choose best penalties
                minObjectPenalty = Math.Min(
                    minObjectPenalty,
                    constraintsSet.ShapeModel.CalculateObjectPenaltyForEdge(minDistanceSqr, edgeConstraint.MaxWidth));
                maxOfMinBackgroundPenalty = Math.Max(
                    maxOfMinBackgroundPenalty,
                    constraintsSet.ShapeModel.CalculateBackgroundPenaltyForEdge(maxDistanceSqr, edgeConstraint.MinWidth));
            }

            return new ObjectBackgroundTerm(minObjectPenalty, maxOfMinBackgroundPenalty);
        }

        private static void MinDistanceForEdge(Vector point, VertexConstraints constraints1, VertexConstraints constraints2, out double minDistanceSqr, out double maxDistanceSqr)
        {
            minDistanceSqr = Double.PositiveInfinity;
            maxDistanceSqr = 0;
            foreach (Vector vertex1 in constraints1.Corners)
            {
                foreach (Vector vertex2 in constraints2.Corners)
                {
                    double distanceSqr = point.DistanceToSegmentSquared(vertex1, vertex2);
                    minDistanceSqr = Math.Min(minDistanceSqr, distanceSqr);
                    maxDistanceSqr = Math.Max(maxDistanceSqr, distanceSqr);
                }
            }

            Vector? projection1 = constraints1.GetClosestPoint(point);
            if (projection1.HasValue)
            {
                foreach (Vector vertex2 in constraints2.Corners)
                    minDistanceSqr = Math.Min(minDistanceSqr, point.DistanceToSegmentSquared(projection1.Value, vertex2));
            }

            Vector? projection2 = constraints2.GetClosestPoint(point);
            if (projection2.HasValue)
            {
                foreach (Vector vertex1 in constraints1.Corners)
                    minDistanceSqr = Math.Min(minDistanceSqr, point.DistanceToSegmentSquared(vertex1, projection2.Value));
            }
        }
    }
}
