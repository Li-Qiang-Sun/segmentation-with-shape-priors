using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public interface IShapeEnergyLowerBoundCalculator
    {
        double CalculateLowerBound(Size imageSize, ShapeConstraints shapeConstraints);
    }
}