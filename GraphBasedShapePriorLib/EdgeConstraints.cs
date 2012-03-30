using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Research.GraphBasedShapePrior
{
    public class EdgeConstraints
    {
        public EdgeConstraints(double minWidth, double maxWidth)
        {
            if (minWidth > maxWidth)
                throw new ArgumentException("Min width should not be greater than max width.");

            this.MinWidth = minWidth;
            this.MaxWidth = maxWidth;
        }

        public EdgeConstraints(double width)
            : this(width, width)
        {
        }

        public double MinWidth { get; private set; }

        public double MaxWidth { get; private set; }

        public Range WidthRange
        {
            get { return new Range(this.MinWidth, this.MaxWidth); }
        }

        public double FreedomLevel
        {
            get { return this.MaxWidth - this.MinWidth; }
        }

        public bool NoFreedom
        {
            // TODO: make this customizable            
            get { return this.FreedomLevel < 1 + 1e-8; }
        }

        public double MiddleWidth
        {
            get { return 0.5 * (this.MinWidth + this.MaxWidth); }
        }

        public EdgeConstraints Collapse()
        {
            return new EdgeConstraints(this.MiddleWidth);
        }

        public List<EdgeConstraints> Split()
        {
            // We want to split constraints in non-overlapping sets
            const double eps = 1e-4;

            List<EdgeConstraints> result = new List<EdgeConstraints>
            {
                new EdgeConstraints(this.MinWidth, this.MiddleWidth - eps),
                new EdgeConstraints(this.MiddleWidth + eps, this.MaxWidth)
            };
            return result;
        }
    }
}
