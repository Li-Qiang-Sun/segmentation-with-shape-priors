using System;
using System.Diagnostics;
using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class BranchAndBoundStatusEventArgs : EventArgs
    {
        public Image StatusImage { get; private set; }

        public Image SegmentationMask { get; private set; }

        public Image UnaryTermsImage { get; private set; }

        public Image ShapeTermsImage { get; private set; }

        public BranchAndBoundStatusEventArgs(
            Image statusImage, Image segmentationMask, Image unaryTermsImage, Image shapeTermsImage)
        {
            this.StatusImage = statusImage;
            this.SegmentationMask = segmentationMask;
            this.UnaryTermsImage = unaryTermsImage;
            this.ShapeTermsImage = shapeTermsImage;
        }
    }
}