using System;

namespace Research.GraphBasedShapePrior
{
    public class SimulatedAnnealingProgressEventArgs<T> : EventArgs
    {
        public T CurrentSolution { get; private set; }

        public T BestSolution { get; private set; }

        public SimulatedAnnealingProgressEventArgs(T currentSolution, T bestSolution)
        {
            if (currentSolution == null)
                throw new ArgumentNullException("currentSolution");
            if (bestSolution == null)
                throw new ArgumentNullException("bestSolution");

            this.CurrentSolution = currentSolution;
            this.BestSolution = bestSolution;
        }
    }
}