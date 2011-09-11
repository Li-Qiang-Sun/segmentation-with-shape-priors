using System.Diagnostics;

namespace Research.GraphBasedShapePrior
{
    public class ShapeVertexParams
    {
        public ShapeVertexParams(double lengthToObjectSizeRatio, double deviation)
        {
            Debug.Assert(lengthToObjectSizeRatio >= 0);
            Debug.Assert(deviation >= 0);

            this.LengthToObjectSizeRatio = lengthToObjectSizeRatio;
            this.RadiusDeviation = deviation;
        }

        public double LengthToObjectSizeRatio { get; private set; }

        public double RadiusDeviation { get; private set; }
    }
}