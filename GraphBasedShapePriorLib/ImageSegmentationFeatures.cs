using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Research.GraphBasedShapePrior
{
    public class ImageSegmentationFeatures
    {
        public ImageSegmentationFeatures(
            double objectColorUnaryTermSum,
            double backgroundColorUnaryTermSum,
            double objectShapeUnaryTermSum,
            double backgroundShapeUnaryTermSum,
            double constantPairwiseTermSum,
            double colorDifferencePairwiseTermSum)
        {
            this.ObjectColorUnary = objectColorUnaryTermSum;
            this.BackgroundColorUnary = backgroundColorUnaryTermSum;
            this.ObjectShapeUnary = objectShapeUnaryTermSum;
            this.BackgroundShapeUnary = backgroundShapeUnaryTermSum;
            this.ConstantPairwise = constantPairwiseTermSum;
            this.ColorDifferencePairwise = colorDifferencePairwiseTermSum;
        }

        public double ObjectColorUnary { get; private set; }

        public double BackgroundColorUnary { get; private set; }

        public double ObjectShapeUnary { get; private set; }

        public double BackgroundShapeUnary { get; private set; }

        public double ConstantPairwise { get; private set; }

        public double ColorDifferencePairwise { get; private set; }

        public double FeatureSum
        {
            get
            {
                return
                    this.ObjectColorUnary +
                    this.BackgroundColorUnary +
                    this.ObjectShapeUnary +
                    this.BackgroundShapeUnary +
                    this.ConstantPairwise +
                    this.ColorDifferencePairwise;
            }
        }
    }
}
