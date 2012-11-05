using System;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior
{
    public class SimpleSegmentationAlgorithm : SegmentationAlgorithmBase
    {
        public Shape Shape { get; set; }
        
        protected override SegmentationSolution SegmentCurrentImage()
        {
            if (this.Shape != null && this.Shape.Structure != this.ShapeModel.Structure)
                throw new InvalidOperationException("Specified shape differs in structure from shape model.");

            double segmentationEnergy = this.ImageSegmentator.SegmentImageWithShapeTerms(
                (x, y) => this.Shape == null ? ObjectBackgroundTerm.Zero : CalculateShapeTerms(new Vector(x, y)));
            double shapeEnergy = this.Shape == null ? 0 : this.ShapeModel.CalculateEnergy(this.Shape);
            double totalEnergy = segmentationEnergy + shapeEnergy * this.ShapeEnergyWeight;
            DebugConfiguration.WriteImportantDebugText(
                "Solution energy: {0:0.0000} ({1:0.0000} + {2:0.0000} * {3:0.0000})",
                totalEnergy,
                segmentationEnergy,
                this.ShapeEnergyWeight,
                shapeEnergy);

            return new SegmentationSolution(this.Shape, this.ImageSegmentator.GetLastSegmentationMask(), totalEnergy);
        }

        private ObjectBackgroundTerm CalculateShapeTerms(Vector point)
        {
            double objectTerm = this.ShapeModel.CalculateObjectPenalty(this.Shape, point);
            double backgroundTerm = this.ShapeModel.CalculateBackgroundPenalty(this.Shape, point);
            return new ObjectBackgroundTerm(objectTerm, backgroundTerm);
        }
    }
}
