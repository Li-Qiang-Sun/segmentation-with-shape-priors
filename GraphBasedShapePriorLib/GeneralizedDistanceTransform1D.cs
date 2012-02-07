using System;

namespace Research.GraphBasedShapePrior
{
    class GeneralizedDistanceTransform1D
    {
        private double[] values;

        private int[] bestIndices;

        public double GridMin { get; private set; }

        public double GridMax { get; private set; }

        public int GridSize { get; private set; }

        private readonly double gridStepSize;

        public bool IsComputed { get; private set; }

        public GeneralizedDistanceTransform1D(double gridMin, double gridMax, int gridSize)
        {
            if (gridMax <= gridMin + 1e-6)
                throw new ArgumentException("gridMax should be greater than gridMin");

            this.GridMin = gridMin;
            this.GridMax = gridMax;
            this.GridSize = gridSize;
            // Our grid covers all the evenly distributed points from min to max in a way that each point is the center of its cell
            this.gridStepSize = (this.GridMax - this.GridMin) / (this.GridSize - 1);
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

        private double GridIndexToCoord(int gridIndex)
        {
            return this.GridMin + gridIndex * this.gridStepSize;
        }

        private int CoordToGridIndex(double coord)
        {
            int gridIndex = (int)((coord - this.GridMin) / this.gridStepSize + 0.5);
            if (gridIndex < 0 || gridIndex >= this.GridSize)
                throw new ArgumentOutOfRangeException("coord");
            return gridIndex;
        }

        public void Compute(double distanceScale, Func<double, double, double> penaltyFunc)
        {
            if (penaltyFunc == null)
                throw new ArgumentNullException("penaltyFunc");
            
            int[] envelope = new int[this.GridSize];
            double[] parabolaRange = new double[this.GridSize + 1];

            double[] functionValues = new double[this.GridSize];
            for (int i = 0; i < this.GridSize; ++i)
                functionValues[i] = penaltyFunc(GridIndexToCoord(i), 0.5 * this.gridStepSize);

            int envelopeSize = 1;
            envelope[0] = 0;
            parabolaRange[0] = Double.NegativeInfinity;
            parabolaRange[1] = Double.PositiveInfinity;
            double intersectionCoeff = 1.0 / (distanceScale * this.gridStepSize * this.gridStepSize);
            for (int i = 1; i < this.GridSize; ++i)
            {
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
            this.values = new double[this.GridSize];
            this.bestIndices = new int[this.GridSize];
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
