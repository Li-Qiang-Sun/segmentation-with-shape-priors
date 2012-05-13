using System;
using System.Diagnostics;
using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class GeneralizedDistanceTransform2D
    {
        private readonly double[,] values;

        private readonly Tuple<int, int>[,] bestIndices;

        private readonly GeneralizedDistanceTransform1D tranformForFixedGridX;

        private readonly GeneralizedDistanceTransform1D infiniteTransformX;
        
        private readonly GeneralizedDistanceTransform1D[] transformsForFixedGridY;

        private readonly GeneralizedDistanceTransform1D[] usedTransformsForFixedGridY;

        public GeneralizedDistanceTransform2D(
            Range rangeX, Range rangeY, Size gridSize)
        {
            if (rangeX.Outside || rangeY.Outside)
                throw new ArgumentException("Outside ranges are not allowed.");
            
            this.RangeX = rangeX;
            this.RangeY = rangeY;
            this.GridSize = gridSize;

            this.values = new double[this.GridSize.Width, this.GridSize.Height];
            this.bestIndices = new Tuple<int, int>[this.GridSize.Width, this.GridSize.Height];
            this.tranformForFixedGridX = new GeneralizedDistanceTransform1D(
                rangeY, this.GridSize.Height);
            this.transformsForFixedGridY = new GeneralizedDistanceTransform1D[this.GridSize.Height];
            this.usedTransformsForFixedGridY = new GeneralizedDistanceTransform1D[this.GridSize.Height];
            for (int y = 0; y < this.GridSize.Height; ++y)
                transformsForFixedGridY[y] = new GeneralizedDistanceTransform1D(rangeX, this.GridSize.Width);

            this.infiniteTransformX = new GeneralizedDistanceTransform1D(rangeX, this.GridSize.Width);
            this.infiniteTransformX.Compute(1, (x, r) => 1e+20);
        }

        public Range RangeX { get; private set; }

        public Range RangeY { get; private set; }

        public Size GridSize { get; private set; }

        public bool IsComputed { get; private set; }

        public void ResetFinitePenaltyRange()
        {
            this.tranformForFixedGridX.ResetFinitePenaltyRange();
            foreach (GeneralizedDistanceTransform1D transform in transformsForFixedGridY)
                transform.ResetFinitePenaltyRange();
        }

        public void AddFinitePenaltyRangeX(Range rangeX)
        {
            foreach (GeneralizedDistanceTransform1D transform in transformsForFixedGridY)
                transform.AddFinitePenaltyRange(rangeX);
        }

        public void AddFinitePenaltyRangeY(Range rangeY)
        {
            this.tranformForFixedGridX.AddFinitePenaltyRange(rangeY);
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
            return this.transformsForFixedGridY[0].CoordToGridIndex(coord);
        }

        public int CoordToGridIndexY(double coord)
        {
            return this.tranformForFixedGridX.CoordToGridIndex(coord);
        }

        public double GridIndexToCoordX(int gridIndex)
        {
            return this.transformsForFixedGridY[0].GridIndexToCoord(gridIndex);
        }

        public double GridIndexToCoordY(int gridIndex)
        {
            return this.tranformForFixedGridX.GridIndexToCoord(gridIndex);
        }

        public void Compute(double distanceScaleX, double distanceScaleY, Func<double, double, double, double, double> penaltyFunc)
        {
            if (penaltyFunc == null)
                throw new ArgumentNullException("penaltyFunc");

            for (int y = 0; y < this.GridSize.Height; ++y)
                this.usedTransformsForFixedGridY[y] = this.infiniteTransformX;
            
            double yRadius = 0.5 * this.RangeY.Length / (this.GridSize.Height - 1);
            foreach (int y in this.tranformForFixedGridX.EnumerateFinitePenaltyGridIndices())
            {
                int yCopy = y;   
                Func<double, double, double> xPenaltyFunc =
                    (x, xRadius) => penaltyFunc(x, GridIndexToCoordY(yCopy), xRadius, yRadius);
                transformsForFixedGridY[y].Compute(distanceScaleX, xPenaltyFunc);
                this.usedTransformsForFixedGridY[y] = this.transformsForFixedGridY[y];
            }

            for (int x = 0; x < this.GridSize.Width; ++x)
            {
                int xCopy = x;
                Func<double, double, double> yPenaltyFunc =
                    (y, _) => this.usedTransformsForFixedGridY[CoordToGridIndexY(y)].GetValueByGridIndex(xCopy);
                this.tranformForFixedGridX.Compute(distanceScaleY, yPenaltyFunc);

                for (int y = 0; y < this.GridSize.Height; ++y)
                {
                    this.values[x, y] = this.tranformForFixedGridX.GetValueByGridIndex(y);
                    int bestY = this.tranformForFixedGridX.GetBestIndexByGridIndex(y);
                    int bestX = this.usedTransformsForFixedGridY[bestY].GetBestIndexByGridIndex(x);
                    this.bestIndices[x, y] = new Tuple<int, int>(bestX, bestY);
                }
            }

            this.IsComputed = true;
        }
    }
}
