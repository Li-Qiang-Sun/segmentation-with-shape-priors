using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace Research.GraphBasedShapePrior
{
    public class ShapeEnergyLowerBoundCalculator : IShapeEnergyLowerBoundCalculator
    {
        private readonly List<GeneralizedDistanceTransform2D> transformPool =
            new List<GeneralizedDistanceTransform2D>();

        private int firstFreeTranform;

        private double currentMaxScaledLength = Double.NegativeInfinity;

        public ShapeEnergyLowerBoundCalculator(int lengthGridSize, int angleGridSize)
        {
            this.LengthGridSize = lengthGridSize;
            this.AngleGridSize = angleGridSize;
        }

        public int LengthGridSize { get; private set; }

        public int AngleGridSize { get; private set; }

        public double CalculateLowerBound(Size imageSize, ShapeConstraints shapeConstraints)
        {
            if (shapeConstraints == null)
                throw new ArgumentNullException("shapeConstraints");

            List<ILengthAngleConstraints> lengthAngleConstraints = CalculateLengthAngleConstraints(shapeConstraints);
            if (shapeConstraints.ShapeModel.PairwiseEdgeConstraintCount == 0)
            {
                double lowerBound = CalculateSingleEdgeLowerBound(shapeConstraints, lengthAngleConstraints);
                Debug.Assert(lowerBound >= 0);
                return lowerBound;
            }

            // Determine max (scaled) length possible
            double maxRatio1 = (from edgePair in shapeConstraints.ShapeModel.ConstrainedEdgePairs
                                select shapeConstraints.ShapeModel.GetEdgePairParams(edgePair.Item1, edgePair.Item2).LengthRatio).Max();
            double maxRatio2 = (from edgePair in shapeConstraints.ShapeModel.ConstrainedEdgePairs
                                select 1.0 / shapeConstraints.ShapeModel.GetEdgePairParams(edgePair.Item1, edgePair.Item2).LengthRatio).Max();
            double maxRatio = Math.Max(maxRatio1, maxRatio2);
            double maxEdgeLength = (new Vector(imageSize.Width, imageSize.Height)).Length;
            double maxScaledLength = maxEdgeLength * maxRatio;

            if (maxScaledLength != this.currentMaxScaledLength)
            {
                this.currentMaxScaledLength = maxScaledLength;
                this.transformPool.Clear();
            }

            this.FreeAllDistanceTransforms();

            // Calculate distance transforms for all the child edges
            List<GeneralizedDistanceTransform2D> childTransforms = new List<GeneralizedDistanceTransform2D>();
            foreach (int edgeIndex in shapeConstraints.ShapeModel.IterateNeighboringEdgeIndices(0))
                childTransforms.Add(CalculateMinEnergiesForAllParentEdges(shapeConstraints, 0, edgeIndex, lengthAngleConstraints));

            // Find best overall solution
            double minEnergySum = Double.PositiveInfinity;
            GeneralizedDistanceTransform2D transform = childTransforms[0];
            foreach (int lengthGridIndex in transform.EnumerateInterestGridIndicesX())
            {
                double length = transform.GridIndexToCoordX(lengthGridIndex);
                double currentMinEnergySum = Double.PositiveInfinity;

                foreach (int angleGridIndex in transform.EnumerateInterestGridIndicesY())
                {
                    double angle = transform.GridIndexToCoordY(angleGridIndex);
                    const double eps = 1e-8;
                    if (angle > Math.PI + eps || angle < -Math.PI - eps)
                        continue;   // Consider only natural angle representations here

                    currentMinEnergySum = Math.Min(currentMinEnergySum, CalculateMinPairwiseEdgeEnergy(length, angle, childTransforms));
                }

                double unaryEdgeEnergy = CalculateMinUnaryEdgeEnergy(0, shapeConstraints, length);
                minEnergySum = Math.Min(minEnergySum, currentMinEnergySum + unaryEdgeEnergy);
            }

            Debug.Assert(minEnergySum >= 0);
            return minEnergySum;
        }

        private static double CalculateSingleEdgeLowerBound(ShapeConstraints shapeConstraints, IList<ILengthAngleConstraints> lengthAngleConstraints)
        {
            // Shape is forced to be a fully-connected tree, so this |E|=1 is the only case possible
            Debug.Assert(shapeConstraints.ShapeModel.Edges.Count == 1);
            Debug.Assert(lengthAngleConstraints.Count == 1);

            double result;

            // Calculate best possible edge width penalty
            EdgeConstraints edgeConstraints = shapeConstraints.EdgeConstraints[0];
            ShapeEdgeParams edgeParams = shapeConstraints.ShapeModel.GetEdgeParams(0);
            Range scaledLengthRange = new Range(
                lengthAngleConstraints[0].LengthBoundary.Left * edgeParams.WidthToEdgeLengthRatio,
                lengthAngleConstraints[0].LengthBoundary.Right * edgeParams.WidthToEdgeLengthRatio);
            Range widthRange = new Range(edgeConstraints.MinWidth, edgeConstraints.MaxWidth);
            if (scaledLengthRange.IntersectsWith(widthRange))
                result = 0;
            else if (scaledLengthRange.Left > widthRange.Right)
                result = MathHelper.Sqr(scaledLengthRange.Left - widthRange.Right);
            else
                result = MathHelper.Sqr(scaledLengthRange.Right - widthRange.Left);

            return result;
        }

        private static List<ILengthAngleConstraints> CalculateLengthAngleConstraints(ShapeConstraints shapeConstraints)
        {
            List<ILengthAngleConstraints> result = new List<ILengthAngleConstraints>();

            for (int i = 0; i < shapeConstraints.ShapeModel.Edges.Count; ++i)
            {
                ShapeEdge edge = shapeConstraints.ShapeModel.Edges[i];
                VertexConstraints vertex1Constraints = shapeConstraints.VertexConstraints[edge.Index1];
                VertexConstraints vertex2Constraints = shapeConstraints.VertexConstraints[edge.Index2];
                //result.Add(BoxLengthAngleConstraints.FromVertexConstraints(vertex1Constraints, vertex2Constraints));
                result.Add(BoxSetLengthAngleConstraints.FromVertexConstraints(vertex1Constraints, vertex2Constraints, 1, 16));
            }

            return result;
        }

        private static double CalculateMinPairwiseEdgeEnergy(
            double length,
            double angle,
            IEnumerable<GeneralizedDistanceTransform2D> transforms)
        {
            double energySum = 0;
            foreach (GeneralizedDistanceTransform2D childTransform in transforms)
            {
                if (!childTransform.AreCoordsComputed(length, angle)) // Sometimes required length wasn't computed due to grid misalignment
                    return 1e+20;

                double energy = childTransform.GetValueByCoords(length, angle);

                // Try other angle representations
                bool angleIsZero = childTransform.CoordToGridIndexY(angle) == childTransform.CoordToGridIndexY(0);
                if (angleIsZero)
                {
                    // Zero has two representations
                    energy = Math.Min(childTransform.GetValueByCoords(length, -Math.PI * 2), energy);
                    energy = Math.Min(childTransform.GetValueByCoords(length, Math.PI * 2), energy);
                }
                else
                {
                    // Other angles have single representation
                    double otherAngle = angle > 0 ? angle - Math.PI * 2 : angle + Math.PI * 2;
                    energy = Math.Min(childTransform.GetValueByCoords(length, otherAngle), energy);
                }

                energySum += energy;
            }

            return energySum;
        }

        private GeneralizedDistanceTransform2D AllocateDistanceTransform()
        {
            GeneralizedDistanceTransform2D result;
            if (this.firstFreeTranform < this.transformPool.Count)
            {
                result = this.transformPool[this.firstFreeTranform++];
                result.ResetFinitePenaltyRange();
                result.ResetInterestRange();
            }
            else
            {
                result = new GeneralizedDistanceTransform2D(
                    new Range(0, this.currentMaxScaledLength),
                    new Range(-Math.PI * 2, Math.PI * 2),
                    new Size(this.LengthGridSize, this.AngleGridSize));
                this.firstFreeTranform++;
                this.transformPool.Add(result);
            }

            return result;
        }

        private void FreeAllDistanceTransforms()
        {
            this.firstFreeTranform = 0;
        }

        private static double CalculateMinUnaryEdgeEnergy(int edgeIndex, ShapeConstraints shapeConstraints, double edgeLength)
        {
            double bestWidth = edgeLength * shapeConstraints.ShapeModel.GetEdgeParams(edgeIndex).WidthToEdgeLengthRatio;
            EdgeConstraints edgeConstraints = shapeConstraints.EdgeConstraints[edgeIndex];
            bestWidth = MathHelper.Trunc(bestWidth, edgeConstraints.MinWidth, edgeConstraints.MaxWidth);
            return shapeConstraints.ShapeModel.CalculateEdgeWidthEnergyTerm(edgeIndex, bestWidth, edgeLength);
        }

        private GeneralizedDistanceTransform2D CalculateMinEnergiesForAllParentEdges(
            ShapeConstraints shapeConstraints,
            int parentEdgeIndex,
            int currentEdgeIndex,
            IList<ILengthAngleConstraints> lengthAngleConstraints)
        {
            // Calculate child transforms
            List<GeneralizedDistanceTransform2D> childDistanceTransforms = new List<GeneralizedDistanceTransform2D>();
            foreach (int neighborEdgeIndex in shapeConstraints.ShapeModel.IterateNeighboringEdgeIndices(currentEdgeIndex))
            {
                // Iterate only through children
                if (neighborEdgeIndex == parentEdgeIndex)
                    continue;

                GeneralizedDistanceTransform2D childTransform = CalculateMinEnergiesForAllParentEdges(
                    shapeConstraints, currentEdgeIndex, neighborEdgeIndex, lengthAngleConstraints);
                Debug.Assert(childTransform.IsComputed);
                childDistanceTransforms.Add(childTransform);
            }

            ShapeEdgePairParams pairParams = shapeConstraints.ShapeModel.GetEdgePairParams(parentEdgeIndex, currentEdgeIndex);
            GeneralizedDistanceTransform2D transform = this.AllocateDistanceTransform();
            SetupTransformFinitePenaltyRanges(transform, pairParams, lengthAngleConstraints[currentEdgeIndex]);
            SetupTransformInterestRanges(transform, lengthAngleConstraints[parentEdgeIndex]);

            Func<double, double, double> penaltyFunction =
                (scaledLength, shiftedAngle) =>
                {
                    double length = scaledLength / pairParams.LengthRatio;
                    double angle = shiftedAngle + pairParams.MeanAngle;
                    
                    double lengthTolerance = transform.GridStepSizeX / pairParams.LengthRatio;
                    double angleTolerance = transform.GridStepSizeY;
                    if (!lengthAngleConstraints[currentEdgeIndex].InRange(length, lengthTolerance, angle, angleTolerance))
                        return 1e+20;

                    double penalty =
                        CalculateMinUnaryEdgeEnergy(currentEdgeIndex, shapeConstraints, length) +
                        CalculateMinPairwiseEdgeEnergy(length, angle, childDistanceTransforms);
                    return penalty;
                };

            transform.Compute(
                1.0 / MathHelper.Sqr(pairParams.LengthDeviation),
                1.0 / MathHelper.Sqr(pairParams.AngleDeviation),
                penaltyFunction);

            return transform;
        }

        private void SetupTransformInterestRanges(GeneralizedDistanceTransform2D transform, ILengthAngleConstraints lengthAngleConstraints)
        {
            Range lengthRange = lengthAngleConstraints.LengthBoundary;
            Range angleRange = lengthAngleConstraints.AngleBoundary;
            
            transform.AddInterestRangeX(lengthRange);

            if (!angleRange.Outside)
            {
                transform.AddInterestRangeY(angleRange);

                if (angleRange.Right > 0)
                {
                    if (angleRange.Left > 0)
                        transform.AddInterestRangeY(new Range(angleRange.Left - Math.PI * 2, angleRange.Right - Math.PI * 2));
                    else
                    {
                        transform.AddInterestRangeY(new Range(-Math.PI * 2, angleRange.Right - Math.PI * 2));
                        transform.AddInterestRangeY(new Range(angleRange.Left + Math.PI * 2, Math.PI * 2));
                    }
                }
                else
                    transform.AddInterestRangeY(new Range(angleRange.Left + Math.PI * 2, angleRange.Right + Math.PI * 2));
            }
            else
            {
                transform.AddInterestRangeY(new Range(-Math.PI, angleRange.Left));
                transform.AddInterestRangeY(new Range(angleRange.Right, Math.PI));

                if (angleRange.Right > 0)
                    transform.AddInterestRangeY(new Range(angleRange.Right - Math.PI * 2, -Math.PI));
                else
                {
                    transform.AddInterestRangeY(new Range(-Math.PI * 2, -Math.PI));
                    transform.AddInterestRangeY(new Range(angleRange.Right + Math.PI * 2, Math.PI * 2));
                }

                if (angleRange.Left < 0)
                    transform.AddInterestRangeY(new Range(Math.PI, angleRange.Left + Math.PI * 2));
                else
                {
                    transform.AddInterestRangeY(new Range(Math.PI, Math.PI * 2));
                    transform.AddInterestRangeY(new Range(-Math.PI * 2, angleRange.Left - Math.PI * 2));
                }
            }

            // Add all the representations of zero angle
            if (angleRange.IntersectsWith(new Range(-transform.GridStepSizeY * 0.5, transform.GridStepSizeY * 0.5)))
            {
                if (!transform.IsCoordYOfInterest(-Math.PI * 2))
                    transform.AddInterestRangeY(new Range(-Math.PI * 2, -Math.PI * 2));
                if (!transform.IsCoordYOfInterest(0))
                    transform.AddInterestRangeY(new Range(0, 0));
                if (!transform.IsCoordYOfInterest(Math.PI * 2))
                    transform.AddInterestRangeY(new Range(Math.PI * 2, Math.PI * 2));    
            }
        }

        private void SetupTransformFinitePenaltyRanges(
            GeneralizedDistanceTransform2D transform, ShapeEdgePairParams pairParams, ILengthAngleConstraints lengthAngleConstraints)
        {
            Range lengthRange = lengthAngleConstraints.LengthBoundary;
            Range angleRange = lengthAngleConstraints.AngleBoundary;
            
            transform.AddFinitePenaltyRangeX(
                new Range(lengthRange.Left * pairParams.LengthRatio, lengthRange.Right * pairParams.LengthRatio));

            if (!angleRange.Outside)
                transform.AddFinitePenaltyRangeY(new Range(angleRange.Left - pairParams.MeanAngle, angleRange.Right - pairParams.MeanAngle));
            else
            {
                transform.AddFinitePenaltyRangeY(new Range(-Math.PI - pairParams.MeanAngle, angleRange.Left - pairParams.MeanAngle));
                transform.AddFinitePenaltyRangeY(new Range(angleRange.Right - pairParams.MeanAngle, Math.PI - pairParams.MeanAngle));
            }
        }
    }
}
