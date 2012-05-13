using System;
using System.Collections.Generic;

namespace Research.GraphBasedShapePrior
{
    public class GeneralizedDistanceTransform1D
    {
        private readonly double[] values;

        private readonly int[] bestIndices;

        private readonly int[] envelope;

        private readonly double[] parabolaRange;

        private readonly double[] functionValues;

        private readonly List<Range> finitePenaltyRanges = new List<Range>();

        public GeneralizedDistanceTransform1D(Range range, int gridSize)
        {
            if (range.Outside)
                throw new ArgumentException("Outside ranges are not allowed.", "range");

            this.Range = range;
            this.GridSize = gridSize;
            // Our grid covers all the evenly distributed points from min to max in a way that each point is the center of its cell
            this.gridStepSize = range.Length / (this.GridSize - 1);

            // Allocate all the necessary things
            this.envelope = new int[this.GridSize];
            this.parabolaRange = new double[this.GridSize + 1];
            this.functionValues = new double[this.GridSize];
            this.values = new double[this.GridSize];
            this.bestIndices = new int[this.GridSize];
        }

        public Range Range { get; private set; }

        public int GridSize { get; private set; }

        private readonly double gridStepSize;

        public bool IsComputed { get; private set; }

        public void ResetFinitePenaltyRange()
        {
            this.finitePenaltyRanges.Clear();
        }

        public void AddFinitePenaltyRange(Range range)
        {
            if (range.Outside)
                throw new ArgumentException("Outside ranges are not supported.", "range");
            if (!this.Range.Contains(range.Left) || !this.Range.Contains(range.Right))
                throw new ArgumentException("Given range is outside range of this distance tranform.", "range");
            
            foreach (Range penaltyRange in this.finitePenaltyRanges)
            {
                if (range.IntersectsWith(penaltyRange))
                    throw new ArgumentException("Given range intersects with previously added ranges.", "range");
            }

            this.finitePenaltyRanges.Add(range);
        }

        public IEnumerable<int> EnumerateFinitePenaltyGridIndices()
        {
            if (this.finitePenaltyRanges.Count == 0)
            {
                for (int i = 0; i < this.GridSize; ++i)
                    yield return i;
                yield break;
            }

            int currentRange = 0;
            while (currentRange < this.finitePenaltyRanges.Count)
            {
                int start = this.CoordToGridIndex(this.finitePenaltyRanges[currentRange].Left);
                int end = this.CoordToGridIndex(this.finitePenaltyRanges[currentRange].Right);
                for (int i = start; i <= end; ++i)
                    yield return i;
                ++currentRange;
            }
        }

        public double GetValueByGridIndex(int gridIndex)
        {
            if (!this.IsComputed)
                throw new InvalidOperationException("You should calculate transform first.");

            return this.values[gridIndex];
        }

        public double GetValueByCoord(double coord)
        {
            return this.GetValueByGridIndex(this.CoordToGridIndex(coord));
        }

        public int GetBestIndexByGridIndex(int gridIndex)
        {
            if (!this.IsComputed)
                throw new InvalidOperationException("You should calculate transform first.");
            
            return this.bestIndices[gridIndex];
        }

        public double GetBestIndexByCoord(double coord)
        {
            return this.GetBestIndexByGridIndex(this.CoordToGridIndex(coord));
        }

        public double GridIndexToCoord(int gridIndex)
        {
            if (gridIndex < 0 || gridIndex >= this.GridSize)
                throw new ArgumentOutOfRangeException("gridIndex");
            return this.Range.Left + gridIndex * this.gridStepSize;
        }

        public int CoordToGridIndex(double coord)
        {
            int gridIndex = (int)((coord - this.Range.Left) / this.gridStepSize + 0.5);
            if (gridIndex < 0 || gridIndex >= this.GridSize)
                throw new ArgumentOutOfRangeException("coord");
            return gridIndex;
        }

        public void Compute(double distanceScale, Func<double, double, double> penaltyFunc)
        {
            if (penaltyFunc == null)
                throw new ArgumentNullException("penaltyFunc");

            int left = -1;
            foreach (int i in EnumerateFinitePenaltyGridIndices())
            {
                functionValues[i] = penaltyFunc(GridIndexToCoord(i), 0.5 * this.gridStepSize);
                if (left == -1)
                    left = i;
            }

            int envelopeSize = 1;
            envelope[0] = left;
            parabolaRange[0] = Double.NegativeInfinity;
            parabolaRange[1] = Double.PositiveInfinity;
            double intersectionCoeff = 1.0 / (distanceScale * this.gridStepSize * this.gridStepSize);
            foreach (int i in EnumerateFinitePenaltyGridIndices())
            {
                if (i == left)
                    continue;
                
                bool inserted = false;
                while (!inserted)
                {
                    int lastEnvCoord = envelope[envelopeSize - 1];
                    double intersectionPoint = (functionValues[i] - functionValues[lastEnvCoord]) * intersectionCoeff + i * i - lastEnvCoord * lastEnvCoord;
                    intersectionPoint /= 2 * (i - lastEnvCoord);

                    if (intersectionPoint >= parabolaRange[envelopeSize - 1])
                    {
                        envelopeSize += 1;
                        envelope[envelopeSize - 1] = i;
                        parabolaRange[envelopeSize - 1] = intersectionPoint;
                        parabolaRange[envelopeSize] = Double.PositiveInfinity;
                        inserted = true;
                    }
                    else
                        envelopeSize -= 1;    
                }      
            }

            int currentParabola = 0;
            for (int i = 0; i < this.GridSize; ++i)
            {
                while (parabolaRange[currentParabola + 1] < i)
                    currentParabola += 1;

                double diff = (i - envelope[currentParabola]) * this.gridStepSize;
                this.values[i] = functionValues[envelope[currentParabola]] + diff * diff * distanceScale;
                this.bestIndices[i] = envelope[currentParabola];
            }

            this.IsComputed = true;
        }
    }
}
