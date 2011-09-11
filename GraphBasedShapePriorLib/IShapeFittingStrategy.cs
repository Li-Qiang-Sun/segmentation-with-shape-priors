using System.Collections.Generic;

namespace Research.GraphBasedShapePrior
{
    public interface IShapeFittingStrategy
    {
        List<Shape> FitShapes(ShapeModel shapeModel, Image2D<bool> mask);
    }
}