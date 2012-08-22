using System;
using System.Collections.Generic;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior
{
    public class IterativeSegmentationAlgorithm : SegmentationAlgorithmBase
    {
        private int maxIterationCount;
        private int weightChangingIterationCount;
        private double minChangeRate;
        
        public IterativeSegmentationAlgorithm()
        {
            this.ShapeFittingStrategy = new SAShapeFittingStrategy();
            this.MaxIterationCount = 20;
            this.WeightChangingIterationCount = 10;
            this.MinChangeRate = 0.0002;
        }

        public IShapeFittingStrategy ShapeFittingStrategy { get; set; }

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

        public int WeightChangingIterationCount
        {
            get { return weightChangingIterationCount; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "Property value should not be negative.");
                weightChangingIterationCount = value;
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

        protected override Image2D<bool> SegmentCurrentImage()
        {
            if (this.ShapeFittingStrategy == null)
                throw new InvalidOperationException("Shape fitting strategy must be specified before running segmentation");
            
            DebugConfiguration.WriteImportantDebugText("Performing initial segmentation...");
            this.ImageSegmentator.SegmentImageWithShapeTerms((x, y) => ObjectBackgroundTerm.Zero);
            Image2D<bool> currentMask = this.ImageSegmentator.GetLastSegmentationMask();

            for (int iteration = 1; iteration <= this.MaxIterationCount; ++iteration)
            {
                DebugConfiguration.WriteImportantDebugText("Iteration {0}", iteration);

                List<Shape> shapes = this.ShapeFittingStrategy.FitShapes(this.ShapeModel, currentMask);
                double shapePriorWeight = (double) iteration / this.WeightChangingIterationCount;
                this.ImageSegmentator.SegmentImageWithShapeTerms(
                    (x, y) => this.CalculateShapeTerms(shapes, shapePriorWeight, new Vector(x, y)));
                Image2D<bool> newMask = this.ImageSegmentator.GetLastSegmentationMask();

                int differentValues = Image2D<bool>.DifferentValueCount(currentMask, newMask);
                double changeRate = (double)differentValues / (this.ImageSegmentator.ImageSize.Width * this.ImageSegmentator.ImageSize.Height);
                DebugConfiguration.WriteImportantDebugText("Changed pixel rate is {0:0.000000}", changeRate);
                if (iteration > this.WeightChangingIterationCount && changeRate < this.MinChangeRate)
                {
                    DebugConfiguration.WriteImportantDebugText("Changed pixel rate is too low, breaking...");
                    break;
                }

                currentMask = newMask;

                if (IterationFinished != null)
                    IterationFinished(this, new SegmentationIterationFinishedEventArgs(iteration, currentMask, shapes));
            }

            DebugConfiguration.WriteImportantDebugText("Finished");

            return currentMask;
        }

        private ObjectBackgroundTerm CalculateShapeTerms(List<Shape> shapes, double shapePriorWeight, Vector point)
        {
            double objectTerm = 0, backgroundTerm = 0;

            // Shape prior
            if (shapes.Count > 0)
            {
                double singleShapePriorWeight = shapePriorWeight / shapes.Count;
                foreach (Shape shape in shapes)
                {
                    objectTerm += this.ShapeModel.GetObjectPenalty(shape, point) * singleShapePriorWeight;
                    backgroundTerm += this.ShapeModel.GetBackgroundPenalty(shape, point) * singleShapePriorWeight;
                }
            }

            return new ObjectBackgroundTerm(objectTerm, backgroundTerm);
        }
    }
}