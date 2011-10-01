using System;
using System.Diagnostics;
using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class DepthFirstBranchAndBoundStatusEventArgs : EventArgs
    {
        public double UpperBound { get; private set; }

        public Image2D<bool> UpperBoundSegmentationMask { get; private set; }
        
        public Image StatusImage { get; private set; }

        public DepthFirstBranchAndBoundStatusEventArgs(double upperBound, Image statusImage, Image2D<bool> upperBoundSegmentationMask)
        {
            Debug.Assert(statusImage != null);

            this.UpperBound = upperBound;
            this.UpperBoundSegmentationMask = upperBoundSegmentationMask;
            this.StatusImage = statusImage;
        }
    }
}