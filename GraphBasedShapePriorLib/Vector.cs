using System;
using System.Diagnostics;

namespace Research.GraphBasedShapePrior
{
    public struct Vector
    {
        public static readonly Vector Zero = new Vector(0, 0);

        public static readonly Vector UnitX = new Vector(1, 0);

        public static readonly Vector UnitY = new Vector(0, 1);
        
        public Vector(double x, double y)
            : this()
        {
            this.X = x;
            this.Y = y;
        }

        public double X { get; set; }

        public double Y { get; set; }

        public double Length
        {
            get { return Math.Sqrt(this.LengthSquared); }
        }

        public double LengthSquared
        {
            get { return this.X * this.X + this.Y * this.Y; }
        }

        public static Vector operator +(Vector vector1, Vector vector2)
        {
            return new Vector(vector1.X + vector2.X, vector1.Y + vector2.Y);
        }

        public static Vector operator -(Vector vector1, Vector vector2)
        {
            return new Vector(vector1.X - vector2.X, vector1.Y - vector2.Y);
        }

        public static Vector operator -(Vector vector)
        {
            return new Vector(-vector.X, -vector.Y);
        }

        public static Vector operator *(Vector vector, double alpha)
        {
            return new Vector(vector.X * alpha, vector.Y * alpha);
        }

        public static Vector operator *(double alpha, Vector vector)
        {
            return vector * alpha;
        }

        public static bool operator ==(Vector vector1, Vector vector2)
        {
            return vector1.X == vector2.X && vector1.Y == vector2.Y;
        }

        public static bool operator !=(Vector vector1, Vector vector2)
        {
            return !(vector1 == vector2);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != typeof(Vector)) return false;
            return this == (Vector)obj;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (this.X.GetHashCode() * 397) ^ this.Y.GetHashCode();
            }
        }

        public double DistanceToPointSquared(Vector vector)
        {
            return (vector - this).LengthSquared;
        }

        public double DistanceToPoint(Vector vector)
        {
            return Math.Sqrt(this.DistanceToPointSquared(vector));
        }

        public double DistanceToSegment(Vector segmentStart, Vector segmentEnd)
        {
            return Math.Sqrt(this.DistanceToSegmentSquared(segmentStart, segmentEnd));
        }

        public void DistanceToSegment(Vector segmentStart, Vector segmentEnd, out double distance, out double alpha)
        {
            double distanceSqr;
            this.DistanceToSegmentSquared(segmentStart, segmentEnd, out distanceSqr, out alpha);
            distance = Math.Sqrt(distanceSqr);
        }

        public double DistanceToSegmentSquared(Vector segmentStart, Vector segmentEnd)
        {
            double distanceSqr, alpha;
            this.DistanceToSegmentSquared(segmentStart, segmentEnd, out distanceSqr, out alpha);
            return distanceSqr;
        }

        public void DistanceToSegmentSquared(Vector segmentStart, Vector segmentEnd, out double distanceSqr, out double alpha)
        {
            Vector v = segmentEnd - segmentStart;
            Vector p = this - segmentStart;

            alpha = DotProduct(v, p) / v.LengthSquared;
            if (alpha >= 0 && alpha <= 1)
                distanceSqr = ((segmentStart + alpha * v) - this).LengthSquared;
            else if (alpha < 0)
                distanceSqr = p.LengthSquared;
            else
                distanceSqr = (this - segmentEnd).LengthSquared;
        }

        public double DistanceToLine(Vector point1, Vector point2)
        {
            return Math.Sqrt(this.DistanceToLineSquared(point1, point2));
        }

        public double DistanceToLineSquared(Vector point1, Vector point2)
        {
            double distanceSqr, alpha;
            this.DistanceToLineSquared(point1, point2, out distanceSqr, out alpha);
            return distanceSqr;
        }

        public void DistanceToLineSquared(Vector point1, Vector point2, out double distanceSqr, out double alpha)
        {
            Vector v = point2 - point1;
            Vector p = this - point1;
            alpha = DotProduct(v, p) / v.LengthSquared;
            distanceSqr = ((point1 + alpha * v) - this).LengthSquared;
        }

        public Vector GetNormalized()
        {
            return this * (1.0 / this.Length);
        }

        public double DistanceToCircleArea(Circle circle)
        {
            return Math.Max((this - circle.Center).Length - circle.Radius, 0);
        }

        public static double AngleBetween(Vector vector1, Vector vector2)
        {
            if (vector1 == vector2)
                return 0;
            vector1 = vector1.GetNormalized();
            vector2 = vector2.GetNormalized();
            double cos = DotProduct(vector1, vector2);
            double sin = CrossProduct(vector1, vector2);
            double angle = Math.Atan2(sin, cos);
            return angle;
        }

        public static double DotProduct(Vector vector1, Vector vector2)
        {
            return vector1.X * vector2.X + vector1.Y * vector2.Y;
        }

        public static double CrossProduct(Vector vector1, Vector vector2)
        {
            return vector1.X * vector2.Y - vector1.Y * vector2.X;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", this.X, this.Y);
        }
    }
}
