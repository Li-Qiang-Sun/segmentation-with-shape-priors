using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior
{
    public class SegmentationIterationFinishedEventArgs : EventArgs
    {
        public SegmentationIterationFinishedEventArgs(
            int iteration,
            Shape shape,
            Image2D<bool> segmentationMask,
            Image2D<ObjectBackgroundTerm> unaryTermsImage,
            Image2D<ObjectBackgroundTerm> shapeTermsImage)
        {
            this.Iteration = iteration;
            this.Shape = shape;
            this.SegmentationMask = segmentationMask;
            this.UnaryTermsImage = unaryTermsImage;
            this.ShapeTermsImage = shapeTermsImage;
        }

        public int Iteration { get; private set; }

        public Shape Shape { get; private set; }

        public Image2D<bool> SegmentationMask { get; private set; }

        public Image2D<ObjectBackgroundTerm> UnaryTermsImage { get; private set; }

        public Image2D<ObjectBackgroundTerm> ShapeTermsImage { get; private set; }
    }
}