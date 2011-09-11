using System;
using System.Diagnostics;

namespace Research.GraphBasedShapePrior
{
    class GeneralizedDistanceTransform1D
    {
        private double[] values;

        public int GridMinInclusive { get; private set; }

        public int GridMaxExclusive { get; private set; }

        public double DistanceScale { get; private set; }

        public Func<int, double> PenaltyFunc { get; private set; }

        public GeneralizedDistanceTransform1D(int gridMinInclusive, int gridMaxExclusive, double distanceScale, Func<int, double> penaltyFunc)
        {
            Debug.Assert(gridMinInclusive < gridMaxExclusive);

            GridMinInclusive = gridMinInclusive;
            GridMaxExclusive = gridMaxExclusive;
            DistanceScale = distanceScale;
            PenaltyFunc = penaltyFunc;

            this.Calculate();
        }

        public double this[int index]
        {
            get { return this.values[index - this.GridMinInclusive]; }
        }

        private void Calculate()
        {
            int length = this.GridMaxExclusive - this.GridMinInclusive;
            int[] envelope = new int[length];
            double[] parabolaRange = new double[length + 1];

            double[] functionValues = new double[length];
            for (int i = 0; i < length; ++i)
                functionValues[i] = this.PenaltyFunc(i + this.GridMinInclusive);

            int envelopeSize = 1;
            envelope[0] = 0;
            parabolaRange[0] = Double.NegativeInfinity;
            parabolaRange[1] = Double.PositiveInfinity;
            for (int i = 1; i < length; ++i)
            {
                bool inserted = false;
                while (!inserted)
                {
                    int lastEnvCoord = envelope[envelopeSize - 1];
                    double intersectionPoint = (functionValues[i] - functionValues[lastEnvCoord]) / this.DistanceScale + i * i - lastEnvCoord * lastEnvCoord;
                    intersectionPoint /= 2 * (i - lastEnvCoord);

                    if (intersectionPoint > parabolaRange[envelopeSize - 1])
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
            this.values = new double[length];
            for (int i = this.GridMinInclusive; i < this.GridMaxExclusive; ++i)
            {
                while (parabolaRange[currentParabola + 1] < i)
                    currentParabola += 1;

                int diff = i - envelope[currentParabola];
                this.values[i] = functionValues[envelope[currentParabola]] + diff * diff;
            }
        }
    }
}
