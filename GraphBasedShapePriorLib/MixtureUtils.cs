using System;
using System.Diagnostics;
using System.Linq;
using MicrosoftResearch.Infer.Distributions;
using MicrosoftResearch.Infer.Maths;
using Research.GraphBasedShapePrior.Util;
using Random = Research.GraphBasedShapePrior.Util.Random;

namespace Research.GraphBasedShapePrior
{
    public static class MixtureUtils
    {
        public static double LogProb<TComponent, TComponentType>(this Mixture<TComponent> mixture, TComponentType what) where TComponent : IDistribution<TComponentType>
        {
            double sum = 0;
            for (int i = 0; i < mixture.Components.Count; ++i)
                sum += mixture.Weights[i] * Math.Exp(mixture.Components[i].GetLogProb(what));
            sum /= mixture.WeightSum();
            return MathHelper.LogInf(sum);
        }

        public static double LogProb(this Mixture<VectorGaussian> mixture, MicrosoftResearch.Infer.Maths.Vector what)
        {
            double sum = 0;
            for (int i = 0; i < mixture.Components.Count; ++i)
            {
                MicrosoftResearch.Infer.Maths.Vector mean = mixture.Components[i].GetMean();
                MicrosoftResearch.Infer.Maths.Vector diff = what - mean;
                PositiveDefiniteMatrix precision = mixture.Components[i].Precision;

                double prob =
                    Math.Exp(-0.5 * precision.QuadraticForm(diff, diff)) * Math.Sqrt(precision.Determinant()) / Math.Pow(2 * Math.PI, what.Count * 0.5);

                sum += mixture.Weights[i] * prob;
            }
            sum /= mixture.WeightSum();
            return MathHelper.LogInf(sum);
        }

        public static Mixture<VectorGaussian> Fit(MicrosoftResearch.Infer.Maths.Vector[] data, int componentCount, int retryCount, double tolerance = 1e-4)
        {
            Debug.Assert(data != null);
            Debug.Assert(data.Length > componentCount * 3);
            Debug.Assert(componentCount > 1);
            Debug.Assert(retryCount >= 0);

            int dimensions = data[0].Count;

            // Find point boundary
            MicrosoftResearch.Infer.Maths.Vector min = data[0].Clone();
            MicrosoftResearch.Infer.Maths.Vector max = min.Clone();
            for (int i = 1; i < data.Length; ++i)
            {
                Debug.Assert(dimensions == data[i].Count);
                for (int j = 0; j < dimensions; ++j)
                {
                    min[j] = Math.Min(min[j], data[i][j]);
                    max[j] = Math.Max(max[j], data[i][j]);
                }

            }

            // Initialize solution
            MicrosoftResearch.Infer.Maths.Vector[] means = new MicrosoftResearch.Infer.Maths.Vector[componentCount];
            PositiveDefiniteMatrix[] covariances = new PositiveDefiniteMatrix[componentCount];
            for (int i = 0; i < componentCount; ++i)
                GenerateRandomMixtureComponent(min, max, out means[i], out covariances[i]);
            double[] weights = Enumerable.Repeat(1.0 / componentCount, componentCount).ToArray();

            // EM algorithm for GMM
            double[,] expectations = new double[data.Length, componentCount];
            double lastEstimate;
            const double negativeInfinity = -1e+20;
            double currentEstimate = negativeInfinity;
            do
            {
                lastEstimate = currentEstimate;

                // E-step: estimate expectations on hidden variables
                for (int i = 0; i < data.Length; ++i)
                {
                    double sum = 0;
                    for (int j = 0; j < componentCount; ++j)
                    {
                        expectations[i, j] =
                            Math.Exp(VectorGaussian.GetLogProb(data[i], means[j], covariances[j])) * weights[j];
                        sum += expectations[i, j];
                    }
                    for (int j = 0; j < componentCount; ++j)
                        expectations[i, j] /= sum;
                }

                // M-step:

                // Re-estimate means
                for (int j = 0; j < componentCount; ++j)
                {
                    means[j] = MicrosoftResearch.Infer.Maths.Vector.Zero(dimensions);
                    double sum = 0;
                    for (int i = 0; i < data.Length; ++i)
                    {
                        means[j] += data[i] * expectations[i, j];
                        sum += expectations[i, j];
                    }
                    means[j] *= 1.0 / sum;
                }

                // Re-estimate covariances
                bool convergenceDetected = false;
                for (int j = 0; j < componentCount; ++j)
                {
                    Matrix covariance = new Matrix(dimensions, dimensions);
                    double sum = 0;
                    for (int i = 0; i < data.Length; ++i)
                    {
                        MicrosoftResearch.Infer.Maths.Vector dataDiff = data[i] - means[j];
                        covariance += dataDiff.Outer(dataDiff) * expectations[i, j];
                        sum += expectations[i, j];
                    }
                    covariance *= 1.0 / sum;
                    covariances[j] = new PositiveDefiniteMatrix(covariance);

                    if (covariances[j].LogDeterminant() < -30)
                    {
                        DebugConfiguration.WriteDebugText("Convergence detected for component {0}", j);
                        if (retryCount == 0)
                            throw new InvalidOperationException("Can't fit GMM. Retry number exceeded.");

                        retryCount -= 1;
                        GenerateRandomMixtureComponent(min, max, out means[j], out covariances[j]);
                        DebugConfiguration.WriteDebugText("Component {0} regenerated", j);
                        
                        convergenceDetected = true;
                    }
                }

                if (convergenceDetected)
                {
                    lastEstimate = negativeInfinity;
                    continue;
                }

                // Re-estimate weights
                double expectationSum = 0;
                for (int j = 0; j < componentCount; ++j)
                {
                    weights[j] = 0;
                    for (int i = 0; i < data.Length; ++i)
                    {
                        weights[j] += expectations[i, j];
                        expectationSum += expectations[i, j];
                    }
                }
                for (int j = 0; j < componentCount; ++j)
                    weights[j] /= expectationSum;

                // Compute likelihood estimate
                currentEstimate = 0;
                for (int i = 0; i < data.Length; ++i)
                {
                    for (int j = 0; j < componentCount; ++j)
                    {
                        currentEstimate +=
                            expectations[i, j] * (VectorGaussian.GetLogProb(data[i], means[j], covariances[j]) + Math.Log(weights[j]));
                    }
                }

                DebugConfiguration.WriteDebugText("L={0:0.000000}", currentEstimate);
            } while (currentEstimate - lastEstimate > tolerance);

            Mixture<VectorGaussian> result = new Mixture<VectorGaussian>();
            for (int j = 0; j < componentCount; ++j)
                result.Add(VectorGaussian.FromMeanAndVariance(means[j], covariances[j]), weights[j]);

            DebugConfiguration.WriteDebugText("GMM successfully fitted.");

            return result;
        }

        private static void GenerateRandomMixtureComponent(
            MicrosoftResearch.Infer.Maths.Vector min, MicrosoftResearch.Infer.Maths.Vector max, out MicrosoftResearch.Infer.Maths.Vector mean, out PositiveDefiniteMatrix covariance)
        {
            Debug.Assert(min != null && max != null);
            Debug.Assert(min.Count == max.Count);

            MicrosoftResearch.Infer.Maths.Vector diff = max - min;

            mean = MicrosoftResearch.Infer.Maths.Vector.Zero(min.Count);
            for (int i = 0; i < min.Count; ++i)
                mean[i] = min[i] + diff[i] * Random.Double();

            covariance = PositiveDefiniteMatrix.IdentityScaledBy(
                min.Count,
                MicrosoftResearch.Infer.Maths.Vector.InnerProduct(diff, diff) / 16);
        }
    }
}
