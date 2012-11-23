using System;
using System.Drawing;
using System.Threading;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior
{
    public abstract class SegmentationAlgorithmBase
    {
        private double colorDifferencePairwiseTermCutoff;
        private double colorDifferencePairwiseTermWeight;
        private double constantPairwiseTermWeight;
        private double shapeEnergyWeight;

        protected SegmentationAlgorithmBase()
        {
            this.ColorDifferencePairwiseTermCutoff = 1.2;
            this.ColorDifferencePairwiseTermWeight = 0.015;
            this.ConstantPairwiseTermWeight = 0;
            this.ObjectColorUnaryTermWeight = 1;
            this.BackgroundColorUnaryTermWeight = 1;
            this.ObjectShapeUnaryTermWeight = 1;
            this.BackgroundShapeUnaryTermWeight = 1;
            this.ShapeEnergyWeight = 1;
        }

        public ShapeModel ShapeModel { get; set; }

        public bool IsRunning { get; private set; }

        public bool IsPaused { get; private set; }

        protected bool IsStopping { get; private set; }

        public bool WasStopped { get; private set; }

        public double ColorDifferencePairwiseTermCutoff
        {
            get { return this.colorDifferencePairwiseTermCutoff; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Property value should be positive.");
                this.colorDifferencePairwiseTermCutoff = value;
            }
        }

        public double ColorDifferencePairwiseTermWeight
        {
            get { return this.colorDifferencePairwiseTermWeight; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "Property value should not be negative.");
                this.colorDifferencePairwiseTermWeight = value;
            }
        }

        public double ConstantPairwiseTermWeight
        {
            get { return this.constantPairwiseTermWeight; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "Property value should not be negative.");
                this.constantPairwiseTermWeight = value;
            }
        }

        public double ObjectColorUnaryTermWeight { get; set; }

        public double BackgroundColorUnaryTermWeight { get; set; }

        public double ObjectShapeUnaryTermWeight  { get; set; }

        public double BackgroundShapeUnaryTermWeight { get; set; }

        public double ShapeEnergyWeight
        {
            get { return this.shapeEnergyWeight; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "Property value should not be negative.");
                this.shapeEnergyWeight = value;
            }
        }

        public ImageSegmentator ImageSegmentator { get; private set; }

        public SegmentationSolution SegmentImage(Image2D<Color> image, ObjectBackgroundColorModels colorModels)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (colorModels == null)
                throw new ArgumentNullException("colorModels");

            this.ImageSegmentator = new ImageSegmentator(
                image,
                colorModels,
                this.ColorDifferencePairwiseTermCutoff,
                this.ColorDifferencePairwiseTermWeight,
                this.ConstantPairwiseTermWeight,
                this.ObjectColorUnaryTermWeight,
                this.BackgroundColorUnaryTermWeight,
                this.ObjectShapeUnaryTermWeight,
                this.BackgroundShapeUnaryTermWeight);

            DebugConfiguration.WriteImportantDebugText(
                "Segmented image size is {0}x{1}.",
                this.ImageSegmentator.ImageSize.Width,
                this.ImageSegmentator.ImageSize.Height);

            SegmentationSolution solution = null;
            try
            {
                if (this.ShapeModel == null)
                    throw new InvalidOperationException("Shape model must be specified before segmenting image.");

                DebugConfiguration.WriteImportantDebugText("Running segmentation algorithm...");
                this.IsRunning = true;
                solution = this.SegmentCurrentImage();

                if (solution == null)
                    throw new InvalidOperationException("Segmentation solution can not be null.");
            }
            finally
            {
                if (this.IsStopping)
                    this.WasStopped = true;
                
                this.IsRunning = false;
                this.IsStopping = false;
            }

            DebugConfiguration.WriteImportantDebugText("Finished");

            return solution;
        }

        public void Pause()
        {
            if (!this.IsRunning)
                throw new InvalidOperationException("Segmentation algorithm is not currently running.");
            if (this.IsPaused)
                throw new InvalidOperationException("Segmentation algorithm is already paused.");

            this.IsPaused = true;
            this.DoPause();
        }

        public void Continue()
        {
            if (!this.IsRunning)
                throw new InvalidOperationException("Segmentation algorithm is not currently running.");
            if (!this.IsPaused)
                throw new InvalidOperationException("Segmentation algorithm should be paused to continue it.");

            this.IsPaused = false;
            this.DoContinue();
        }
        
        public void Stop()
        {
            if (this.IsPaused)
                throw new InvalidOperationException("Segmentation algorithm can't be stopped while it is paused.");
            if (!this.IsRunning)
                return;

            this.IsStopping = true;
            this.DoStop();
        }

        protected abstract SegmentationSolution SegmentCurrentImage();

        protected virtual void DoPause()
        {
            
        }

        protected virtual void DoContinue()
        {
            
        }

        protected virtual void DoStop()
        {
            
        }

        protected void WaitIfPaused()
        {
            while (this.IsPaused)
                Thread.Sleep(10);
        }
    }
}