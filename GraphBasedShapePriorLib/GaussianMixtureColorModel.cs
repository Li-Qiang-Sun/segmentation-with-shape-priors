using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using MicrosoftResearch.Infer.Distributions;

namespace Research.GraphBasedShapePrior
{
    [DataContract]
    public class GaussianMixtureColorModel : IColorModel
    {
        [DataMember]
        private readonly Mixture<VectorGaussian> mixture;

        public GaussianMixtureColorModel(Mixture<VectorGaussian> mixture)
        {
            if (mixture == null)
                throw new ArgumentNullException("mixture");
            this.mixture = mixture;
        }

        public int ComponentCount
        {
            get { return this.mixture.Components.Count; }
        }

        public ReadOnlyCollection<double> Weights
        {
            get { return this.mixture.Weights.AsReadOnly(); }
        }

        public ReadOnlyCollection<VectorGaussian> Components
        {
            get { return this.mixture.Components.AsReadOnly(); }
        }

        public static GaussianMixtureColorModel Fit(IEnumerable<Color> pixels, int mixtureComponentCount, double stopTolerance)
        {
            if (pixels == null)
                throw new ArgumentNullException("pixels");
            if (mixtureComponentCount < 2)
                throw new ArgumentOutOfRangeException("mixtureComponentCount", "Mixture component count should be 2 or more.");
            
            MicrosoftResearch.Infer.Maths.Vector[] observedData = new MicrosoftResearch.Infer.Maths.Vector[pixels.Count()];
            int index = 0;
            foreach (Color pixel in pixels)
                observedData[index++] = pixel.ToInferNetVector();

            Mixture<VectorGaussian> result = MixtureUtils.Fit(observedData, mixtureComponentCount, mixtureComponentCount * 5, stopTolerance);
            return new GaussianMixtureColorModel(result);
        }
        
        public double LogProb(Color color)
        {
            return mixture.LogProb(color.ToInferNetVector());
        }
    }
}
