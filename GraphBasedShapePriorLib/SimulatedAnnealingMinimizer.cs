using System;
using Random = Research.GraphBasedShapePrior.Util.Random;

namespace Research.GraphBasedShapePrior
{
    public class SimulatedAnnealingMinimizer<T>
    {
        private int maxIterations;
        private int maxStallingIterations;
        private int reannealingInterval;
        private double startTemperature;
        private int reportRate;

        public SimulatedAnnealingMinimizer()
        {
            this.maxIterations = 5000;
            this.maxStallingIterations = 1000;
            this.reannealingInterval = 500;
            this.startTemperature = 100;
            this.reportRate = 100;
        }

        public event EventHandler<SimulatedAnnealingProgressEventArgs<T>> AnnealingProgress;

        public int MaxIterations
        {
            get { return maxIterations; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                maxIterations = value;
            }
        }

        public int MaxStallingIterations
        {
            get { return maxStallingIterations; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                maxStallingIterations = value;
            }
        }

        public int ReannealingInterval
        {
            get { return reannealingInterval; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                reannealingInterval = value;
            }
        }

        public int ReportRate
        {
            get { return reportRate; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                reportRate = value;
            }
        }

        public double StartTemperature
        {
            get { return startTemperature; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                startTemperature = value;
            }
        }

        public T Run(T startSolution, Func<T, double, T> mutationFunction, Func<T, double> objectiveFunction)
        {
            if (startSolution == null)
                throw new ArgumentNullException("startSolution");
            if (mutationFunction == null)
                throw new ArgumentNullException("mutationFunction");
            if (objectiveFunction == null)
                throw new ArgumentNullException("objectiveFunction");
            
            T bestSolution = startSolution;
            double minObjective = objectiveFunction(bestSolution);

            int lastUpdateIteration = 0;
            int currentIteration = 0;
            int iterationsFromLastReannealing = 0;
            int acceptedSolutionsFromLastReannealing = 0;
            double prevObjective = minObjective;
            T prevSolution = bestSolution;
            while (currentIteration < this.MaxIterations && currentIteration - lastUpdateIteration < this.MaxStallingIterations)
            {
                double temperature = CalcTemperature(iterationsFromLastReannealing);
                T currentSolution = mutationFunction(prevSolution, temperature);
                double currentObjective = objectiveFunction(currentSolution);
                double acceptanceProb = CalcAcceptanceProbability(prevObjective, currentObjective, temperature);
                
                if (Random.Double() < acceptanceProb)
                {
                    prevObjective = currentObjective;
                    prevSolution = currentSolution;
                    ++acceptedSolutionsFromLastReannealing;
                }

                if (currentObjective < minObjective)
                {
                    lastUpdateIteration = currentIteration;
                    minObjective = currentObjective;
                    bestSolution = currentSolution;

                    DebugConfiguration.WriteDebugText("Best solution update: {0:0.000}", minObjective);
                }

                ++currentIteration;
                if (currentIteration % this.reportRate == 0)
                {
                    DebugConfiguration.WriteDebugText("Iteration {0}", currentIteration);

                    if (this.AnnealingProgress != null)
                        this.AnnealingProgress(this, new SimulatedAnnealingProgressEventArgs<T>(currentSolution, bestSolution));
                }

                if (acceptedSolutionsFromLastReannealing >= this.ReannealingInterval)
                {
                    iterationsFromLastReannealing = 0;
                    acceptedSolutionsFromLastReannealing = 0;
                    DebugConfiguration.WriteDebugText("Reannealing");
                }
                else
                {
                    ++iterationsFromLastReannealing;
                }
            }

            return bestSolution;
        }

        private double CalcTemperature(int iterationFromReannealing)
        {
            return startTemperature / Math.Log(iterationFromReannealing + 2, 2); // iterationFromReannealing is zero-based
        }

        private static double CalcAcceptanceProbability(double oldObjective, double newObjective, double temperature)
        {
            return newObjective < oldObjective ? 1 : 1 / (1 + Math.Exp((newObjective - oldObjective) / temperature));
        }
    }
}
