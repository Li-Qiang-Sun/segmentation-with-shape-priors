using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MicrosoftResearch.Infer.Maths;
using Research.GraphBasedShapePrior.Util;

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

        public double Freedom
        {
            get { return this.MaxWidth - this.MinWidth; }
        }

        public double MiddleWidth
        {
            get { return 0.5 * (this.MinWidth + this.MaxWidth); }
        }

        public EdgeConstraints Collapse()
        {
            return new EdgeConstraints(this.MiddleWidth);
        }

        public EdgeConstraints CollapseRandomly()
        {
            return new EdgeConstraints(this.MinWidth + this.Freedom * Rand.Double());
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

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            EdgeConstraints objCasted = (EdgeConstraints)obj;
            return objCasted.MinWidth == this.MinWidth && objCasted.MaxWidth == this.MaxWidth;
        }

        public override int GetHashCode()
        {
            return this.MinWidth.GetHashCode() ^ this.MaxWidth.GetHashCode();
        }

        public static bool operator ==(EdgeConstraints left, EdgeConstraints right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);
            return left.Equals(right);
        }

        public static bool operator !=(EdgeConstraints left, EdgeConstraints right)
        {
            return !(left == right);
        }
    }
}
