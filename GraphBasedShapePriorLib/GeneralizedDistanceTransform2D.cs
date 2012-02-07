using System;
using System.Diagnostics;
using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class GeneralizedDistanceTransform2D
    {
        public Vector GridMin { get; private set; }

        public Vector GridMax { get; private set; }

        public Size GridSize { get; private set; }

        public bool IsComputed { get; private set; }
        
        private double[,] values;

        private Tuple<int, int>[,] bestIndices;

        private readonly double gridStepX;

        private readonly double gridStepY;

        public GeneralizedDistanceTransform2D(
            Vector gridMin,
            Vector gridMax,
            Size gridSize)
        {
            this.GridMin = gridMin;
            this.GridMax = gridMax;
            this.GridSize = gridSize;

            // Our grid covers all the evenly distributed points from min to max in a way that each point is the center of its cell
            this.gridStepX = (this.GridMax.X - this.GridMin.X) / (this.GridSize.Width - 1);
            this.gridStepY = (this.GridMax.Y - this.GridMin.Y) / (this.GridSize.Height - 1);
        }

        public double GetValueByGridIndices(int gridX, int gridY)
        {
            if (!this.IsComputed)
                throw new InvalidOperationException("You should calculate transform first.");

            return this.values[gridX, gridY];
        }

        public double GetValueByCoords(double coordX, double coordY)
        {
            return this.GetValueByGridIndices(CoordToGridIndexX(coordX), CoordToGridIndexY(coordY));
        }

        public Tuple<int, int> GetBestIndicesByGridIndices(int gridX, int gridY)
        {
            if (!this.IsComputed)
                throw new InvalidOperationException("You should calculate transform first.");

            return this.bestIndices[gridX, gridY];
        }

        public Tuple<int, int> GetBestIndicesByCoords(double coordX, double coordY)
        {
            return this.GetBestIndicesByGridIndices(this.CoordToGridIndexX(coordX), this.CoordToGridIndexY(coordY));
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

        public void Compute(double distanceScaleX, double distanceScaleY, Func<double, double, double, double, double> penaltyFunc)
        {
            if (penaltyFunc == null)
                throw new ArgumentNullException("penaltyFunc");
            
            GeneralizedDistanceTransform1D[] distanseTransformsForFixedGridY = new GeneralizedDistanceTransform1D[this.GridSize.Height];
            for (int y = 0; y < this.GridSize.Height; ++y)
            {
                int yCopy = y;   
                Func<double, double, double> xPenaltyFunc =
                    (x, xRadius) => penaltyFunc(x, GridIndexToCoordY(yCopy), xRadius, 0.5 * this.gridStepY);
                distanseTransformsForFixedGridY[y] = new GeneralizedDistanceTransform1D(
                    this.GridMin.X, this.GridMax.X, this.GridSize.Width);
                distanseTransformsForFixedGridY[y].Compute(distanceScaleX, xPenaltyFunc);
            }

            this.values = new double[this.GridSize.Width, this.GridSize.Height];
            this.bestIndices = new Tuple<int, int>[this.GridSize.Width, this.GridSize.Height];
            for (int x = 0; x < this.GridSize.Width; ++x)
            {
                int xCopy = x;
                Func<double, double, double> yPenaltyFunc =
                    (y, yRadius) => distanseTransformsForFixedGridY[CoordToGridIndexY(y)].GetValueByGridIndex(xCopy);
                GeneralizedDistanceTransform1D distanceTranformForX = new GeneralizedDistanceTransform1D(
                    this.GridMin.Y, this.GridMax.Y, this.GridSize.Height);
                distanceTranformForX.Compute(distanceScaleY, yPenaltyFunc);

                for (int y = 0; y < this.GridSize.Height; ++y)
                {
                    this.values[x, y] = distanceTranformForX.GetValueByGridIndex(y);
                    int bestY = distanceTranformForX.GetBestIndexByGridIndex(y);
                    int bestX = distanseTransformsForFixedGridY[bestY].GetBestIndexByGridIndex(x);
                    this.bestIndices[x, y] = new Tuple<int, int>(bestX, bestY);
                }
            }

            this.IsComputed = true;
        }
    }
}
