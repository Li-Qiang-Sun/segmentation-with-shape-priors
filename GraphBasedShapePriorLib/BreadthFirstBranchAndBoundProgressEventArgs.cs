using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class BreadthFirstBranchAndBoundProgressEventArgs : BranchAndBoundProgressEventArgs
    {
        public double LowerBound { get; private set; }
        
        public int FrontSize { get; private set; }

        public double FrontItemsPerSecond { get; private set; }

        public BreadthFirstBranchAndBoundProgressEventArgs(
            double lowerBound,
            int frontSize,
            double frontItemsPerSecond,
            Image segmentationMask,
            Image unaryTermsImage,
            Image shapeTermsImage,
            ShapeConstraints constraints,
            Image bestMaskEstimate)
            : base(segmentationMask, unaryTermsImage, shapeTermsImage, constraints, bestMaskEstimate)
        {
            this.LowerBound = lowerBound;
            this.FrontSize = frontSize;
            this.FrontItemsPerSecond = frontItemsPerSecond;
        }
    }
}