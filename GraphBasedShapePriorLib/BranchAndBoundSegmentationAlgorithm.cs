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

        private double minVertexRadius = 3;

        private double maxVertexRadius = 20;

        private bool shouldSwitchToDfs;

        private bool shouldStop;

        private bool isRunning;

        private bool isPaused;

        private Image2D<Color> segmentedImage;

        private Image2D<ObjectBackgroundTerm> shapeUnaryTerms;

        private IBranchAndBoundShapeTermsCalculator shapeTermsCalculator = new CpuBranchAndBoundShapeTermsCalculator();

        public BranchAndBoundSegmentationAlgorithm()
        {
        }

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

        public double MinVertexRadius
        {
            get { return minVertexRadius; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.minVertexRadius = value;
            }
        }

        public double MaxVertexRadius
        {
            get { return maxVertexRadius; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.maxVertexRadius = value;
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
            Image2D<Color> shrinkedImage,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel)
        {
            Debug.Assert(shrinkedImage != null);
            Debug.Assert(backgroundColorModel != null);
            Debug.Assert(objectColorModel != null);

            if (this.minVertexRadius >= this.maxVertexRadius)
                throw new InvalidOperationException("Min vertex radius should be less than max vertex radius.");

            this.isRunning = true;
            this.shouldSwitchToDfs = false;
            this.shouldStop = false;
            this.isPaused = false;

            this.segmentedImage = shrinkedImage;
            this.shapeUnaryTerms = new Image2D<ObjectBackgroundTerm>(shrinkedImage.Width, shrinkedImage.Height);

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
            VertexConstraintSet initialConstraints = VertexConstraintSet.CreateFromBounds(
                this.ShapeModel, Vector.Zero, new Vector(this.segmentedImage.Width, this.segmentedImage.Height), this.minVertexRadius, this.maxVertexRadius);

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
            VertexConstraintSet initialConstraints = VertexConstraintSet.CreateFromBounds(
                this.ShapeModel, Vector.Zero, new Vector(this.segmentedImage.Width, this.segmentedImage.Height), this.minVertexRadius, this.maxVertexRadius);
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

                List<VertexConstraintSet> expandedConstraints = parentLowerBound.Constraints.SplitMostViolated();
                foreach (VertexConstraintSet constraintsSet in expandedConstraints)
                {
                    EnergyBound lowerBound = this.CalculateEnergyBound(constraintsSet);
                    front.Add(lowerBound);

                    // Uncomment for strong invariants check
                    //Tuple<double, double>[,] lowerBoundShapeTerm = new Tuple<double, double>[shrinkedImage.Width, shrinkedImage.Height];
                    //for (int i = 0; i < shrinkedImage.Width; ++i)
                    //    for (int j = 0; j < shrinkedImage.Height; ++j)
                    //        lowerBoundShapeTerm[i, j] = CalculateShapeTerm(lowerBound.Constraints, new Point(i, j));
                    //Tuple<double, double>[,] parentLowerBoundShapeTerm = new Tuple<double, double>[shrinkedImage.Width, shrinkedImage.Height];
                    //for (int i = 0; i < shrinkedImage.Width; ++i)
                    //    for (int j = 0; j < shrinkedImage.Height; ++j)
                    //        parentLowerBoundShapeTerm[i, j] = CalculateShapeTerm(parentLowerBound.Constraints, new Point(i, j));
                    //for (int i = 0; i < shrinkedImage.Width; ++i)
                    //    for (int j = 0; j < shrinkedImage.Height; ++j)
                    //    {
                    //        Debug.Assert(lowerBoundShapeTerm[i, j].Item1 >= parentLowerBoundShapeTerm[i, j].Item1 - 1e-7);
                    //        Debug.Assert(lowerBoundShapeTerm[i, j].Item2 >= parentLowerBoundShapeTerm[i, j].Item2 - 1e-7);
                    //        //CalculateShapeTerm(lowerBound.Constraints, new Point(0, 67));
                    //        //CalculateShapeTerm(parentLowerBound.Constraints, new Point(0, 67));
                    //    }

                    // Lower bound should not decrease (check always, it's important!)
                    Debug.Assert(lowerBound.SegmentationEnergy >= parentLowerBound.SegmentationEnergy - 1e-6);
                    Debug.Assert(lowerBound.ShapeEnergy >= parentLowerBound.ShapeEnergy - 1e-6);

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

                    // Compute constraint violations
                    double maxRadiusConstraintViolation = 0, maxCoordConstraintViolation = 0;
                    for (int vertex = 0; vertex < this.ShapeModel.VertexCount; ++vertex)
                    {
                        VertexConstraint vertexConstraints = currentMin.Constraints.GetConstraintsForVertex(vertex);
                        maxRadiusConstraintViolation = Math.Max(
                            maxRadiusConstraintViolation, vertexConstraints.MaxRadius - vertexConstraints.MinRadius);
                        maxCoordConstraintViolation = Math.Max(
                            maxCoordConstraintViolation, vertexConstraints.MaxCoord.X - vertexConstraints.MinCoord.X);
                        maxCoordConstraintViolation = Math.Max(
                            maxCoordConstraintViolation, vertexConstraints.MaxCoord.Y - vertexConstraints.MinCoord.Y);
                    }

                    DebugConfiguration.WriteDebugText("Current constraint violations: {0:0.0} (radius), {1:0.0} (coord)", maxRadiusConstraintViolation, maxCoordConstraintViolation);
                    DebugConfiguration.WriteDebugText();

                    // Report status
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
            VertexConstraintSet currentNode,
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

            List<VertexConstraintSet> children = currentNode.SplitMostViolated();

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
            VertexConstraintSet constraintsSet = VertexConstraintSet.CreateFromShape(shape);
            return this.CalculateEnergyBound(constraintsSet);
        }

        private void GetUnaryTermMasks(VertexConstraintSet constraints, out Image segmentationMask, out Image unaryTermsMask, out Image shapeTermsMask)
        {
            this.SegmentImageWithConstraints(constraints);
            segmentationMask = Image2D.ToRegularImage(this.ImageSegmentator.GetLastSegmentationMask());
            const double unaryTermDeviation = 20;
            unaryTermsMask = Image2D.ToRegularImage(this.ImageSegmentator.GetLastUnaryTerms(), -unaryTermDeviation, unaryTermDeviation);
            shapeTermsMask = Image2D.ToRegularImage(this.ImageSegmentator.GetLastShapeTerms(), -unaryTermDeviation, unaryTermDeviation);
        }

        private void ReportDepthFirstSearchStatus(VertexConstraintSet currentNode, EnergyBound upperBound)
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

        private EnergyBound CalculateEnergyBound(VertexConstraintSet constraintsSet)
        {
            double segmentationEnergy = this.CalculateMinSegmentationEnergy(constraintsSet);
            double shapeEnergy = this.CalculateMinShapeEnergy(constraintsSet, this.segmentedImage.Rectangle.Size);
            return new EnergyBound(constraintsSet, shapeEnergy, segmentationEnergy, this.ShapeEnergyWeight);
        }

        private double CalculateMinSegmentationEnergy(VertexConstraintSet constraintsSet)
        {
            this.shapeTermsCalculator.CalculateShapeTerms(constraintsSet, this.shapeUnaryTerms);
            return this.ImageSegmentator.SegmentImageWithShapeTerms(point => this.shapeUnaryTerms[point.X, point.Y]);
        }

        private Image2D<bool> SegmentImageWithConstraints(VertexConstraintSet constraintsSet)
        {
            this.shapeTermsCalculator.CalculateShapeTerms(constraintsSet, this.shapeUnaryTerms);
            this.ImageSegmentator.SegmentImageWithShapeTerms(point => this.shapeUnaryTerms[point.X, point.Y]);
            return this.ImageSegmentator.GetLastSegmentationMask();
        }

        // TODO: make this shit private
        public double CalculateMinShapeEnergy(VertexConstraintSet constraintsSet, Size imageSize)
        {
            double objectSize = ImageSizeToObjectSizeEstimate(imageSize);

            // Here we use the fact that energy can be separated into vertex energy that depends on radii
            // and edge energy that depends on edge vertex positions

            double minVertexEnergySum = 0;
            for (int vertexIndex = 0; vertexIndex < this.ShapeModel.VertexCount; ++vertexIndex)
                minVertexEnergySum += this.ShapeModel.CalculateVertexEnergyTerm(
                    vertexIndex,
                    objectSize,
                    this.GetBestVertexRadius(vertexIndex, constraintsSet, objectSize));

            double minEdgeEnergySum = 0;
            if (this.ShapeModel.PairwiseEdgeConstraintCount > 0)
            {
                double maxRatio1 = (from edgePair in this.ShapeModel.ConstrainedEdgePairs
                                    select this.ShapeModel.GetEdgeParams(edgePair.Item1, edgePair.Item2).LengthRatio).Max();
                double maxRatio2 = (from edgePair in this.ShapeModel.ConstrainedEdgePairs
                                    select 1.0 / this.ShapeModel.GetEdgeParams(edgePair.Item1, edgePair.Item2).LengthRatio).Max();
                double maxRatio = Math.Max(maxRatio1, maxRatio2);
                double maxEdgeLength = Math.Sqrt(imageSize.Width * imageSize.Width + imageSize.Height * imageSize.Height);
                double maxScaledLength = maxEdgeLength * maxRatio;

                List<GeneralizedDistanceTransform2D> childTransforms = new List<GeneralizedDistanceTransform2D>();
                foreach (int edgeIndex in this.ShapeModel.IterateNeighboringEdgeIndices(0))
                    childTransforms.Add(CalculateMinEnergiesForAllParentEdges(constraintsSet, 0, edgeIndex, maxScaledLength));

                Range lengthRange, angleRange;
                constraintsSet.DetermineEdgeLimits(0, out lengthRange, out angleRange);

                minEdgeEnergySum = Double.PositiveInfinity;
                GeneralizedDistanceTransform2D transform = childTransforms[0];
                for (int lengthGridIndex = transform.CoordToGridIndexX(lengthRange.Left);
                    lengthGridIndex <= transform.CoordToGridIndexX(lengthRange.Right);
                    ++lengthGridIndex)
                {
                    double length = transform.GridIndexToCoordX(lengthGridIndex);

                    if (angleRange.Outside)
                    {
                        for (int angleGridIndex = transform.CoordToGridIndexY(angleRange.Right);
                            angleGridIndex <= transform.CoordToGridIndexY(Math.PI);
                            ++angleGridIndex)
                        {
                            double angle = transform.GridIndexToCoordY(angleGridIndex);
                            minEdgeEnergySum = Math.Min(minEdgeEnergySum, CalculdateMinEdgeEnergy(length, angle, childTransforms));
                        }

                        for (int angleGridIndex = transform.CoordToGridIndexY(-Math.PI);
                            angleGridIndex <= transform.CoordToGridIndexY(angleRange.Left);
                            ++angleGridIndex)
                        {
                            double angle = transform.GridIndexToCoordY(angleGridIndex);
                            minEdgeEnergySum = Math.Min(minEdgeEnergySum, CalculdateMinEdgeEnergy(length, angle, childTransforms));
                        }
                    }
                    else
                    {
                        for (int angleGridIndex = transform.CoordToGridIndexY(angleRange.Left);
                            angleGridIndex <= transform.CoordToGridIndexY(angleRange.Right);
                            ++angleGridIndex)
                        {
                            double angle = transform.GridIndexToCoordY(angleGridIndex);
                            minEdgeEnergySum = Math.Min(minEdgeEnergySum, CalculdateMinEdgeEnergy(length, angle, childTransforms));
                        }
                    }
                }
            }

            return minVertexEnergySum + minEdgeEnergySum;
        }

        private double CalculdateMinEdgeEnergy(double length, double angle, IEnumerable<GeneralizedDistanceTransform2D> transforms)
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

        private double GetBestVertexRadius(int vertexIndex, VertexConstraintSet constraintsSet, double objectSize)
        {
            VertexConstraint constraints = constraintsSet.GetConstraintsForVertex(vertexIndex);
            double bestRadius = this.ShapeModel.GetVertexParams(vertexIndex).RadiusToObjectSizeRatio * objectSize;
            return MathHelper.Trunc(bestRadius, constraints.MinRadius, constraints.MaxRadius);
        }

        private GeneralizedDistanceTransform2D CalculateMinEnergiesForAllParentEdges(
            VertexConstraintSet constraintsSet,
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
                    constraintsSet, currentEdgeIndex, neighborEdgeIndex, maxScaledLength);
                Debug.Assert(childTransform.IsComputed);
                childDistanceTransforms.Add(childTransform);
            }

            Range lengthRange, angleRange;
            constraintsSet.DetermineEdgeLimits(currentEdgeIndex, out lengthRange, out angleRange);

            ShapeEdgePairParams pairParams = this.ShapeModel.GetEdgeParams(parentEdgeIndex, currentEdgeIndex);
            
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
                    Range currentLengthRange = new Range(length - lengthRadius, length + lengthRadius, false);
                    Range currentAngleRange = new Range(angle - angleRadius, angle + angleRadius, false);
                    const double eps = 1e-6;
                    if (angle >= Math.PI + eps || angle <= -Math.PI - eps ||
                        !currentLengthRange.IntersectsWith(lengthRange) || !currentAngleRange.IntersectsWith(angleRange))
                    {
                        return 1e+20; // Return something close to infinity
                    }

                    return CalculdateMinEdgeEnergy(length, angle, childDistanceTransforms);
                };

            transform.Compute(
                1.0 / MathHelper.Sqr(pairParams.LengthDeviation),
                1.0 / MathHelper.Sqr(pairParams.AngleDeviation),
                penaltyFunction);

            return transform;
        }

        private class EnergyBound : IComparable<EnergyBound>
        {
            public VertexConstraintSet Constraints { get; private set; }

            public double Bound { get; private set; }

            public double ShapeEnergy { get; private set; }

            public double SegmentationEnergy { get; private set; }

            private static long instanceCount;

            private readonly long instanceId;

            public EnergyBound(
                VertexConstraintSet constraints,
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
