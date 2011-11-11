using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using MicrosoftResearch.Infer.Distributions;

namespace Research.GraphBasedShapePrior
{
    public class BranchAndBoundSegmentatorBase : SegmentatorBase
    {
        private int statusReportRate = 50;

        private int bfsFrontSaveRate = Int32.MaxValue;

        private BranchAndBoundType branchAndBoundType = BranchAndBoundType.Combined;

        private int maxBfsIterationsInCombinedMode = 10000;

        private int lengthGridSize = 300;

        private int angleGridSize = 720;

        private bool shouldSwitchToDfs;

        private bool shouldStop;

        private bool isRunning;

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

        public void ForceStop()
        {
            if (!this.isRunning)
                throw new InvalidOperationException("You can't force stop if segmentation is not running.");
            this.shouldStop = true;
        }

        public void ForceSwitchToDfsBranchAndBound()
        {
            if (!this.isRunning)
                throw new InvalidOperationException("You can't force switch to dfs b&b if segmentation is not running.");
            if (this.branchAndBoundType != BranchAndBoundType.Combined)
                throw new InvalidOperationException("You can force switch to dfs b&b only in combined mode.");
            this.shouldSwitchToDfs = true;
        }

        protected BranchAndBoundSegmentatorBase()
        {
        }

        protected override Image2D<bool> SegmentImageImpl(
            Image2D<Color> shrinkedImage,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel)
        {
            this.isRunning = true;
            this.shouldSwitchToDfs = false;
            this.shouldStop = false;

            if (this.BranchAndBoundStarted != null)
                this.BranchAndBoundStarted(this, EventArgs.Empty);

            switch (BranchAndBoundType)
            {
                case BranchAndBoundType.DepthFirst:
                    return this.DepthFirstBranchAndBound(
                        shrinkedImage, backgroundColorModel, objectColorModel);
                case BranchAndBoundType.BreadthFirst:
                    return this.BreadthFirstBranchAndBound(
                        shrinkedImage, backgroundColorModel, objectColorModel);
                case BranchAndBoundType.Combined:
                    return this.CombinedBranchAndBound(
                        shrinkedImage, backgroundColorModel, objectColorModel);
            }

            Debug.Fail("We should never get there");
            return null;
        }

        private Image2D<bool> BreadthFirstBranchAndBound(
            Image2D<Color> shrinkedImage,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel)
        {
            DateTime startTime = DateTime.Now;
            DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound started.");

            SortedSet<EnergyBound> front = this.BreadthFirstBranchAndBoundTraverse(
                shrinkedImage, backgroundColorModel, objectColorModel, Int32.MaxValue);

            if (front.Min.Constraints.CheckIfSatisfied())
            {
                DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound finished in {0}.", DateTime.Now - startTime);
                DebugConfiguration.WriteImportantDebugText("Min energy value is {0}", front.Min.Bound);
                return this.SegmentImageWithConstraints(front.Min.Constraints, shrinkedImage, backgroundColorModel, objectColorModel).SegmentationMask;
            }

            DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound forced to stop after {0}.", DateTime.Now - startTime);
            DebugConfiguration.WriteImportantDebugText("Min energy value achieved is {0}", front.Min.Bound);
            return null;
        }

        private Image2D<bool> CombinedBranchAndBound(
            Image2D<Color> shrinkedImage,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel)
        {
            DateTime startTime = DateTime.Now;
            DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound started.");

            SortedSet<EnergyBound> front = this.BreadthFirstBranchAndBoundTraverse(
                shrinkedImage, backgroundColorModel, objectColorModel, this.MaxBfsIterationsInCombinedMode);

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
                return this.SegmentImageWithConstraints(front.Min.Constraints, shrinkedImage, backgroundColorModel, objectColorModel).SegmentationMask;

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
            EnergyBound bestUpperBound = MakeMeanShapeBasedSolutionGuess(shrinkedImage, backgroundColorModel, objectColorModel);
            DebugConfiguration.WriteImportantDebugText("Upper bound from mean shape is {0}", bestUpperBound.Bound);
            foreach (EnergyBound energyBound in sortedFront)
            {
                this.DepthFirstBranchAndBoundTraverse(
                    shrinkedImage,
                    backgroundColorModel,
                    objectColorModel,
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
            return this.SegmentImageWithConstraints(bestUpperBound.Constraints, shrinkedImage, backgroundColorModel, objectColorModel).SegmentationMask;
        }

        private Image2D<bool> DepthFirstBranchAndBound(
            Image2D<Color> shrinkedImage,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel)
        {
            VertexConstraintSet initialConstraints = VertexConstraintSet.ConstraintToImage(
                this.ShapeModel, shrinkedImage.Rectangle.Size);

            DateTime startTime = DateTime.Now;
            DebugConfiguration.WriteImportantDebugText("Depth-first branch-and-bound started.");

            EnergyBound bestUpperBound = this.MakeMeanShapeBasedSolutionGuess(shrinkedImage, backgroundColorModel, objectColorModel);
            int lowerBoundsCalculated = 0;
            int upperBoundsCalculated = 0;
            int branchesTruncated = 0;
            int iteration = 1;
            this.DepthFirstBranchAndBoundTraverse(
                shrinkedImage,
                backgroundColorModel,
                objectColorModel,
                initialConstraints,
                null,
                ref bestUpperBound,
                ref lowerBoundsCalculated,
                ref upperBoundsCalculated,
                ref branchesTruncated,
                ref iteration);

            DebugConfiguration.WriteImportantDebugText(String.Format("Depth-first branch-and-bound finished in {0}.", DateTime.Now - startTime));
            return this.SegmentImageWithConstraints(bestUpperBound.Constraints, shrinkedImage, backgroundColorModel, objectColorModel).SegmentationMask;
        }

        private SortedSet<EnergyBound> BreadthFirstBranchAndBoundTraverse(
            Image2D<Color> shrinkedImage,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel,
            int maxIterations)
        {
            VertexConstraintSet initialConstraints = VertexConstraintSet.ConstraintToImage(
                this.ShapeModel, shrinkedImage.Rectangle.Size);
            SortedSet<EnergyBound> front = new SortedSet<EnergyBound>();
            front.Add(this.CalculateEnergyBound(
                initialConstraints, shrinkedImage, backgroundColorModel, objectColorModel));

            int currentIteration = 1;
            DateTime startTime = DateTime.Now;
            DateTime lastOutputTime = startTime;
            int processedConstraintSets = 0;
            while (!front.Min.Constraints.CheckIfSatisfied() && currentIteration <= maxIterations && !this.shouldStop && !this.shouldSwitchToDfs)
            {
                EnergyBound parentLowerBound = front.Min;
                front.Remove(parentLowerBound);

                List<VertexConstraintSet> expandedConstraints = parentLowerBound.Constraints.SplitMostViolated();
                foreach (VertexConstraintSet constraintsSet in expandedConstraints)
                {
                    EnergyBound lowerBound = this.CalculateEnergyBound(
                        constraintsSet, shrinkedImage, backgroundColorModel, objectColorModel);
                    lowerBound.CleanupSegmentationMask();
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
                    Trace.Assert(lowerBound.SegmentationEnergy >= parentLowerBound.SegmentationEnergy - 1e-6);
                    Trace.Assert(lowerBound.ShapeEnergy >= parentLowerBound.ShapeEnergy - 1e-6);

                    ++processedConstraintSets;
                }

                if (currentIteration % this.BfsFrontSaveRate == 0)
                {
                    string directory = string.Format("front_{0:00000}", currentIteration);
                    SaveEnergyBounds(shrinkedImage, front, directory);
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
                    this.ReportBreadthFirstSearchStatus(shrinkedImage, front, processingSpeed);

                    lastOutputTime = currentTime;
                    processedConstraintSets = 0;
                }

                currentIteration += 1;
            }

            // Always report status in the end
            this.ReportBreadthFirstSearchStatus(shrinkedImage, front, 0);

            return front;
        }

        private void DepthFirstBranchAndBoundTraverse(
            Image2D<Color> shrinkedImage,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel,
            VertexConstraintSet currentNode,
            EnergyBound currentNodeLowerBound,
            ref EnergyBound bestUpperBound,
            ref int lowerBoundsCalculated,
            ref int upperBoundsCalculated,
            ref int branchesTruncated,
            ref int iteration)
        {
            Debug.Assert(!currentNode.CheckIfSatisfied());

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
                this.ReportDepthFirstSearchStatus(shrinkedImage, currentNode, bestUpperBound);
            }

            List<VertexConstraintSet> children = currentNode.SplitMostViolated();

            // Traverse only subtrees with good lower bounds
            for (int i = 0; i < children.Count; ++i)
            {
                EnergyBound upperBound = this.CalculateEnergyBound(
                    children[i].GuessSolution(), shrinkedImage, backgroundColorModel, objectColorModel);
                upperBoundsCalculated += 1;
                if (upperBound.Bound < bestUpperBound.Bound)
                {
                    bestUpperBound = upperBound;
                    DebugConfiguration.WriteImportantDebugText("Upper bound updated at iteration {0} to {1}.", iteration, bestUpperBound.Bound);
                }

                // Lower bound equals upper bound for leafs
                if (children[i].CheckIfSatisfied())
                    continue;

                EnergyBound lowerBound = this.CalculateEnergyBound(
                    children[i], shrinkedImage, backgroundColorModel, objectColorModel);
                lowerBoundsCalculated += 1;

                // No minimum in that subtree
                if (lowerBound.Bound >= bestUpperBound.Bound)
                {
                    branchesTruncated += 1;
                    continue;
                }

                iteration += 1;
                this.DepthFirstBranchAndBoundTraverse(
                    shrinkedImage,
                    backgroundColorModel,
                    objectColorModel,
                    children[i],
                    lowerBound,
                    ref bestUpperBound,
                    ref lowerBoundsCalculated,
                    ref upperBoundsCalculated,
                    ref branchesTruncated,
                    ref iteration);
            }
        }

        private EnergyBound MakeMeanShapeBasedSolutionGuess(
            Image2D<Color> shrinkedImage, Mixture<VectorGaussian> backgroundColorModel, Mixture<VectorGaussian> objectColorModel)
        {
            Shape shape = this.ShapeModel.FitMeanShape(shrinkedImage.Rectangle.Size);
            VertexConstraintSet constraintsSet = VertexConstraintSet.CreateFromShape(shape);
            return this.CalculateEnergyBound(constraintsSet, shrinkedImage, backgroundColorModel, objectColorModel);
        }

        private void ReportDepthFirstSearchStatus(Image2D<Color> shrinkedImage, VertexConstraintSet currentNode, EnergyBound upperBound)
        {
            // Draw current constraints on top of an image
            Image statusImage = Image2D.ToRegularImage(shrinkedImage);
            using (Graphics graphics = Graphics.FromImage(statusImage))
                currentNode.Draw(graphics);

            // Raise status report event
            DepthFirstBranchAndBoundStatusEventArgs args = new DepthFirstBranchAndBoundStatusEventArgs(
                upperBound.Bound, statusImage, upperBound.SegmentationMask);
            if (this.DepthFirstBranchAndBoundStatus != null)
                this.DepthFirstBranchAndBoundStatus.Invoke(this, args);
        }

        private void ReportBreadthFirstSearchStatus(Image2D<Color> shrinkedImage, SortedSet<EnergyBound> front, double processingSpeed)
        {
            // Draw current constraints on top of an image
            Image statusImage = Image2D.ToRegularImage(shrinkedImage);
            using (Graphics graphics = Graphics.FromImage(statusImage))
                front.Min.Constraints.Draw(graphics);

            // Raise status report event
            BreadthFirstBranchAndBoundStatusEventArgs args = new BreadthFirstBranchAndBoundStatusEventArgs(front.Min.Bound, front.Count, processingSpeed, statusImage);
            if (this.BreadthFirstBranchAndBoundStatus != null)
                this.BreadthFirstBranchAndBoundStatus.Invoke(this, args);
        }

        private void SaveEnergyBounds(Image2D<Color> shrinkedImage, IEnumerable<EnergyBound> bounds, string dir)
        {
            int index = 0;
            Directory.CreateDirectory(dir);
            using (StreamWriter writer = new StreamWriter(Path.Combine(dir, "stats.txt")))
            {
                foreach (EnergyBound energyBound in bounds)
                {
                    Image image = Image2D.ToRegularImage(shrinkedImage);
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

        private EnergyBound CalculateEnergyBound(
            VertexConstraintSet constraintsSet,
            Image2D<Color> shrinkedImage,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel)
        {
            ImageSegmentationInfo boundInfo = this.SegmentImageWithConstraints(
                constraintsSet, shrinkedImage, backgroundColorModel, objectColorModel);

            double segmentationEnergy = boundInfo.Energy;
            double shapeEnergy = this.CalculateMinShapeEnergy(constraintsSet, shrinkedImage.Rectangle.Size);    
            return new EnergyBound(constraintsSet, shapeEnergy, segmentationEnergy, this.ShapeEnergyWeight, boundInfo.SegmentationMask);
        }

        private ImageSegmentationInfo SegmentImageWithConstraints(
            VertexConstraintSet constraintsSet,
            Image2D<Color> shrinkedImage,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel)
        {
            Image2D<Tuple<double, double>> shapeUnaryPotentials = new Image2D<Tuple<double, double>>(shrinkedImage.Width, shrinkedImage.Height);
            this.PrepareShapeUnaryPotentials(constraintsSet, shapeUnaryPotentials);
            
            return this.SegmentImage(
                shrinkedImage,
                backgroundColorModel,
                objectColorModel,
                point => shapeUnaryPotentials[point.X, point.Y],
                false);
        }

        // TODO: make this shit protected
        // TODO: make this shit abstract after fix of some strage compiler bug
        public virtual void PrepareShapeUnaryPotentials(VertexConstraintSet constraintsSet, Image2D<Tuple<double, double>> result)
        {
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
                double energy = childTransform.GetByCoords(length, angle);

                // Check other possible angle representations
                bool angleIsZero = childTransform.CoordToGridIndexY(angle) == childTransform.CoordToGridIndexY(0);
                if (angleIsZero)
                {
                    // Zero has two representations
                    energy = Math.Min(childTransform.GetByCoords(length, -Math.PI * 2), energy);
                    energy = Math.Min(childTransform.GetByCoords(length, Math.PI * 2), energy);
                }
                else
                {
                    // Other angles have single representation
                    double otherAngle = angle > 0 ? angle - Math.PI * 2 : angle + Math.PI * 2;
                    energy = Math.Min(childTransform.GetByCoords(length, otherAngle), energy);
                }

                energySum += energy;
            }

            return energySum;
        }

        private double GetBestVertexRadius(int vertexIndex, VertexConstraintSet constraintsSet, double objectSize)
        {
            VertexConstraint constraints = constraintsSet.GetConstraintsForVertex(vertexIndex);
            double bestRadius = this.ShapeModel.GetVertexParams(vertexIndex).RadiusToObjectSizeRatio * objectSize;
            return MathHelper.Trunc(bestRadius, constraints.MinRadius, constraints.MinRadius);
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
                childDistanceTransforms.Add(childTransform);
            }

            Range lengthRange, angleRange;
            constraintsSet.DetermineEdgeLimits(currentEdgeIndex, out lengthRange, out angleRange);

            ShapeEdgePairParams pairParams = this.ShapeModel.GetEdgeParams(parentEdgeIndex, currentEdgeIndex);

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
                    if (!currentLengthRange.IntersectsWith(lengthRange) || !currentAngleRange.IntersectsWith(angleRange))
                    {
                        return 1e+20; // Return something close to infinity
                    }

                    return CalculdateMinEdgeEnergy(length, angle, childDistanceTransforms);
                };

            return new GeneralizedDistanceTransform2D(
                new Vector(0, -Math.PI * 2),
                new Vector(maxScaledLength, Math.PI * 2),
                new Size(this.LengthGridSize, this.AngleGridSize), 
                1.0 / MathHelper.Sqr(pairParams.LengthDeviation),
                1.0 / MathHelper.Sqr(pairParams.AngleDeviation),
                penaltyFunction);
        }

        private class EnergyBound : IComparable<EnergyBound>
        {
            public VertexConstraintSet Constraints { get; private set; }

            public double Bound { get; private set; }

            public double ShapeEnergy { get; private set; }

            public double SegmentationEnergy { get; private set; }

            public Image2D<bool> SegmentationMask { get; private set; }

            private static long instanceCount;

            private readonly long instanceId;

            public EnergyBound(
                VertexConstraintSet constraints,
                double shapeEnergy,
                double segmentationEnergy,
                double shapeEnergyWeight,
                Image2D<bool> segmentationMask)
            {
                Debug.Assert(constraints != null);
                Debug.Assert(segmentationMask != null);

                this.Constraints = constraints;
                this.ShapeEnergy = shapeEnergy;
                this.SegmentationEnergy = segmentationEnergy;
                this.SegmentationMask = segmentationMask;
                this.Bound = shapeEnergy * shapeEnergyWeight + segmentationEnergy;

                this.instanceId = Interlocked.Increment(ref instanceCount);
            }

            public void CleanupSegmentationMask()
            {
                this.SegmentationMask = null;
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
