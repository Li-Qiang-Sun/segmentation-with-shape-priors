using System;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior
{
    public class SimpleSegmentationAlgorithm : SegmentationAlgorithmBase
    {
        public Shape Shape { get; set; }
        
        protected override Image2D<bool> SegmentCurrentImage()
        {
            if (this.Shape == null)
                throw new InvalidOperationException("Valid shape should be provided before running segmentation.");
            if (this.Shape.Structure != this.ShapeModel.Structure)
                throw new InvalidOperationException("Specified shape differs in structure from shape model.");

            double segmentationEnergy = this.ImageSegmentator.SegmentImageWithShapeTerms((x, y) => CalculateShapeTerms(new Vector(x, y)));
            double shapeEnergy = this.ShapeModel.CalculateEnergy(this.Shape);
            double totalEnergy = segmentationEnergy + shapeEnergy * this.ShapeEnergyWeight;
            DebugConfiguration.WriteImportantDebugText(
                "Solution energy: {0:0.0000} ({1:0.0000} + {2:0.0000} * {3:0.0000})",
                totalEnergy,
                segmentationEnergy,
                this.ShapeEnergyWeight,
                shapeEnergy);

            // TODO: remove me
            Image2D.SaveToFile(this.ImageSegmentator.GetLastShapeTerms(), -10, 10, "shape_terms.png");

            return this.ImageSegmentator.GetLastSegmentationMask();
        }

        private ObjectBackgroundTerm CalculateShapeTerms(Vector point)
        {
            double objectTerm = this.ShapeModel.GetObjectPenalty(this.Shape, point);
            double backgroundTerm = this.ShapeModel.GetBackgroundPenalty(this.Shape, point);
            return new ObjectBackgroundTerm(objectTerm, backgroundTerm);
        }
    }
}
