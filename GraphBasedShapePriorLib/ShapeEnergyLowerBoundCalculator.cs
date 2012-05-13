using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace Research.GraphBasedShapePrior
{
    public class ShapeEnergyLowerBoundCalculator : IShapeEnergyLowerBoundCalculator
    {
        private readonly LinkedList<GeneralizedDistanceTransform2D> usedDistanceTransforms =
            new LinkedList<GeneralizedDistanceTransform2D>();

        private readonly LinkedList<GeneralizedDistanceTransform2D> freeDistanceTransforms =
            new LinkedList<GeneralizedDistanceTransform2D>();

        private double currentMaxScaledLength;
        
        public ShapeEnergyLowerBoundCalculator(int lengthGridSize, int angleGridSize)
        {
            this.LengthGridSize = lengthGridSize;
            this.AngleGridSize = angleGridSize;
            this.SkipEarlyCalculations = false;
        }

        public int LengthGridSize { get; private set; }

        public int AngleGridSize { get; private set; }

        public bool SkipEarlyCalculations { get; set; }

        public double CalculateLowerBound(Size imageSize, ShapeConstraints shapeConstraints)
        {
            if (shapeConstraints == null)
                throw new ArgumentNullException("shapeConstraints");

            // If constraints have a lot of freedom, bound will be zero anyway. Let's not calculate it.
            if (this.SkipEarlyCalculations && TooEarlyForCalculations(imageSize, shapeConstraints))
                return 0;
            
            this.FreeAllDistanceTransforms();

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
            this.currentMaxScaledLength = maxEdgeLength * maxRatio;

            // Calculate distance transforms for all the child edges
            List<GeneralizedDistanceTransform2D> childTransforms = new List<GeneralizedDistanceTransform2D>();
            foreach (int edgeIndex in shapeConstraints.ShapeModel.IterateNeighboringEdgeIndices(0))
                childTransforms.Add(CalculateMinEnergiesForAllParentEdges(shapeConstraints, 0, edgeIndex));

            // Find best overall solution
            double minEnergySum = Double.PositiveInfinity;
            GeneralizedDistanceTransform2D transform = childTransforms[0];
            for (int lengthGridIndex = transform.CoordToGridIndexX(lengthRanges[0].Left);
                 lengthGridIndex <= transform.CoordToGridIndexX(lengthRanges[0].Right);
                 ++lengthGridIndex)
            {
                double length = transform.GridIndexToCoordX(lengthGridIndex);
                double currentMinEnergySum = Double.PositiveInfinity;

                if (angleRanges[0].Outside)
                {
                    for (int angleGridIndex = transform.CoordToGridIndexY(angleRanges[0].Right);
                         angleGridIndex <= transform.CoordToGridIndexY(Math.PI);
                         ++angleGridIndex)
                    {
                        double angle = transform.GridIndexToCoordY(angleGridIndex);
                        currentMinEnergySum = Math.Min(currentMinEnergySum, CalculateMinPairwiseEdgeEnergy(length, angle, childTransforms));
                    }

                    for (int angleGridIndex = transform.CoordToGridIndexY(-Math.PI);
                         angleGridIndex <= transform.CoordToGridIndexY(angleRanges[0].Left);
                         ++angleGridIndex)
                    {
                        double angle = transform.GridIndexToCoordY(angleGridIndex);
                        currentMinEnergySum = Math.Min(currentMinEnergySum, CalculateMinPairwiseEdgeEnergy(length, angle, childTransforms));
                    }
                }
                else
                {
                    for (int angleGridIndex = transform.CoordToGridIndexY(angleRanges[0].Left);
                         angleGridIndex <= transform.CoordToGridIndexY(angleRanges[0].Right);
                         ++angleGridIndex)
                    {
                        double angle = transform.GridIndexToCoordY(angleGridIndex);
                        currentMinEnergySum = Math.Min(currentMinEnergySum, CalculateMinPairwiseEdgeEnergy(length, angle, childTransforms));
                    }
                }

                double unaryEdgeEnergy = CalculateMinUnaryEdgeEnergy(0, shapeConstraints, length);
                minEnergySum = Math.Min(minEnergySum, currentMinEnergySum + unaryEdgeEnergy);
            }

            Debug.Assert(minEnergySum >= 0);
            return minEnergySum;
        }

        private bool TooEarlyForCalculations(Size imageSize, ShapeConstraints shapeConstraints)
        {
            return false;

            // TODO: think more about conditions here, make it work
            //Vector maxFreedom = new Vector(imageSize.Width * 0.3, imageSize.Height * 0.3);
            //int notTooFreeCount = 0;
            //for (int i = 0; i < shapeConstraints.VertexConstraints.Count; ++i)
            //{
            //    if (shapeConstraints.VertexConstraints[i].XRange.Length <= maxFreedom.X &&
            //        shapeConstraints.VertexConstraints[i].YRange.Length <= maxFreedom.Y)
            //    {
            //        ++notTooFreeCount;
            //    }
            //}

            //const double threshold = 0.8;
            //return (double) notTooFreeCount / shapeConstraints.VertexConstraints.Count < threshold;
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
                double energy = childTransform.GetValueByCoords(length, angle);

                // Check other possible angle representations
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
            if (this.freeDistanceTransforms.Count > 0)
            {
                result = this.freeDistanceTransforms.First.Value;
                this.freeDistanceTransforms.RemoveFirst();
            }
            else
            {
                result = new GeneralizedDistanceTransform2D(
                    new Range(0, this.currentMaxScaledLength), 
                    new Range(-Math.PI * 2, Math.PI * 2), 
                    new Size(this.LengthGridSize, this.AngleGridSize));
            }

            this.usedDistanceTransforms.AddFirst(result);
            return result;
        }

        private void FreeAllDistanceTransforms()
        {
            foreach (GeneralizedDistanceTransform2D transform in this.usedDistanceTransforms)
                this.freeDistanceTransforms.AddFirst(transform);
            foreach (GeneralizedDistanceTransform2D transform in freeDistanceTransforms)
                transform.ResetFinitePenaltyRange();
            this.usedDistanceTransforms.Clear();
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
            int currentEdgeIndex)
        {
            List<GeneralizedDistanceTransform2D> childDistanceTransforms = new List<GeneralizedDistanceTransform2D>();
            foreach (int neighborEdgeIndex in shapeConstraints.ShapeModel.IterateNeighboringEdgeIndices(currentEdgeIndex))
            {
                // Iterate only through children
                if (neighborEdgeIndex == parentEdgeIndex)
                    continue;

                GeneralizedDistanceTransform2D childTransform = CalculateMinEnergiesForAllParentEdges(
                    shapeConstraints, currentEdgeIndex, neighborEdgeIndex);
                Debug.Assert(childTransform.IsComputed);
                childDistanceTransforms.Add(childTransform);
            }

            // Boundaries for possible lengths and angles of the current edge
            Range lengthRange, angleRange;
            shapeConstraints.DetermineEdgeLimits(currentEdgeIndex, out lengthRange, out angleRange);

            // (parent, current) edge pair parameters
            ShapeEdgePairParams pairParams = shapeConstraints.ShapeModel.GetEdgePairParams(parentEdgeIndex, currentEdgeIndex);

            // Create GDT with finite penalties only in the allowed area
            GeneralizedDistanceTransform2D transform = this.AllocateDistanceTransform();
            transform.AddFinitePenaltyRangeX(
                new Range(lengthRange.Left * pairParams.LengthRatio, lengthRange.Right * pairParams.LengthRatio));
            if (!angleRange.Outside)
            {
                transform.AddFinitePenaltyRangeY(
                    new Range(angleRange.Left - pairParams.MeanAngle, angleRange.Right - pairParams.MeanAngle));
            }
            else
            {
                transform.AddFinitePenaltyRangeY(
                    new Range(-Math.PI - pairParams.MeanAngle, angleRange.Left - pairParams.MeanAngle));
                transform.AddFinitePenaltyRangeY(
                    new Range(angleRange.Right - pairParams.MeanAngle, Math.PI - pairParams.MeanAngle));
            }

            Func<double, double, double, double, double> penaltyFunction =
                (scaledLength, shiftedAngle, scaledLengthRadius, shiftedAngleRadius) =>
                {
                    double length = scaledLength / pairParams.LengthRatio;
                    double angle = shiftedAngle + pairParams.MeanAngle;

                    return
                        CalculateMinUnaryEdgeEnergy(currentEdgeIndex, shapeConstraints, length) +
                        CalculateMinPairwiseEdgeEnergy(length, angle, childDistanceTransforms);
                };

            transform.Compute(
                1.0 / MathHelper.Sqr(pairParams.LengthDeviation),
                1.0 / MathHelper.Sqr(pairParams.AngleDeviation),
                penaltyFunction);

            return transform;
        }
    }
}
