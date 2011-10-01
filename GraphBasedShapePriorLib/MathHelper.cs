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
            const double threshold = 1e-20;
            if (x < threshold)
                return Math.Log(threshold);
            return Math.Log(x);
        }
    }
}
