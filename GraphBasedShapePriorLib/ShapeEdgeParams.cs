using System;
using System.Runtime.Serialization;

namespace Research.GraphBasedShapePrior
{
    [DataContract]
    public class ShapeEdgeParams
    {
        public ShapeEdgeParams(double widthToEdgeLengthRatio, double widthToEdgeLengthRatioDeviation)
        {
            if (widthToEdgeLengthRatio <= 0)
                throw new ArgumentOutOfRangeException("widthToEdgeLengthRatio", "Width to edge length ratio should be positive.");
            if (widthToEdgeLengthRatioDeviation <= 0)
                throw new ArgumentOutOfRangeException("widthToEdgeLengthRatioDeviation", "Relative width deviation should be positive.");

            this.WidthToEdgeLengthRatio = widthToEdgeLengthRatio;
            this.WidthToEdgeLengthRatioDeviation = widthToEdgeLengthRatioDeviation;
        }

        [DataMember]
        public double WidthToEdgeLengthRatio { get; private set; }

        [DataMember]
        public double WidthToEdgeLengthRatioDeviation { get; private set; }
    }
}