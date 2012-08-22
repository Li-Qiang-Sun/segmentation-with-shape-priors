using System.Collections.Generic;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior
{
    public interface IShapeFittingStrategy
    {
        List<Shape> FitShapes(ShapeModel shapeModel, Image2D<bool> mask);
    }
}