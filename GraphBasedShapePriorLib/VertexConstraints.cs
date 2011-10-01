using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class VertexConstraints
    {
        private readonly List<Vector> corners = new List<Vector>();

        public VertexConstraints(Point minCoordInclusive, Point maxCoordExclusive, int minRadiusInclusive, int maxRadiusExclusive)
        {
            Debug.Assert(minCoordInclusive.X < maxCoordExclusive.X && minCoordInclusive.Y < maxCoordExclusive.Y);
            Debug.Assert(minRadiusInclusive >= 0 && minRadiusInclusive < maxRadiusExclusive);

            this.MinCoordInclusive = minCoordInclusive;
            this.MaxCoordExclusive = maxCoordExclusive;
            this.MinRadiusInclusive = minRadiusInclusive;
            this.MaxRadiusExclusive = maxRadiusExclusive;

            this.corners.Add(new Vector(this.MinCoordInclusive.X, this.MinCoordInclusive.Y));
            this.corners.Add(new Vector(this.MinCoordInclusive.X, this.MaxCoordExclusive.Y - 1));
            this.corners.Add(new Vector(this.MaxCoordExclusive.X - 1, this.MaxCoordExclusive.Y - 1));
            this.corners.Add(new Vector(this.MaxCoordExclusive.X - 1, this.MinCoordInclusive.Y));
        }

        public Point MinCoordInclusive { get; private set; }

        public Point MaxCoordExclusive { get; private set; }

        public int MinRadiusInclusive { get; private set; }

        public int MaxRadiusExclusive { get; private set; }

        public Rectangle CoordRectangle
        {
            get
            {
                return new Rectangle(
                    this.MinCoordInclusive.X,
                    this.MinCoordInclusive.Y,
                    this.MaxCoordExclusive.X - this.MinCoordInclusive.X,
                    this.MaxCoordExclusive.Y - this.MinCoordInclusive.Y);
            }
        }

        public int RadiusViolation
        {
            get { return this.MaxRadiusExclusive - this.MinRadiusInclusive - 1; }
        }

        public int CoordViolation
        {
            get
            {
                return Math.Max(
                    this.MaxCoordExclusive.X - this.MinCoordInclusive.X - 1,
                    this.MaxCoordExclusive.Y - this.MinCoordInclusive.Y - 1);
            }
        }

        public int MiddleRadius
        {
            get { return (MinRadiusInclusive + MaxRadiusExclusive) / 2; }
        }

        public Point MiddleCoord
        {
            get
            {
                return new Point(
                    (MinCoordInclusive.X + MaxCoordExclusive.X) / 2,
                    (MinCoordInclusive.Y + MaxCoordExclusive.Y) / 2);
            }
        }

        public bool RadiusSatisfied
        {
            get { return this.RadiusViolation == 0; }
        }

        public bool CoordSatisfied
        {
            get { return this.CoordViolation == 0; }
        }

        public List<VertexConstraints> SplitByRadius()
        {
            Debug.Assert(!this.RadiusSatisfied);

            return new List<VertexConstraints>
            {
                new VertexConstraints(MinCoordInclusive, MaxCoordExclusive, MinRadiusInclusive, this.MiddleRadius),
                new VertexConstraints(MinCoordInclusive, MaxCoordExclusive, this.MiddleRadius, MaxRadiusExclusive)
            };
        }

        public VertexConstraints Collapse()
        {
            int radius = this.MiddleRadius;
            Point coord = this.MiddleCoord;
            return new VertexConstraints(
                coord,
                new Point(coord.X + 1, coord.Y + 1), 
                radius,
                radius + 1);
        }

        public List<VertexConstraints> SplitByCoords()
        {
            Debug.Assert(!this.CoordSatisfied);

            Point middle = this.MiddleCoord;
            List<VertexConstraints> result = new List<VertexConstraints>();
            if (middle.X != MinCoordInclusive.X && middle.Y != MinCoordInclusive.Y)
                result.Add(new VertexConstraints(MinCoordInclusive, middle, MinRadiusInclusive, MaxRadiusExclusive));
            if (middle.Y != MinCoordInclusive.Y)
                result.Add(new VertexConstraints(new Point(middle.X, MinCoordInclusive.Y), new Point(MaxCoordExclusive.X, middle.Y), MinRadiusInclusive, MaxRadiusExclusive));
            if (middle.X != MinCoordInclusive.X)
                result.Add(new VertexConstraints(new Point(MinCoordInclusive.X, middle.Y), new Point(middle.X, MaxCoordExclusive.Y), MinRadiusInclusive, MaxRadiusExclusive));
            result.Add(new VertexConstraints(middle, MaxCoordExclusive, MinRadiusInclusive, MaxRadiusExclusive));

            // We should split at least something
            Debug.Assert(result.Count >= 2);
            return result;
        }

        public bool Contains(Point point)
        {
            return this.CoordRectangle.Contains(point);
        }

        public IEnumerable<Vector> Iterate()
        {
            for (int x = this.MinCoordInclusive.X; x <= this.MaxCoordExclusive.X; ++x)
                for (int y = this.MinCoordInclusive.Y; y <= this.MaxCoordExclusive.Y; ++y)
                    yield return new Vector(x, y);
        }

        public IEnumerable<Vector> IterateBorder()
        {
            // Top side
            for (int x = this.MinCoordInclusive.X; x < this.MaxCoordExclusive.X; ++x)
                yield return new Vector(x, this.MinCoordInclusive.Y);

            // Right side
            for (int y = this.MinCoordInclusive.Y; y < this.MaxCoordExclusive.Y; ++y)
                yield return new Vector(this.MaxCoordExclusive.X - 1, y);

            // Bottom side
            for (int x = this.MaxCoordExclusive.X - 1; x >= this.MinCoordInclusive.X; --x)
                yield return new Vector(x, this.MaxCoordExclusive.Y - 1);

            // Left side
            for (int y = this.MaxCoordExclusive.Y - 1; y >= this.MinCoordInclusive.Y; --y)
                yield return new Vector(this.MinCoordInclusive.X, y);
        }

        /// <summary>
        /// Iterate through all four corners.
        /// </summary>
        /// <returns>Enumerable containing all four corners.</returns>
        public IEnumerable<Vector> IterateCorners()
        {
            return this.corners;
        }

        /// <summary>
        /// Iterate through all four corners and also point closest to given (if given point is outside).
        /// </summary>
        /// <param name="point">Point closest to which must be returned.</param>
        /// <returns>Enumerable containing all four corners and also point closest to the given one (if given point is outside and does not match with one of the corners).</returns>
        public IEnumerable<Vector> IterateCornersAndClosestPoint(Point point)
        {
            List<Vector> result = new List<Vector>(this.corners);

            if (point.X >= MinCoordInclusive.X && point.X < MaxCoordExclusive.X)
            {
                if (point.Y < MinCoordInclusive.Y)
                    result.Add(new Vector(point.X, MinCoordInclusive.Y));
                else if (point.Y >= MaxCoordExclusive.Y)
                    result.Add(new Vector(point.X, MaxCoordExclusive.Y - 1));
            }
            else if (point.Y >= MinCoordInclusive.Y && point.Y < MaxCoordExclusive.Y)
            {
                if (point.X < MinCoordInclusive.X)
                    result.Add(new Vector(MinCoordInclusive.X, point.Y));
                else if (point.X >= MaxCoordExclusive.X)
                    result.Add(new Vector(MaxCoordExclusive.X - 1, point.Y));
            }

            return result;
        }

        public int Area
        {
            get
            {
                return (this.MaxCoordExclusive.X - this.MinCoordInclusive.X) * (this.MaxCoordExclusive.Y - this.MinCoordInclusive.Y);
            }
        }

        public override string ToString()
        {
            return String.Format(
                "X in [{0}, {1}), Y in [{2}, {3}), R in [{4}, {5}).",
                this.MinCoordInclusive.X,
                this.MaxCoordExclusive.X,
                this.MinCoordInclusive.Y,
                this.MaxCoordExclusive.Y,
                this.MinRadiusInclusive,
                this.MaxRadiusExclusive);
        }
    }
}
