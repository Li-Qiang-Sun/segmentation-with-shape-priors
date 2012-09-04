using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MicrosoftResearch.Infer.Maths;

namespace Research.GraphBasedShapePrior.Util
{
    /// <summary>
    /// Proxy for all the random number generation. Can be used to set fixed random seed globally.
    /// </summary>
    public static class Random
    {
        public static void SetSeed(int seed)
        {
            Rand.Restart(seed);
        }

        public static double Double()
        {
            return Rand.Double();
        }

        public static double Double(double start, double end)
        {
            return start + Rand.Double() * (end - start);
        }

        public static int Int()
        {
            return Rand.Int();
        }

        public static int Int(int startInclusive, int endExclusive)
        {
            return Rand.Int(startInclusive, endExclusive);
        }

        public static int Int(int endExclusive)
        {
            return Rand.Int(endExclusive);
        }

        public static double Normal(double mean, double stddev)
        {
            return Rand.Normal(mean, stddev);
        }

        public static double Normal(double mean, double stddev, double lowerBound)
        {
            return stddev * Rand.NormalGreaterThan(lowerBound - mean) + mean;
        }
    }
}
