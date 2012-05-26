using System;
using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public abstract class SegmentationAlgorithmBase
    {
        private int mixtureComponentCount;
        private double brightnessBinaryTermCutoff;
        private double constantBinaryTermWeight;
        private double unaryTermWeight;
        private double shapeUnaryTermWeight;
        private double shapeEnergyWeight;

        protected SegmentationAlgorithmBase()
        {
            this.MixtureComponentCount = 3;
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
                if (value < 2)
                    throw new ArgumentOutOfRangeException("value", "Property value should be 2 or more.");
                mixtureComponentCount = value;
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
            if (this.ShapeModel == null)
                throw new InvalidOperationException("Shape model must be specified before segmenting image.");

            this.ImageSegmentator = new ImageSegmentator(
                image,
                estimatedObjectLocation,
                this.BrightnessBinaryTermCutoff,
                this.ConstantBinaryTermWeight,
                this.UnaryTermWeight,
                this.ShapeUnaryTermWeight,
                this.MixtureComponentCount);

            DebugConfiguration.WriteImportantDebugText(
                "Truncated image size is {0}x{1}.",
                this.ImageSegmentator.ImageSize.Width,
                this.ImageSegmentator.ImageSize.Height);
            
            Image2D<bool> mask;
            if (this.ShapeUnaryTermWeight > 0)
            {
                DebugConfiguration.WriteImportantDebugText("Running segmentation algorithm...");
                mask = this.SegmentCurrentImage();
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

        protected abstract Image2D<bool> SegmentCurrentImage();
    }
}