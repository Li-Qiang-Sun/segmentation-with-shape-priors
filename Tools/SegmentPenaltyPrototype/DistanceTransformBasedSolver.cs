using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace Research.GraphBasedShapePrior.Tools.SegmentPenaltyPrototype
{
    class DistanceTransformBasedSolver
    {
        private readonly GeneralizedDistanceTransform2D transform;

        private readonly Tuple<Vector, Vector>[,] bestEdges;

        private readonly double[,] bestPenalties;

        public DistanceTransformBasedSolver(Size coordSteps, Vector coordMax)
        {
            this.transform = new GeneralizedDistanceTransform2D(Vector.Zero, coordMax, coordSteps);

            this.bestEdges = new Tuple<Vector, Vector>[coordSteps.Width, coordSteps.Height];
            this.bestPenalties = new double[coordSteps.Width, coordSteps.Height];
        }

        public void Solve(VertexConstraint constraint1, Vector penalty1, VertexConstraint constraint2, Vector penalty2)
        {
            this.PrepareBestEdges(constraint1, penalty1, constraint2, penalty2);
            this.transform.Compute(
                1,
                1,
                (x, y, xRadius, yRadius) =>
                {
                    int xIndex = this.transform.CoordToGridIndexX(x);
                    int yIndex = this.transform.CoordToGridIndexY(y);
                    return bestPenalties[xIndex, yIndex];
                });
        }

        public double GetObjective(Vector point)
        {
            return this.transform.GetValueByCoords(point.X, point.Y);
        }

        public void GetBestEdge(Vector point, out Vector edgePoint1, out Vector edgePoint2)
        {
            Tuple<int, int> bestIndices = this.transform.GetBestIndicesByCoords(point.X, point.Y);
            Tuple<Vector, Vector> bestEdge = bestEdges[bestIndices.Item1, bestIndices.Item2];
            edgePoint1 = bestEdge.Item1;
            edgePoint2 = bestEdge.Item2;
        }

        public Vector GetBestPoint(Vector point)
        {
            Tuple<int, int> bestIndices = this.transform.GetBestIndicesByCoords(point.X, point.Y);
            Vector result = new Vector(
                transform.GridIndexToCoordX(bestIndices.Item1),
                transform.GridIndexToCoordY(bestIndices.Item2));
            return result;
        }

        private void PrepareBestEdges(VertexConstraint constraint1, Vector penalty1, VertexConstraint constraint2, Vector penalty2)
        {
            Polygon convexHull = Polygon.ConvexHull(constraint1.Corners.Concat(constraint2.Corners).ToList());

            for (int xIndex = 0; xIndex < transform.GridSize.Width; ++xIndex)
            {
                for (int yIndex = 0; yIndex < transform.GridSize.Height; ++yIndex)
                {
                    Vector point = new Vector(transform.GridIndexToCoordX(xIndex), transform.GridIndexToCoordY(yIndex));
                    Tuple<Vector, Vector, double> bestSolution = null;

                    if (convexHull.IsPointInside(point))
                    {
                        // Trying every possible corner for each end
                        for (int i = 0; i < 4; ++i)
                        {
                            Tuple<Vector, Vector, double> solution1 = FindBestEdge(
                                point, constraint1.Corners[i], penalty1, constraint2, penalty2);
                            Tuple<Vector, Vector, double> solution2 = FindBestEdge(
                                point, constraint2.Corners[i], penalty2, constraint1, penalty1);
                            if (solution2 != null) // Restore point order
                                solution2 = new Tuple<Vector, Vector, double>(solution2.Item2, solution2.Item1,
                                                                              solution2.Item3);

                            Tuple<Vector, Vector, double> currentBestSolution = solution1;
                            if (solution2 != null && (solution1 == null || solution1.Item3 > solution2.Item3))
                                currentBestSolution = bestSolution;
                            if (currentBestSolution != null &&
                                (bestSolution == null || bestSolution.Item3 > currentBestSolution.Item3))
                                bestSolution = currentBestSolution;
                        }
                    }

                    if (bestSolution == null)
                        this.bestPenalties[xIndex, yIndex] = 1e+20;
                    else
                    {
                        this.bestEdges[xIndex, yIndex] = new Tuple<Vector, Vector>(
                            bestSolution.Item1, bestSolution.Item2);
                        this.bestPenalties[xIndex, yIndex] = bestSolution.Item3;
                    }
                }
            }
        }

        private static Tuple<Vector, Vector, double> FindBestEdge(
            Vector point,
            Vector fixedPoint,
            Vector fixedPointPenalty,
            VertexConstraint freePointRange,
            Vector freePointPenalty)
        {
            Vector bestFreePoint = Vector.Zero;
            double bestFreePointPenalty = Double.PositiveInfinity;
            bool bestFreePointFound = false;

            // Try to use point as the second end
            if (freePointRange.Contains(point))
            {
                bestFreePointFound = true;
                bestFreePoint = point;
                bestFreePointPenalty = Vector.DotProduct(bestFreePoint, freePointPenalty);
            }

            // Try to find intersection with vertex constraint borders
            Vector direction = point - fixedPoint;
            for (int i = 0; i < 4; ++i)
            {
                Vector borderStart = freePointRange.Corners[i];
                Vector borderDirection = freePointRange.Corners[(i + 1) % 4] - freePointRange.Corners[i];
                Tuple<double, double> intersection = MathHelper.LineIntersection(
                    point, direction, borderStart, borderDirection);

                // Skip if no intersection
                const double eps = 1e-6;
                if (intersection == null || intersection.Item1 < -eps || intersection.Item2 < -eps || intersection.Item2 > 1 + eps)
                    continue;

                // Compare with best, swap if necessary
                Vector intersectionPoint = borderStart + borderDirection * intersection.Item2;
                double penalty = Vector.DotProduct(intersectionPoint, freePointPenalty);
                if (penalty < bestFreePointPenalty)
                {
                    bestFreePointFound = true;
                    bestFreePointPenalty = penalty;
                    bestFreePoint = intersectionPoint;
                }
            }

            if (bestFreePointFound)
            {
                double basePenalty = Vector.DotProduct(fixedPoint, fixedPointPenalty);
                return new Tuple<Vector, Vector, double>(fixedPoint, bestFreePoint, bestFreePointPenalty + basePenalty);
            }

            return null;
        }
    }
}
