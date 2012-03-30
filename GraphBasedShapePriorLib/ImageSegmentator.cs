using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using MicrosoftResearch.Infer.Distributions;
using Research.GraphBasedShapePrior.GraphCuts;

namespace Research.GraphBasedShapePrior
{
    /// <summary>
    /// Allows to segment the image with varying shape terms.
    /// </summary>
    public class ImageSegmentator
    {
        private Image2D<ObjectBackgroundTerm> colorTerms;

        private Image2D<ObjectBackgroundTerm> lastUnaryTerms;

        private Image2D<ObjectBackgroundTerm> lastShapeTerms;

        private Image2D<bool> lastSegmentationMask;

        private Image2D<Tuple<double, double, double>> pairwiseTerms;

        private readonly GraphCutCalculator graphCutCalculator;

        private bool firstTime = true;

        public ImageSegmentator(
            Image2D<Color> image,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel,
            double brightnessBinaryTermCutoff,
            double constantBinaryTermWeight,
            double unaryTermWeight,
            double shapeUnaryTermWeight)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (backgroundColorModel == null)
                throw new ArgumentNullException("backgroundColorModel");
            if (objectColorModel == null)
                throw new ArgumentNullException("objectColorModel");
            
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

            this.graphCutCalculator = new GraphCutCalculator(image.Width, image.Height);

            this.PreparePairwiseTerms(image);
            this.PrepareColorTerms(image, backgroundColorModel, objectColorModel);
            this.PrepareUnaryTermHolders(image);
        }

        private void PrepareUnaryTermHolders(Image2D<Color> image)
        {
            this.lastUnaryTerms = new Image2D<ObjectBackgroundTerm>(image.Width, image.Height);
            this.lastShapeTerms = new Image2D<ObjectBackgroundTerm>(image.Width, image.Height);
            this.lastSegmentationMask = new Image2D<bool>(image.Width, image.Height);
        }

        private void PrepareColorTerms(Image2D<Color> image, Mixture<VectorGaussian> backgroundColorModel, Mixture<VectorGaussian> objectColorModel)
        {
            this.colorTerms = new Image2D<ObjectBackgroundTerm>(image.Width, image.Height);
            for (int x = 0; x < image.Width; ++x)
            {
                for (int y = 0; y < image.Height; ++y)
                {
                    MicrosoftResearch.Infer.Maths.Vector imagePixel = image[x, y].ToInferNetVector();
                    double objectTerm = -objectColorModel.LogProb(imagePixel);
                    double backgroundTerm = -backgroundColorModel.LogProb(imagePixel);
                    this.colorTerms[x, y] = new ObjectBackgroundTerm(objectTerm, backgroundTerm);
                }
            }
        }

        private void PreparePairwiseTerms(Image2D<Color> image)
        {
            this.pairwiseTerms = new Image2D<Tuple<double, double, double>>(image.Width, image.Height);

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
                        this.graphCutCalculator.SetNeighborWeights(x, y, Neighbor.Right, weightRight);
                    }
                    if (y < image.Height - 1)
                    {
                        weightBottom = CalculateBinaryTerm(
                            image, meanBrightnessDiff, new Point(x, y), new Point(x, y + 1));
                        this.graphCutCalculator.SetNeighborWeights(x, y, Neighbor.Bottom, weightBottom);
                    }
                    if (x < image.Width - 1 && y < image.Height - 1)
                    {
                        weightBottomRight = CalculateBinaryTerm(
                            image, meanBrightnessDiff, new Point(x, y), new Point(x + 1, y + 1));
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

        public double SegmentImageWithShapeTerms(
            Func<Point, ObjectBackgroundTerm> shapeTermCalculator)
        {
            // Calculate shape terms, check for changes))
            for (int x = 0; x < this.lastUnaryTerms.Width; ++x)
            {
                for (int y = 0; y < this.lastUnaryTerms.Height; ++y)
                {
                    ObjectBackgroundTerm shapeTerms = shapeTermCalculator(new Point(x, y));
                    
                    if (firstTime || shapeTerms != this.lastShapeTerms[x, y])
                    {
                        double objectTermNew = (this.colorTerms[x, y].ObjectTerm + shapeTerms.ObjectTerm * this.ShapeUnaryTermWeight) * this.UnaryTermWeight;
                        double backgroundTermNew = (this.colorTerms[x, y].BackgroundTerm + shapeTerms.BackgroundTerm * this.ShapeUnaryTermWeight) * this.UnaryTermWeight;
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
    }
}
