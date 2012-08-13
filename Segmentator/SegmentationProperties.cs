using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Segmentator
{
    enum ModelType
    {
        OneEdge,
        TwoEdges,
        Letter1,
        Letter2,
        Letter3,
        Letter4,
        Letter5,
        Cow1,
        Cow2,
        Cow3,
        Giraffe1,
        Giraffe2,
        Giraffe3,
        Giraffe4,
    }
    
    class SegmentationProperties
    {
        [Category("Low-level energy")]
        public double ShapeTermWeight { get; set; }

        [Category("Low-level energy")]
        public double UnaryTermWeight { get; set; }

        [Category("Low-level energy")]
        public double ConstantBinaryTermWeight { get; set; }

        [Category("Low-level energy")]
        public double BrightnessBinaryTermCutoff { get; set; }

        [Category("High-level energy")]
        public double ShapeEnergyWeight { get; set; }

        [Category("High-level energy")]
        public double MinEdgeWidth { get; set; }

        [Category("High-level energy")]
        public double MaxEdgeWidth { get; set; }

        [Category("High-level energy")]
        public double BackgroundDistanceCoeff { get; set; }

        [Category("Algorithm")]
        public bool UseTwoStepApproach { get; set; }

        [Category("Algorithm")]
        public int BfsIterations { get; set; }

        [Category("Algorithm")]
        public int ReportRate { get; set; }

        [Category("Algorithm")]
        public double MaxBfsUpperBoundEstimateProbability { get; set; }

        [Category("Algorithm")]
        public double MaxCoordFreedomPre { get; set; }

        [Category("Algorithm")]
        public double MaxCoordFreedom { get; set; }

        [Category("Algorithm")]
        public double MaxWidthFreedomPre { get; set; }

        [Category("Algorithm")]
        public double MaxWidthFreedom { get; set; }

        [Category("Algorithm")]
        public int LengthGridSize { get; set; }

        [Category("Algorithm")]
        public int AngleGridSize { get; set; }

        [Category("Algorithm")]
        public int LengthGridSizePre { get; set; }

        [Category("Algorithm")]
        public int AngleGridSizePre { get; set; }

        [Category("Model")]
        public ModelType ModelType { get; set; }

        [Category("Model")]
        public int MixtureComponents { get; set; }

        public SegmentationProperties()
        {
            this.ShapeTermWeight = 0.5;
            this.UnaryTermWeight = 0.5;
            this.ConstantBinaryTermWeight = 0;
            this.BrightnessBinaryTermCutoff = 0.01;

            this.ShapeEnergyWeight = 100;
            this.MinEdgeWidth = 5;
            this.MaxEdgeWidth = 15;
            this.BackgroundDistanceCoeff = 5;

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

            this.ModelType = ModelType.Letter1;
            this.MixtureComponents = 3;
        }
    }
}
