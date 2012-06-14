using System;
using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class BranchAndBoundCompletedEventArgs : EventArgs
    {
        public Image CollapsedSolutionSegmentationMask { get; private set; }

        public Image CollapsedSolutionUnaryTermsImage { get; private set; }

        public Image CollapsedSolutionShapeTermsImage { get; private set; }
        
        public ShapeConstraints ResultConstraints { get; private set; }

        public double LowerBound { get; private set; }

        public BranchAndBoundCompletedEventArgs(
            Image collapsedSolutionSegmentationMask,
            Image collapsedSolutionUnaryTermsImage,
            Image collapsedSolutionShapeTermsImage,
            ShapeConstraints resultConstraints,
            double lowerBound)
        {
            if (collapsedSolutionSegmentationMask == null)
                throw new ArgumentNullException("collapsedSolutionSegmentationMask");
            if (collapsedSolutionUnaryTermsImage == null)
                throw new ArgumentNullException("collapsedSolutionUnaryTermsImage");
            if (collapsedSolutionShapeTermsImage == null)
                throw new ArgumentNullException("collapsedSolutionShapeTermsImage");
            if (resultConstraints == null)
                throw new ArgumentNullException("resultConstraints");

            this.CollapsedSolutionSegmentationMask = collapsedSolutionSegmentationMask;
            this.CollapsedSolutionUnaryTermsImage = collapsedSolutionUnaryTermsImage;
            this.CollapsedSolutionShapeTermsImage = collapsedSolutionShapeTermsImage;
            this.ResultConstraints = resultConstraints;
            this.LowerBound = lowerBound;
        }
    }
}
