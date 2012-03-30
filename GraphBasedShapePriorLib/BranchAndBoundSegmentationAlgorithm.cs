using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using MicrosoftResearch.Infer.Distributions;

namespace Research.GraphBasedShapePrior
{
    public class BranchAndBoundSegmentationAlgorithm : SegmentationAlgorithmBase
    {
        private int statusReportRate = 50;

        private int bfsFrontSaveRate = Int32.MaxValue;

        private BranchAndBoundType branchAndBoundType = BranchAndBoundType.Combined;

        private int maxBfsIterationsInCombinedMode = 10000;

        private int lengthGridSize = 201;

        private int angleGridSize = 201;

        private double minEdgeWidth = 3;

        private double maxEdgeWidth = 20;

        private bool shouldSwitchToDfs;

        private bool shouldStop;

        private bool isRunning;

        private bool isPaused;

        private Image2D<Color> segmentedImage;

        private Image2D<ObjectBackgroundTerm> shapeUnaryTerms;

        private IBranchAndBoundShapeTermsCalculator shapeTermsCalculator = new CpuBranchAndBoundShapeTermsCalculator();

        public event EventHandler<BreadthFirstBranchAndBoundStatusEventArgs> BreadthFirstBranchAndBoundStatus;

        public event EventHandler<DepthFirstBranchAndBoundStatusEventArgs> DepthFirstBranchAndBoundStatus;

        public event EventHandler BranchAndBoundStarted;

        public event EventHandler SwitchToDfsBranchAndBound;

        public int StatusReportRate
        {
            get { return this.statusReportRate; }
            set
            {
                if (this.isRunning)
                    throw new InvalidOperationException("You can't change the value of this property while segmentation is running.");
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.statusReportRate = value;
            }
        }

        public int BfsFrontSaveRate
        {
            get { return this.bfsFrontSaveRate; }
            set
            {
                if (this.isRunning)
                    throw new InvalidOperationException("You can't change the value of this property while segmentation is running.");
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.bfsFrontSaveRate = value;
            }
        }

        public int LengthGridSize
        {
            get { return this.lengthGridSize; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.lengthGridSize = value;
            }
        }

        public int AngleGridSize
        {
            get { return this.angleGridSize; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.angleGridSize = value;
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

        public IBranchAndBoundShapeTermsCalculator ShapeTermCalculator
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

        protected override Image2D<bool> SegmentImageImpl(
            Image2D<Color> segmentedImage,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel)
        {
            Debug.Assert(segmentedImage != null);
            Debug.Assert(backgroundColorModel != null);
            Debug.Assert(objectColorModel != null);

            if (this.minEdgeWidth >= this.maxEdgeWidth)
                throw new InvalidOperationException("Min edge width should be less than max edge width.");

            this.isRunning = true;
            this.shouldSwitchToDfs = false;
            this.shouldStop = false;
            this.isPaused = false;

            this.segmentedImage = segmentedImage;
            this.shapeUnaryTerms = new Image2D<ObjectBackgroundTerm>(segmentedImage.Width, segmentedImage.Height);

            if (this.BranchAndBoundStarted != null)
                this.BranchAndBoundStarted(this, EventArgs.Empty);

            switch (BranchAndBoundType)
            {
                case BranchAndBoundType.DepthFirst:
                    return this.DepthFirstBranchAndBound();
                case BranchAndBoundType.BreadthFirst:
                    return this.BreadthFirstBranchAndBound();
                case BranchAndBoundType.Combined:
                    return this.CombinedBranchAndBound();
            }

            Debug.Fail("We should never get there");
            return null;
        }

        private Image2D<bool> BreadthFirstBranchAndBound()
        {
            DateTime startTime = DateTime.Now;
            DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound started.");

            SortedSet<EnergyBound> front = this.BreadthFirstBranchAndBoundTraverse(Int32.MaxValue);

            if (front.Min.Constraints.CheckIfSatisfied())
            {
                DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound finished in {0}.", DateTime.Now - startTime);
                DebugConfiguration.WriteImportantDebugText("Min energy value is {0}", front.Min.Bound);
                return this.SegmentImageWithConstraints(front.Min.Constraints);
            }

            DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound forced to stop after {0}.", DateTime.Now - startTime);
            DebugConfiguration.WriteImportantDebugText("Min energy value achieved is {0}", front.Min.Bound);
            return null;
        }

        private Image2D<bool> CombinedBranchAndBound()
        {
            DateTime startTime = DateTime.Now;
            DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound started.");

            SortedSet<EnergyBound> front = this.BreadthFirstBranchAndBoundTraverse(this.MaxBfsIterationsInCombinedMode);

            if (this.shouldStop)
            {
                DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound forced to stop after {0}.", DateTime.Now - startTime);
                DebugConfiguration.WriteImportantDebugText("Min energy value achieved is {0}", front.Min.Bound);
                DebugConfiguration.WriteImportantDebugText("Combined branch-and-bound interrupted.");
                return null;
            }

            DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound finished in {0}.", DateTime.Now - startTime);
            DebugConfiguration.WriteImportantDebugText("Best lower bound is {0}", front.Min.Bound);

            if (front.Min.Constraints.CheckIfSatisfied())
                return this.SegmentImageWithConstraints(front.Min.Constraints);

            // Sort front by depth
            List<EnergyBound> sortedFront = new List<EnergyBound>(front);
            sortedFront.Sort((item1, item2) => Math.Sign(item1.Constraints.GetViolationSum() - item2.Constraints.GetViolationSum()));

            startTime = DateTime.Now;
            DebugConfiguration.WriteImportantDebugText("Switching to depth-first branch-and-bound.");

            if (this.SwitchToDfsBranchAndBound != null)
                this.SwitchToDfsBranchAndBound(this, EventArgs.Empty);

            int lowerBoundsCalculated = 0;
            int upperBoundsCalculated = 0;
            int branchesTruncated = 0;
            int iteration = 1;
            int currentBound = 1;
            EnergyBound bestUpperBound = MakeMeanShapeBasedSolutionGuess();
            DebugConfiguration.WriteImportantDebugText("Upper bound from mean shape is {0}", bestUpperBound.Bound);
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

            DebugConfiguration.WriteImportantDebugText(String.Format("Depth-first branch-and-bound finished in {0}.", DateTime.Now - startTime));

            Debug.Assert(bestUpperBound != null);
            return this.SegmentImageWithConstraints(bestUpperBound.Constraints);
        }

        private Image2D<bool> DepthFirstBranchAndBound()
        {
            ShapeConstraints initialConstraints = ShapeConstraints.CreateFromBounds(
                this.ShapeModel, Vector.Zero, new Vector(this.segmentedImage.Width, this.segmentedImage.Height), this.minEdgeWidth, this.maxEdgeWidth);

            DateTime startTime = DateTime.Now;
            DebugConfiguration.WriteImportantDebugText("Depth-first branch-and-bound started.");

            EnergyBound bestUpperBound = this.MakeMeanShapeBasedSolutionGuess();
            int lowerBoundsCalculated = 0;
            int upperBoundsCalculated = 0;
            int branchesTruncated = 0;
            int iteration = 1;
            this.DepthFirstBranchAndBoundTraverse(
                initialConstraints,
                null,
                ref bestUpperBound,
                ref lowerBoundsCalculated,
                ref upperBoundsCalculated,
                ref branchesTruncated,
                ref iteration);

            DebugConfiguration.WriteImportantDebugText(String.Format("Depth-first branch-and-bound finished in {0}.", DateTime.Now - startTime));
            return this.SegmentImageWithConstraints(bestUpperBound.Constraints);
        }

        private SortedSet<EnergyBound> BreadthFirstBranchAndBoundTraverse(int maxIterations)
        {
            ShapeConstraints initialConstraints = ShapeConstraints.CreateFromBounds(
                this.ShapeModel, Vector.Zero, new Vector(this.segmentedImage.Width, this.segmentedImage.Height), this.minEdgeWidth, this.maxEdgeWidth);
            SortedSet<EnergyBound> front = new SortedSet<EnergyBound>();
            front.Add(this.CalculateEnergyBound(initialConstraints));

            int currentIteration = 1;
            DateTime startTime = DateTime.Now;
            DateTime lastOutputTime = startTime;
            int processedConstraintSets = 0;
            while (!front.Min.Constraints.CheckIfSatisfied() && currentIteration <= maxIterations && !this.shouldStop && !this.shouldSwitchToDfs)
            {
                this.WaitIfPaused();

                EnergyBound parentLowerBound = front.Min;
                front.Remove(parentLowerBound);

                List<ShapeConstraints> expandedConstraints = parentLowerBound.Constraints.SplitMostViolated();
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

                    ++processedConstraintSets;
                }

                if (currentIteration % this.BfsFrontSaveRate == 0)
                {
                    string directory = string.Format("front_{0:00000}", currentIteration);
                    SaveEnergyBounds(front, directory);
                }

                // Some debug output
                if (currentIteration % this.StatusReportRate == 0)
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
                    double processingSpeed = processedConstraintSets / (currentTime - lastOutputTime).TotalSeconds;
                    DebugConfiguration.WriteDebugText("Processing speed is {0:0.000} items per sec", processingSpeed);

                    double maxVertexConstraintsFreedom = currentMin.Constraints.VertexConstraints.Max(c => c.FreedomLevel);
                    double maxEdgeConstraintsFreedom = currentMin.Constraints.EdgeConstraints.Max(c => c.FreedomLevel);
                    DebugConfiguration.WriteDebugText(
                        "Max vertex freedom: {0:0.00}, max edge freedom: {1:0.00}",
                        maxVertexConstraintsFreedom,
                        maxEdgeConstraintsFreedom);

                    DebugConfiguration.WriteDebugText();

                    // Report status););
                    this.ReportBreadthFirstSearchStatus(front, processingSpeed);

                    lastOutputTime = currentTime;
                    processedConstraintSets = 0;
                }

                currentIteration += 1;
            }

            // Always report status in the end
            this.ReportBreadthFirstSearchStatus(front, 0);

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
            Debug.Assert(!currentNode.CheckIfSatisfied());

            this.WaitIfPaused();

            if (iteration % this.StatusReportRate == 0)
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

            List<ShapeConstraints> children = currentNode.SplitMostViolated();

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

                if (children[i].CheckIfSatisfied())
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

        private EnergyBound MakeMeanShapeBasedSolutionGuess()
        {
            Shape shape = this.ShapeModel.FitMeanShape(this.segmentedImage.Rectangle.Size);
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
            // Draw current constraints on top of an image
            Image statusImage = Image2D.ToRegularImage(this.segmentedImage);
            using (Graphics graphics = Graphics.FromImage(statusImage))
                currentNode.Draw(graphics);

            // In order to report various masks we need to segment image again
            Image segmentationMask, unaryTermsImage, shapeTermsImage;
            this.GetUnaryTermMasks(currentNode, out segmentationMask, out unaryTermsImage, out shapeTermsImage);

            // Raise status report event
            DepthFirstBranchAndBoundStatusEventArgs args = new DepthFirstBranchAndBoundStatusEventArgs(
                upperBound.Bound, statusImage, segmentationMask, unaryTermsImage, shapeTermsImage);
            if (this.DepthFirstBranchAndBoundStatus != null)
                this.DepthFirstBranchAndBoundStatus.Invoke(this, args);
        }

        private void ReportBreadthFirstSearchStatus(
            SortedSet<EnergyBound> front,
            double processingSpeed)
        {
            EnergyBound currentMin = front.Min;

            // Draw current constraints on top of an image
            Image statusImage = Image2D.ToRegularImage(this.segmentedImage);
            using (Graphics graphics = Graphics.FromImage(statusImage))
                currentMin.Constraints.Draw(graphics);

            // In order to report various masks we need to segment image again
            Image segmentationMask, unaryTermsImage, shapeTermsImage;
            this.GetUnaryTermMasks(currentMin.Constraints, out segmentationMask, out unaryTermsImage, out shapeTermsImage);

            // Raise status report event
            BreadthFirstBranchAndBoundStatusEventArgs args = new BreadthFirstBranchAndBoundStatusEventArgs(
                front.Min.Bound, front.Count, processingSpeed, statusImage, segmentationMask, unaryTermsImage, shapeTermsImage);
            if (this.BreadthFirstBranchAndBoundStatus != null)
                this.BreadthFirstBranchAndBoundStatus.Invoke(this, args);
        }

        private void SaveEnergyBounds(IEnumerable<EnergyBound> bounds, string dir)
        {
            int index = 0;
            Directory.CreateDirectory(dir);
            using (StreamWriter writer = new StreamWriter(Path.Combine(dir, "stats.txt")))
            {
                foreach (EnergyBound energyBound in bounds)
                {
                    Image image = Image2D.ToRegularImage(this.segmentedImage);
                    using (Graphics graphics = Graphics.FromImage(image))
                        energyBound.Constraints.Draw(graphics);
                    image.Save(Path.Combine(dir, string.Format("{0:00}.png", index)));

                    writer.WriteLine("{0}\t{1}\t{2}", index, energyBound.Bound, energyBound.Constraints.GetViolationSum());
                    index += 1;

                    // Save only first 50 items
                    if (index >= 50)
                        break;
                }
            }
        }

        private EnergyBound CalculateEnergyBound(ShapeConstraints constraintsSet)
        {
            double segmentationEnergy = this.CalculateMinSegmentationEnergy(constraintsSet);
            double shapeEnergy = this.CalculateMinShapeEnergy(constraintsSet);
            return new EnergyBound(constraintsSet, shapeEnergy, segmentationEnergy, this.ShapeEnergyWeight);
        }

        private double CalculateMinSegmentationEnergy(ShapeConstraints constraintsSet)
        {
            this.shapeTermsCalculator.CalculateShapeTerms(constraintsSet, this.shapeUnaryTerms);
            return this.ImageSegmentator.SegmentImageWithShapeTerms(point => this.shapeUnaryTerms[point.X, point.Y]);
        }

        private Image2D<bool> SegmentImageWithConstraints(ShapeConstraints constraintsSet)
        {
            this.shapeTermsCalculator.CalculateShapeTerms(constraintsSet, this.shapeUnaryTerms);
            this.ImageSegmentator.SegmentImageWithShapeTerms(point => this.shapeUnaryTerms[point.X, point.Y]);
            return this.ImageSegmentator.GetLastSegmentationMask();
        }

        private void CalculateLengthAngleRanges(ShapeConstraints shapeConstraints, out List<Range> lengthRanges, out List<Range> angleRanges)
        {
            lengthRanges = new List<Range>();
            angleRanges = new List<Range>();

            for (int i = 0; i < shapeConstraints.ShapeModel.Edges.Count; ++i)
            {
                Range lengthRange, angleRange;
                shapeConstraints.DetermineEdgeLimits(i, out lengthRange, out angleRange);
                Debug.Assert(!lengthRange.Outside);

                lengthRanges.Add(lengthRange);
                angleRanges.Add(angleRange);
            }
        }
        
        // TODO: extract this stuff to a separate class (IShapeEnergyLowerBoundComputer)
        public double CalculateMinShapeEnergy(ShapeConstraints shapeConstraints)
        {
            double minEnergySum = Double.PositiveInfinity;
            
            List<Range> lengthRanges, angleRanges;
            CalculateLengthAngleRanges(shapeConstraints, out lengthRanges, out angleRanges);
            double maxEdgeLength = lengthRanges.Max(range => range.Right);

            if (this.ShapeModel.PairwiseEdgeConstraintCount == 0)
            {
                // Shape is forced to be a fully-connected tree, so this |E|=1 is the only case possible
                Debug.Assert(this.ShapeModel.Edges.Count == 1);
                Debug.Assert(lengthRanges.Count == 1);
                Debug.Assert(angleRanges.Count == 1);

                // Calculate best possible edge width penalty
                EdgeConstraints edgeConstraints = shapeConstraints.EdgeConstraints[0];
                ShapeEdgeParams edgeParams = this.ShapeModel.GetEdgeParams(0);
                Range scaledLengthRange = new Range(
                    lengthRanges[0].Left * edgeParams.WidthToEdgeLengthRatio,
                    lengthRanges[0].Right * edgeParams.WidthToEdgeLengthRatio);
                Range widthRange = new Range(edgeConstraints.MinWidth, edgeConstraints.MaxWidth);
                if (scaledLengthRange.IntersectsWith(widthRange))
                    minEnergySum = 0;
                else if (scaledLengthRange.Left > widthRange.Right)
                    minEnergySum = MathHelper.Sqr(scaledLengthRange.Left - widthRange.Right);
                else
                    minEnergySum = MathHelper.Sqr(scaledLengthRange.Right - widthRange.Left);

                return minEnergySum;
            }

            double maxRatio1 = (from edgePair in this.ShapeModel.ConstrainedEdgePairs
                                select this.ShapeModel.GetEdgePairParams(edgePair.Item1, edgePair.Item2).LengthRatio).Max();
            double maxRatio2 = (from edgePair in this.ShapeModel.ConstrainedEdgePairs
                                select 1.0 / this.ShapeModel.GetEdgePairParams(edgePair.Item1, edgePair.Item2).LengthRatio).Max();
            double maxRatio = Math.Max(maxRatio1, maxRatio2);
            double maxScaledLength = maxEdgeLength * maxRatio;

            List<GeneralizedDistanceTransform2D> childTransforms = new List<GeneralizedDistanceTransform2D>();
            foreach (int edgeIndex in this.ShapeModel.IterateNeighboringEdgeIndices(0))
                childTransforms.Add(CalculateMinEnergiesForAllParentEdges(shapeConstraints, 0, edgeIndex, maxScaledLength));


            GeneralizedDistanceTransform2D transform = childTransforms[0];
            for (int lengthGridIndex = transform.CoordToGridIndexX(lengthRanges[0].Left);
                lengthGridIndex <= transform.CoordToGridIndexX(lengthRanges[0].Right);
                ++lengthGridIndex)
            {
                double length = transform.GridIndexToCoordX(lengthGridIndex);
                double currentMinEnergySum = Double.PositiveInfinity;

                if (angleRanges[0].Outside)
                {
                    for (int angleGridIndex = transform.CoordToGridIndexY(angleRanges[0].Right);
                        angleGridIndex <= transform.CoordToGridIndexY(Math.PI);
                        ++angleGridIndex)
                    {
                        double angle = transform.GridIndexToCoordY(angleGridIndex);
                        currentMinEnergySum = Math.Min(currentMinEnergySum, CalculdateMinPairwiseEdgeEnergy(length, angle, childTransforms));
                    }

                    for (int angleGridIndex = transform.CoordToGridIndexY(-Math.PI);
                        angleGridIndex <= transform.CoordToGridIndexY(angleRanges[0].Left);
                        ++angleGridIndex)
                    {
                        double angle = transform.GridIndexToCoordY(angleGridIndex);
                        currentMinEnergySum = Math.Min(currentMinEnergySum, CalculdateMinPairwiseEdgeEnergy(length, angle, childTransforms));
                    }
                }
                else
                {
                    for (int angleGridIndex = transform.CoordToGridIndexY(angleRanges[0].Left);
                        angleGridIndex <= transform.CoordToGridIndexY(angleRanges[0].Right);
                        ++angleGridIndex)
                    {
                        double angle = transform.GridIndexToCoordY(angleGridIndex);
                        currentMinEnergySum = Math.Min(currentMinEnergySum, CalculdateMinPairwiseEdgeEnergy(length, angle, childTransforms));
                    }
                }

                double unaryEdgeEnergy = CalculateMinUnaryEdgeEnergy(0, shapeConstraints, length);
                minEnergySum = Math.Min(minEnergySum, currentMinEnergySum + unaryEdgeEnergy);
            }

            return minEnergySum;
        }

        private double CalculdateMinPairwiseEdgeEnergy(double length, double angle, IEnumerable<GeneralizedDistanceTransform2D> transforms)
        {
            double energySum = 0;
            foreach (GeneralizedDistanceTransform2D childTransform in transforms)
            {
                double energy = childTransform.GetValueByCoords(length, angle);

                // Check other possible angle representations
                bool angleIsZero = childTransform.CoordToGridIndexY(angle) == childTransform.CoordToGridIndexY(0);
                if (angleIsZero)
                {
                    // Zero has two representations
                    energy = Math.Min(childTransform.GetValueByCoords(length, -Math.PI * 2), energy);
                    energy = Math.Min(childTransform.GetValueByCoords(length, Math.PI * 2), energy);
                }
                else
                {
                    // Other angles have single representation
                    double otherAngle = angle > 0 ? angle - Math.PI * 2 : angle + Math.PI * 2;
                    energy = Math.Min(childTransform.GetValueByCoords(length, otherAngle), energy);
                }

                energySum += energy;
            }

            return energySum;
        }

        private double CalculateMinUnaryEdgeEnergy(int edgeIndex, ShapeConstraints shapeConstraints, double edgeLength)
        {
            double bestWidth = edgeLength * this.ShapeModel.GetEdgeParams(edgeIndex).WidthToEdgeLengthRatio;
            EdgeConstraints edgeConstraints = shapeConstraints.EdgeConstraints[edgeIndex];
            bestWidth = MathHelper.Trunc(bestWidth, edgeConstraints.MinWidth, edgeConstraints.MaxWidth);
            return this.ShapeModel.CalculateEdgeWidthEnergyTerm(edgeIndex, bestWidth, edgeLength);
        }

        private GeneralizedDistanceTransform2D CalculateMinEnergiesForAllParentEdges(
            ShapeConstraints shapeConstraints,
            int parentEdgeIndex,
            int currentEdgeIndex,
            double maxScaledLength)
        {
            List<GeneralizedDistanceTransform2D> childDistanceTransforms = new List<GeneralizedDistanceTransform2D>();
            foreach (int neighborEdgeIndex in this.ShapeModel.IterateNeighboringEdgeIndices(currentEdgeIndex))
            {
                // Iterate only through children
                if (neighborEdgeIndex == parentEdgeIndex)
                    continue;

                GeneralizedDistanceTransform2D childTransform = CalculateMinEnergiesForAllParentEdges(
                    shapeConstraints, currentEdgeIndex, neighborEdgeIndex, maxScaledLength);
                Debug.Assert(childTransform.IsComputed);
                childDistanceTransforms.Add(childTransform);
            }

            Range lengthRange, angleRange;
            shapeConstraints.DetermineEdgeLimits(currentEdgeIndex, out lengthRange, out angleRange);

            ShapeEdgePairParams pairParams = this.ShapeModel.GetEdgePairParams(parentEdgeIndex, currentEdgeIndex);

            GeneralizedDistanceTransform2D transform = new GeneralizedDistanceTransform2D(
                new Vector(0, -Math.PI * 2),
                new Vector(maxScaledLength, Math.PI * 2),
                new Size(this.LengthGridSize, this.AngleGridSize));

            Func<double, double, double, double, double> penaltyFunction =
                (scaledLength, shiftedAngle, scaledLengthRadius, shiftedAngleRadius) =>
                {
                    double length = scaledLength / pairParams.LengthRatio;
                    double angle = shiftedAngle + pairParams.MeanAngle;
                    double lengthRadius = scaledLengthRadius / pairParams.LengthRatio;
                    double angleRadius = shiftedAngleRadius;

                    // Disallow invalid configurations
                    Range currentLengthRange = new Range(length - lengthRadius, length + lengthRadius);
                    Range currentAngleRange = new Range(angle - angleRadius, angle + angleRadius);
                    const double eps = 1e-6;
                    if (angle >= Math.PI + eps || angle <= -Math.PI - eps ||
                        !currentLengthRange.IntersectsWith(lengthRange) || !currentAngleRange.IntersectsWith(angleRange))
                    {
                        return 1e+20; // Return something close to infinity
                    }

                    return
                        CalculateMinUnaryEdgeEnergy(currentEdgeIndex, shapeConstraints, length) +
                        CalculdateMinPairwiseEdgeEnergy(length, angle, childDistanceTransforms);
                };

            transform.Compute(
                1.0 / MathHelper.Sqr(pairParams.LengthDeviation),
                1.0 / MathHelper.Sqr(pairParams.AngleDeviation),
                penaltyFunction);

            return transform;
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
