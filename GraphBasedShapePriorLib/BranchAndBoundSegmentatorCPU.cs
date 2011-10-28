using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Research.GraphBasedShapePrior
{
    public class BranchAndBoundSegmentatorCpu : BranchAndBoundSegmentatorBase
    {
        public override void PrepareShapeUnaryPotentials(ShapeConstraintsSet constraintsSet, Image2D<Tuple<double, double>> result)
        {
            for (int x = 0; x < result.Width; ++x)
                for (int y = 0; y < result.Height; ++y)
                    result[x, y] = CalculateShapeTerm(constraintsSet, new Point(x, y));
        }

        private static Tuple<double, double> CalculateShapeTerm(ShapeConstraintsSet constraintsSet, Point point)
        {
            Vector pointAsVec = new Vector(point.X, point.Y);

            // Calculate weight to sink (min price for object label at (x, y))
            double minDistanceToEdge = Double.PositiveInfinity;
            bool inConvexHull = false;

            // First check if pixel is in convex hull for some edge
            for (int edgeIndex = 0; edgeIndex < constraintsSet.ShapeModel.Edges.Count; ++edgeIndex)
            {
                ShapeEdge edge = constraintsSet.ShapeModel.Edges[edgeIndex];
                Polygon convexHull = constraintsSet.GetConvexHullForVertexPair(edge.Index1, edge.Index2);
                if (convexHull.IsPointInside(pointAsVec))
                {
                    minDistanceToEdge = 0;
                    inConvexHull = true;
                    break;
                }
            }

            // If not, find closest edge possible
            if (!inConvexHull)
            {
                for (int edgeIndex = 0; edgeIndex < constraintsSet.ShapeModel.Edges.Count; ++edgeIndex)
                {
                    double distance;

                    ShapeEdge edge = constraintsSet.ShapeModel.Edges[edgeIndex];
                    VertexConstraints constraints1 = constraintsSet.GetConstraintsForVertex(edge.Index1);
                    VertexConstraints constraints2 = constraintsSet.GetConstraintsForVertex(edge.Index2);

                    Vector? closestPoint1 = constraints1.GetClosestPoint(point);
                    Vector? closestPoint2 = constraints2.GetClosestPoint(point);
                    for (int corner1 = 0; corner1 < 4; ++corner1)
                    {

                        for (int corner2 = 0; corner2 < 4; ++corner2)
                        {
                            // Check pair of corners
                            Polygon pulleyPoints = constraintsSet.GetPulleyPointsForVertexPair(
                                edge.Index1, edge.Index2, corner1, corner2, true);
                            distance = constraintsSet.ShapeModel.CalculateDistanceToEdge(
                                pointAsVec,
                                new Circle(constraints1.Corners[corner1], constraints1.MaxRadiusExclusive - 1),
                                new Circle(constraints2.Corners[corner2], constraints2.MaxRadiusExclusive - 1),
                                pulleyPoints);
                            minDistanceToEdge = Math.Min(minDistanceToEdge, distance);

                            // Check closest point simultaneously
                            if (corner1 == 0 && closestPoint1.HasValue)
                            {
                                distance = constraintsSet.ShapeModel.CalculateDistanceToEdge(
                                    pointAsVec,
                                    new Circle(closestPoint1.Value, constraints1.MaxRadiusExclusive - 1),
                                    new Circle(constraints2.Corners[corner2], constraints2.MaxRadiusExclusive - 1));
                                minDistanceToEdge = Math.Min(minDistanceToEdge, distance);
                            }
                        }

                        // Also check closest point
                        if (closestPoint2.HasValue)
                        {
                            distance = constraintsSet.ShapeModel.CalculateDistanceToEdge(
                                pointAsVec,
                                new Circle(constraints1.Corners[corner1], constraints1.MaxRadiusExclusive - 1),
                                new Circle(closestPoint2.Value, constraints2.MaxRadiusExclusive - 1));
                            minDistanceToEdge = Math.Min(minDistanceToEdge, distance);
                        }
                    }

                    // Also try both corners
                    if (closestPoint1.HasValue && closestPoint2.HasValue)
                    {
                        distance = constraintsSet.ShapeModel.CalculateDistanceToEdge(
                            pointAsVec,
                            new Circle(closestPoint1.Value, constraints1.MaxRadiusExclusive - 1),
                            new Circle(closestPoint2.Value, constraints2.MaxRadiusExclusive - 1));
                        minDistanceToEdge = Math.Min(minDistanceToEdge, distance);
                    }
                }
            }

            double toSink = constraintsSet.ShapeModel.CalculateObjectPenaltyFromDistance(minDistanceToEdge);

            // Calculate weight to source (min price for background label at (x, y))
            double maxDistanceToEdge = Double.PositiveInfinity;
            for (int edgeIndex = 0; edgeIndex < constraintsSet.ShapeModel.Edges.Count; ++edgeIndex)
            {
                ShapeEdge edge = constraintsSet.ShapeModel.Edges[edgeIndex];
                VertexConstraints constraints1 = constraintsSet.GetConstraintsForVertex(edge.Index1);
                VertexConstraints constraints2 = constraintsSet.GetConstraintsForVertex(edge.Index2);

                // Solution will connect 2 corners (need to prove this fact)
                double maxDistanceToCurrentEdge = 0;
                for (int corner1 = 0; corner1 < 4; ++corner1)
                {
                    for (int corner2 = 0; corner2 < 4; ++corner2)
                    {
                        Polygon pulleyPoints = constraintsSet.GetPulleyPointsForVertexPair(
                            edge.Index1, edge.Index2, corner1, corner2, false);
                        double distance = constraintsSet.ShapeModel.CalculateDistanceToEdge(
                            pointAsVec,
                            new Circle(constraints1.Corners[corner1], constraints1.MinRadiusInclusive),
                            new Circle(constraints2.Corners[corner2], constraints2.MinRadiusInclusive),
                            pulleyPoints);

                        maxDistanceToCurrentEdge = Math.Max(maxDistanceToCurrentEdge, distance);
                    }
                }

                maxDistanceToEdge = Math.Min(maxDistanceToEdge, maxDistanceToCurrentEdge);
            }

            double toSource = constraintsSet.ShapeModel.CalculateBackgroundPenaltyFromDistance(maxDistanceToEdge);

            return new Tuple<double, double>(toSource, toSink);
        }
    }
}
