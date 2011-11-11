using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class VertexConstraint
    {
        private readonly Vector[] corners = new Vector[4];

        private readonly ReadOnlyCollection<Vector> cornersReadOnly;

        public VertexConstraint(Vector coord, double radius)
            : this(coord, coord, radius, radius)
        {
        }

        public VertexConstraint(Vector minCoordInclusive, Vector maxCoordExclusive, double minRadiusInclusive, double maxRadiusExclusive)
        {
            Debug.Assert(minCoordInclusive.X <= maxCoordExclusive.X && minCoordInclusive.Y <= maxCoordExclusive.Y);
            Debug.Assert(minRadiusInclusive >= 0 && minRadiusInclusive <= maxRadiusExclusive);

            this.MinCoord = minCoordInclusive;
            this.MaxCoord = maxCoordExclusive;
            this.MinRadius = minRadiusInclusive;
            this.MaxRadius = maxRadiusExclusive;

            this.corners[0] = new Vector(this.MinCoord.X, this.MinCoord.Y);
            this.corners[1] = new Vector(this.MinCoord.X, this.MaxCoord.Y);
            this.corners[2] = new Vector(this.MaxCoord.X, this.MaxCoord.Y);
            this.corners[3] = new Vector(this.MaxCoord.X, this.MinCoord.Y);

            this.cornersReadOnly = new ReadOnlyCollection<Vector>(corners);
        }

        public Vector MinCoord { get; private set; }

        public Vector MaxCoord { get; private set; }

        public double MinRadius { get; private set; }

        public double MaxRadius { get; private set; }

        public RectangleF CoordRectangle
        {
            get
            {
                return new RectangleF(
                    (float) this.MinCoord.X,
                    (float)this.MinCoord.Y,
                    (float)(this.MaxCoord.X - this.MinCoord.X),
                    (float)(this.MaxCoord.Y - this.MinCoord.Y));
            }
        }

        public double RadiusViolation
        {
            get { return this.MaxRadius - this.MinRadius; }
        }

        public double CoordViolation
        {
            get
            {
                return Math.Max(
                    this.MaxCoord.X - this.MinCoord.X,
                    this.MaxCoord.Y - this.MinCoord.Y);
            }
        }

        public double MiddleRadius
        {
            get { return 0.5 * (MinRadius + MaxRadius); }
        }

        public Vector MiddleCoord
        {
            get { return 0.5 * (this.MinCoord + this.MaxCoord); }
        }

        public bool RadiusSatisfied
        {
            // TODO: make this customizable
            get { return this.RadiusViolation < 1 + 1e-8; }
        }

        public bool CoordSatisfied
        {
            // TODO: make this customizable
            get { return this.CoordViolation <= 1 + 1e-8; }
        }

        public List<VertexConstraint> SplitByRadius()
        {
            Debug.Assert(!this.RadiusSatisfied);

            return new List<VertexConstraint>
            {
                new VertexConstraint(MinCoord, MaxCoord, MinRadius, this.MiddleRadius),
                new VertexConstraint(MinCoord, MaxCoord, this.MiddleRadius, MaxRadius)
            };
        }

        public VertexConstraint Collapse()
        {
            return new VertexConstraint(this.MiddleCoord, this.MiddleRadius);
        }

        public List<VertexConstraint> SplitByCoords()
        {
            Vector middle = this.MiddleCoord;
            List<VertexConstraint> result = new List<VertexConstraint>();
            if (middle.X != MinCoord.X && middle.Y != MinCoord.Y)
                result.Add(new VertexConstraint(MinCoord, middle, MinRadius, MaxRadius));
            if (middle.Y != MinCoord.Y)
                result.Add(new VertexConstraint(new Vector(middle.X, MinCoord.Y), new Vector(MaxCoord.X, middle.Y), MinRadius, MaxRadius));
            if (middle.X != MinCoord.X)
                result.Add(new VertexConstraint(new Vector(MinCoord.X, middle.Y), new Vector(middle.X, MaxCoord.Y), MinRadius, MaxRadius));
            result.Add(new VertexConstraint(middle, MaxCoord, MinRadius, MaxRadius));

            // We should split at least something
            Debug.Assert(result.Count >= 2);
            return result;
        }

        public bool Contains(Point point)
        {
            return this.CoordRectangle.Contains(point);
        }

        public ReadOnlyCollection<Vector> Corners
        {
            get { return this.cornersReadOnly; }
        }

        public Vector? GetClosestPoint(Vector point)
        {
            if (point.X >= MinCoord.X && point.X <= MaxCoord.X)
            {
                if (point.Y <= MinCoord.Y)
                    return new Vector(point.X, MinCoord.Y);
                if (point.Y >= MaxCoord.Y)
                    return new Vector(point.X, MaxCoord.Y);
            }

            if (point.Y >= MinCoord.Y && point.Y <= MaxCoord.Y)
            {
                if (point.X <= MinCoord.X)
                    return new Vector(MinCoord.X, point.Y);
                if (point.X >= MaxCoord.X)
                    return new Vector(MaxCoord.X, point.Y);
            }

            return null;
        }

        public double Area
        {
            get
            {
                return (this.MaxCoord.X - this.MinCoord.X) * (this.MaxCoord.Y - this.MinCoord.Y);
            }
        }

        public override string ToString()
        {
            return String.Format(
                "X in [{0:0.0000}, {1:0.0000}), Y in [{2:0.0000}, {3:0.0000}), R in [{4:0.0000}, {5:0.0000}).",
                this.MinCoord.X,
                this.MaxCoord.X,
                this.MinCoord.Y,
                this.MaxCoord.Y,
                this.MinRadius,
                this.MaxRadius);
        }
    }
}
