using System;
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
        [DisplayName("Object shape unary term weight")]
        public double ObjectShapeUnaryTermWeight { get; set; }

        [Category("Low-level energy")]
        [DisplayName("Background shape unary term weight")]
        public double BackgroundShapeUnaryTermWeight { get; set; }

        [Category("Low-level energy")]
        [DisplayName("Object color unary term weight")]
        public double ObjectColorUnaryTermWeight { get; set; }

        [Category("Low-level energy")]
        [DisplayName("Background color unary term weight")]
        public double BackgroundColorUnaryTermWeight { get; set; }

        [Category("Low-level energy")]
        [DisplayName("Pairwise constant term weight")]
        public double ConstantPairwiseTermWeight { get; set; }

        [Category("Low-level energy")]
        [DisplayName("Pairwise color term weight")]
        public double ColorDifferencePairwiseTermWeight { get; set; }

        [Category("Low-level energy")]
        [DisplayName("Pairwise color term cutoff")]
        public double ColorDifferencePairwiseTermCutoff { get; set; }

        #endregion

        #region High-level energy

        [Category("High-level energy")]
        [DisplayName("Shape energy weight")]
        public double ShapeEnergyWeight { get; set; }

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
        public double AnnealingStartTemperature { get; set; }

        #endregion

        #region Shape mutation

        [Category("Shape mutation")]
        [DisplayName("Edge width mutation weight")]
        public double EdgeWidthMutationWeight { get; set; }

        [Category("Shape mutation")]
        [DisplayName("Edge width mutation power")]
        public double EdgeWidthMutationPower { get; set; }

        [Category("Shape mutation")]
        [DisplayName("Edge length mutation weight")]
        public double EdgeLengthMutationWeight { get; set; }

        [Category("Shape mutation")]
        [DisplayName("Edge length mutation power")]
        public double EdgeLengthMutationPower { get; set; }

        [Category("Shape mutation")]
        [DisplayName("Edge angle mutation weight")]
        public double EdgeAngleMutationWeight { get; set; }

        [Category("Shape mutation")]
        [DisplayName("Edge angle mutation power")]
        public double EdgeAngleMutationPower { get; set; }

        [Category("Shape mutation")]
        [DisplayName("Shape translation weight")]
        public double ShapeTranslationWeight { get; set; }

        [Category("Shape mutation")]
        [DisplayName("Shape translation power")]
        public double ShapeTranslationPower { get; set; }

        [Category("Shape mutation")]
        [DisplayName("Shape scale weight")]
        public double ShapeScaleWeight { get; set; }

        [Category("Shape mutation")]
        [DisplayName("Shape scale power")]
        public double ShapeScalePower { get; set; }

        [Category("Shape mutation")]
        [DisplayName("Root edge angle")]
        public double RootEdgeAngle { get; set; }

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

        [Category("Main")]
        [DisplayName("Initial shape")]
        [Editor(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string InitialShape { get; set; }

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
            this.ObjectShapeUnaryTermWeight = 9.680417;
            this.BackgroundShapeUnaryTermWeight = 9.680417;
            this.ObjectColorUnaryTermWeight = 9.258642;
            this.BackgroundColorUnaryTermWeight = 9.258642;
            this.ColorDifferencePairwiseTermWeight = 0.781996;
            this.ColorDifferencePairwiseTermCutoff = 0.2;
            this.ConstantPairwiseTermWeight = 0;

            this.ShapeEnergyWeight = 1.0;

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

            this.EdgeWidthMutationWeight = 0.2;
            this.EdgeLengthMutationWeight = 0.25;
            this.EdgeAngleMutationWeight = 0.25;
            this.ShapeTranslationWeight = 0;
            this.ShapeScaleWeight = 0;
            this.EdgeWidthMutationPower = 0.1;
            this.EdgeLengthMutationPower = 0.3;
            this.EdgeAngleMutationPower = Math.PI * 0.25;
            this.ShapeTranslationPower = 0.1;
            this.ShapeScalePower = 0.1;
            this.RootEdgeAngle = -Math.PI * 0.5;

            this.MaxAnnealingIterations = 5000;
            this.MaxAnnealingStallingIterations = 1000;
            this.ReannealingInterval = 500;
            this.AnnealingReportRate = 5;
            this.AnnealingStartTemperature = 0.7;

            this.Algorithm = SegmentationAlgorithm.Simple;
            this.ShapeModel = @"..\..\..\Data\giraffes\giraffe_learned.shp";
            this.ColorModel = @"..\..\..\Data\giraffes\lssvm\color_model.clr";
            this.ImageToSegment = @"..\..\..\Data\giraffes\lssvm\image_000.png";
            this.InitialShape = @"..\..\..\Data\giraffes\lssvm\shape_000.s";
            this.DownscaledImageSize = 140;
        }
    }
}
