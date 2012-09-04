using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms.Design;

namespace Segmentator
{    
    enum SegmentationAlgorithm
    {
        BranchAndBound,
        CoordinateDescent,
        Annealing,
        Simple
    }
    
    class SegmentationProperties
    {        
        #region Low-level energy

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

        #endregion

        #region High-level energy

        [Category("High-level energy")]
        [DisplayName("Shape energy weight")]
        public double ShapeEnergyWeight { get; set; }

        [Category("High-level energy")]
        [DisplayName("Background distance coeff")]
        public double BackgroundDistanceCoeff { get; set; }

        #endregion

        #region Branch-and-bound

        [Category("Branch-and-bound")]
        [DisplayName("Min edge width")]
        public double MinEdgeWidth { get; set; }

        [Category("Branch-and-bound")]
        [DisplayName("Max edge width")]
        public double MaxEdgeWidth { get; set; }

        [Category("Branch-and-bound")]
        [DisplayName("Use pre-step")]
        public bool UseTwoStepApproach { get; set; }

        [Category("Branch-and-bound")]
        [DisplayName("Report rate")]
        public int BranchAndBoundReportRate { get; set; }

        [Category("Branch-and-bound")]
        [DisplayName("Max coord freedom on pre-step")]
        public double MaxCoordFreedomPre { get; set; }

        [Category("Branch-and-bound")]
        [DisplayName("Max coord freedom")]
        public double MaxCoordFreedom { get; set; }

        [Category("Branch-and-bound")]
        [DisplayName("Max width freedom on pre-step")]
        public double MaxWidthFreedomPre { get; set; }

        [Category("Branch-and-bound")]
        [DisplayName("Max width freedom")]
        public double MaxWidthFreedom { get; set; }

        [Category("Branch-and-bound")]
        [DisplayName("Length grid size")]
        public int LengthGridSize { get; set; }

        [Category("Branch-and-bound")]
        [DisplayName("Angle grid size")]
        public int AngleGridSize { get; set; }

        [Category("Branch-and-bound")]
        [DisplayName("Length grid size for pre-step")]
        public int LengthGridSizePre { get; set; }

        [Category("Branch-and-bound")]
        [DisplayName("Angle grid size for pre-step")]
        public int AngleGridSizePre { get; set; }

        #endregion

        #region Annealing

        [Category("Simulated annealing")]
        [DisplayName("Max iterations")]
        public int MaxAnnealingIterations { get; set; }

        [Category("Simulated annealing")]
        [DisplayName("Max stall iterations")]
        public int MaxAnnealingStallingIterations { get; set; }

        [Category("Simulated annealing")]
        [DisplayName("Number of solution updates before reannealing")]
        public int ReannealingInterval { get; set; }

        [Category("Simulated annealing")]
        [DisplayName("Report rate")]
        public int AnnealingReportRate { get; set; }

        [Category("Simulated annealing")]
        [DisplayName("StartTemperature")]
        public int AnnealingStartTemperature { get; set; }

        #endregion

        #region Shape mutation

        [Category("Shape mutation")]
        [DisplayName("Vertex mutation probability")]
        public double VertexMutationProbability { get; set; }

        [Category("Shape mutation")]
        [DisplayName("Vertex mutation stddev (relative to image size)")]
        public double VertexMutationRelativeDeviation { get; set; }

        [Category("Shape mutation")]
        [DisplayName("Edge mutation stddev (relative to image size)")]
        public double EdgeMutationRelativeDeviation { get; set; }

        [Category("Shape mutation")]
        [DisplayName("Max number of pointwise mutations for a single shape")]
        public int MaxMutationCount { get; set; }

        #endregion

        #region Coordinate descent

        [Category("Coordinate descent")]
        [DisplayName("Min coordinate descent iterations")]
        public int MinDescentIterations { get; set; }

        [Category("Coordinate descent")]
        [DisplayName("Max coordinate descent iterations")]
        public int MaxDescentIterations { get; set; }

        [Category("Coordinate descent")]
        [DisplayName("Min pixel change rate to continue coordinate descent")]
        public double MinDescentPixelChangeRate { get; set; }

        #endregion

        #region Main

        [Category("Main")]
        [DisplayName("Algorithm")]
        public SegmentationAlgorithm Algorithm { get; set; }
        
        [Category("Main")]
        [DisplayName("Shape model")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string ShapeModel { get; set; }

        [Category("Main")]
        [DisplayName("Color model")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string ColorModel { get; set; }

        [Category("Main")]
        [DisplayName("Image to segment")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string ImageToSegment { get; set; }

        [Category("Main")]
        [DisplayName("Downscaled image size")]
        public int DownscaledImageSize { get; set; }

        #endregion

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
            this.ShapeTermWeight = 1.5;
            this.UnaryTermWeight = 3;
            this.ConstantBinaryTermWeight = 0;
            this.BrightnessBinaryTermCutoff = 0.01;

            this.ShapeEnergyWeight = 100;
            this.BackgroundDistanceCoeff = 0.5;

            this.MinEdgeWidth = 5;
            this.MaxEdgeWidth = 15;
            this.UseTwoStepApproach = true;
            this.BranchAndBoundReportRate = 500;
            this.MaxCoordFreedom = 4;
            this.MaxCoordFreedomPre = 20;
            this.MaxWidthFreedom = 4;
            this.MaxWidthFreedomPre = 20;
            this.LengthGridSizePre = 101;
            this.AngleGridSizePre = 101;
            this.LengthGridSize = 301;
            this.AngleGridSize = 301;

            this.MinDescentIterations = 3;
            this.MaxDescentIterations = 20;
            this.MinDescentPixelChangeRate = 0.0002;
            
            this.VertexMutationProbability = 0.8;
            this.VertexMutationRelativeDeviation = 0.3;
            this.EdgeMutationRelativeDeviation = 0.1;
            this.MaxMutationCount = 3;

            this.MaxAnnealingIterations = 5000;
            this.MaxAnnealingStallingIterations = 1000;
            this.ReannealingInterval = 500;
            this.AnnealingReportRate = 5;
            this.AnnealingStartTemperature = 1000;

            this.Algorithm = SegmentationAlgorithm.Simple;
            this.ShapeModel = @"..\..\..\Data\giraffes\giraffe.shp";
            this.ColorModel = @"..\..\..\Data\giraffes\giraffe_3.clr";
            this.ImageToSegment = @"..\..\..\Data\giraffes\train\giraffe_train_5.jpg";
            this.DownscaledImageSize = 80;
        }
    }
}
