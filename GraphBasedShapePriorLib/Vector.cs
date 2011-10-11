using System;
using System.Diagnostics;

namespace Research.GraphBasedShapePrior
{
    public struct Vector
    {
        public double X { get; set; }

        public double Y { get; set; }

        public Vector(double x, double y)
            : this()
        {
            this.X = x;
            this.Y = y;
        }

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

        public double DistanceToSegmentSquared(Vector segmentStart, Vector segmentEnd)
        {
            double distanceSqr, alpha;
            this.DistanceToSegmentSquared(segmentStart, segmentEnd, out distanceSqr, out alpha);
            return distanceSqr;
        }

        public void DistanceToSegmentSquared(Vector segmentStart, Vector segmentEnd, out double distanceSqr, out double alpha)
        {
            Debug.Assert(segmentStart != segmentEnd);

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

        public Vector GetNormalized()
        {
            return this * (1.0 / this.Length);
        }

        public double DistanceToCircle(Circle circle)
        {
            double distToCenter = (this - circle.Center).Length;
            return Math.Abs(distToCenter - circle.Radius);
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
            if (angle < 0)
                angle = Math.PI * 2 + angle;
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
    }
}
