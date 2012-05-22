using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class VertexConstraints
    {
        private readonly Vector[] corners = new Vector[4];

        private readonly ReadOnlyCollection<Vector> cornersReadOnly;

        public VertexConstraints(Vector coord)
            : this(coord, coord)
        {
        }

        public VertexConstraints(Vector minCoord, Vector maxCoord)
        {
            Debug.Assert(minCoord.X <= maxCoord.X && minCoord.Y <= maxCoord.Y);

            this.MinCoord = minCoord;
            this.MaxCoord = maxCoord;

            this.corners[0] = new Vector(this.MinCoord.X, this.MinCoord.Y);
            this.corners[1] = new Vector(this.MinCoord.X, this.MaxCoord.Y);
            this.corners[2] = new Vector(this.MaxCoord.X, this.MaxCoord.Y);
            this.corners[3] = new Vector(this.MaxCoord.X, this.MinCoord.Y);

            this.cornersReadOnly = new ReadOnlyCollection<Vector>(corners);
        }

        public Vector MinCoord { get; private set; }

        public Vector MaxCoord { get; private set; }

        public Range XRange
        {
            get { return new Range(MinCoord.X, MaxCoord.X); }
        }

        public Range YRange
        {
            get { return new Range(MinCoord.Y, MaxCoord.Y); }
        }

        public RectangleF CoordRectangle
        {
            get
            {
                return new RectangleF(
                    (float)this.MinCoord.X,
                    (float)this.MinCoord.Y,
                    (float)(this.MaxCoord.X - this.MinCoord.X),
                    (float)(this.MaxCoord.Y - this.MinCoord.Y));
            }
        }

        public double Freedom
        {
            get
            {
                return Math.Max(
                    this.MaxCoord.X - this.MinCoord.X,
                    this.MaxCoord.Y - this.MinCoord.Y);
            }
        }

        public Vector MiddleCoord
        {
            get { return 0.5 * (this.MinCoord + this.MaxCoord); }
        }

        public VertexConstraints Collapse()
        {
            return new VertexConstraints(this.MiddleCoord);
        }

        public List<VertexConstraints> Split()
        {
            // We'll use it to split into non-intersecting sets
            const double eps = 1e-4;

            Vector middle = this.MiddleCoord;
            List<VertexConstraints> result = new List<VertexConstraints>();
            if (middle.X != MinCoord.X && middle.Y != MinCoord.Y)
                result.Add(new VertexConstraints(MinCoord, new Vector(middle.X - eps, middle.Y - eps)));
            if (middle.Y != MinCoord.Y)
                result.Add(new VertexConstraints(new Vector(middle.X + eps, MinCoord.Y), new Vector(MaxCoord.X, middle.Y - eps)));
            if (middle.X != MinCoord.X)
                result.Add(new VertexConstraints(new Vector(MinCoord.X, middle.Y + eps), new Vector(middle.X - eps, MaxCoord.Y)));
            result.Add(new VertexConstraints(new Vector(middle.X + eps, middle.Y + eps), MaxCoord));

            // We should split at least something
            Debug.Assert(result.Count >= 2);
            return result;
        }

        public bool Contains(Vector vector)
        {
            return
                vector.X >= this.MinCoord.X &&
                vector.Y >= this.MinCoord.Y &&
                vector.X <= this.MaxCoord.X &&
                vector.Y <= this.MaxCoord.Y;
        }

        public ReadOnlyCollection<Vector> Corners
        {
            get { return this.cornersReadOnly; }
        }

        public double Area
        {
            get { return (this.MaxCoord.X - this.MinCoord.X) * (this.MaxCoord.Y - this.MinCoord.Y); }
        }

        public override string ToString()
        {
            return String.Format(
                "X in [{0:0.0000}, {1:0.0000}), Y in [{2:0.0000}, {3:0.0000})",
                this.MinCoord.X,
                this.MaxCoord.X,
                this.MinCoord.Y,
                this.MaxCoord.Y);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            VertexConstraints objCasted = (VertexConstraints) obj;
            return objCasted.MinCoord == this.MinCoord && objCasted.MaxCoord == this.MaxCoord;
        }

        public override int GetHashCode()
        {
            return this.MinCoord.GetHashCode() ^ this.MaxCoord.GetHashCode();
        }

        public static bool operator ==(VertexConstraints left, VertexConstraints right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);
            return left.Equals(right);
        }

        public static bool operator !=(VertexConstraints left, VertexConstraints right)
        {
            return !(left == right);
        }
    }
}
