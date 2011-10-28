using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using MicrosoftResearch.Infer.Distributions;

namespace Research.GraphBasedShapePrior
{
    public class IterativeSegmentator : SegmentatorBase
    {
        public IShapeFittingStrategy ShapeFittingStrategy { get; set; }

        public int MaxIterationCount { get; set; }

        public int WeightChangingIterationCount { get; set; }

        public double MinChangeRate { get; set; }

        public IterativeSegmentator()
        {
            this.ShapeFittingStrategy = new SAShapeFittingStrategy();
            this.MaxIterationCount = 20;
            this.WeightChangingIterationCount = 10;
            this.MinChangeRate = 0.0002;
        }

        public event EventHandler<SegmentationIterationFinishedEventArgs> IterationFinished;

        protected override Image2D<bool> SegmentImageImpl(
            Image2D<Color> shrinkedImage,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel)
        {
            Debug.Assert(this.ShapeFittingStrategy != null);
            Debug.Assert(this.MaxIterationCount >= 0);
            Debug.Assert(this.WeightChangingIterationCount >= 0);
            Debug.Assert(this.WeightChangingIterationCount <= this.MaxIterationCount);
            Debug.Assert(this.MinChangeRate >= 0 && this.MinChangeRate <= 1);
            
            DebugConfiguration.WriteImportantDebugText("Performing initial segmentation...");
            Image2D<bool> currentMask = SegmentImage(
                shrinkedImage,
                backgroundColorModel,
                objectColorModel, point => new Tuple<double, double>(0, 0),
                false).SegmentationMask;

            for (int iteration = 1; iteration <= this.MaxIterationCount; ++iteration)
            {
                DebugConfiguration.WriteImportantDebugText("Iteration {0}", iteration);

                List<Shape> shapes = this.ShapeFittingStrategy.FitShapes(this.ShapeModel, currentMask);
                double shapePriorWeight = (double) iteration / this.WeightChangingIterationCount;
                Image2D<bool> newMask = SegmentImage(
                    shrinkedImage,
                    backgroundColorModel,
                    objectColorModel,
                    point => this.CalculateShapeTerms(shapes, shapePriorWeight, point),
                    false).SegmentationMask;

                int differentValues = Image2D<bool>.DifferentValueCount(currentMask, newMask);
                double changeRate = (double)differentValues / (shrinkedImage.Width * shrinkedImage.Height);
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

        private Tuple<double, double> CalculateShapeTerms(
            List<Shape> shapes,
            double shapePriorWeight,
            Point point)
        {
            double toSource = 0, toSink = 0;

            // Shape prior
            if (shapes.Count > 0)
            {
                double singleShapePriorWeight = shapePriorWeight / shapes.Count;
                foreach (Shape shape in shapes)
                {
                    toSource += shape.GetObjectPenalty(point) * singleShapePriorWeight;
                    toSink += shape.GetBackgroundPenalty(point) * singleShapePriorWeight;
                }
            }

            return new Tuple<double, double>(toSource, toSink);
        }
    }
}