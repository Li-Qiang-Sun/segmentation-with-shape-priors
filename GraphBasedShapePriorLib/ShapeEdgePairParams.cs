using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Research.GraphBasedShapePrior
{
    [DataContract]
    public class ShapeEdgePairParams
    {
        public ShapeEdgePairParams(double meanAngle, double meanLengthRatio, double angleDeviation, double lengthDiffDeviation)
        {
            Debug.Assert(meanAngle >= -Math.PI && meanAngle <= Math.PI);
            Debug.Assert(meanLengthRatio >= 0);
            Debug.Assert(angleDeviation >= 0);
            Debug.Assert(lengthDiffDeviation >= 0);

            this.MeanAngle = meanAngle;
            this.MeanLengthRatio = meanLengthRatio;
            this.AngleDeviation = angleDeviation;
            this.LengthDiffDeviation = lengthDiffDeviation;
        }

        public ShapeEdgePairParams Swap()
        {
            return new ShapeEdgePairParams(-this.MeanAngle, 1.0 / this.MeanLengthRatio, this.AngleDeviation, this.LengthDiffDeviation);
        }

        /// <summary>
        /// Gets mean angle from first to second edge.
        /// </summary>
        /// <remarks>
        /// For angle calculations we consider edge as v2-v1 vector.
        /// </remarks>
        [DataMember]
        public double MeanAngle { get; private set; }

        /// <summary>
        /// Gets first edge to second edge mean length ratio.
        /// </summary>
        [DataMember]
        public double MeanLengthRatio { get; private set; }

        /// <summary>
        /// Gets angle constraint softness.
        /// </summary>
        [DataMember]
        public double AngleDeviation { get; private set; }

        /// <summary>
        /// Gets length constraint softness.
        /// </summary>
        [DataMember]
        public double LengthDiffDeviation { get; private set; }
    }
}