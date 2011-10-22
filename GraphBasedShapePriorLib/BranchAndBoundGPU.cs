using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TidePowerd.DeviceMethods;
using TidePowerd.DeviceMethods.Vectors;

namespace Research.GraphBasedShapePrior
{
    public static class BranchAndBoundGPU
    {
        [Function]
        public static bool PointInConvexHull(Int16Vector2 point, Int16Vector2[] convexHullPoints)
        {
            // Assumes that points are in clockwise order
            bool inside = true;
            for (int i = 0; i < convexHullPoints.Length; ++i)
            {
                Int16Vector2 diff1 = GPUMathHelper.VectorSub(
                    point, convexHullPoints[i]);
                Int16Vector2 diff2 = GPUMathHelper.VectorSub(
                    convexHullPoints[(i + 1) % convexHullPoints.Length], convexHullPoints[i]);
                inside &= GPUMathHelper.CrossProduct(diff1, diff2) >= 0;
            }

            return inside;
        }

        [Function]
        private static float DistToCircleOuter(Int16Vector2 point, Int16Vector2 center, short radius)
        {
            return DeviceMath.Max(
                GPUMathHelper.Length(GPUMathHelper.VectorSub(point, center)) - radius,
                0);
        }

        [Function]
        public static float DistanceToPulley(
            Int16Vector2 point,
            Int16Vector2 point1,
            short radius1,
            Int16Vector2 point2,
            short radius2)
        {
            // First circle should always be bigger
            if (radius1 < radius2)
            {
                short tempRadius = radius1;
                radius1 = radius2;
                radius2 = tempRadius;
                Int16Vector2 tempPoint = point1;
                point1 = point2;
                point2 = tempPoint;
            }

            // Singular pulley
            if (GPUMathHelper.CircleInCircle(point1, radius1, point2, radius2))
                return DistToCircleOuter(point, point1, radius1);

            // Dist to circles
            float distance = DistToCircleOuter(point, point1, radius1);
            distance = DeviceMath.Min(distance, DistToCircleOuter(point, point2, radius2));
            if (distance == 0)
                return 0;

            float edgeLength = GPUMathHelper.Length(GPUMathHelper.VectorSub(point1, point2));
            float cosAngle = (radius1 - radius2) / edgeLength;
            float angle = DeviceMath.Acos(cosAngle);
            float lineAngle = DeviceMath.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            float cosPlusPlus = DeviceMath.Cos(lineAngle + angle);
            float sinPlusPlus = DeviceMath.Sin(lineAngle + angle);
            float cosPlusMinus = DeviceMath.Cos(lineAngle - angle);
            float sinPlusMinus = DeviceMath.Sin(lineAngle - angle);

            // Find pulley points
            SingleVector2 line1Point1 = new SingleVector2(
                point1.X + radius1 * cosPlusPlus,
                point1.Y + radius1 * sinPlusPlus);
            SingleVector2 line2Point1 = new SingleVector2(
                point1.X + radius1 * cosPlusMinus,
                point1.Y + radius1 * sinPlusMinus);
            SingleVector2 line1Point2 = new SingleVector2(
                point2.X + radius2 * cosPlusPlus,
                point2.Y + radius2 * sinPlusPlus);
            SingleVector2 line2Point2 = new SingleVector2(
                point2.X + radius2 * cosPlusMinus,
                point2.Y + radius2 * sinPlusMinus);

            // Check if point is inside pulley
            SingleVector2 pointSingle = new SingleVector2(point.X, point.Y);
            if (GPUMathHelper.CrossProduct(GPUMathHelper.VectorSub(pointSingle, line1Point1), GPUMathHelper.VectorSub(line2Point1, line1Point1)) >= 0 &&
                GPUMathHelper.CrossProduct(GPUMathHelper.VectorSub(pointSingle, line2Point1), GPUMathHelper.VectorSub(line2Point2, line2Point1)) >= 0 &&
                GPUMathHelper.CrossProduct(GPUMathHelper.VectorSub(pointSingle, line2Point2), GPUMathHelper.VectorSub(line1Point2, line2Point2)) >= 0 &&
                GPUMathHelper.CrossProduct(GPUMathHelper.VectorSub(pointSingle, line1Point2), GPUMathHelper.VectorSub(line1Point1, line1Point2)) >= 0)
            {
                return 0;
            }

            SingleVector2 pointAsSingle = GPUMathHelper.ToSingle2(point);
            distance = DeviceMath.Min(
                distance, GPUMathHelper.DistanceToSegment(pointAsSingle, line1Point1, line1Point2));
            distance = DeviceMath.Min(
                distance, GPUMathHelper.DistanceToSegment(pointAsSingle, line2Point1, line2Point2));
            return distance;
        }

        [Function]
        private static float LogInf(float value)
        {
            const float threshold = 1e-15f;
            if (value < threshold)
                return DeviceMath.Log(threshold);
            return DeviceMath.Log(value);
        }

        [Function]
        private static float DistanceToPenalty(float distance, float cutoff)
        {
            return -distance * cutoff * cutoff;
        }

        [Function]
        private static Int16Vector2 GetClosestPoint(Int16Vector2 point, Int16Vector2[] corners)
        {
            // Clockwise order from bottom left (min) corner assumed
            Int16Vector2 min = corners[0];
            Int16Vector2 max = corners[2];
            
            if (point.X >= min.X && point.X <= max.X)
            {
                if (point.Y <= min.Y)
                    return new Int16Vector2(point.X, min.Y);
                if (point.Y >= max.Y)
                    return new Int16Vector2(point.X, max.Y);
            }

            if (point.Y >= min.Y && point.Y <= max.Y)
            {
                if (point.X <= min.X)
                    return new Int16Vector2(min.X, point.Y);
                if (point.X >= max.X)
                    return new Int16Vector2(max.X, point.Y);
            }

            // Just because we have to return some point
            return min;
        }

        [Kernel]
        private static void CalcMinObjectPenaltyKernel(
            Int16Vector2 imageSize,
            Int16Vector2[] convexHull,
            Int16Vector2[] corners1,
            Int16Vector2[] corners2,
            Int16Vector2 maxRadii,
            float distanceCutoff,
            float[] minObjectPenalties)
        {
            Int16Vector2 point;
            point.X = (short)((BlockIndex.X * BlockDimension.X) + ThreadIndex.X);
            point.Y = (short)((BlockIndex.Y * BlockDimension.Y) + ThreadIndex.Y);
            if (point.X >= imageSize.X || point.Y >= imageSize.Y)
                return;
            int index = point.X + point.Y * imageSize.X;

            float distance = Single.MaxValue;
            if (PointInConvexHull(point, convexHull))
            {
                minObjectPenalties[index] = 0;
            }
            else
            {
                for (int corner1 = 0; corner1 < 4; ++corner1)
                    for (int corner2 = 0; corner2 < 4; ++corner2)
                    {
                        float distanceToEdge = DistanceToPulley(
                            point,
                            corners1[corner1],
                            maxRadii.X,
                            corners2[corner2],
                            maxRadii.Y);
                        distance = DeviceMath.Min(distance, distanceToEdge);
                    }

                Int16Vector2 closestPoint1 = GetClosestPoint(point, corners1);
                Int16Vector2 closestPoint2 = GetClosestPoint(point, corners2);

                for (int corner = 0; corner < 4; ++corner)
                {
                    float distanceToEdge1 = DistanceToPulley(
                        point,
                        closestPoint1,
                        maxRadii.X,
                        corners2[corner],
                        maxRadii.Y);
                    distance = DeviceMath.Min(distance, distanceToEdge1);

                    float distanceToEdge2 = DistanceToPulley(
                        point,
                        corners1[corner],
                        maxRadii.X,
                        closestPoint2,
                        maxRadii.Y);
                    distance = DeviceMath.Min(distance, distanceToEdge2);
                }

                float distanceBetweenClosestPoints = DistanceToPulley(
                        point,
                        closestPoint1,
                        maxRadii.X,
                        closestPoint2,
                        maxRadii.Y);
                distance = DeviceMath.Min(distance, distanceBetweenClosestPoints);
            }

            float penalty = DistanceToPenalty(distance, distanceCutoff);
            minObjectPenalties[index] = DeviceMath.Min(minObjectPenalties[index], penalty);
        }

        private static Int16Vector2[] ConvertToInt16Vector2Array(IEnumerable<Vector> points)
        {
            return points.Select(p => new Int16Vector2((short)Math.Round(p.X), (short)Math.Round(p.Y))).ToArray();
        }

        public static void CalculateShapeUnaryTerms(
            ShapeConstraintsSet constraints,
            Image2D<Tuple<double, double>> result)
        {
            float[] storage = new float[result.Width * result.Height];

            // Cleanup storage
            for (int i = 0; i < storage.Length; ++i)
                storage[i] = Single.PositiveInfinity;

            // Calculate min object penalty
            foreach (ShapeEdge edge in constraints.ShapeModel.Edges)
            {
                VertexConstraints vertexConstraints1 = constraints.GetConstraintsForVertex(edge.Index1);
                VertexConstraints vertexConstraints2 = constraints.GetConstraintsForVertex(edge.Index2);
                Polygon convexHull = constraints.GetConvexHullForVertexPair(edge.Index1, edge.Index2);
                Int16Vector2[] convexHullPoints = ConvertToInt16Vector2Array(convexHull.Vertices);
                Int16Vector2[] corners1 = ConvertToInt16Vector2Array(vertexConstraints1.Corners);
                Int16Vector2[] corners2 = ConvertToInt16Vector2Array(vertexConstraints2.Corners);

                // Prepare GPU grid
                const int blockSize = 16;
                Launcher.SetBlockSize(blockSize, blockSize);
                Launcher.SetGridSize(
                    result.Width / blockSize + result.Width % blockSize == 0 ? 0 : 1,
                    result.Height / blockSize + result.Height % blockSize == 0 ? 0 : 1);

                // Run kernel to update penalty storage
                CalcMinObjectPenaltyKernel(
                    new Int16Vector2((short)result.Width, (short)result.Height),
                    convexHullPoints,
                    corners1,
                    corners2,
                    new Int16Vector2((short)vertexConstraints1.MaxRadiusExclusive, (short)vertexConstraints2.MaxRadiusExclusive),
                    (float)constraints.ShapeModel.Cutoff,
                    storage);
            }

            // Copy object penalties
            for (int x = 0; x < result.Width; ++x)
                for (int y = 0; y < result.Height; ++y)
                    result[x, y] = new Tuple<double, double>(0, storage[x + y * result.Width]);

            // Clear stored convex hulls
            constraints.ClearCaches();
        }
    }
}
