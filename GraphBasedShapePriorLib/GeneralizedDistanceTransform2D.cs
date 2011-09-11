using System;
using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class GeneralizedDistanceTransform2D
    {
        public Point GridMinInclusive { get; private set; }

        public Point GridMaxExclusive { get; private set; }

        public double DistanceScaleX { get; private set; }

        public double DistanceScaleY { get; private set; }

        public Func<Point, double> PenaltyFunc { get; private set; }

        private double[,] values;

        public GeneralizedDistanceTransform2D(
            Point gridMinInclusive,
            Point gridMaxExclusive,
            double distanceScaleX,
            double distanceScaleY,
            Func<Point, double> penaltyFunc)
        {
            GridMinInclusive = gridMinInclusive;
            GridMaxExclusive = gridMaxExclusive;
            DistanceScaleX = distanceScaleX;
            DistanceScaleY = distanceScaleY;
            PenaltyFunc = penaltyFunc;

            this.Calculate();
        }

        public double this[int x, int y]
        {
            get { return this.values[x, y]; }
        }

        private void Calculate()
        {
            GeneralizedDistanceTransform1D[] distanseTransformsForFixedY = new GeneralizedDistanceTransform1D[this.GridMaxExclusive.Y - this.GridMinInclusive.Y];
            for (int y = this.GridMinInclusive.Y; y < this.GridMaxExclusive.Y; ++y)
            {
                int yCopy = y;   
                Func<int, double> xPenaltyFunc = x => this.PenaltyFunc(new Point(x, yCopy));
                distanseTransformsForFixedY[y - this.GridMinInclusive.Y] = new GeneralizedDistanceTransform1D(
                    this.GridMinInclusive.X, this.GridMaxExclusive.X, this.DistanceScaleX, xPenaltyFunc);
            }

            this.values = new double[this.GridMaxExclusive.X - this.GridMinInclusive.X, this.GridMaxExclusive.Y - this.GridMinInclusive.Y];
            for (int x = this.GridMinInclusive.X; x < this.GridMaxExclusive.X; ++x)
            {
                int xCopy = x;
                Func<int, double> yPenaltyFunc = y => distanseTransformsForFixedY[y][xCopy];
                GeneralizedDistanceTransform1D distanceTranformForX = new GeneralizedDistanceTransform1D(
                    this.GridMinInclusive.Y, this.GridMaxExclusive.Y, this.DistanceScaleY, yPenaltyFunc);

                for (int y = this.GridMinInclusive.Y; y < this.GridMaxExclusive.Y; ++y)
                    this.values[x, y] = distanceTranformForX[y];
            }
        }
    }
}
