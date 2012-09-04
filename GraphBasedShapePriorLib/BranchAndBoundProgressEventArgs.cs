using System;
using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class BranchAndBoundProgressEventArgs : EventArgs
    {
        public double LowerBound { get; private set; }

        // TODO: return Image2D
        public Image SegmentationMask { get; private set; }

        // TODO: return Image2D
        public Image UnaryTermsImage { get; private set; }

        // TODO: return Image2D
        public Image ShapeTermsImage { get; private set; }

        public ShapeConstraints Constraints { get; private set; }

        public BranchAndBoundProgressEventArgs(
            double lowerBound, Image segmentationMask, Image unaryTermsImage, Image shapeTermsImage, ShapeConstraints constraints)
        {
            this.LowerBound = lowerBound;
            this.SegmentationMask = segmentationMask;
            this.UnaryTermsImage = unaryTermsImage;
            this.ShapeTermsImage = shapeTermsImage;
            this.Constraints = constraints;
        }
    }
}