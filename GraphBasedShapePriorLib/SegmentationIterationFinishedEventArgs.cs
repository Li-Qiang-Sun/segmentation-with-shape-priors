using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior
{
    public class SegmentationIterationFinishedEventArgs : EventArgs
    {
        public SegmentationIterationFinishedEventArgs(int iteration, Shape shape, Image segmentationMask, Image unaryTermsImage, Image shapeTermsImage)
        {
            this.Iteration = iteration;
            this.Shape = shape;
            this.SegmentationMask = segmentationMask;
            this.UnaryTermsImage = unaryTermsImage;
            this.ShapeTermsImage = shapeTermsImage;
        }

        public int Iteration { get; private set; }

        public Shape Shape { get; private set; }

        // TODO: return Image2D
        public Image SegmentationMask { get; private set; }

        // TODO: return Image2D
        public Image UnaryTermsImage { get; private set; }

        // TODO: return Image2D
        public Image ShapeTermsImage { get; private set; }
    }
}