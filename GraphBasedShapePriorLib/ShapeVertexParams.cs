using System.Diagnostics;

namespace Research.GraphBasedShapePrior
{
    public class ShapeVertexParams
    {
        public ShapeVertexParams(double radiusToObjectSizeRatio, double radiusRelativeDeviation)
        {
            Debug.Assert(radiusToObjectSizeRatio >= 0);
            Debug.Assert(radiusRelativeDeviation >= 0);

            this.RadiusToObjectSizeRatio = radiusToObjectSizeRatio;
            this.RadiusRelativeDeviation = radiusRelativeDeviation;
        }

        public double RadiusToObjectSizeRatio { get; private set; }

        public double RadiusRelativeDeviation { get; private set; }
    }
}