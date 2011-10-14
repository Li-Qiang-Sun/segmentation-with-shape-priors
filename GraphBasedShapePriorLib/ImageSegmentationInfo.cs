using System;

namespace Research.GraphBasedShapePrior
{
    public class ImageSegmentationInfo
    {
        public double Energy { get; private set; }

        public Image2D<bool> SegmentationMask { get; private set; }

        public ImageSegmentationInfo(double energy, Image2D<bool> segmentationMask)
        {
            this.Energy = energy;
            this.SegmentationMask = segmentationMask;
        }
    }
}
