using System;
using System.Drawing;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior
{
    public class CoordinateDescentSegmentationAlgorithm : SegmentationAlgorithmBase
    {
        private int minIterationCount;
        private int maxIterationCount;
        private double minChangeRate;

        public CoordinateDescentSegmentationAlgorithm()
        {
            this.ShapeFitter = new SimulatedAnnealingMinimizer<Shape>();
            this.ShapeFitter.MaxIterations = 5000;
            this.ShapeFitter.MaxStallingIterations = 1000;
            this.ShapeFitter.ReannealingInterval = 500;
            this.ShapeFitter.StartTemperature = 1000;
            
            this.ShapeMutator = new ShapeMutator();

            this.MaxIterationCount = 20;
            this.MinIterationCount = 3;
            this.MinChangeRate = 0.0002;
        }

        public SimulatedAnnealingMinimizer<Shape> ShapeFitter { get; private set; }

        public ShapeMutator ShapeMutator { get; private set; }

        public int MaxIterationCount
        {
            get { return maxIterationCount; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "Property value should not be negative.");
                maxIterationCount = value;
            }
        }

        public int MinIterationCount
        {
            get { return minIterationCount; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "Property value should not be negative.");
                minIterationCount = value;
            }
        }

        public double MinChangeRate
        {
            get { return minChangeRate; }
            set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException("value", "Property value should be in [0, 1] range.");
                minChangeRate = value;
            }
        }

        public event EventHandler<SegmentationIterationFinishedEventArgs> IterationFinished;

        protected override SegmentationSolution SegmentCurrentImage()
        {
            DebugConfiguration.WriteImportantDebugText("Performing initial segmentation...");
            this.ImageSegmentator.SegmentImageWithShapeTerms((x, y) => ObjectBackgroundTerm.Zero);
            Image2D<bool> prevMask = this.ImageSegmentator.GetLastSegmentationMask();
            Shape prevShape = this.ShapeModel.FitMeanShape(
                this.ImageSegmentator.ImageSize.Width, this.ImageSegmentator.ImageSize.Height);
            double prevEnergy = 0;

            for (int iteration = 1; iteration <= this.MaxIterationCount && !this.IsStopping; ++iteration)
            {
                this.WaitIfPaused();
                
                DebugConfiguration.WriteImportantDebugText("Iteration {0}", iteration);

                Image2D<bool> prevMaskCopy = prevMask;
                Shape currentShape = this.ShapeFitter.Run(
                    prevShape,
                    (s, t) => this.ShapeMutator.MutateShape(s, this.ShapeModel, this.ImageSegmentator.ImageSize, t / this.ShapeFitter.StartTemperature),
                    s => this.CalcObjective(s, prevMaskCopy));
                
                double currentEnergy = this.ImageSegmentator.SegmentImageWithShapeTerms((x, y) => this.ShapeModel.CalculatePenalties(currentShape, new Vector(x, y)));
                Image2D<bool> currentMask = this.ImageSegmentator.GetLastSegmentationMask();

                int differentValues = Image2D<bool>.DifferentValueCount(prevMask, currentMask);
                double changedPixelRate = (double)differentValues / (this.ImageSegmentator.ImageSize.Width * this.ImageSegmentator.ImageSize.Height);

                DebugConfiguration.WriteImportantDebugText("On iteration {0}:", iteration);
                DebugConfiguration.WriteImportantDebugText("Energy is {0:0.000}", currentEnergy);
                DebugConfiguration.WriteImportantDebugText("Changed pixel rate is {0:0.000000}", changedPixelRate);
                DebugConfiguration.WriteImportantDebugText();

                if (iteration > this.MinIterationCount && changedPixelRate < this.MinChangeRate)
                {
                    DebugConfiguration.WriteImportantDebugText("Changed pixel rate is too low, breaking...");
                    break;
                }

                prevMask = currentMask;
                prevShape = currentShape;
                prevEnergy = currentEnergy;

                if (IterationFinished != null)
                {
                    IterationFinished(this, new SegmentationIterationFinishedEventArgs(
                        iteration,
                        currentShape,
                        this.ImageSegmentator.GetLastSegmentationMask(),
                        this.ImageSegmentator.GetLastUnaryTerms(),
                        this.ImageSegmentator.GetLastShapeTerms()));
                }
            }

            return new SegmentationSolution(prevShape, prevMask, prevEnergy);
        }

        private double CalcObjective(Shape shape, Image2D<bool> mask)
        {
            double shapeEnergy = this.ShapeModel.CalculateEnergy(shape);
            double labelingEnergy = CalcShapeLabelingEnergy(shape, mask);
            return shapeEnergy * this.ShapeEnergyWeight + labelingEnergy;
        }

        private double CalcShapeLabelingEnergy(Shape shape, Image2D<bool> mask)
        {
            double shapeTermSum = 0;
            for (int x = 0; x < mask.Width; ++x)
            {
                for (int y = 0; y < mask.Height; ++y)
                {
                    
                    if (mask[x, y])
                        shapeTermSum += this.ShapeModel.CalculateObjectPenalty(shape, new Vector(x, y)) * this.ObjectShapeUnaryTermWeight;
                    else
                        shapeTermSum += this.ShapeModel.CalculateBackgroundPenalty(shape, new Vector(x, y)) * this.BackgroundShapeUnaryTermWeight;
                }
            }

            return shapeTermSum * this.ImageSegmentator.UnaryTermScaleCoeff;
        }
    }
}