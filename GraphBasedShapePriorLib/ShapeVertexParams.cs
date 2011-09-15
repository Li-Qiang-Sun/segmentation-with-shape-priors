using System.Diagnostics;

namespace Research.GraphBasedShapePrior
{
    public class ShapeVertexParams
    {
        public ShapeVertexParams(double lengthToObjectSizeRatio, double radiusRelativeDeviation)
        {
            Debug.Assert(lengthToObjectSizeRatio >= 0);
            Debug.Assert(radiusRelativeDeviation >= 0);

            this.LengthToObjectSizeRatio = lengthToObjectSizeRatio;
            this.RadiusRelativeDeviation = radiusRelativeDeviation;
        }

        public double LengthToObjectSizeRatio { get; private set; }

        public double RadiusRelativeDeviation { get; private set; }
    }
}