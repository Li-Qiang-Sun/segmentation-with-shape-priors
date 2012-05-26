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
        public int BfsIterations { get; set; }

        [Category("Algorithm")]
        public int ReportRate { get; set; }

        [Category("Algorithm")]
        public int FrontSaveRate { get; set; }

        [Category("Algorithm")]
        public int BfsUpperBoundEstimateUpdateRate { get; set; }

        [Category("Algorithm")]
        public double MaxCoordFreedom { get; set; }

        [Category("Algorithm")]
        public double MaxWidthFreedom { get; set; }

        [Category("Algorithm")]
        public ModelType Model { get; set; }

        public SegmentationProperties()
        {
            this.ShapeTermWeight = 0.3;
            this.UnaryTermWeight = 0.5;
            this.ConstantBinaryTermWeight = 0;
            this.BrightnessBinaryTermCutoff = 0.01;

            this.ShapeEnergyWeight = 100;
            this.MinEdgeWidth = 10;
            this.MaxEdgeWidth = 20;
            this.BackgroundDistanceCoeff = 1;

            this.BfsIterations = 1000000;
            this.BfsUpperBoundEstimateUpdateRate = 100;
            this.ReportRate = 50;
            this.FrontSaveRate = 100000;
            this.MaxCoordFreedom = 3;
            this.MaxWidthFreedom = 3;
            this.Model = ModelType.OneEdge;
        }
    }
}
