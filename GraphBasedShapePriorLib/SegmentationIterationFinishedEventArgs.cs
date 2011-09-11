using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Research.GraphBasedShapePrior
{
    public class SegmentationIterationFinishedEventArgs : EventArgs
    {
        private readonly List<Shape> shapes;

        public SegmentationIterationFinishedEventArgs(int iteration, Image2D<bool> mask, IEnumerable<Shape> shapes)
        {
            this.Iteration = iteration;
            this.Mask = mask;
            this.shapes = new List<Shape>(shapes);
        }

        public int Iteration { get; private set; }

        public Image2D<bool> Mask { get; private set; }

        public ReadOnlyCollection<Shape> Shapes
        {
            get { return this.shapes.AsReadOnly(); }
        }
    }
}