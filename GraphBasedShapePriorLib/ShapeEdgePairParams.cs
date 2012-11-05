using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Research.GraphBasedShapePrior
{
    [DataContract]
    public class ShapeEdgePairParams
    {
        [DataMember]
        private double meanAngle;

        [DataMember]
        private double meanLengthRatio;

        [DataMember]
        private double angleDeviation;

        [DataMember]
        private double lengthDiffDeviation;
        
        public ShapeEdgePairParams(double meanAngle, double meanLengthRatio, double angleDeviation, double lengthDiffDeviation)
        {
            if (meanAngle < -Math.PI || meanAngle > Math.PI)
                throw new ArgumentOutOfRangeException("meanAngle", "Mean angle should be in [-pi, pi] range.");
            if (meanLengthRatio <= 0)
                throw new ArgumentOutOfRangeException("meanLengthRatio", "Mean length ratio should be positive.");
            if (angleDeviation <= 0)
                throw new ArgumentOutOfRangeException("angleDeviation", "Angle deviation should be positive.");
            if (lengthDiffDeviation <= 0)
                throw new ArgumentOutOfRangeException("lengthDiffDeviation", "Length diff deviation should be positive.");

            this.meanAngle = meanAngle;
            this.meanLengthRatio = meanLengthRatio;
            this.angleDeviation = angleDeviation;
            this.lengthDiffDeviation = lengthDiffDeviation;
        }

        public ShapeEdgePairParams Swap()
        {
            return new ShapeEdgePairParams(-this.MeanAngle, 1.0 / this.MeanLengthRatio, this.AngleDeviation, this.LengthDiffDeviation);
        }

        public double MeanAngle
        {
            get { return this.meanAngle; }
            set
            {
                if (value < -Math.PI || value > Math.PI)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be in [-pi, pi] range.");
                this.meanAngle = value;
            }
        }
        
        public double MeanLengthRatio
        {
            get { return this.meanLengthRatio; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.meanLengthRatio = value;
            }
        }

        public double AngleDeviation
        {
            get { return this.angleDeviation; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.angleDeviation = value;
            }
        }
        
        public double LengthDiffDeviation
        {
            get { return this.lengthDiffDeviation; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.lengthDiffDeviation = value;
            }
        }
    }
}