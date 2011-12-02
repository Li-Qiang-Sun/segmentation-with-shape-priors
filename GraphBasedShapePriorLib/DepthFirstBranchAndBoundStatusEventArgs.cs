using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class DepthFirstBranchAndBoundStatusEventArgs : BranchAndBoundStatusEventArgs
    {
        public double UpperBound { get; private set; }

        public DepthFirstBranchAndBoundStatusEventArgs(double upperBound, Image statusImage, Image segmentationMask, Image unaryTermsImage, Image shapeTermsImage)
            : base(statusImage, segmentationMask, unaryTermsImage, shapeTermsImage)
        {
            this.UpperBound = upperBound;
        }
    }
}