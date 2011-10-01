using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using MicrosoftResearch.Infer.Distributions;
using Research.GraphBasedShapePrior.GraphCuts;

namespace Research.GraphBasedShapePrior
{
    public abstract class SegmentatorBase
    {
        public ShapeModel ShapeModel { get; set; }

        public int MixtureComponentCount { get; set; }

        public double MixtureFittingThreshold { get; set; }

        public double BrightnessBinaryTermCutoff { get; set; }

        public double ConstantBinaryTermWeight { get; set; }

        public double UnaryTermWeight { get; set; }

        public double ShapeUnaryTermWeight { get; set; }

        public double ShapeEnergyWeight { get; set; }

        // Cached info about last segmentation

        private Tuple<double, double>[,] lastColorTerms;

        private Tuple<double, double>[,] lastUnaryTerms;

        private Tuple<double, double, double>[,] lastPairwiseTerms;

        private GraphCutCalculator lastCalculator;

        private Image2D<Color> lastImage;

        protected SegmentatorBase()
        {
            this.MixtureComponentCount = 3;
            this.MixtureFittingThreshold = 0.2;
            this.BrightnessBinaryTermCutoff = 1.2;
            this.ConstantBinaryTermWeight = 1;
            this.UnaryTermWeight = 0.15;
            this.ShapeUnaryTermWeight = 1;
            this.ShapeEnergyWeight = 25;
        }

        public Image2D<bool> SegmentImage(Image2D<Color> image, Rectangle estimatedObjectLocation)
        {
            Debug.Assert(image != null);
            Debug.Assert(image.Rectangle.Contains(estimatedObjectLocation));

            Debug.Assert(this.ShapeModel != null);
            Debug.Assert(this.MixtureComponentCount >= 1);
            Debug.Assert(this.MixtureComponentCount >= 0 && this.MixtureFittingThreshold <= 1);
            Debug.Assert(this.BrightnessBinaryTermCutoff >= 0);
            Debug.Assert(this.ConstantBinaryTermWeight >= 0);
            Debug.Assert(this.UnaryTermWeight >= 0);
            Debug.Assert(this.ShapeEnergyWeight >= 0);

            DebugConfiguration.WriteImportantDebugText("Learning color models for object and background...");
            Mixture<VectorGaussian> backgroundColorModel, objectColorModel;
            EstimateColorModels(image, estimatedObjectLocation, out backgroundColorModel, out objectColorModel);

            DebugConfiguration.WriteImportantDebugText("Shrinking image...");
            image = image.Shrink(estimatedObjectLocation);
            double objectSize = Math.Max(estimatedObjectLocation.Width, estimatedObjectLocation.Height);

            Image2D<bool> mask = this.SegmentImageImpl(image, objectSize, backgroundColorModel, objectColorModel);

            DebugConfiguration.WriteImportantDebugText("Finished");

            return mask;
        }

        protected abstract Image2D<bool> SegmentImageImpl(
            Image2D<Color> shrinkedImage,
            double objectSize,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel);

        /// <summary>
        /// Segment given image.
        /// </summary>
        /// <param name="image">Image to segment.</param>
        /// <param name="backgroundColorModel">Background color model</param>
        /// <param name="objectColorModel">Object color model.</param>
        /// <param name="shapeTermCalculator">Shape term calculator.</param>
        /// <param name="nonShapeContentChanged">Pass true if image or background/object models have changed since the last call (disables some optimizations).</param>
        /// <remarks>This method is optimized for repeated calls with the same image and color models but different shape terms.</remarks>
        /// <returns>Segmentation info.</returns>
        protected ImageSegmentationInfo SegmentImage(
            Image2D<Color> image,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel,
            Func<Point, Tuple<double, double>> shapeTermCalculator,
            bool nonShapeContentChanged)
        {
            Debug.Assert(image != null);
            Debug.Assert(backgroundColorModel != null);
            Debug.Assert(objectColorModel != null);
            Debug.Assert(shapeTermCalculator != null);

            // Here we optimize for the case when only shape potentials change
            bool imageChanged = false;
            if (nonShapeContentChanged || this.lastImage == null || this.lastImage != image)
            {
                imageChanged = true;
                this.lastImage = image;

                if (this.lastCalculator != null)
                    this.lastCalculator.Dispose();

                // Create new calculator
                this.lastCalculator = new GraphCutCalculator(image.Width, image.Height);

                // Create unary & pairwise terms holders
                this.lastUnaryTerms = new Tuple<double, double>[image.Width, image.Height];
                this.lastPairwiseTerms = new Tuple<double, double, double>[image.Width, image.Height];

                // Calculate pairwise terms
                double meanBrightnessDiff = this.CalculateMeanBrightnessDifference(image);
                for (int x = 0; x < image.Width; ++x)
                {
                    for (int y = 0; y < image.Height; ++y)
                    {
                        double weightRight = 0, weightBottom = 0, weightBottomRight = 0;
                        if (x < image.Width - 1)
                        {
                            weightRight = CalculateBinaryTerm(
                                image, meanBrightnessDiff, new Point(x, y), new Point(x + 1, y));
                            this.lastCalculator.SetNeighborWeights(x, y, Neighbor.Right, weightRight);
                        }
                        if (y < image.Height - 1)
                        {
                            weightBottom = CalculateBinaryTerm(
                                image, meanBrightnessDiff, new Point(x, y), new Point(x, y + 1));
                            this.lastCalculator.SetNeighborWeights(x, y, Neighbor.Bottom, weightBottom);
                        }
                        if (x < image.Width - 1 && y < image.Height - 1)
                        {
                            weightBottomRight = CalculateBinaryTerm(
                                image, meanBrightnessDiff, new Point(x, y), new Point(x + 1, y + 1));
                            this.lastCalculator.SetNeighborWeights(x, y, Neighbor.RightBottom, weightBottomRight);
                        }

                        this.lastPairwiseTerms[x, y] = new Tuple<double, double, double>(
                            weightRight, weightBottom, weightBottomRight);
                    }
                }

                // Calculate color terms
                this.lastColorTerms = new Tuple<double, double>[image.Width,image.Height];
                for (int x = 0; x < image.Width; ++x)
                {
                    for (int y = 0; y < image.Height; ++y)
                    {
                        MicrosoftResearch.Infer.Maths.Vector imagePixel = image[x, y].ToInferNetVector();
                        double toSource = -backgroundColorModel.LogProb(imagePixel);
                        double toSink = -objectColorModel.LogProb(imagePixel);
                        this.lastColorTerms[x, y] = new Tuple<double, double>(toSource, toSink);
                    }
                }
            }

            // Calculate shape terms, see if something changed, fill unary terms
            for (int x = 0; x < image.Width; ++x)
            {
                for (int y = 0; y < image.Height; ++y)
                {
                    Tuple<double, double> shapeTerms = shapeTermCalculator(new Point(x, y));
                    Debug.Assert(shapeTerms.Item1 >= 0 && shapeTerms.Item2 >= 0);
                    double toSourceNew = (this.lastColorTerms[x, y].Item1 + shapeTerms.Item1 * this.ShapeUnaryTermWeight) * this.UnaryTermWeight;
                    double toSinkNew = (this.lastColorTerms[x, y].Item2 + shapeTerms.Item2 * this.ShapeUnaryTermWeight) * this.UnaryTermWeight;

                    if (imageChanged || toSourceNew != this.lastUnaryTerms[x, y].Item1 || toSinkNew != this.lastUnaryTerms[x, y].Item2)
                    {
                        Debug.Assert(!Double.IsInfinity(toSourceNew) && !Double.IsNaN(toSourceNew));
                        Debug.Assert(!Double.IsInfinity(toSinkNew) && !Double.IsNaN(toSinkNew));

                        if (imageChanged)
                            this.lastCalculator.SetTerminalWeights(x, y, toSourceNew, toSinkNew);
                        else
                        {
                            this.lastCalculator.UpdateTerminalWeights(
                                x, y, this.lastUnaryTerms[x, y].Item1, this.lastUnaryTerms[x, y].Item2, toSourceNew, toSinkNew);
                        }
                        this.lastUnaryTerms[x, y] = new Tuple<double, double>(toSourceNew, toSinkNew);
                    }
                }
            }

            double maxFlow = this.lastCalculator.Calculate();

            // Create segmentation mask
            Image2D<bool> segmentationMask = new Image2D<bool>(image.Width, image.Height);
            for (int x = 0; x < image.Width; ++x)
                for (int y = 0; y < image.Height; ++y)
                {
                    bool isObject = this.lastCalculator.BelongsToSource(x, y);
                    segmentationMask[x, y] = isObject;
                }

            // Compute energy
            double energy = 0;
            for (int x = 0; x < image.Width; ++x)
                for (int y = 0; y < image.Height; ++y)
                {
                    energy += segmentationMask[x, y] ? this.lastUnaryTerms[x, y].Item2 : this.lastUnaryTerms[x, y].Item1;
                    if (x < image.Width - 1 && segmentationMask[x, y] != segmentationMask[x + 1, y])
                        energy += this.lastPairwiseTerms[x, y].Item1;
                    if (y < image.Height - 1 && segmentationMask[x, y] != segmentationMask[x, y + 1])
                        energy += this.lastPairwiseTerms[x, y].Item2;
                    if (x < image.Width - 1 && y < image.Height - 1 && segmentationMask[x, y] != segmentationMask[x + 1, y + 1])
                        energy += this.lastPairwiseTerms[x, y].Item3;
                }

            Debug.Assert(Math.Abs(energy - maxFlow) < 1e-6 || !nonShapeContentChanged);
            return new ImageSegmentationInfo(energy, segmentationMask);
        }

        private double CalculateMeanBrightnessDifference(Image2D<Color> image)
        {
            double sum = 0;
            int count = 0;
            for (int x = 0; x < image.Width; ++x)
            {
                for (int y = 0; y < image.Height; ++y)
                {
                    if (x < image.Width - 1)
                    {
                        sum += GetBrightnessDifference(image, new Point(x, y), new Point(x + 1, y));
                        count += 1;
                    }
                    if (y < image.Height - 1)
                    {
                        sum += GetBrightnessDifference(image, new Point(x, y), new Point(x, y + 1));
                        count += 1;
                    }
                    if (x < image.Width - 1 && y < image.Height - 1)
                    {
                        sum += GetBrightnessDifference(image, new Point(x, y), new Point(x + 1, y + 1));
                        count += 1;
                    }
                }
            }

            return sum / count;
        }

        private double GetBrightnessDifference(Image2D<Color> image, Point point1, Point point2)
        {
            Color color1 = image[point1.X, point1.Y];
            Color color2 = image[point2.X, point2.Y];
            Color colorDiff = Color.FromArgb(Math.Abs(color1.R - color2.R), Math.Abs(color1.G - color2.G), Math.Abs(color1.B - color2.B));
            return colorDiff.GetBrightness();
        }

        private double CalculateBinaryTerm(
            Image2D<Color> image, double meanBrightnessDiff, Point point1, Point point2)
        {
            double brightnessDiff = GetBrightnessDifference(image, point1, point2);
            double brightnessDiffSqr = brightnessDiff * brightnessDiff;
            double meanDiffSqr = meanBrightnessDiff * meanBrightnessDiff;
            return Math.Exp(-this.BrightnessBinaryTermCutoff * brightnessDiffSqr / meanDiffSqr) + this.ConstantBinaryTermWeight;
        }

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