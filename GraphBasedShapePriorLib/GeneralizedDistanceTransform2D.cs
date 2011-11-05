using System;
using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class GeneralizedDistanceTransform2D
    {
        public Vector GridMin { get; private set; }

        public Vector GridMax { get; private set; }

        public double DistanceScaleX { get; private set; }

        public double DistanceScaleY { get; private set; }

        public Size GridSize { get; private set; }


        public Func<double, double, double> PenaltyFunc { get; private set; }

        private double[,] values;

        public GeneralizedDistanceTransform2D(
            Vector gridMin,
            Vector gridMax,
            Size gridSize,
            double distanceScaleX,
            double distanceScaleY,
            Func<double, double, double> penaltyFunc)
        {
            this.GridMin = gridMin;
            this.GridMax = gridMax;
            this.GridSize = gridSize;
            this.DistanceScaleX = distanceScaleX;
            this.DistanceScaleY = distanceScaleY;
            this.PenaltyFunc = penaltyFunc;

            this.Calculate();
        }

        public double GetByGridIndices(int gridX, int gridY)
        {
            return this.values[gridX, gridY];
        }

        public double GetByCoords(double coordX, double coordY)
        {
            return this.values[CoordToGridIndexX(coordX), CoordToGridIndexY(coordY)];
        }

        public int CoordToGridIndexX(double coord)
        {
            return CoordToGridIndex(coord, this.GridMin.X, this.GridMax.X, this.GridSize.Width);
        }

        public int CoordToGridIndexY(double coord)
        {
            return CoordToGridIndex(coord, this.GridMin.Y, this.GridMax.Y, this.GridSize.Height);
        }

        private static int CoordToGridIndex(double coord, double gridMin, double gridMax, int gridSize)
        {
            if (coord < gridMin || coord > gridMax)
                throw new ArgumentOutOfRangeException("coord");

            double relativeCoord = (coord - gridMin) / (gridMax - gridMin);
            int gridIndex = (int)(relativeCoord * gridSize);
            return gridIndex;
        }

        private static double GridIndexToCoord(int gridIndex, double gridMin, double gridMax, int gridSize)
        {
            return gridMin + (double)gridIndex / gridSize * (gridMax - gridMin);
        }

        private void Calculate()
        {
            GeneralizedDistanceTransform1D[] distanseTransformsForFixedGridY = new GeneralizedDistanceTransform1D[this.GridSize.Height];
            for (int y = 0; y < this.GridSize.Height; ++y)
            {
                int yCopy = y;   
                Func<double, double> xPenaltyFunc =
                    x => this.PenaltyFunc(x, GridIndexToCoord(yCopy, this.GridMin.Y, this.GridMax.Y, this.GridSize.Height));
                distanseTransformsForFixedGridY[y] = new GeneralizedDistanceTransform1D(
                    this.GridMin.X, this.GridMax.X, this.GridSize.Width, this.DistanceScaleX, xPenaltyFunc);
            }

            this.values = new double[this.GridSize.Width, this.GridSize.Height];
            for (int x = 0; x < this.GridSize.Width; ++x)
            {
                int xCopy = x;
                Func<double, double> yPenaltyFunc =
                    y => distanseTransformsForFixedGridY[CoordToGridIndexY(y)].GetByGridIndex(xCopy);
                GeneralizedDistanceTransform1D distanceTranformForX = new GeneralizedDistanceTransform1D(
                    this.GridMin.Y, this.GridMax.Y, this.GridSize.Height, this.DistanceScaleY, yPenaltyFunc);

                for (int y = 0; y < this.GridSize.Height; ++y)
                    this.values[x, y] = distanceTranformForX.GetByGridIndex(y);
            }
        }
    }
}
