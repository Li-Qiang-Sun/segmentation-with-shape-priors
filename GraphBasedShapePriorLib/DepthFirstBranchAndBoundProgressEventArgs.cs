using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class DepthFirstBranchAndBoundProgressEventArgs : BranchAndBoundProgressEventArgs
    {
        public double UpperBound { get; private set; }

        public DepthFirstBranchAndBoundProgressEventArgs(
            double upperBound,
            Image segmentationMask,
            Image unaryTermsImage,
            Image shapeTermsImage,
            ShapeConstraints constraints,
            Image bestMaskEstimate)
            : base(segmentationMask, unaryTermsImage, shapeTermsImage, constraints, bestMaskEstimate)
        {
            this.UpperBound = upperBound;
        }
    }
}