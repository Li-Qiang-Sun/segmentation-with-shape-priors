using System;
using System.Diagnostics;

namespace Research.GraphBasedShapePrior
{
    public static class MathHelper
    {
        public static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        public static double ToDegrees(double radians)
        {
            return radians * 180 / Math.PI;
        }

        public static double Trunc(double value, double min, double max)
        {
            return Math.Min(max, Math.Max(value, min));
        }

        public static double Sqr(double x)
        {
            return x * x;
        }

        public static double LogInf(double x)
        {
            Debug.Assert(x >= 0);
            const double threshold = 1e-15;
            if (x < threshold)
                return Math.Log(threshold);
            return Math.Log(x);
        }

        public static Polygon SolvePulleyProblem(Circle circle1, Circle circle2)
        {
            if (circle1.Radius < circle2.Radius)
                Helper.Swap(ref circle1, ref circle2);

            Debug.Assert(!circle1.Contains(circle2));

            double edgeLength = (circle1.Center - circle2.Center).Length;
            double cosAngle = (circle1.Radius - circle2.Radius) / edgeLength;
            double angle = Math.Acos(cosAngle);
            Debug.Assert(angle >= 0 && angle <= Math.PI / 2);

            double lineAngle = Math.Atan2(circle2.Center.Y - circle1.Center.Y, circle2.Center.X - circle1.Center.X);
            double cosPlusPlus = Math.Cos(lineAngle + angle);
            double sinPlusPlus = Math.Sin(lineAngle + angle);
            double cosPlusMinus = Math.Cos(lineAngle - angle);
            double sinPlusMinus = Math.Sin(lineAngle - angle);
            
            Vector line1Point1 = new Vector(
                circle1.Center.X + circle1.Radius * cosPlusPlus,
                circle1.Center.Y + circle1.Radius * sinPlusPlus);
            Vector line2Point1 = new Vector(
                circle1.Center.X + circle1.Radius * cosPlusMinus,
                circle1.Center.Y + circle1.Radius * sinPlusMinus);
            Vector line1Point2 = new Vector(
                circle2.Center.X + circle2.Radius * cosPlusPlus,
                circle2.Center.Y + circle2.Radius * sinPlusPlus);
            Vector line2Point2 = new Vector(
                circle2.Center.X + circle2.Radius * cosPlusMinus,
                circle2.Center.Y + circle2.Radius * sinPlusMinus);

            return Polygon.FromPoints(line1Point1, line1Point2, line2Point2, line2Point1);
        }
    }
}
