using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Research.GraphBasedShapePrior
{
    public struct Range
    {
        public double Left { get; private set; }

        public double Right { get; private set; }

        public bool Outside { get; private set; }

        public static readonly Range Everything = new Range(Double.NegativeInfinity, Double.PositiveInfinity);

        public Range(double left, double right)
            : this(left, right, false)
        {
        }
        
        public Range(double left, double right, bool outside)
            : this()
        {
            if (left > right)
                throw new ArgumentOutOfRangeException("left", "left should not be greater than right");

            this.Left = left;
            this.Right = right;
            this.Outside = outside;
        }

        public bool Contains(double coord)
        {
            if (this.Outside)
                return coord <= this.Left || coord >= this.Right;
            return coord >= this.Left && coord <= this.Right;
        }

        public bool IntersectsWith(Range other)
        {
            // Regular ranges
            if (!this.Outside && !other.Outside)
            {
                return
                    this.Left >= other.Left && this.Left <= other.Right ||
                    other.Left >= this.Left && other.Left <= this.Right;
            }
            // Two inverted ranges always intersect
            if (this.Outside && other.Outside)
                return true;
            // Regular range intersects with inverted if it's not inside
            if (this.Outside)
                return other.Left <= this.Left || other.Right >= this.Right;
            return this.Left <= other.Left || this.Right >= other.Right;
        }

        public Range Invert()
        {
            return new Range(this.Left, this.Right, !this.Outside);
        }
    }
}
