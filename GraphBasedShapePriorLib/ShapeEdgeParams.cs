using System.Diagnostics;

namespace Research.GraphBasedShapePrior
{
    public class ShapeEdgeParams
    {
        public ShapeEdgeParams(double widthToEdgeLengthRatio, double relativeWidthDeviation)
        {
            Debug.Assert(widthToEdgeLengthRatio >= 0);
            Debug.Assert(relativeWidthDeviation >= 0);

            this.WidthToEdgeLengthRatio = widthToEdgeLengthRatio;
            this.RelativeWidthDeviation = relativeWidthDeviation;
        }

        public double WidthToEdgeLengthRatio { get; private set; }

        public double RelativeWidthDeviation { get; private set; }
    }
}