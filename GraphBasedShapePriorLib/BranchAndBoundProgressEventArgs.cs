using System;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior
{
    public class BranchAndBoundProgressEventArgs : EventArgs
    {
        public double LowerBound { get; private set; }

        public Image2D<bool> SegmentationMask { get; private set; }

        public Image2D<ObjectBackgroundTerm> UnaryTermsImage { get; private set; }

        public Image2D<ObjectBackgroundTerm> ShapeTermsImage { get; private set; }

        public ShapeConstraints Constraints { get; private set; }

        public BranchAndBoundProgressEventArgs(
            double lowerBound,
            Image2D<bool> segmentationMask,
            Image2D<ObjectBackgroundTerm> unaryTermsImage,
            Image2D<ObjectBackgroundTerm> shapeTermsImage,
            ShapeConstraints constraints)
        {
            if (segmentationMask == null)
                throw new ArgumentNullException("segmentationMask");
            if (unaryTermsImage == null)
                throw new ArgumentNullException("unaryTermsImage");
            if (shapeTermsImage == null)
                throw new ArgumentNullException("shapeTermsImage");
            if (constraints == null)
                throw new ArgumentNullException("constraints");
            
            this.LowerBound = lowerBound;
            this.SegmentationMask = segmentationMask;
            this.UnaryTermsImage = unaryTermsImage;
            this.ShapeTermsImage = shapeTermsImage;
            this.Constraints = constraints;
        }
    }
}