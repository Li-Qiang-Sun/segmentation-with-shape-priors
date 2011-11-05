using System;
using System.Diagnostics;

namespace Research.GraphBasedShapePrior
{
    class GeneralizedDistanceTransform1D
    {
        private double[] values;

        public double GridMin { get; private set; }

        public double GridMax { get; private set; }

        public int GridSize { get; private set; }

        public double DistanceScale { get; private set; }

        public Func<double, double> PenaltyFunc { get; private set; }

        public GeneralizedDistanceTransform1D(double gridMin, double gridMax, int gridSize, double distanceScale, Func<double, double> penaltyFunc)
        {
            if (gridMax <= gridMin + 1e-6)
                throw new ArgumentException("gridMax should be greater than gridMin");

            this.GridMin = gridMin;
            this.GridMax = gridMax;
            this.DistanceScale = distanceScale;
            this.PenaltyFunc = penaltyFunc;
            this.GridSize = gridSize;

            this.Calculate();
        }

        public double GetByGridIndex(int gridIndex)
        {
            return this.values[gridIndex];
        }

        public double GetByCoord(double coord)
        {
            if (coord < this.GridMin || coord > this.GridMax)
                throw new ArgumentOutOfRangeException("coord");

            double relativeCoord = (coord - this.GridMin) / (this.GridMax - this.GridMin);
            int gridIndex = (int) (relativeCoord * this.GridSize);
            return this.GetByGridIndex(gridIndex);
        }

        private double GridIndexToCoord(int gridIndex)
        {
            return this.GridMin + (double) gridIndex / this.GridSize * (this.GridMax - this.GridMin);
        }

        private void Calculate()
        {
            int[] envelope = new int[this.GridSize];
            double[] parabolaRange = new double[this.GridSize + 1];

            double[] functionValues = new double[this.GridSize];
            for (int i = 0; i < this.GridSize; ++i)
                functionValues[i] = this.PenaltyFunc(GridIndexToCoord(i));

            double gridScale = this.GridSize / (this.GridMax - this.GridMin);

            int envelopeSize = 1;
            envelope[0] = 0;
            parabolaRange[0] = Double.NegativeInfinity;
            parabolaRange[1] = Double.PositiveInfinity;
            for (int i = 1; i < this.GridSize; ++i)
            {
                bool inserted = false;
                while (!inserted)
                {
                    int lastEnvCoord = envelope[envelopeSize - 1];
                    double intersectionPoint = (functionValues[i] - functionValues[lastEnvCoord]) / (this.DistanceScale * gridScale) + i * i - lastEnvCoord * lastEnvCoord;
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
            for (int i = 0; i < this.GridSize; ++i)
            {
                while (parabolaRange[currentParabola + 1] < i)
                    currentParabola += 1;

                double diff = (i - envelope[currentParabola]) / gridScale;
                this.values[i] = functionValues[envelope[currentParabola]] + diff * diff * this.DistanceScale;
            }
        }
    }
}
