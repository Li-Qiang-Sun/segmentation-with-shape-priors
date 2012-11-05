using System;
using System.Runtime.Serialization;

namespace Research.GraphBasedShapePrior
{
    [DataContract]
    public class ShapeEdgeParams
    {
        [DataMember]
        private double widthToEdgeLengthRatio;

        [DataMember]
        private double widthToEdgeLengthRatioDeviation;
        
        public ShapeEdgeParams(double widthToEdgeLengthRatio, double widthToEdgeLengthRatioDeviation)
        {
            if (widthToEdgeLengthRatio <= 0)
                throw new ArgumentOutOfRangeException("widthToEdgeLengthRatio", "Width to edge length ratio should be positive.");
            if (widthToEdgeLengthRatioDeviation <= 0)
                throw new ArgumentOutOfRangeException("widthToEdgeLengthRatioDeviation", "Relative width deviation should be positive.");

            this.widthToEdgeLengthRatio = widthToEdgeLengthRatio;
            this.widthToEdgeLengthRatioDeviation = widthToEdgeLengthRatioDeviation;
        }
        
        public double WidthToEdgeLengthRatio
        {
            get { return this.widthToEdgeLengthRatio; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.widthToEdgeLengthRatio = value;
            }
        }
        
        public double WidthToEdgeLengthRatioDeviation
        {
            get { return this.widthToEdgeLengthRatioDeviation; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.widthToEdgeLengthRatioDeviation = value;
            }
        }
    }
}