using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using MicrosoftResearch.Infer.Distributions;
using Research.GraphBasedShapePrior.GraphCuts;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior
{
    /// <summary>
    /// Allows to segment the image with varying shape terms.
    /// </summary>
    public class ImageSegmentator
    {
        private readonly Image2D<Color> segmentedImage;
        
        private Image2D<ObjectBackgroundTerm> colorTerms;

        private Image2D<ObjectBackgroundTerm> lastUnaryTerms;

        private Image2D<ObjectBackgroundTerm> lastShapeTerms;

        private Image2D<bool> lastSegmentationMask;

        private Image2D<Tuple<double, double, double>> pairwiseTerms;

        private readonly GraphCutCalculator graphCutCalculator;

        private bool firstTime = true;

        public ImageSegmentator(
            Image2D<Color> image,
            ObjectBackgroundColorModels colorModels,
            double brightnessBinaryTermCutoff,
            double constantBinaryTermWeight,
            double unaryTermWeight,
            double shapeUnaryTermWeight)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (colorModels == null)
                throw new ArgumentNullException("colorModels");

            if (brightnessBinaryTermCutoff <= 0)
                throw new ArgumentOutOfRangeException("brightnessBinaryTermCutoff", "Parameter value should be positive.");
            if (constantBinaryTermWeight < 0)
                throw new ArgumentOutOfRangeException("constantBinaryTermWeight", "Parameter value should not be negative.");
            if (unaryTermWeight < 0)
                throw new ArgumentOutOfRangeException("unaryTermWeight", "Parameter value should not be negative.");
            if (shapeUnaryTermWeight < 0)
                throw new ArgumentOutOfRangeException("shapeUnaryTermWeight", "Parameter value should be not be negative.");

            this.BrightnessBinaryTermCutoff = brightnessBinaryTermCutoff;
            this.ConstantBinaryTermWeight = constantBinaryTermWeight;
            this.UnaryTermWeight = unaryTermWeight;
            this.ShapeUnaryTermWeight = shapeUnaryTermWeight;

            this.PrepareColorTerms(image, colorModels);
            this.segmentedImage = image;

            this.graphCutCalculator = new GraphCutCalculator(this.segmentedImage.Width, this.segmentedImage.Height);

            this.PreparePairwiseTerms();
            this.PrepareOther();
        }

        private void PrepareOther()
        {
            this.lastUnaryTerms = new Image2D<ObjectBackgroundTerm>(this.ImageSize.Width, this.ImageSize.Height);
            this.lastShapeTerms = new Image2D<ObjectBackgroundTerm>(this.ImageSize.Width, this.ImageSize.Height);
            this.lastSegmentationMask = new Image2D<bool>(this.ImageSize.Width, this.ImageSize.Height);
        }

        private void PreparePairwiseTerms()
        {
            this.pairwiseTerms = new Image2D<Tuple<double, double, double>>(this.segmentedImage.Width, this.segmentedImage.Height);

            double meanBrightnessDiff = CalculateMeanBrightnessDifference(this.segmentedImage);
            for (int x = 0; x < this.segmentedImage.Width; ++x)
            {
                for (int y = 0; y < this.segmentedImage.Height; ++y)
                {
                    double weightRight = 0, weightBottom = 0, weightBottomRight = 0;
                    if (x < this.segmentedImage.Width - 1)
                    {
                        weightRight = CalculateBinaryTerm(
                            this.segmentedImage, meanBrightnessDiff, new Point(x, y), new Point(x + 1, y));
                        this.graphCutCalculator.SetNeighborWeights(x, y, Neighbor.Right, weightRight);
                    }
                    if (y < this.segmentedImage.Height - 1)
                    {
                        weightBottom = CalculateBinaryTerm(
                            this.segmentedImage, meanBrightnessDiff, new Point(x, y), new Point(x, y + 1));
                        this.graphCutCalculator.SetNeighborWeights(x, y, Neighbor.Bottom, weightBottom);
                    }
                    if (x < this.segmentedImage.Width - 1 && y < this.segmentedImage.Height - 1)
                    {
                        weightBottomRight = CalculateBinaryTerm(
                            this.segmentedImage, meanBrightnessDiff, new Point(x, y), new Point(x + 1, y + 1));
                        this.graphCutCalculator.SetNeighborWeights(x, y, Neighbor.RightBottom, weightBottomRight);
                    }

                    this.pairwiseTerms[x, y] = new Tuple<double, double, double>(
                        weightRight, weightBottom, weightBottomRight);
                }
            }
        }

        public double BrightnessBinaryTermCutoff { get; private set; }

        public double ConstantBinaryTermWeight { get; private set; }

        public double UnaryTermWeight { get; private set; }

        public double ShapeUnaryTermWeight { get; private set; }

        public Size ImageSize
        {
            get { return this.segmentedImage.Rectangle.Size; }
        }

        public Image2D<bool> GetLastSegmentationMask()
        {
            if (this.firstTime)
                throw new InvalidOperationException("You should perform segmentation first.");
            return this.lastSegmentationMask.Clone();
        }

        public Image2D<ObjectBackgroundTerm> GetLastUnaryTerms()
        {
            if (this.firstTime)
                throw new InvalidOperationException("You should perform segmentation first.");
            return this.lastUnaryTerms.Clone();
        }

        public Image2D<ObjectBackgroundTerm> GetLastShapeTerms()
        {
            if (this.firstTime)
                throw new InvalidOperationException("You should perform segmentation first.");
            return this.lastShapeTerms.Clone();
        }

        public Image2D<ObjectBackgroundTerm> GetColorTerms()
        {
            return this.colorTerms.Clone();
        }

        public Image2D<Color> GetSegmentedImage()
        {
            return this.segmentedImage.Clone();
        }

        public double SegmentImageWithShapeTerms(
            Func<int, int, ObjectBackgroundTerm> shapeTermCalculator)
        {
            // Calculate shape terms, check for changes
            for (int x = 0; x < this.lastUnaryTerms.Width; ++x)
            {
                for (int y = 0; y < this.lastUnaryTerms.Height; ++y)
                {
                    ObjectBackgroundTerm shapeTerms = shapeTermCalculator(x, y);
                    
                    if (firstTime || shapeTerms != this.lastShapeTerms[x, y])
                    {
                        double objectTermNew = this.colorTerms[x, y].ObjectTerm * this.UnaryTermWeight + shapeTerms.ObjectTerm * this.ShapeUnaryTermWeight;
                        double backgroundTermNew = this.colorTerms[x, y].BackgroundTerm * this.UnaryTermWeight + shapeTerms.BackgroundTerm * this.ShapeUnaryTermWeight;
                        Debug.Assert(!Double.IsInfinity(objectTermNew) && !Double.IsNaN(objectTermNew));
                        Debug.Assert(!Double.IsInfinity(backgroundTermNew) && !Double.IsNaN(backgroundTermNew));

                        if (firstTime)
                            this.graphCutCalculator.SetTerminalWeights(x, y, backgroundTermNew, objectTermNew);
                        else
                        {
                            this.graphCutCalculator.UpdateTerminalWeights(
                                x, y, this.lastUnaryTerms[x, y].BackgroundTerm, this.lastUnaryTerms[x, y].ObjectTerm, backgroundTermNew, objectTermNew);
                        }

                        this.lastShapeTerms[x, y] = shapeTerms;
                        this.lastUnaryTerms[x, y] = new ObjectBackgroundTerm(objectTermNew, backgroundTermNew);
                    }
                }
            }

            // Actually segment image
            this.graphCutCalculator.Calculate();

            // Fill segmentation mask
            for (int x = 0; x < this.lastSegmentationMask.Width; ++x)
                for (int y = 0; y < this.lastSegmentationMask.Height; ++y)
                {
                    bool isObject = this.graphCutCalculator.BelongsToSource(x, y);
                    this.lastSegmentationMask[x, y] = isObject;
                }

            // Compute energy
            double energy = 0;
            for (int x = 0; x < this.lastSegmentationMask.Width; ++x)
            {
                for (int y = 0; y < this.lastSegmentationMask.Height; ++y)
                {
                    energy += this.lastSegmentationMask[x, y] ? this.lastUnaryTerms[x, y].ObjectTerm : this.lastUnaryTerms[x, y].BackgroundTerm;
                    if (x < this.lastSegmentationMask.Width - 1 && this.lastSegmentationMask[x, y] != this.lastSegmentationMask[x + 1, y])
                        energy += this.pairwiseTerms[x, y].Item1;
                    if (y < this.lastSegmentationMask.Height - 1 && this.lastSegmentationMask[x, y] != this.lastSegmentationMask[x, y + 1])
                        energy += this.pairwiseTerms[x, y].Item2;
                    if (x < this.lastSegmentationMask.Width - 1 && y < this.lastSegmentationMask.Height - 1 && this.lastSegmentationMask[x, y] != this.lastSegmentationMask[x + 1, y + 1])
                        energy += this.pairwiseTerms[x, y].Item3;
                }
            }

            this.firstTime = false;

            return energy;
        }

        private static double CalculateMeanBrightnessDifference(Image2D<Color> image)
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

        private static double GetBrightnessDifference(Image2D<Color> image, Point point1, Point point2)
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

        private void PrepareColorTerms(Image2D<Color> image, ObjectBackgroundColorModels colorModels)
        {
            this.colorTerms = new Image2D<ObjectBackgroundTerm>(image.Width, image.Height);
            for (int x = 0; x < image.Width; ++x)
            {
                for (int y = 0; y < image.Height; ++y)
                {
                    double objectTerm = -colorModels.ObjectColorModel.LogProb(image[x, y]);
                    double backgroundTerm = -colorModels.BackgroundColorModel.LogProb(image[x, y]);
                    this.colorTerms[x, y] = new ObjectBackgroundTerm(objectTerm, backgroundTerm);
                }
            }
        }
    }
}
