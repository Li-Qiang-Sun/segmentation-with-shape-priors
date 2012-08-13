using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Research.GraphBasedShapePrior.ShapeModelLearning
{
    class AlgorithmProperties
    {
        [Category("Shape learning")]
        public double LearnedObjectSize { get; set; }

        [Category("Color learning")]
        public int MixtureComponentCount { get; set; }

        [Category("Color learning")]
        public int MaxPixelsToLearnFrom { get; set; }

        [Category("Color learning")]
        public double StopTolerance { get; set; }

        [Category("Segmentation")]
        public double SegmentedImageSize { get; set; }

        [Category("Segmentation")]
        public double ShapeTermWeight { get; set; }

        [Category("Segmentation")]
        public double UnaryTermWeight { get; set; }

        [Category("Segmentation")]
        public double ConstantBinaryTermWeight { get; set; }

        [Category("Segmentation")]
        public double BrightnessBinaryTermCutoff { get; set; }

        [Category("Segmentation")]
        public double ShapeEnergyWeight { get; set; }

        [Category("Segmentation")]
        public double BackgroundDistanceCoeff { get; set; }

        [Category("Segmentation")]
        public int MixtureComponents { get; set; }

        public AlgorithmProperties()
        {
            this.LearnedObjectSize = 100;

            this.MixtureComponentCount = 3;
            this.MaxPixelsToLearnFrom = 10000;
            this.StopTolerance = 1;

            this.SegmentedImageSize = 140;
            this.ShapeTermWeight = 0.5;
            this.UnaryTermWeight = 0.5;
            this.ShapeEnergyWeight = 100;
            this.ConstantBinaryTermWeight = 0;
            this.BrightnessBinaryTermCutoff = 0.01;
            this.BackgroundDistanceCoeff = 5;
            this.MixtureComponents = 3;
        }
    }
}
