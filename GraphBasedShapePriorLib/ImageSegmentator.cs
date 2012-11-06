using System;
using System.Diagnostics;
using System.Drawing;
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

        private Image2D<Tuple<double, double, double>> scaledPairwiseTerms;

        private readonly GraphCutCalculator graphCutCalculator;

        private bool firstTime = true;

        public ImageSegmentator(
            Image2D<Color> image,
            ObjectBackgroundColorModels colorModels,
            double colorDifferencePairwiseTermCutoff,
            double colorDifferencePairwiseTermWeight,
            double constantPairwiseTermWeight,
            double colorUnaryTermWeight,
            double shapeUnaryTermWeight)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (colorModels == null)
                throw new ArgumentNullException("colorModels");

            if (colorDifferencePairwiseTermCutoff <= 0)
                throw new ArgumentOutOfRangeException("colorDifferencePairwiseTermCutoff", "Parameter value should be positive.");
            if (colorDifferencePairwiseTermWeight < 0)
                throw new ArgumentOutOfRangeException("colorDifferencePairwiseTermWeight", "Parameter value should not be negative.");
            if (constantPairwiseTermWeight < 0)
                throw new ArgumentOutOfRangeException("constantPairwiseTermWeight", "Parameter value should not be negative.");
            if (colorUnaryTermWeight < 0)
                throw new ArgumentOutOfRangeException("colorUnaryTermWeight", "Parameter value should not be negative.");
            if (shapeUnaryTermWeight < 0)
                throw new ArgumentOutOfRangeException("shapeUnaryTermWeight", "Parameter value should not be negative.");

            this.ColorDifferencePairwiseTermCutoff = colorDifferencePairwiseTermCutoff;
            this.ColorDifferencePairwiseTermWeight = colorDifferencePairwiseTermWeight;
            this.ConstantPairwiseTermWeight = constantPairwiseTermWeight;
            this.ColorUnaryTermWeight = colorUnaryTermWeight;
            this.ShapeUnaryTermWeight = shapeUnaryTermWeight;

            this.segmentedImage = image;
            
            this.UnaryTermScaleCoeff = 1.0 / (image.Width * image.Height);
            this.PairwiseTermScaleCoeff = 1.0 / Math.Sqrt(image.Width * image.Height);

            this.graphCutCalculator = new GraphCutCalculator(this.segmentedImage.Width, this.segmentedImage.Height);

            this.PrepareColorTerms(colorModels);
            this.PreparePairwiseTerms();
            this.PrepareOther();
        }

        public double UnaryTermScaleCoeff { get; private set; }

        public double PairwiseTermScaleCoeff { get; private set; }

        private void PrepareOther()
        {
            this.lastUnaryTerms = new Image2D<ObjectBackgroundTerm>(this.ImageSize.Width, this.ImageSize.Height);
            this.lastShapeTerms = new Image2D<ObjectBackgroundTerm>(this.ImageSize.Width, this.ImageSize.Height);
            this.lastSegmentationMask = new Image2D<bool>(this.ImageSize.Width, this.ImageSize.Height);
        }

        private void PreparePairwiseTerms()
        {
            this.scaledPairwiseTerms = new Image2D<Tuple<double, double, double>>(this.segmentedImage.Width, this.segmentedImage.Height);

            double meanBrightnessDiff = CalculateMeanBrightnessDifference();
            for (int x = 0; x < this.segmentedImage.Width; ++x)
            {
                for (int y = 0; y < this.segmentedImage.Height; ++y)
                {
                    double weightRight = 0, weightBottom = 0, weightBottomRight = 0;
                    if (x < this.segmentedImage.Width - 1)
                    {
                        weightRight = CalculateScaledPairwiseTerms(meanBrightnessDiff, new Point(x, y), new Point(x + 1, y));
                        this.graphCutCalculator.SetNeighborWeights(x, y, Neighbor.Right, weightRight);
                    }
                    if (y < this.segmentedImage.Height - 1)
                    {
                        weightBottom = CalculateScaledPairwiseTerms(meanBrightnessDiff, new Point(x, y), new Point(x, y + 1));
                        this.graphCutCalculator.SetNeighborWeights(x, y, Neighbor.Bottom, weightBottom);
                    }
                    if (x < this.segmentedImage.Width - 1 && y < this.segmentedImage.Height - 1)
                    {
                        weightBottomRight = CalculateScaledPairwiseTerms(meanBrightnessDiff, new Point(x, y), new Point(x + 1, y + 1));
                        this.graphCutCalculator.SetNeighborWeights(x, y, Neighbor.RightBottom, weightBottomRight);
                    }

                    this.scaledPairwiseTerms[x, y] = new Tuple<double, double, double>(weightRight, weightBottom, weightBottomRight);
                }
            }
        }

        public double ColorDifferencePairwiseTermCutoff { get; private set; }

        public double ColorDifferencePairwiseTermWeight { get; private set; }

        public double ConstantPairwiseTermWeight { get; private set; }

        public double ColorUnaryTermWeight { get; private set; }

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

        public Image2D<double> GetHorizontalColorDifferencePairwiseTerms()
        {
            Image2D<double> result = new Image2D<double>(this.ImageSize.Width, this.ImageSize.Height);
            for (int i = 0; i < result.Width; ++i)
            {
                for (int j = 0; j < result.Height; ++j)
                {
                    double notScaledWeight = this.scaledPairwiseTerms[i, j].Item1 / this.PairwiseTermScaleCoeff;
                    result[i, j] = (notScaledWeight - this.ConstantPairwiseTermWeight) / this.ColorDifferencePairwiseTermWeight;
                }
            }

            return result;
        }

        public Image2D<Color> GetSegmentedImage()
        {
            return this.segmentedImage.Clone();
        }

        public double SegmentImageWithShapeTerms(
            Func<int, int, ObjectBackgroundTerm> shapeTermCalculator)
        {
            // Calculate shape terms, check for changes)
            for (int x = 0; x < this.lastUnaryTerms.Width; ++x)
            {
                for (int y = 0; y < this.lastUnaryTerms.Height; ++y)
                {
                    ObjectBackgroundTerm shapeTerms = shapeTermCalculator(x, y);
                    
                    if (firstTime || shapeTerms != this.lastShapeTerms[x, y])
                    {
                        double objectTermNew = this.UnaryTermScaleCoeff * (this.colorTerms[x, y].ObjectTerm * this.ColorUnaryTermWeight + shapeTerms.ObjectTerm * this.ShapeUnaryTermWeight);
                        double backgroundTermNew = this.UnaryTermScaleCoeff * (this.colorTerms[x, y].BackgroundTerm * this.ColorUnaryTermWeight + shapeTerms.BackgroundTerm * this.ShapeUnaryTermWeight);
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
            double graphCutEnergy = this.graphCutCalculator.Calculate();
            bool wasFirstTime = this.firstTime;
            this.firstTime = false;

            // Fill segmentation mask
            for (int x = 0; x < this.lastSegmentationMask.Width; ++x)
            {
                for (int y = 0; y < this.lastSegmentationMask.Height; ++y)
                {
                    bool isObject = this.graphCutCalculator.BelongsToSource(x, y);
                    this.lastSegmentationMask[x, y] = isObject;
                }
            }

            // Compute energy
            double unaryColorTermSum, unaryShapeTermSum, colorDifferencePairwiseTermSum, constantPairwiseTermSum;
            this.ExtractSegmentationFeaturesForMask(
                this.lastSegmentationMask,
                out unaryColorTermSum,
                out unaryShapeTermSum,
                out colorDifferencePairwiseTermSum,
                out constantPairwiseTermSum);
            double energy = unaryColorTermSum + unaryShapeTermSum + colorDifferencePairwiseTermSum + constantPairwiseTermSum;

            // Sanity check: energies should be the same if graph cut calculator is "fresh"
            Debug.Assert(!wasFirstTime || Math.Abs(graphCutEnergy - energy) < 1e-6);

            return energy;
        }

        public void ExtractSegmentationFeaturesForMask(
            Image2D<bool> mask,
            out double unaryColorTermSum,
            out double unaryShapeTermSum,
            out double colorDifferencePairwiseTermSum,
            out double constantPairwiseTermSum)
        {
            if (this.firstTime)
                throw new InvalidOperationException("You should perform segmentation first.");

            unaryColorTermSum = 0;
            unaryShapeTermSum = 0;

            int nonZeroPairwiseTermsCount = 0;
            double pairwiseTermSum = 0;

            for (int x = 0; x < mask.Width; ++x)
            {
                for (int y = 0; y < mask.Height; ++y)
                {
                    if (mask[x, y])
                    {
                        unaryColorTermSum += this.colorTerms[x, y].ObjectTerm;
                        unaryShapeTermSum += this.lastShapeTerms[x, y].ObjectTerm;
                    }
                    else
                    {
                        unaryColorTermSum += this.colorTerms[x, y].BackgroundTerm;
                        unaryShapeTermSum += this.lastShapeTerms[x, y].BackgroundTerm;
                    }

                    if (x < mask.Width - 1 && mask[x, y] != mask[x + 1, y])
                    {
                        pairwiseTermSum += this.scaledPairwiseTerms[x, y].Item1;
                        ++nonZeroPairwiseTermsCount;
                    }
                    if (y < mask.Height - 1 && mask[x, y] != mask[x, y + 1])
                    {
                        pairwiseTermSum += this.scaledPairwiseTerms[x, y].Item2;
                        ++nonZeroPairwiseTermsCount;
                    }
                    if (x < mask.Width - 1 && y < mask.Height - 1 && mask[x, y] != mask[x + 1, y + 1])
                    {
                        pairwiseTermSum += this.scaledPairwiseTerms[x, y].Item3;
                        ++nonZeroPairwiseTermsCount;
                    }
                }
            }

            unaryColorTermSum *= this.ColorUnaryTermWeight * this.UnaryTermScaleCoeff;
            unaryShapeTermSum *= this.ShapeUnaryTermWeight * this.UnaryTermScaleCoeff;

            constantPairwiseTermSum = nonZeroPairwiseTermsCount * this.ConstantPairwiseTermWeight * this.PairwiseTermScaleCoeff;
            colorDifferencePairwiseTermSum = pairwiseTermSum - constantPairwiseTermSum;
        }

        private double CalculateMeanBrightnessDifference()
        {
            double sum = 0;
            int count = 0;
            for (int x = 0; x < this.ImageSize.Width; ++x)
            {
                for (int y = 0; y < this.ImageSize.Height; ++y)
                {
                    if (x < this.ImageSize.Width - 1)
                    {
                        sum += GetBrightnessDifference(new Point(x, y), new Point(x + 1, y));
                        count += 1;
                    }
                    if (y < this.ImageSize.Height - 1)
                    {
                        sum += GetBrightnessDifference(new Point(x, y), new Point(x, y + 1));
                        count += 1;
                    }
                    if (x < this.ImageSize.Width - 1 && y < this.ImageSize.Height - 1)
                    {
                        sum += GetBrightnessDifference(new Point(x, y), new Point(x + 1, y + 1));
                        count += 1;
                    }
                }
            }

            return sum / count;
        }

        private double GetBrightnessDifference(Point point1, Point point2)
        {
            Color color1 = this.segmentedImage[point1.X, point1.Y];
            Color color2 = this.segmentedImage[point2.X, point2.Y];
            Color colorDiff = Color.FromArgb(Math.Abs(color1.R - color2.R), Math.Abs(color1.G - color2.G), Math.Abs(color1.B - color2.B));
            return colorDiff.GetBrightness();
        }

        private double CalculateScaledPairwiseTerms(double meanBrightnessDiff, Point point1, Point point2)
        {
            double brightnessDiff = GetBrightnessDifference(point1, point2);
            double brightnessDiffSqr = brightnessDiff * brightnessDiff;
            double meanDiffSqr = meanBrightnessDiff * meanBrightnessDiff;
            double colorDiffBinaryTerm = Math.Exp(-this.ColorDifferencePairwiseTermCutoff * brightnessDiffSqr / meanDiffSqr) * this.ColorDifferencePairwiseTermWeight;
            double constantBinaryTerm = this.ConstantPairwiseTermWeight;
            return (colorDiffBinaryTerm + constantBinaryTerm) * this.PairwiseTermScaleCoeff;
        }

        private void PrepareColorTerms(ObjectBackgroundColorModels colorModels)
        {
            this.colorTerms = new Image2D<ObjectBackgroundTerm>(this.ImageSize.Width, this.ImageSize.Height);
            for (int x = 0; x < this.ImageSize.Width; ++x)
            {
                for (int y = 0; y < this.ImageSize.Height; ++y)
                {
                    Color color = this.segmentedImage[x, y];
                    double objectTerm = -colorModels.ObjectColorModel.LogProb(color);
                    double backgroundTerm = -colorModels.BackgroundColorModel.LogProb(color);
                    this.colorTerms[x, y] = new ObjectBackgroundTerm(objectTerm, backgroundTerm);
                }
            }
        }
    }
}
