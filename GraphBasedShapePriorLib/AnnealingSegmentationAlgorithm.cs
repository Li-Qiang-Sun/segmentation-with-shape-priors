using System;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior
{
    public class AnnealingSegmentationAlgorithm : SegmentationAlgorithmBase
    {
        public SimulatedAnnealingMinimizer<SegmentationSolution> SolutionFitter { get; private set; }

        public ShapeMutator ShapeMutator { get; private set; }

        public AnnealingSegmentationAlgorithm()
        {
            this.ShapeMutator = new ShapeMutator();
            
            this.SolutionFitter = new SimulatedAnnealingMinimizer<SegmentationSolution>();
            this.SolutionFitter.MaxIterations = 5000;
            this.SolutionFitter.MaxStallingIterations = 1000;
            this.SolutionFitter.ReannealingInterval = 500;
            this.SolutionFitter.ReportRate = 5;
            this.SolutionFitter.StartTemperature = 1000;
        }
        
        protected override SegmentationSolution SegmentCurrentImage()
        {
            Shape startShape = this.ShapeModel.FitMeanShape(
                this.ImageSegmentator.ImageSize.Width, this.ImageSegmentator.ImageSize.Height);
            Image2D<bool> startMask = this.ShapeToMask(startShape);
            SegmentationSolution startSolution = new SegmentationSolution(startShape, startMask);

            return this.SolutionFitter.Run(startSolution, this.MutateSolution, this.CalcObjective);
        }

        private double CalcObjective(SegmentationSolution solution)
        {
            double shapeEnergy = this.ShapeModel.CalculateEnergy(solution.Shape);
            double labelingEnergy = this.ImageSegmentator.SegmentImageWithShapeTerms(
                (x, y) => this.ShapeModel.CalculatePenalties(solution.Shape, new Vector(x, y)));
            return shapeEnergy * this.ShapeEnergyWeight + labelingEnergy;
        }

        private SegmentationSolution MutateSolution(SegmentationSolution solution, double temperature)
        {
            Shape mutatedShape = this.ShapeMutator.MutateShape(
                solution.Shape, this.ImageSegmentator.ImageSize, temperature / this.SolutionFitter.StartTemperature);
            Image2D<bool> mutatedShapeMask = this.ShapeToMask(mutatedShape);
            return new SegmentationSolution(mutatedShape, mutatedShapeMask);
        }

        private Image2D<bool> ShapeToMask(Shape shape)
        {
            this.ImageSegmentator.SegmentImageWithShapeTerms((x, y) => this.ShapeModel.CalculatePenalties(shape, new Vector(x, y)));
            return this.ImageSegmentator.GetLastSegmentationMask();
        }
    }
}
