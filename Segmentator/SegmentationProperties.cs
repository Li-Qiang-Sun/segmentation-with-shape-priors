using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Segmentator
{
    enum Model
    {
        OneEdge,
        TwoEdges,
        Letter
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
        public double ShapeDistanceCutoff { get; set; }

        [Category("High-level energy")]
        public double MinVertexRadius { get; set; }

        [Category("High-level energy")]
        public double MaxVertexRadius { get; set; }

        [Category("Algorithm")]
        public int BfsIterations { get; set; }

        [Category("Algorithm")]
        public int ReportRate { get; set; }

        [Category("Algorithm")]
        public int FrontSaveRate { get; set; }

        [Category("Algorithm")]
        public Model Model { get; set; }

        public SegmentationProperties()
        {
            this.ShapeTermWeight = 1;
            this.UnaryTermWeight = 1;
            this.ConstantBinaryTermWeight = 0;
            this.BrightnessBinaryTermCutoff = 0.01;

            this.ShapeEnergyWeight = 10;
            this.ShapeDistanceCutoff = 1;
            this.MinVertexRadius = 9;
            this.MaxVertexRadius = 10;

            this.BfsIterations = 10000;
            this.ReportRate = 5;
            this.FrontSaveRate = 100000;
            this.Model = Model.OneEdge;
        }
    }
}
