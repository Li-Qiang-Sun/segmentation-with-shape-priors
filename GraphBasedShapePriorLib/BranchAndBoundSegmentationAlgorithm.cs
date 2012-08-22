using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using MicrosoftResearch.Infer.Maths;
using Research.GraphBasedShapePrior.Util;
using Vector = Research.GraphBasedShapePrior.Util.Vector;

namespace Research.GraphBasedShapePrior
{
    public class BranchAndBoundSegmentationAlgorithm : SegmentationAlgorithmBase
    {
        private const int DefaultLengthGridSize = 201;

        private const int DefaultAngleGridSize = 201;

        private IShapeEnergyLowerBoundCalculator shapeEnergyLowerBoundCalculator = new ShapeEnergyLowerBoundCalculator(DefaultLengthGridSize, DefaultAngleGridSize);

        private int pregressReportRate = 50;

        private double maxBfsUpperBoundEstimateProbability = 0.5;

        private BranchAndBoundType branchAndBoundType = BranchAndBoundType.Combined;

        private int maxBfsIterationsInCombinedMode = 10000;

        private double minEdgeWidth = 3;

        private double maxEdgeWidth = 20;

        private double maxCoordFreedom = 1;

        private double maxWidthFreedom = 1;

        private bool shouldSwitchToDfs;

        private bool shouldStop;

        private bool isRunning;

        private bool isPaused;

        private Image2D<ObjectBackgroundTerm> shapeUnaryTerms;

        private IShapeTermsLowerBoundCalculator shapeTermsCalculator = new CpuShapeTermsLowerBoundCalculator();

        private DateTime startTime;

        private ShapeConstraints startConstraints;

        public event EventHandler<BreadthFirstBranchAndBoundProgressEventArgs> BreadthFirstBranchAndBoundProgress;

        public event EventHandler<DepthFirstBranchAndBoundProgressEventArgs> DepthFirstBranchAndBoundProgress;

        public event EventHandler BranchAndBoundStarted;

        public event EventHandler<BranchAndBoundCompletedEventArgs> BranchAndBoundCompleted;

        public event EventHandler SwitchToDfsBranchAndBound;

        public int ProgressReportRate
        {
            get { return this.pregressReportRate; }
            set
            {
                if (this.isRunning)
                    throw new InvalidOperationException("You can't change the value of this property while segmentation is running.");
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.pregressReportRate = value;
            }
        }

        public double MaxBfsUpperBoundEstimateProbability
        {
            get { return this.maxBfsUpperBoundEstimateProbability; }
            set
            {
                if (this.isRunning)
                    throw new InvalidOperationException("You can't change the value of this property while segmentation is running.");
                if (value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be in [0, 1] range.");
                this.maxBfsUpperBoundEstimateProbability = value;
            }
        }

        public double MinEdgeWidth
        {
            get { return this.minEdgeWidth; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.minEdgeWidth = value;
            }
        }

        public double MaxEdgeWidth
        {
            get { return this.maxEdgeWidth; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.maxEdgeWidth = value;
            }
        }

        public double MaxCoordFreedom
        {
            get { return this.maxCoordFreedom; }
            set
            {
                if (this.isRunning)
                    throw new InvalidOperationException("You can't change the value of this property while segmentation is running.");
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.maxCoordFreedom = value;
            }
        }

        public double MaxWidthFreedom
        {
            get { return this.maxWidthFreedom; }
            set
            {
                if (this.isRunning)
                    throw new InvalidOperationException("You can't change the value of this property while segmentation is running.");
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.maxWidthFreedom = value;
            }
        }

        public int MaxBfsIterationsInCombinedMode
        {
            get { return this.maxBfsIterationsInCombinedMode; }
            set
            {
                if (this.isRunning)
                    throw new InvalidOperationException("You can't change the value of this property while segmentation is running.");
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.maxBfsIterationsInCombinedMode = value;
            }
        }

        public BranchAndBoundType BranchAndBoundType
        {
            get { return this.branchAndBoundType; }
            set
            {
                if (this.isRunning)
                    throw new InvalidOperationException("You can't change the value of this property while segmentation is running.");
                this.branchAndBoundType = value;
            }
        }

        public IShapeTermsLowerBoundCalculator ShapeTermCalculator
        {
            get { return this.shapeTermsCalculator; }
            set
            {
                if (this.isRunning)
                    throw new InvalidOperationException("You can't change the value of this property while segmentation is running.");
                if (value == null)
                    throw new ArgumentNullException("value");
                this.shapeTermsCalculator = value;
            }
        }

        public IShapeEnergyLowerBoundCalculator ShapeEnergyLowerBoundCalculator
        {
            get { return this.shapeEnergyLowerBoundCalculator; }
            set
            {
                if (this.isRunning)
                    throw new InvalidOperationException("You can't change the value of this property while segmentation is running.");
                if (value == null)
                    throw new ArgumentNullException("value");
                this.shapeEnergyLowerBoundCalculator = value;
            }
        }

        public ShapeConstraints StartConstraints
        {
            get { return this.startConstraints; }
            set
            {
                if (this.isRunning)
                    throw new InvalidOperationException("You can't change the value of this property while segmentation is running.");
                this.startConstraints = value;
            }
        }

        public bool IsPaused
        {
            get { return this.isPaused; }
        }

        public void ForceStop()
        {
            if (!this.isRunning)
                throw new InvalidOperationException("You can't force stop if segmentation is not running.");
            if (this.isPaused)
                throw new InvalidOperationException("You can't stop algorithm if it's paused.");
            this.shouldStop = true;
        }

        public void ForceSwitchToDfsBranchAndBound()
        {
            if (!this.isRunning)
                throw new InvalidOperationException("You can't force switch to dfs b&b if segmentation is not running.");
            if (this.isPaused)
                throw new InvalidOperationException("You can't switch to dfs b&b if algorithm paused.");
            if (this.branchAndBoundType != BranchAndBoundType.Combined)
                throw new InvalidOperationException("You can force switch to dfs b&b only in combined mode.");
            this.shouldSwitchToDfs = true;
        }

        public void Pause()
        {
            if (!this.isRunning)
                throw new InvalidOperationException("You can't pause algorithm if it's not running.");
            if (this.isPaused)
                throw new InvalidOperationException("Algorithm is already paused.");
            this.isPaused = true;
        }

        public void Continue()
        {
            if (!this.isPaused)
                throw new InvalidOperationException("You can't continue algorithm if it's not paused.");
            this.isPaused = false;
        }

        private void WaitIfPaused()
        {
            while (this.isPaused)
                Thread.Sleep(10);
        }

        protected override Image2D<bool> SegmentCurrentImage()
        {
            if (this.minEdgeWidth >= this.maxEdgeWidth)
                throw new InvalidOperationException("Min edge width should be less than max edge width.");
            if (this.startConstraints != null)
            {
                if (this.startConstraints.ShapeStructure != this.ShapeModel.Structure)
                    throw new InvalidOperationException("Given start constraints have shape structure different from the one specified in shape model.");
                // TODO: make this check work
                //foreach (VertexConstraints vertexConstraints in this.startConstraints.VertexConstraints)
                //{
                //    RectangleF imageRectangle =
                //        new RectangleF(0, 0, this.ImageSegmentator.ImageSize.Width, this.ImageSegmentator.ImageSize.Height);
                //    if (!imageRectangle.Contains(vertexConstraints.CoordRectangle))
                //        throw new InvalidOperationException("Given start constraints are not fully inside the segmented image.");
                //}
            }

            this.isRunning = true;
            this.shouldSwitchToDfs = false;
            this.shouldStop = false;
            this.isPaused = false;

            this.shapeUnaryTerms = new Image2D<ObjectBackgroundTerm>(
                this.ImageSegmentator.ImageSize.Width, this.ImageSegmentator.ImageSize.Height);

            ShapeConstraints constraints = this.startConstraints;
            if (constraints == null)
            {
                constraints = ShapeConstraints.CreateFromBounds(
                    this.ShapeModel.Structure,
                    Vector.Zero,
                    new Vector(this.ImageSegmentator.ImageSize.Width, this.ImageSegmentator.ImageSize.Height),
                    this.minEdgeWidth,
                    this.maxEdgeWidth);
            }

            if (this.BranchAndBoundStarted != null)
                this.BranchAndBoundStarted(this, EventArgs.Empty);

            Image2D<bool> result = null;
            switch (BranchAndBoundType)
            {
                case BranchAndBoundType.DepthFirst:
                    result = this.DepthFirstBranchAndBound(constraints);
                    break;
                case BranchAndBoundType.BreadthFirst:
                    result = this.BreadthFirstBranchAndBound(constraints);
                    break;
                case BranchAndBoundType.Combined:
                    result = this.CombinedBranchAndBound(constraints);
                    break;
                default:
                    Debug.Fail("We should never get there");
                    break;
            }

            this.isRunning = false;
            return result;
        }

        private Image2D<bool> BreadthFirstBranchAndBound(ShapeConstraints constraints)
        {
            this.startTime = DateTime.Now;
            DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound started.");

            SortedSet<EnergyBound> front = this.BreadthFirstBranchAndBoundTraverse(constraints, Int32.MaxValue);
            ReportBranchAndBoundCompletion(front.Min);

            if (front.Min.Constraints.CheckIfSatisfied(this.maxCoordFreedom, this.maxWidthFreedom))
            {
                DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound finished in {0}.", DateTime.Now - this.startTime);
                DebugConfiguration.WriteImportantDebugText("Best lower bound is {0:0.0000}", front.Min.Bound);
                
                EnergyBound collapsedBfsSolution = this.CalculateEnergyBound(front.Min.Constraints.Collapse());
                DebugConfiguration.WriteImportantDebugText(
                    "Collapsed solution energy value is {0:0.0000} ({1:0.0000} + {2:0.0000})",
                    collapsedBfsSolution.Bound,
                    collapsedBfsSolution.SegmentationEnergy,
                    collapsedBfsSolution.ShapeEnergy * this.ShapeEnergyWeight);
                return this.ImageSegmentator.GetLastSegmentationMask();
            }

            DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound forced to stop after {0}.", DateTime.Now - this.startTime);
            DebugConfiguration.WriteImportantDebugText("Min energy value achieved is {0:0.0000}", front.Min.Bound);
            return null;
        }

        private Image2D<bool> CombinedBranchAndBound(ShapeConstraints constraints)
        {
            this.startTime = DateTime.Now;
            DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound started.");

            SortedSet<EnergyBound> front = this.BreadthFirstBranchAndBoundTraverse(constraints, this.MaxBfsIterationsInCombinedMode);

            if (this.shouldStop)
            {
                ReportBranchAndBoundCompletion(front.Min);

                DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound forced to stop after {0}.", DateTime.Now - this.startTime);
                DebugConfiguration.WriteImportantDebugText("Min energy value achieved is {0:0.0000}", front.Min.Bound);
                DebugConfiguration.WriteImportantDebugText("Combined branch-and-bound interrupted.");
                return null;
            }

            DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound finished in {0}.", DateTime.Now - this.startTime);
            DebugConfiguration.WriteImportantDebugText(
                "Best lower bound is {0:0.0000} ({1:0.0000} + {2:0.0000})",
                front.Min.Bound,
                front.Min.SegmentationEnergy,
                front.Min.ShapeEnergy * this.ShapeEnergyWeight);

            if (front.Min.Constraints.CheckIfSatisfied(this.maxCoordFreedom, this.maxWidthFreedom))
            {
                ReportBranchAndBoundCompletion(front.Min);
                EnergyBound collapsedBfsSolution = this.CalculateEnergyBound(front.Min.Constraints.Collapse());
                DebugConfiguration.WriteImportantDebugText(
                    "Collapsed solution energy value is {0:0.0000} ({1:0.0000} + {2:0.0000})",
                    collapsedBfsSolution.Bound,
                    collapsedBfsSolution.SegmentationEnergy,
                    collapsedBfsSolution.ShapeEnergy * this.ShapeEnergyWeight);
                return this.ImageSegmentator.GetLastSegmentationMask();
            }

            // Sort front by depth
            List<EnergyBound> sortedFront = new List<EnergyBound>(front);
            sortedFront.Sort((item1, item2) => Math.Sign(item1.Constraints.GetFreedomSum() - item2.Constraints.GetFreedomSum()));

            DebugConfiguration.WriteImportantDebugText("Switching to depth-first branch-and-bound.");

            if (this.SwitchToDfsBranchAndBound != null)
                this.SwitchToDfsBranchAndBound(this, EventArgs.Empty);

            int lowerBoundsCalculated = 0;
            int upperBoundsCalculated = 0;
            int branchesTruncated = 0;
            int iteration = 1;
            int currentBound = 1;
            EnergyBound bestUpperBound = MakeMeanShapeBasedSolutionGuess();
            DebugConfiguration.WriteImportantDebugText("Upper bound from mean shape is {0:0.0000}", bestUpperBound.Bound);
            foreach (EnergyBound energyBound in sortedFront)
            {
                this.DepthFirstBranchAndBoundTraverse(
                    energyBound.Constraints,
                    null,
                    ref bestUpperBound,
                    ref lowerBoundsCalculated,
                    ref upperBoundsCalculated,
                    ref branchesTruncated,
                    ref iteration);

                DebugConfiguration.WriteImportantDebugText("{0} of {1} front items processed by depth-first search.", currentBound, front.Count);
                currentBound += 1;
            }

            DebugConfiguration.WriteImportantDebugText(String.Format("Depth-first branch-and-bound finished in {0}.", DateTime.Now - this.startTime));

            Debug.Assert(bestUpperBound != null);
            ReportBranchAndBoundCompletion(bestUpperBound);

            EnergyBound collapsedDfsSolution = this.CalculateEnergyBound(bestUpperBound.Constraints.Collapse());
            DebugConfiguration.WriteImportantDebugText(
                    "Collapsed solution energy value is {0:0.0000} ({1:0.0000} + {2:0.0000})",
                    collapsedDfsSolution.Bound,
                    collapsedDfsSolution.SegmentationEnergy,
                    collapsedDfsSolution.ShapeEnergy * this.ShapeEnergyWeight);
            return this.ImageSegmentator.GetLastSegmentationMask();
        }

        private Image2D<bool> DepthFirstBranchAndBound(ShapeConstraints constraints)
        {
            this.startTime = DateTime.Now;
            DebugConfiguration.WriteImportantDebugText("Depth-first branch-and-bound started.");

            EnergyBound bestUpperBound = this.MakeMeanShapeBasedSolutionGuess();
            int lowerBoundsCalculated = 0;
            int upperBoundsCalculated = 0;
            int branchesTruncated = 0;
            int iteration = 1;
            this.DepthFirstBranchAndBoundTraverse(
                constraints,
                null,
                ref bestUpperBound,
                ref lowerBoundsCalculated,
                ref upperBoundsCalculated,
                ref branchesTruncated,
                ref iteration);

            ReportBranchAndBoundCompletion(bestUpperBound);
            DebugConfiguration.WriteImportantDebugText(String.Format("Depth-first branch-and-bound finished in {0}.", DateTime.Now - this.startTime));

            EnergyBound collapsedDfsSolution = this.CalculateEnergyBound(bestUpperBound.Constraints.Collapse());
            DebugConfiguration.WriteImportantDebugText(
                    "Collapsed solution energy value is {0:0.0000} ({1:0.0000} + {2:0.0000})",
                    collapsedDfsSolution.Bound,
                    collapsedDfsSolution.SegmentationEnergy,
                    collapsedDfsSolution.ShapeEnergy * this.ShapeEnergyWeight);
            return this.ImageSegmentator.GetLastSegmentationMask();
        }

        private SortedSet<EnergyBound> BreadthFirstBranchAndBoundTraverse(ShapeConstraints constraints, int maxIterations)
        {
            SortedSet<EnergyBound> front = new SortedSet<EnergyBound>();
            front.Add(this.CalculateEnergyBound(constraints));

            int currentIteration = 1;
            DateTime lastOutputTime = startTime;
            int processedConstraintSets = 0, upperBoundGuesses = 0;
            EnergyBound bestUpperBoundGuess = null;
            while (!front.Min.Constraints.CheckIfSatisfied(this.maxCoordFreedom, this.maxWidthFreedom) &&
                   currentIteration <= maxIterations &&
                   !this.shouldStop &&
                   !this.shouldSwitchToDfs)
            {
                this.WaitIfPaused();

                EnergyBound parentLowerBound = front.Min;
                front.Remove(parentLowerBound);

                List<ShapeConstraints> expandedConstraints = parentLowerBound.Constraints.SplitMostFree(this.maxCoordFreedom, this.maxWidthFreedom);
                foreach (ShapeConstraints constraintsSet in expandedConstraints)
                {
                    EnergyBound lowerBound = this.CalculateEnergyBound(constraintsSet);
                    front.Add(lowerBound);

                    // Uncomment for strong invariants check
                    //ObjectBackgroundTerm[,] lowerBoundShapeTerm = new ObjectBackgroundTerm[this.segmentedImage.Width, this.segmentedImage.Height];
                    //for (int i = 0; i < this.segmentedImage.Width; ++i)
                    //    for (int j = 0; j < this.segmentedImage.Height; ++j)
                    //        lowerBoundShapeTerm[i, j] = CpuBranchAndBoundShapeTermsCalculator.CalculateShapeTerm(lowerBound.Constraints, new Point(i, j));
                    //ObjectBackgroundTerm[,] parentLowerBoundShapeTerm = new ObjectBackgroundTerm[this.segmentedImage.Width, this.segmentedImage.Height];
                    //for (int i = 0; i < this.segmentedImage.Width; ++i)
                    //    for (int j = 0; j < this.segmentedImage.Height; ++j)
                    //        parentLowerBoundShapeTerm[i, j] = CpuBranchAndBoundShapeTermsCalculator.CalculateShapeTerm(parentLowerBound.Constraints, new Point(i, j));
                    //for (int i = 0; i < this.segmentedImage.Width; ++i)
                    //    for (int j = 0; j < this.segmentedImage.Height; ++j)
                    //    {
                    //        Debug.Assert(lowerBoundShapeTerm[i, j].ObjectTerm >= parentLowerBoundShapeTerm[i, j].ObjectTerm - 1e-7);
                    //        Debug.Assert(lowerBoundShapeTerm[i, j].BackgroundTerm >= parentLowerBoundShapeTerm[i, j].BackgroundTerm - 1e-7);
                    //        //CalculateShapeTerm(lowerBound.Constraints, new Point(0, 67));
                    //        //CalculateShapeTerm(parentLowerBound.Constraints, new Point(0, 67));
                    //    }

                    // Lower bound should not decrease (check always, it's important!)
                    Trace.Assert(lowerBound.SegmentationEnergy >= parentLowerBound.SegmentationEnergy - 1e-6);
                    Trace.Assert(lowerBound.ShapeEnergy >= parentLowerBound.ShapeEnergy - 1e-6);

                    //this.CalculateEnergyBound(lowerBound.Constraints);
                    //this.CalculateEnergyBound(parentLowerBound.Constraints);

                    // Try to guess solution sometimes, always remember our best guess
                    if (Rand.Double() < this.GetBfsUpperBoundEstimateProbability(constraintsSet))
                    {
                        EnergyBound upperBoundGuess = this.CalculateEnergyBound(constraintsSet.CollapseRandomly());
                        if (bestUpperBoundGuess == null || upperBoundGuess.Bound < bestUpperBoundGuess.Bound)
                            bestUpperBoundGuess = upperBoundGuess;
                        ++upperBoundGuesses;
                    }

                    ++processedConstraintSets;
                }

                // Some debug output
                if (currentIteration % this.ProgressReportRate == 0)
                {
                    DateTime currentTime = DateTime.Now;
                    EnergyBound currentMin = front.Min;

                    DebugConfiguration.WriteDebugText(
                        "On iteration {0} front contains {1} constraint sets.", currentIteration, front.Count);
                    DebugConfiguration.WriteDebugText(
                        "Current lower bound is {0:0.0000} ({1:0.0000} + {2:0.0000}).",
                        currentMin.Bound,
                        currentMin.SegmentationEnergy,
                        currentMin.ShapeEnergy * this.ShapeEnergyWeight);
                    if (bestUpperBoundGuess != null)
                        DebugConfiguration.WriteDebugText("Best known upper bound is {0:0.0000}, {1} guesses total", bestUpperBoundGuess.Bound, upperBoundGuesses);
                    double processingSpeed = processedConstraintSets / (currentTime - lastOutputTime).TotalSeconds;
                    DebugConfiguration.WriteDebugText("Processing speed is {0:0.000} items per sec", processingSpeed);

                    double maxVertexConstraintsFreedom = currentMin.Constraints.VertexConstraints.Max(c => c.Freedom);
                    double maxEdgeConstraintsFreedom = currentMin.Constraints.EdgeConstraints.Max(c => c.Freedom);
                    DebugConfiguration.WriteDebugText(
                        "Max vertex freedom: {0:0.00}, max edge freedom: {1:0.00}",
                        maxVertexConstraintsFreedom,
                        maxEdgeConstraintsFreedom);

                    DebugConfiguration.WriteDebugText("Elapsed time: {0}", DateTime.Now - this.startTime);

                    DebugConfiguration.WriteDebugText();

                    this.ReportBreadthFirstSearchStatus(front, processingSpeed, bestUpperBoundGuess);

                    lastOutputTime = currentTime;
                    processedConstraintSets = 0;
                }

                currentIteration += 1;
            }

            return front;
        }

        private void DepthFirstBranchAndBoundTraverse(
            ShapeConstraints currentNode,
            EnergyBound currentNodeLowerBound,
            ref EnergyBound bestUpperBound,
            ref int lowerBoundsCalculated,
            ref int upperBoundsCalculated,
            ref int branchesTruncated,
            ref int iteration)
        {
            Debug.Assert(!currentNode.CheckIfSatisfied(this.maxCoordFreedom, this.maxWidthFreedom));

            this.WaitIfPaused();

            if (iteration % this.ProgressReportRate == 0)
            {
                // Write some text
                DebugConfiguration.WriteDebugText(
                    "On iteration {0} best upper bound is {1:0.0000} ({2:0.0000} + {3:0.0000}).",
                    iteration,
                    bestUpperBound.Bound,
                    bestUpperBound.SegmentationEnergy,
                    bestUpperBound.ShapeEnergy * this.ShapeEnergyWeight);
                if (currentNodeLowerBound != null) // In case it was the initial node
                {
                    DebugConfiguration.WriteDebugText(
                        "Current lower bound: {0:0.0000} ({1:0.0000} + {2:0.0000})",
                        currentNodeLowerBound.Bound,
                        currentNodeLowerBound.SegmentationEnergy,
                        currentNodeLowerBound.ShapeEnergy * this.ShapeEnergyWeight);
                }
                DebugConfiguration.WriteDebugText("Lower bounds calculated: {0}", lowerBoundsCalculated);
                DebugConfiguration.WriteDebugText("Upper bounds calculated: {0}", upperBoundsCalculated);
                DebugConfiguration.WriteDebugText("Branches truncated: {0}", branchesTruncated);
                DebugConfiguration.WriteDebugText();

                // Report status
                this.ReportDepthFirstSearchStatus(currentNode, bestUpperBound);
            }

            List<ShapeConstraints> children = currentNode.SplitMostFree(this.maxCoordFreedom, this.maxWidthFreedom);

            // Traverse only subtrees with good lower bounds
            for (int i = 0; i < children.Count; ++i)
            {
                EnergyBound lowerBound = this.CalculateEnergyBound(children[i]);
                lowerBoundsCalculated += 1;

                // No minimum in that subtree
                if (lowerBound.Bound >= bestUpperBound.Bound)
                {
                    branchesTruncated += 1;
                    continue;
                }

                if (children[i].CheckIfSatisfied(this.maxCoordFreedom, this.maxWidthFreedom))
                {
                    // This lower bound is exact and, so, it's upper bound
                    if (lowerBound.Bound < bestUpperBound.Bound)
                    {
                        bestUpperBound = lowerBound;
                        DebugConfiguration.WriteImportantDebugText("Upper bound updated at iteration {0} to {1}.", iteration, bestUpperBound.Bound);
                    }

                    continue;
                }

                iteration += 1;
                this.DepthFirstBranchAndBoundTraverse(
                    children[i],
                    lowerBound,
                    ref bestUpperBound,
                    ref lowerBoundsCalculated,
                    ref upperBoundsCalculated,
                    ref branchesTruncated,
                    ref iteration);
            }
        }

        private double GetBfsUpperBoundEstimateProbability(ShapeConstraints constraintsSet)
        {
            double maxVertexConstraintsFreedom = constraintsSet.VertexConstraints.Max(c => c.Freedom);
            double maxEdgeConstraintsFreedom = constraintsSet.EdgeConstraints.Max(c => c.Freedom);
            double prob = 1;
            prob *= Math.Min(1, this.MaxCoordFreedom / maxVertexConstraintsFreedom);
            prob *= Math.Min(1, this.MaxWidthFreedom / maxEdgeConstraintsFreedom);
            prob *= this.maxBfsUpperBoundEstimateProbability;
            prob = Math.Pow(prob, 1.3);
            return prob;
        }

        private EnergyBound MakeMeanShapeBasedSolutionGuess()
        {
            Shape shape = this.ShapeModel.FitMeanShape(this.ImageSegmentator.ImageSize.Width, this.ImageSegmentator.ImageSize.Height);
            ShapeConstraints constraintsSet = ShapeConstraints.CreateFromShape(shape);
            return this.CalculateEnergyBound(constraintsSet);
        }

        private void GetUnaryTermMasks(ShapeConstraints constraints, out Image segmentationMask, out Image unaryTermsMask, out Image shapeTermsMask)
        {
            this.SegmentImageWithConstraints(constraints);
            segmentationMask = Image2D.ToRegularImage(this.ImageSegmentator.GetLastSegmentationMask());
            const double unaryTermDeviation = 20;
            unaryTermsMask = Image2D.ToRegularImage(this.ImageSegmentator.GetLastUnaryTerms(), -unaryTermDeviation, unaryTermDeviation);
            double shapeTermDeviation =
                this.ShapeUnaryTermWeight > 1e-6
                    ? unaryTermDeviation / this.ShapeUnaryTermWeight
                    : 1000;
            shapeTermsMask = Image2D.ToRegularImage(this.ImageSegmentator.GetLastShapeTerms(), -shapeTermDeviation, shapeTermDeviation);
        }

        private void ReportDepthFirstSearchStatus(ShapeConstraints currentNode, EnergyBound upperBound)
        {
            // In order to report various masks we need to segment images again
            Image currentMask, currentUnaryTermsImage, currentShapeTermsImage;
            this.GetUnaryTermMasks(currentNode, out currentMask, out currentUnaryTermsImage, out currentShapeTermsImage);
            Image upperBoundMask, upperBoundUnaryTermsImage, upperBoundShapeTermsImage;
            this.GetUnaryTermMasks(upperBound.Constraints, out upperBoundMask, out upperBoundUnaryTermsImage, out upperBoundShapeTermsImage);

            // Raise status report event
            DepthFirstBranchAndBoundProgressEventArgs args = new DepthFirstBranchAndBoundProgressEventArgs(
                upperBound.Bound, currentMask, currentUnaryTermsImage, currentShapeTermsImage, currentNode, upperBoundMask);
            if (this.DepthFirstBranchAndBoundProgress != null)
                this.DepthFirstBranchAndBoundProgress.Invoke(this, args);
        }

        // TODO: remove code duplication here
        private void ReportBreadthFirstSearchStatus(
            SortedSet<EnergyBound> front,
            double processingSpeed,
            EnergyBound bestUpperBoundGuess)
        {
            EnergyBound currentMin = front.Min;

            // In order to report various masks we need to segment image again
            Image currentMask, currentUnaryTermsImage, currentShapeTermsImage;
            this.GetUnaryTermMasks(currentMin.Constraints, out currentMask, out currentUnaryTermsImage, out currentShapeTermsImage);
            Image upperBoundMask = null;
            if (bestUpperBoundGuess != null)
            {
                Image upperBoundUnaryTermsImage, upperBoundShapeTermsImage;
                this.GetUnaryTermMasks(bestUpperBoundGuess.Constraints, out upperBoundMask, out upperBoundUnaryTermsImage, out upperBoundShapeTermsImage);
            }

            // Raise status report event
            BreadthFirstBranchAndBoundProgressEventArgs args = new BreadthFirstBranchAndBoundProgressEventArgs(
                front.Min.Bound, front.Count, processingSpeed, currentMask, currentUnaryTermsImage, currentShapeTermsImage, currentMin.Constraints, upperBoundMask);
            if (this.BreadthFirstBranchAndBoundProgress != null)
                this.BreadthFirstBranchAndBoundProgress.Invoke(this, args);
        }

        private void ReportBranchAndBoundCompletion(EnergyBound result)
        {
            // In order to report various masks we need to segment image again
            Image collapsedMask, collapsedUnaryTermsImage, collapsedShapeTermsImage;
            this.GetUnaryTermMasks(result.Constraints.Collapse(), out collapsedMask, out collapsedUnaryTermsImage, out collapsedShapeTermsImage);

            BranchAndBoundCompletedEventArgs args = new BranchAndBoundCompletedEventArgs(
                collapsedMask, collapsedUnaryTermsImage, collapsedShapeTermsImage, result.Constraints, result.Bound);
            if (this.BranchAndBoundCompleted != null)
                this.BranchAndBoundCompleted.Invoke(this, args);
        }

        private EnergyBound CalculateEnergyBound(ShapeConstraints constraintsSet)
        {
            this.shapeTermsCalculator.CalculateShapeTerms(this.ShapeModel, constraintsSet, this.shapeUnaryTerms);
            double segmentationEnergy = this.ImageSegmentator.SegmentImageWithShapeTerms((x, y) => this.shapeUnaryTerms[x, y]);
            double shapeEnergy = this.shapeEnergyLowerBoundCalculator.CalculateLowerBound(this.ImageSegmentator.ImageSize, this.ShapeModel, constraintsSet);
            return new EnergyBound(constraintsSet, shapeEnergy, segmentationEnergy, this.ShapeEnergyWeight);
        }

        private Image2D<bool> SegmentImageWithConstraints(ShapeConstraints constraintsSet)
        {
            this.shapeTermsCalculator.CalculateShapeTerms(this.ShapeModel, constraintsSet, this.shapeUnaryTerms);
            this.ImageSegmentator.SegmentImageWithShapeTerms((x, y) => this.shapeUnaryTerms[x, y]);
            return this.ImageSegmentator.GetLastSegmentationMask();
        }

        private class EnergyBound : IComparable<EnergyBound>
        {
            public ShapeConstraints Constraints { get; private set; }

            public double Bound { get; private set; }

            public double ShapeEnergy { get; private set; }

            public double SegmentationEnergy { get; private set; }

            private static long instanceCount;

            private readonly long instanceId;

            public EnergyBound(
                ShapeConstraints constraints,
                double shapeEnergy,
                double segmentationEnergy,
                double shapeEnergyWeight)
            {
                Debug.Assert(constraints != null);

                this.Constraints = constraints;
                this.ShapeEnergy = shapeEnergy;
                this.SegmentationEnergy = segmentationEnergy;
                this.Bound = shapeEnergy * shapeEnergyWeight + segmentationEnergy;

                this.instanceId = Interlocked.Increment(ref instanceCount);
            }

            public int CompareTo(EnergyBound other)
            {
                if (this.Bound < other.Bound)
                    return -1;
                if (this.Bound > other.Bound)
                    return 1;
                return Comparer<long>.Default.Compare(this.instanceId, other.instanceId);
            }
        }
    }
}
