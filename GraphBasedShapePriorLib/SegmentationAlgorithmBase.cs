using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using MicrosoftResearch.Infer.Distributions;

namespace Research.GraphBasedShapePrior
{
    public abstract class SegmentationAlgorithmBase
    {
        private int mixtureComponentCount;
        private double mixtureFittingThreshold;
        private double brightnessBinaryTermCutoff;
        private double constantBinaryTermWeight;
        private double unaryTermWeight;
        private double shapeUnaryTermWeight;
        private double shapeEnergyWeight;
        
        protected SegmentationAlgorithmBase()
        {
            this.MixtureComponentCount = 3;
            this.MixtureFittingThreshold = 0.2;
            this.BrightnessBinaryTermCutoff = 1.2;
            this.ConstantBinaryTermWeight = 1;
            this.UnaryTermWeight = 0.15;
            this.ShapeUnaryTermWeight = 1;
            this.ShapeEnergyWeight = 25;
        }

        public ShapeModel ShapeModel { get; set; }

        public int MixtureComponentCount
        {
            get { return mixtureComponentCount; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value", "Property value should be positive.");
                mixtureComponentCount = value;
            }
        }

        public double MixtureFittingThreshold
        {
            get { return mixtureFittingThreshold; }
            set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException("value", "Property value should be in [0, 1] range.");
                mixtureFittingThreshold = value;
            }
        }

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

        public Image2D<bool> SegmentImage(Image2D<Color> image, Rectangle estimatedObjectLocation)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (!image.Rectangle.Contains(estimatedObjectLocation))
                throw new ArgumentException("Estimated object location should be inside the image.");

            if (this.ShapeModel == null)
                throw new InvalidOperationException("Shape model must be specified before segmentation.");

            DebugConfiguration.WriteImportantDebugText("Learning color models for object and background...");
            Mixture<VectorGaussian> backgroundColorModel, objectColorModel;
            EstimateColorModels(image, estimatedObjectLocation, out backgroundColorModel, out objectColorModel);

            DebugConfiguration.WriteImportantDebugText("Shrinking image...");
            image = image.Shrink(estimatedObjectLocation);

            this.ImageSegmentator = new ImageSegmentator(
                image,
                backgroundColorModel,
                objectColorModel,
                this.BrightnessBinaryTermCutoff,
                this.ConstantBinaryTermWeight,
                this.UnaryTermWeight, this.ShapeUnaryTermWeight);

            Image2D<bool> mask;
            if (this.ShapeUnaryTermWeight > 0)
            {
                DebugConfiguration.WriteImportantDebugText("Running segmentation algorithm...");
                mask = this.SegmentImageImpl(image, backgroundColorModel, objectColorModel);
            }
            else
            {
                DebugConfiguration.WriteImportantDebugText("Shape does not affect segmentation, so just segmenting image..");
                this.ImageSegmentator.SegmentImageWithShapeTerms(p => ObjectBackgroundTerm.Zero);
                mask = this.ImageSegmentator.GetLastSegmentationMask();
            }
            
            DebugConfiguration.WriteImportantDebugText("Finished");

            return mask;
        }

        protected abstract Image2D<bool> SegmentImageImpl(
            Image2D<Color> shrinkedImage,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel);

        private void EstimateColorModels(
            Image2D<Color> image,
            Rectangle estimatedObjectLocation,
            out Mixture<VectorGaussian> backgroundColorModel,
            out Mixture<VectorGaussian> objectColorModel)
        {
            Debug.Assert(image != null);
            Debug.Assert(image.Rectangle.Contains(estimatedObjectLocation));

            // Extract background pixels (yeah, it can be done faster)
            List<Color> backgroundPixels = new List<Color>();
            for (int i = 0; i < image.Width; ++i)
                for (int j = 0; j < image.Height; ++j)
                    if (!estimatedObjectLocation.Contains(i, j))
                        backgroundPixels.Add(image[i, j]);

            // Fit GMM for background
            backgroundColorModel = FitGaussianMixture(backgroundPixels);

            // Find most unprobable background pixels in bbox
            List<Tuple<Color, double>> innerPixelsWithProb = new List<Tuple<Color, double>>();
            for (int i = estimatedObjectLocation.Left; i < estimatedObjectLocation.Right; ++i)
                for (int j = estimatedObjectLocation.Top; j < estimatedObjectLocation.Bottom; ++j)
                {
                    Color color = image[i, j];
                    double logProb = backgroundColorModel.LogProb(color.ToInferNetVector());
                    innerPixelsWithProb.Add(new Tuple<Color, double>(color, logProb));
                }
            innerPixelsWithProb.Sort(
                (t1, t2) =>
                    t1.Item2 < t2.Item2
                    ? -1
                    : (t1.Item2 == t2.Item2 ? 0 : 1));

            // Fit GMM for foreground
            List<Color> objectPixels =
                innerPixelsWithProb.Take((int)(innerPixelsWithProb.Count * this.MixtureFittingThreshold)).Select(t => t.Item1).ToList();
            objectColorModel = FitGaussianMixture(objectPixels);

            // Re-learn GMM for background with some new data
            List<Color> moreBackgroundPixels =
                innerPixelsWithProb.Skip((int)(innerPixelsWithProb.Count * (1 - this.MixtureFittingThreshold))).Select(t => t.Item1).ToList();
            backgroundPixels.AddRange(moreBackgroundPixels);
            backgroundColorModel = FitGaussianMixture(backgroundPixels);
        }

        private Mixture<VectorGaussian> FitGaussianMixture(List<Color> pixels)
        {
            Debug.Assert(pixels != null);
            Debug.Assert(pixels.Count > this.MixtureComponentCount);

            MicrosoftResearch.Infer.Maths.Vector[] observedData = new MicrosoftResearch.Infer.Maths.Vector[pixels.Count];
            for (int i = 0; i < pixels.Count; ++i)
                observedData[i] = pixels[i].ToInferNetVector();

            Mixture<VectorGaussian> result = MixtureUtils.Fit(
                observedData, this.MixtureComponentCount, this.MixtureComponentCount, 1);
            return result;
        }
    }
}