using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class BreadthFirstBranchAndBoundStatusEventArgs : BranchAndBoundStatusEventArgs
    {
        public double LowerBound { get; private set; }
        
        public int FrontSize { get; private set; }

        public double FrontItemsPerSecond { get; private set; }

        public BreadthFirstBranchAndBoundStatusEventArgs(
            double lowerBound,
            int frontSize,
            double frontItemsPerSecond,
            Image statusImage,
            Image segmentationMask,
            Image unaryTermsImage,
            Image shapeTermsImage,
            Image bestMaskEstimate)
            : base(statusImage, segmentationMask, unaryTermsImage, shapeTermsImage, bestMaskEstimate)
        {
            this.LowerBound = lowerBound;
            this.FrontSize = frontSize;
            this.FrontItemsPerSecond = frontItemsPerSecond;
        }
    }
}