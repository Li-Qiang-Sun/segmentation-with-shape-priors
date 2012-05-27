using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using MicrosoftResearch.Infer.Distributions;

namespace Research.GraphBasedShapePrior
{
    public class GaussianMixtureModel : IColorModel
    {
        private readonly Mixture<VectorGaussian> mixture;

        public GaussianMixtureModel(Mixture<VectorGaussian> mixture)
        {
            if (mixture == null)
                throw new ArgumentNullException("mixture");
            this.mixture = mixture;
        }

        public static GaussianMixtureModel Fit(IList<Color> pixels, int mixtureComponentCount, double stopTolerance)
        {
            if (pixels == null)
                throw new ArgumentNullException("pixels");
            if (mixtureComponentCount < 1)
                throw new ArgumentOutOfRangeException("mixtureComponentCount", "Mixture component count should be positive.");
            
            MicrosoftResearch.Infer.Maths.Vector[] observedData = new MicrosoftResearch.Infer.Maths.Vector[pixels.Count];
            for (int i = 0; i < pixels.Count; ++i)
                observedData[i] = pixels[i].ToInferNetVector();

            Mixture<VectorGaussian> result = MixtureUtils.Fit(observedData, mixtureComponentCount, mixtureComponentCount, stopTolerance);
            return new GaussianMixtureModel(result);
        }
        
        public double LogProb(Color color)
        {
            return mixture.LogProb(color.ToInferNetVector());
        }
    }
}
