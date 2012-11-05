using System;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior
{
    public class SegmentationSolution
    {
        public SegmentationSolution(Shape shape, Image2D<bool> mask, double energy)
        {
            if (shape == null && mask == null)
                throw new ArgumentException("Segmentation solution should contain something.");
            
            this.Shape = shape;
            this.Mask = mask;
            this.Energy = energy;
        }

        private SegmentationSolution()
        {
        }

        public static SegmentationSolution None = new SegmentationSolution();

        public Shape Shape { get; private set; }

        public Image2D<bool> Mask { get; private set; }

        public double Energy { get; private set; }
    }
}
