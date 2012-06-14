using System;
using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class BranchAndBoundProgressEventArgs : EventArgs
    {
        public Image SegmentationMask { get; private set; }

        public Image UnaryTermsImage { get; private set; }

        public Image ShapeTermsImage { get; private set; }

        public ShapeConstraints Constraints { get; private set; }

        public Image BestMaskEstimate { get; private set; }

        public BranchAndBoundProgressEventArgs(
            Image segmentationMask, Image unaryTermsImage, Image shapeTermsImage, ShapeConstraints constraints, Image bestMaskEstimate)
        {
            this.SegmentationMask = segmentationMask;
            this.UnaryTermsImage = unaryTermsImage;
            this.ShapeTermsImage = shapeTermsImage;
            this.Constraints = constraints;
            this.BestMaskEstimate = bestMaskEstimate;
        }
    }
}