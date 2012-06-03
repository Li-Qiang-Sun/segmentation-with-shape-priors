using System;
using System.Collections.Generic;
using System.Linq;

namespace Research.GraphBasedShapePrior
{
    public class GeneralizedDistanceTransform1D
    {
        private readonly double[] values;

        private readonly int[] bestIndices;

        private readonly int[] envelope;

        private readonly double[] parabolaRange;

        private readonly double[] functionValues;

        private readonly int[] timeStamps;

        private readonly List<Range> finitePenaltyRanges = new List<Range>();

        private readonly List<Range> interestRanges = new List<Range>();

        private int currentTimeStamp;

        public GeneralizedDistanceTransform1D(Range range, int gridSize)
        {
            if (range.Outside)
                throw new ArgumentException("Outside ranges are not allowed.", "range");

            this.Range = range;
            this.GridSize = gridSize;
            // Our grid covers all the evenly distributed points from min to max in a way that each point is the center of its cell
            this.GridStepSize = range.Length / (this.GridSize - 1);

            // Allocate all the necessary things
            this.envelope = new int[this.GridSize];
            this.parabolaRange = new double[this.GridSize + 1];
            this.functionValues = new double[this.GridSize];
            this.values = new double[this.GridSize];
            this.bestIndices = new int[this.GridSize];
            this.timeStamps = new int[this.GridSize];
        }

        public Range Range { get; private set; }

        public int GridSize { get; private set; }

        public double GridStepSize { get; private set; }

        public bool IsComputed { get; private set; }

        public void ResetFinitePenaltyRange()
        {
            this.finitePenaltyRanges.Clear();
        }

        public void AddFinitePenaltyRange(Range range)
        {
            AddRange(range, this.finitePenaltyRanges);
        }

        public IEnumerable<int> EnumerateFinitePenaltyGridIndices()
        {
            return EnumerateRangeIndices(this.finitePenaltyRanges);
        }

        public bool IsFinitePenaltyCoord(double coord)
        {
            return this.IsCoordInRange(coord, this.finitePenaltyRanges);
        }

        public void ResetInterestRange()
        {
            this.interestRanges.Clear();
        }

        public void AddInterestRange(Range range)
        {
            AddRange(range, this.interestRanges);
        }

        public IEnumerable<int> EnumerateInterestGridIndices()
        {
            return EnumerateRangeIndices(this.interestRanges);
        }

        public bool IsCoordOfInterest(double coord)
        {
            return this.IsCoordInRange(coord, this.interestRanges);
        }

        public bool IsGridIndexComputed(int gridIndex)
        {
            return this.timeStamps[gridIndex] == this.currentTimeStamp;
        }

        public bool IsCoordComputed(double coord)
        {
            return IsGridIndexComputed(CoordToGridIndex(coord));
        }

        public double GetValueByGridIndex(int gridIndex)
        {
            if (!this.IsComputed)
                throw new InvalidOperationException("You should calculate transform first.");
            if (!this.IsGridIndexComputed(gridIndex))
                throw new ArgumentException("Given coord was out of interest during last computation.");

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
            return this.Range.Left + gridIndex * this.GridStepSize;
        }

        public int CoordToGridIndex(double coord)
        {
            int gridIndex = (int)((coord - this.Range.Left) / this.GridStepSize + 0.5);
            if (gridIndex < 0 || gridIndex >= this.GridSize)
                throw new ArgumentOutOfRangeException("coord");
            return gridIndex;
        }

        public void Compute(double distanceScale, Func<double, double> penaltyFunc)
        {
            if (penaltyFunc == null)
                throw new ArgumentNullException("penaltyFunc");

            ++this.currentTimeStamp;

            // Sort ranges by left end
            if (!this.TryEstablishRangeOrdering(this.finitePenaltyRanges))
                throw new InvalidOperationException("Given finite penalty ranges ovelap.");
            if (!this.TryEstablishRangeOrdering(this.interestRanges))
                throw new InvalidOperationException("Given interest ranges ovelap.");

            // Calculate penalty (where it's finite)
            int left = -1;
            foreach (int i in EnumerateFinitePenaltyGridIndices())
            {
                functionValues[i] = penaltyFunc(GridIndexToCoord(i));
                if (left == -1)
                    left = i;
            }

            // Find lower envelope
            int envelopeSize = 1;
            envelope[0] = left;
            parabolaRange[0] = Double.NegativeInfinity;
            parabolaRange[1] = Double.PositiveInfinity;
            double intersectionCoeff = 1.0 / (distanceScale * this.GridStepSize * this.GridStepSize);
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

            // Find parabola from envelope for each index of interest
            int currentParabola = 0;
            foreach (int i in this.EnumerateInterestGridIndices())
            {
                while (parabolaRange[currentParabola + 1] < i)
                    currentParabola += 1;

                double diff = (i - envelope[currentParabola]) * this.GridStepSize;
                this.values[i] = functionValues[envelope[currentParabola]] + diff * diff * distanceScale;
                this.bestIndices[i] = envelope[currentParabola];
                this.timeStamps[i] = currentTimeStamp;
            }

            this.IsComputed = true;
        }

        private void AddRange(Range range, IList<Range> rangeCollection)
        {
            if (range.Outside)
                throw new ArgumentException("Outside ranges are not supported.", "range");
            if (!this.Range.Contains(range.Left) || !this.Range.Contains(range.Right))
                throw new ArgumentException("Given range is outside range of this distance tranform.", "range");

            rangeCollection.Add(range);
            this.IsComputed = false;
        }

        private bool TryEstablishRangeOrdering(List<Range> rangeCollection)
        {
            rangeCollection.Sort((r1, r2) => Comparer<double>.Default.Compare(r1.Left, r2.Left));
            const double eps = 1e-10;
            for (int i = 1; i < rangeCollection.Count; ++i)
            {
                if (rangeCollection[i - 1].Right > rangeCollection[i].Left + eps)
                    return false;
            }

            return true;
        }

        private IEnumerable<int> EnumerateRangeIndices(IList<Range> rangeCollection)
        {
            if (rangeCollection.Count == 0)
            {
                for (int i = 0; i < this.GridSize; ++i)
                    yield return i;
                yield break;
            }

            int currentRange = 0;
            int prevIndex = -1;
            while (currentRange < rangeCollection.Count)
            {
                int start = this.CoordToGridIndex(rangeCollection[currentRange].Left);
                int end = this.CoordToGridIndex(rangeCollection[currentRange].Right);
                for (int i = start; i <= end; ++i)
                {
                    if (i != prevIndex) // In case two ranges almost overlap
                        yield return i;
                    prevIndex = i;
                }
                ++currentRange;
            }
        }

        private bool IsCoordInRange(double coord, IEnumerable<Range> rangeCollection)
        {
            return rangeCollection.Any(r => r.Contains(coord));
        }
    }
}
