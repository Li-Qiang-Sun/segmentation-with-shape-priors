using System;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior
{
    public class AnnealingSegmentationAlgorithm : SegmentationAlgorithmBase
    {
        public SimulatedAnnealingMinimizer<Shape> SolutionFitter { get; private set; }

        public ShapeMutator ShapeMutator { get; private set; }

        public Func<Shape, double> AdditionalShapePenalty { get; set; }

        public Shape StartShape { get; set; }

        public AnnealingSegmentationAlgorithm()
        {
            this.ShapeMutator = new ShapeMutator();

            this.SolutionFitter = new SimulatedAnnealingMinimizer<Shape>();
            this.SolutionFitter.MaxIterations = 5000;
            this.SolutionFitter.MaxStallingIterations = 1000;
            this.SolutionFitter.ReannealingInterval = 500;
            this.SolutionFitter.ReportRate = 5;
            this.SolutionFitter.StartTemperature = 1000;
        }
        
        protected override SegmentationSolution SegmentCurrentImage()
        {
            Shape startShape = this.StartShape;
            if (startShape == null)
            {
                startShape = this.ShapeModel.FitMeanShape(
                    this.ImageSegmentator.ImageSize.Width, this.ImageSegmentator.ImageSize.Height);
            }

            Shape solutionShape = this.SolutionFitter.Run(startShape, this.MutateSolution, s => this.CalcObjective(s, false));
            double solutionEnergy = CalcObjective(solutionShape, true);
            Image2D<bool> solutionMask = this.ImageSegmentator.GetLastSegmentationMask();
            return new SegmentationSolution(solutionShape, solutionMask, solutionEnergy);
        }

        private double CalcObjective(Shape shape, bool report)
        {
            double shapeEnergy = this.ShapeModel.CalculateEnergy(shape);
            double labelingEnergy = this.ImageSegmentator.SegmentImageWithShapeTerms(
                (x, y) => this.ShapeModel.CalculatePenalties(shape, new Vector(x, y)));
            double energy = shapeEnergy * this.ShapeEnergyWeight + labelingEnergy;
            double additionalPenalty = this.AdditionalShapePenalty == null ? 0 : this.AdditionalShapePenalty(shape);
            double totalEnergy = energy + additionalPenalty;

            if (report)
            {
                DebugConfiguration.WriteImportantDebugText(
                    "Solution energy: {0:0.0000} ({1:0.0000} + {2:0.0000} * {3:0.0000} + {4:0.0000})",
                    totalEnergy,
                    labelingEnergy,
                    this.ShapeEnergyWeight,
                    shapeEnergy,
                    additionalPenalty);
            }

            return totalEnergy;
        }

        private Shape MutateSolution(Shape shape, double temperature)
        {
            return this.ShapeMutator.MutateShape(
                shape, this.ShapeModel, this.ImageSegmentator.ImageSize, temperature / this.SolutionFitter.StartTemperature);
        }
    }
}
