namespace Research.GraphBasedShapePrior
{
    public interface IShapeTermsLowerBoundCalculator
    {
        void CalculateShapeTerms(ShapeModel model, ShapeConstraints constraints, Image2D<ObjectBackgroundTerm> result);
    }
}
