using System;
using System.Diagnostics;
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

        public Func<double, double, double, double, double> PenaltyFunc { get; private set; }

        private double[,] values;

        private readonly double gridStepX;

        private readonly double gridStepY;

        public GeneralizedDistanceTransform2D(
            Vector gridMin,
            Vector gridMax,
            Size gridSize,
            double distanceScaleX,
            double distanceScaleY,
            Func<double, double, double, double, double> penaltyFunc)
        {
            this.GridMin = gridMin;
            this.GridMax = gridMax;
            this.GridSize = gridSize;
            this.DistanceScaleX = distanceScaleX;
            this.DistanceScaleY = distanceScaleY;
            this.PenaltyFunc = penaltyFunc;

            // Our grid covers all the evenly distributed points from min to max in a way that each point is the center of its cell
            this.gridStepX = (this.GridMax.X - this.GridMin.X) / (this.GridSize.Width - 1);
            this.gridStepY = (this.GridMax.Y - this.GridMin.Y) / (this.GridSize.Height - 1);

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
            return CoordToGridIndex(coord, this.GridMin.X, this.gridStepX, this.GridSize.Width);
        }

        public int CoordToGridIndexY(double coord)
        {
            return CoordToGridIndex(coord, this.GridMin.Y, this.gridStepY, this.GridSize.Height);
        }

        public double GridIndexToCoordX(int gridIndex)
        {
            return GridIndexToCoord(gridIndex, this.GridMin.X, this.gridStepX);
        }

        public double GridIndexToCoordY(int gridIndex)
        {
            return GridIndexToCoord(gridIndex, this.GridMin.Y, this.gridStepY);
        }

        private static int CoordToGridIndex(double coord, double gridMin, double gridStepSize, int gridSize)
        {
            int gridIndex = (int)((coord - gridMin) / gridStepSize + 0.5);
            if (gridIndex < 0 || gridIndex >= gridSize)
                throw new ArgumentOutOfRangeException("coord");
            return gridIndex;
        }

        private static double GridIndexToCoord(int gridIndex, double gridMin, double gridStepSize)
        {
            return gridMin + gridIndex * gridStepSize;
        }        

        private void Calculate()
        {
            GeneralizedDistanceTransform1D[] distanseTransformsForFixedGridY = new GeneralizedDistanceTransform1D[this.GridSize.Height];
            for (int y = 0; y < this.GridSize.Height; ++y)
            {
                int yCopy = y;   
                Func<double, double, double> xPenaltyFunc =
                    (x, xRadius) => this.PenaltyFunc(x, GridIndexToCoordY(yCopy), xRadius, 0.5 * this.gridStepY);
                distanseTransformsForFixedGridY[y] = new GeneralizedDistanceTransform1D(
                    this.GridMin.X, this.GridMax.X, this.GridSize.Width, this.DistanceScaleX, xPenaltyFunc);
            }

            this.values = new double[this.GridSize.Width, this.GridSize.Height];
            for (int x = 0; x < this.GridSize.Width; ++x)
            {
                int xCopy = x;
                Func<double, double, double> yPenaltyFunc =
                    (y, yRadius) => distanseTransformsForFixedGridY[CoordToGridIndexY(y)].GetByGridIndex(xCopy);
                GeneralizedDistanceTransform1D distanceTranformForX = new GeneralizedDistanceTransform1D(
                    this.GridMin.Y, this.GridMax.Y, this.GridSize.Height, this.DistanceScaleY, yPenaltyFunc);

                for (int y = 0; y < this.GridSize.Height; ++y)
                    this.values[x, y] = distanceTranformForX.GetByGridIndex(y);
            }
        }
    }
}
