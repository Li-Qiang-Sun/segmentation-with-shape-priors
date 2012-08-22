using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Segmentator
{    
    class SegmentationProperties
    {
        [Category("Low-level energy")]
        [DisplayName("Shape term weight")]
        public double ShapeTermWeight { get; set; }

        [Category("Low-level energy")]
        [DisplayName("Unary term weight")]
        public double UnaryTermWeight { get; set; }

        [Category("Low-level energy")]
        [DisplayName("Constant binary term weight")]
        public double ConstantBinaryTermWeight { get; set; }

        [Category("Low-level energy")]
        [DisplayName("Binary term cutoff")]
        public double BrightnessBinaryTermCutoff { get; set; }

        [Category("High-level energy")]
        [DisplayName("Shape energy weight")]
        public double ShapeEnergyWeight { get; set; }

        [Category("High-level energy")]
        [DisplayName("Min edge width")]
        public double MinEdgeWidth { get; set; }

        [Category("High-level energy")]
        [DisplayName("Max edge width")]
        public double MaxEdgeWidth { get; set; }

        [Category("High-level energy")]
        [DisplayName("Background distance coeff")]
        public double BackgroundDistanceCoeff { get; set; }

        [Category("Algorithm")]
        [DisplayName("Use pre-step")]
        public bool UseTwoStepApproach { get; set; }

        [Category("Algorithm")]
        [DisplayName("BFS iterations")]
        public int BfsIterations { get; set; }

        [Category("Algorithm")]
        [DisplayName("Report rate")]
        public int ReportRate { get; set; }

        [Category("Algorithm")]
        [DisplayName("Upper bound guessing prob")]
        public double MaxBfsUpperBoundEstimateProbability { get; set; }

        [Category("Algorithm")]
        [DisplayName("Max coord freedom on pre-step")]
        public double MaxCoordFreedomPre { get; set; }

        [Category("Algorithm")]
        [DisplayName("Max coord freedom")]
        public double MaxCoordFreedom { get; set; }

        [Category("Algorithm")]
        [DisplayName("Max width freedom on pre-step")]
        public double MaxWidthFreedomPre { get; set; }

        [Category("Algorithm")]
        [DisplayName("Max width freedom")]
        public double MaxWidthFreedom { get; set; }

        [Category("Algorithm")]
        [DisplayName("Length grid size")]
        public int LengthGridSize { get; set; }

        [Category("Algorithm")]
        [DisplayName("Angle grid size")]
        public int AngleGridSize { get; set; }

        [Category("Algorithm")]
        [DisplayName("Length grid size for pre-step")]
        public int LengthGridSizePre { get; set; }

        [Category("Algorithm")]
        [DisplayName("Angle grid size for pre-step")]
        public int AngleGridSizePre { get; set; }

        [Category("Model")]
        [DisplayName("Shape model")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string ShapeModel { get; set; }

        [Category("Model")]
        [DisplayName("Color model")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string ColorModel { get; set; }

        [Category("Model")]
        [DisplayName("Image to segment")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string ImageToSegment { get; set; }

        [Category("Model")]
        [DisplayName("Downscaled image size")]
        public int DownscaledImageSize { get; set; }

        public void Validate()
        {
            if (this.ShapeModel == null)
                throw new PropertyValidationException("Shape model should be specified.");
            if (this.ColorModel == null)
                throw new PropertyValidationException("Color model should be specified.");
            if (this.ImageToSegment == null)
                throw new PropertyValidationException("Image to segment should be specified.");
            if (this.DownscaledImageSize <= 0)
                throw new PropertyValidationException("Downscaled image size should be positive.");
        }

        public SegmentationProperties()
        {
            this.ShapeTermWeight = 0.5;
            this.UnaryTermWeight = 3;
            this.ConstantBinaryTermWeight = 0;
            this.BrightnessBinaryTermCutoff = 0.01;

            this.ShapeEnergyWeight = 100;
            this.MinEdgeWidth = 5;
            this.MaxEdgeWidth = 15;
            this.BackgroundDistanceCoeff = 0.5;

            this.UseTwoStepApproach = true;
            this.BfsIterations = 10000000;
            this.MaxBfsUpperBoundEstimateProbability = 1;
            this.ReportRate = 500;
            this.MaxCoordFreedom = 4;
            this.MaxCoordFreedomPre = 20;
            this.MaxWidthFreedom = 4;
            this.MaxWidthFreedomPre = 20;
            this.LengthGridSizePre = 101;
            this.AngleGridSizePre = 101;
            this.LengthGridSize = 301;
            this.AngleGridSize = 301;

            this.ShapeModel = null;
            this.ColorModel = null;
            this.ImageToSegment = null;
            this.DownscaledImageSize = 140;
        }
    }
}
