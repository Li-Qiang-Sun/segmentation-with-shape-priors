using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace Research.GraphBasedShapePrior
{
    public class GeneralizedDistanceTransform2D
    {
        private readonly double[,] values;

        private readonly int[,] timeStamps;

        private readonly Tuple<int, int>[,] bestIndices;

        private readonly GeneralizedDistanceTransform1D infiniteTransformX;

        private readonly GeneralizedDistanceTransform1D[] transformsForFixedGridX;
        
        private readonly GeneralizedDistanceTransform1D[] transformsForFixedGridY;

        private readonly GeneralizedDistanceTransform1D[] usedTransformsForFixedGridY;

        private int currentTimeStamp = 0;

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
            this.timeStamps = new int[this.GridSize.Width, this.GridSize.Height];

            this.transformsForFixedGridX = new GeneralizedDistanceTransform1D[this.GridSize.Width];
            for (int x = 0; x < this.GridSize.Width; ++x)
                this.transformsForFixedGridX[x] = new GeneralizedDistanceTransform1D(rangeY, this.GridSize.Height);
            
            this.transformsForFixedGridY = new GeneralizedDistanceTransform1D[this.GridSize.Height];
            this.usedTransformsForFixedGridY = new GeneralizedDistanceTransform1D[this.GridSize.Height];
            for (int y = 0; y < this.GridSize.Height; ++y)
                transformsForFixedGridY[y] = new GeneralizedDistanceTransform1D(rangeX, this.GridSize.Width);

            this.infiniteTransformX = new GeneralizedDistanceTransform1D(rangeX, this.GridSize.Width);
            this.infiniteTransformX.Compute(1, x => 1e+20);
        }

        public Range RangeX { get; private set; }

        public Range RangeY { get; private set; }

        public Size GridSize { get; private set; }

        public bool IsComputed { get; private set; }

        public double GridStepSizeX
        {
            get { return this.transformsForFixedGridY[0].GridStepSize; }
        }

        public double GridStepSizeY
        {
            get { return this.transformsForFixedGridX[0].GridStepSize; }
        }

        public void ResetFinitePenaltyRange()
        {
            this.IsComputed = false;
            foreach (GeneralizedDistanceTransform1D transform in transformsForFixedGridX)
                transform.ResetFinitePenaltyRange();
            foreach (GeneralizedDistanceTransform1D transform in transformsForFixedGridY)
                transform.ResetFinitePenaltyRange();
        }

        public void AddFinitePenaltyRangeX(Range rangeX)
        {
            this.IsComputed = false;
            foreach (GeneralizedDistanceTransform1D transform in transformsForFixedGridY)
                transform.AddFinitePenaltyRange(rangeX);
        }

        public void AddFinitePenaltyRangeY(Range rangeY)
        {
            this.IsComputed = false;
            foreach (GeneralizedDistanceTransform1D transform in transformsForFixedGridX)
                transform.AddFinitePenaltyRange(rangeY);
        }

        public void ResetInterestRange()
        {
            this.IsComputed = false;
            foreach (GeneralizedDistanceTransform1D transform in transformsForFixedGridX)
                transform.ResetInterestRange();
            foreach (GeneralizedDistanceTransform1D transform in transformsForFixedGridY)
                transform.ResetInterestRange();
        }

        public void AddInterestRangeX(Range rangeX)
        {
            this.IsComputed = false;
            foreach (GeneralizedDistanceTransform1D transform in transformsForFixedGridY)
                transform.AddInterestRange(rangeX);
        }

        public void AddInterestRangeY(Range rangeY)
        {
            this.IsComputed = false;
            foreach (GeneralizedDistanceTransform1D transform in transformsForFixedGridX)
                transform.AddInterestRange(rangeY);
        }

        public bool IsCoordXOfInterest(double coordX)
        {
            return this.transformsForFixedGridY[0].IsCoordOfInterest(coordX);
        }

        public bool IsCoordYOfInterest(double coordY)
        {
            return this.transformsForFixedGridX[0].IsCoordOfInterest(coordY);
        }

        public bool AreGridIndicesComputed(int gridX, int gridY)
        {
            return this.timeStamps[gridX, gridY] == this.currentTimeStamp;
        }

        public bool AreCoordsComputed(double x, double y)
        {
            return this.AreGridIndicesComputed(this.CoordToGridIndexX(x), this.CoordToGridIndexY(y));
        }

        public IEnumerable<int> EnumerateInterestGridIndicesX()
        {
            return this.transformsForFixedGridY[0].EnumerateInterestGridIndices();
        }

        public IEnumerable<int> EnumerateInterestGridIndicesY()
        {
            return this.transformsForFixedGridX[0].EnumerateInterestGridIndices();
        }

        public double GetValueByGridIndices(int gridX, int gridY)
        {
            if (!this.IsComputed)
                throw new InvalidOperationException("You should calculate transform first.");
            if (!this.AreGridIndicesComputed(gridX, gridY))
                throw new ArgumentException("Given coords were out of interest during last computation.");

            return this.values[gridX, gridY];
        }

        public bool TryGetValueByGridIndices(int gridX, int gridY, out double value)
        {
            if (!this.IsComputed)
                throw new InvalidOperationException("You should calculate transform first.");
            
            if (!this.AreGridIndicesComputed(gridX, gridY))
            {
                value = 0;
                return false;
            }
            
            value = this.values[gridX, gridY];
            return true;
        }

        public double GetValueByCoords(double coordX, double coordY)
        {
            return this.GetValueByGridIndices(CoordToGridIndexX(coordX), CoordToGridIndexY(coordY));
        }

        public bool TryGetValueByCoords(double coordX, double coordY, out double value)
        {
            return this.TryGetValueByGridIndices(CoordToGridIndexX(coordX), CoordToGridIndexY(coordY), out value);
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
            return this.transformsForFixedGridX[0].CoordToGridIndex(coord);
        }

        public double GridIndexToCoordX(int gridIndex)
        {
            return this.transformsForFixedGridY[0].GridIndexToCoord(gridIndex);
        }

        public double GridIndexToCoordY(int gridIndex)
        {
            return this.transformsForFixedGridX[0].GridIndexToCoord(gridIndex);
        }

        public void Compute(double distanceScaleX, double distanceScaleY, Func<double, double, double> penaltyFunc)
        {
            if (penaltyFunc == null)
                throw new ArgumentNullException("penaltyFunc");

            ++this.currentTimeStamp;

            for (int y = 0; y < this.GridSize.Height; ++y)
                this.usedTransformsForFixedGridY[y] = this.infiniteTransformX;

            Parallel.ForEach(
                this.transformsForFixedGridX[0].EnumerateFinitePenaltyGridIndices(),
                y =>
                {
                    Func<double, double> xPenaltyFunc = x => penaltyFunc(x, GridIndexToCoordY(y));
                    transformsForFixedGridY[y].Compute(distanceScaleX, xPenaltyFunc);
                    this.usedTransformsForFixedGridY[y] = this.transformsForFixedGridY[y];
                });

            Parallel.ForEach(
                this.transformsForFixedGridY[0].EnumerateInterestGridIndices(),
                x =>
                {
                    Func<double, double> yPenaltyFunc =
                        y => this.usedTransformsForFixedGridY[CoordToGridIndexY(y)].GetValueByGridIndex(x);
                    this.transformsForFixedGridX[x].Compute(distanceScaleY, yPenaltyFunc);

                    foreach (int y in this.transformsForFixedGridX[x].EnumerateInterestGridIndices())
                    {
                        this.values[x, y] = this.transformsForFixedGridX[x].GetValueByGridIndex(y);
                        int bestY = this.transformsForFixedGridX[x].GetBestIndexByGridIndex(y);
                        int bestX = this.usedTransformsForFixedGridY[bestY].GetBestIndexByGridIndex(x);
                        this.bestIndices[x, y] = new Tuple<int, int>(bestX, bestY);
                        this.timeStamps[x, y] = this.currentTimeStamp;
                    }
                });

            this.IsComputed = true;
        }
    }
}
