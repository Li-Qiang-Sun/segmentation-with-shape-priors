using System;
using System.Diagnostics;

namespace Research.GraphBasedShapePrior
{
    public class ShapeEdgePairParams
    {
        public ShapeEdgePairParams(double meanAngle, double lengthRatio, double angleDeviation, double lengthDeviation)
        {
            Debug.Assert(meanAngle >= -Math.PI && meanAngle <= Math.PI);
            Debug.Assert(lengthRatio >= 0);
            Debug.Assert(angleDeviation >= 0);
            Debug.Assert(lengthDeviation >= 0);

            this.MeanAngle = meanAngle;
            this.LengthRatio = lengthRatio;
            this.AngleDeviation = angleDeviation;
            this.LengthDeviation = lengthDeviation;
        }

        public ShapeEdgePairParams Swap()
        {
            return new ShapeEdgePairParams(-this.MeanAngle, 1.0 / this.LengthRatio, this.AngleDeviation, this.LengthDeviation);
        }

        /// <summary>
        /// Gets mean angle from first to second edge.
        /// </summary>
        /// <remarks>
        /// For angle calculations we consider edge as v2-v1 vector.
        /// </remarks>
        public double MeanAngle { get; private set; }

        /// <summary>
        /// Gets first edge to second edge length ratio (mean).
        /// </summary>
        public double LengthRatio { get; private set; }

        /// <summary>
        /// Gets angle constraint softness.
        /// </summary>
        public double AngleDeviation { get; private set; }

        /// <summary>
        /// Gets length constraint softness.
        /// </summary>
        public double LengthDeviation { get; private set; }
    }
}