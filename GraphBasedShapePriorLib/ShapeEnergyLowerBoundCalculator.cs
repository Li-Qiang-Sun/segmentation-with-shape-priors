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

            List<Range> lengthRanges, angleRanges;
            CalculateLengthAngleRanges(shapeConstraints, out lengthRanges, out angleRanges);

            if (shapeConstraints.ShapeModel.PairwiseEdgeConstraintCount == 0)
            {
                double lowerBound = CalculateSingleEdgeLowerBound(shapeConstraints, lengthRanges, angleRanges);
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
                childTransforms.Add(CalculateMinEnergiesForAllParentEdges(shapeConstraints, 0, edgeIndex, lengthRanges, angleRanges));

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
                    currentMinEnergySum = Math.Min(currentMinEnergySum, CalculateMinPairwiseEdgeEnergy(length, angle, childTransforms));
                }

                double unaryEdgeEnergy = CalculateMinUnaryEdgeEnergy(0, shapeConstraints, length);
                minEnergySum = Math.Min(minEnergySum, currentMinEnergySum + unaryEdgeEnergy);
            }

            Debug.Assert(minEnergySum >= 0);
            return minEnergySum;
        }

        private static double CalculateSingleEdgeLowerBound(ShapeConstraints shapeConstraints, List<Range> lengthRanges, List<Range> angleRanges)
        {
            // Shape is forced to be a fully-connected tree, so this |E|=1 is the only case possible
            Debug.Assert(shapeConstraints.ShapeModel.Edges.Count == 1);
            Debug.Assert(lengthRanges.Count == 1);
            Debug.Assert(angleRanges.Count == 1);

            double result;
            
            // Calculate best possible edge width penalty
            EdgeConstraints edgeConstraints = shapeConstraints.EdgeConstraints[0];
            ShapeEdgeParams edgeParams = shapeConstraints.ShapeModel.GetEdgeParams(0);
            Range scaledLengthRange = new Range(
                lengthRanges[0].Left * edgeParams.WidthToEdgeLengthRatio,
                lengthRanges[0].Right * edgeParams.WidthToEdgeLengthRatio);
            Range widthRange = new Range(edgeConstraints.MinWidth, edgeConstraints.MaxWidth);
            if (scaledLengthRange.IntersectsWith(widthRange))
                result = 0;
            else if (scaledLengthRange.Left > widthRange.Right)
                result = MathHelper.Sqr(scaledLengthRange.Left - widthRange.Right);
            else
                result = MathHelper.Sqr(scaledLengthRange.Right - widthRange.Left);

            return result;
        }

        private static void CalculateLengthAngleRanges(ShapeConstraints shapeConstraints, out List<Range> lengthRanges, out List<Range> angleRanges)
        {
            lengthRanges = new List<Range>();
            angleRanges = new List<Range>();

            for (int i = 0; i < shapeConstraints.ShapeModel.Edges.Count; ++i)
            {
                Range lengthRange, angleRange;
                shapeConstraints.DetermineEdgeLimits(i, out lengthRange, out angleRange);
                Debug.Assert(!lengthRange.Outside);

                lengthRanges.Add(lengthRange);
                angleRanges.Add(angleRange);
            }
        }

        private static double CalculateMinPairwiseEdgeEnergy(double length, double angle, IEnumerable<GeneralizedDistanceTransform2D> transforms)
        {
            double energySum = 0;
            foreach (GeneralizedDistanceTransform2D childTransform in transforms)
            {
                if (!childTransform.AreCoordsComputed(length, angle)) // Sometimes required length wasn't computed due to grid misalignment
                    return 1e+20;
                energySum += childTransform.GetValueByCoords(length, angle);
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
            List<Range> lengthRanges,
            List<Range> angleRanges)
        {
            // Calculate child transforms
            List<GeneralizedDistanceTransform2D> childDistanceTransforms = new List<GeneralizedDistanceTransform2D>();
            foreach (int neighborEdgeIndex in shapeConstraints.ShapeModel.IterateNeighboringEdgeIndices(currentEdgeIndex))
            {
                // Iterate only through children
                if (neighborEdgeIndex == parentEdgeIndex)
                    continue;

                GeneralizedDistanceTransform2D childTransform = CalculateMinEnergiesForAllParentEdges(
                    shapeConstraints, currentEdgeIndex, neighborEdgeIndex, lengthRanges, angleRanges);
                Debug.Assert(childTransform.IsComputed);
                childDistanceTransforms.Add(childTransform);
            }

            ShapeEdgePairParams pairParams = shapeConstraints.ShapeModel.GetEdgePairParams(parentEdgeIndex, currentEdgeIndex);
            GeneralizedDistanceTransform2D transform = this.AllocateDistanceTransform();            
            SetupTransformFinitePenaltyRanges(
                transform, pairParams, lengthRanges[currentEdgeIndex], angleRanges[currentEdgeIndex]);
            SetupTransformInterestRanges(
                transform, lengthRanges[parentEdgeIndex], angleRanges[parentEdgeIndex]);

            Func<double, double, double, double, double> penaltyFunction =
                (scaledLength, shiftedAngle, scaledLengthRadius, shiftedAngleRadius) =>
                {
                    double length = scaledLength / pairParams.LengthRatio;
                    double angle = shiftedAngle + pairParams.MeanAngle;
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

        private void SetupTransformInterestRanges(GeneralizedDistanceTransform2D transform, Range lengthRange, Range angleRange)
        {
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

            // Always add all the representations of zero (for simplicity)
            if (angleRange.Contains(0))
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
            GeneralizedDistanceTransform2D transform, ShapeEdgePairParams pairParams, Range lengthRange, Range angleRange)
        {
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
