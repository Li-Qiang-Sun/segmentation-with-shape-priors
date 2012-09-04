using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior
{
    public abstract class SegmentationAlgorithmBase
    {
        private double brightnessBinaryTermCutoff;
        private double constantBinaryTermWeight;
        private double unaryTermWeight;
        private double shapeUnaryTermWeight;
        private double shapeEnergyWeight;

        protected SegmentationAlgorithmBase()
        {
            this.BrightnessBinaryTermCutoff = 1.2;
            this.ConstantBinaryTermWeight = 1;
            this.UnaryTermWeight = 0.15;
            this.ShapeUnaryTermWeight = 0.15;
            this.ShapeEnergyWeight = 25;
        }

        public ShapeModel ShapeModel { get; set; }

        public bool IsRunning { get; private set; }

        public bool IsPaused { get; private set; }

        protected bool IsStopping { get; private set; }

        public bool WasStopped { get; private set; }

        public double BrightnessBinaryTermCutoff
        {
            get { return brightnessBinaryTermCutoff; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Property value should be positive.");
                brightnessBinaryTermCutoff = value;
            }
        }

        public double ConstantBinaryTermWeight
        {
            get { return constantBinaryTermWeight; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "Property value should not be negative.");
                constantBinaryTermWeight = value;
            }
        }

        public double UnaryTermWeight
        {
            get { return unaryTermWeight; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "Property value should not be negative.");
                unaryTermWeight = value;
            }
        }

        public double ShapeUnaryTermWeight
        {
            get { return shapeUnaryTermWeight; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "Property value should not be negative.");
                shapeUnaryTermWeight = value;
            }
        }

        public double ShapeEnergyWeight
        {
            get { return shapeEnergyWeight; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "Property value should not be negative.");
                shapeEnergyWeight = value;
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
                this.BrightnessBinaryTermCutoff,
                this.ConstantBinaryTermWeight,
                this.UnaryTermWeight,
                this.ShapeUnaryTermWeight);

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

        public ObjectBackgroundColorModels LearnObjectBackgroundMixtureModels(
            Image2D<Color> image,
            Rectangle estimatedObjectLocation,
            int mixtureComponentCount)
        {            
            if (image == null)
                throw new ArgumentNullException("image");
            if (!image.Rectangle.Contains(estimatedObjectLocation))
                throw new ArgumentException("Object location should be inside given image");
            if (estimatedObjectLocation.Width == image.Width && estimatedObjectLocation.Height == image.Height)
                throw new ArgumentException("Estimated object location should be strictly inside an image.");

            const double stopTolerance = 1;
            const double mixtureFittingThreshold = 0.2;

            // Extract background pixels (yeah, it can be done faster)
            List<Color> backgroundPixels = new List<Color>();
            for (int i = 0; i < image.Width; ++i)
                for (int j = 0; j < image.Height; ++j)
                    if (!estimatedObjectLocation.Contains(i, j))
                        backgroundPixels.Add(image[i, j]);

            // Fit GMM for background
            GaussianMixtureColorModel backgroundColorModel = GaussianMixtureColorModel.Fit(backgroundPixels, mixtureComponentCount, stopTolerance);

            // Find most unprobable background pixels in bbox
            List<Tuple<Color, double>> innerPixelsWithProb = new List<Tuple<Color, double>>();
            for (int i = estimatedObjectLocation.Left; i < estimatedObjectLocation.Right; ++i)
                for (int j = estimatedObjectLocation.Top; j < estimatedObjectLocation.Bottom; ++j)
                {
                    Color color = image[i, j];
                    double logProb = backgroundColorModel.LogProb(color);
                    innerPixelsWithProb.Add(new Tuple<Color, double>(color, logProb));
                }
            innerPixelsWithProb.Sort(
                (t1, t2) => Comparer<double>.Default.Compare(t1.Item2, t2.Item2));

            // Fit GMM for foreground
            List<Color> objectPixels =
                innerPixelsWithProb.Take((int)(innerPixelsWithProb.Count * mixtureFittingThreshold)).Select(t => t.Item1).ToList();
            GaussianMixtureColorModel objectColorModel = GaussianMixtureColorModel.Fit(objectPixels, mixtureComponentCount, stopTolerance);

            // Re-learn GMM for background with some new data
            List<Color> moreBackgroundPixels =
                innerPixelsWithProb.Skip((int)(innerPixelsWithProb.Count * (1 - mixtureFittingThreshold))).Select(t => t.Item1).ToList();
            backgroundPixels.AddRange(moreBackgroundPixels);
            backgroundColorModel = GaussianMixtureColorModel.Fit(backgroundPixels, mixtureComponentCount, stopTolerance);

            return new ObjectBackgroundColorModels(objectColorModel, backgroundColorModel);
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