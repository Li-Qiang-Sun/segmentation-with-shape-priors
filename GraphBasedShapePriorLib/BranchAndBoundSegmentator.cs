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
    public class BranchAndBoundSegmentator : SegmentatorBase
    {
        public event EventHandler<BreadthFirstBranchAndBoundStatusEventArgs> BreadthFirstBranchAndBoundStatus;

        public event EventHandler<DepthFirstBranchAndBoundStatusEventArgs> DepthFirstBranchAndBoundStatus;

        public int StatusReportRate { get; set; }

        public BranchAndBoundType BranchAndBoundType { get; set; }

        public int MaxBfsIterationsInCombinedMode { get; set; }

        public BranchAndBoundSegmentator()
        {
            this.StatusReportRate = 50;
            this.MaxBfsIterationsInCombinedMode = 3000;
            this.BranchAndBoundType = BranchAndBoundType.BreadthFirst;
        }

        protected override Image2D<bool> SegmentImageImpl(
            Image2D<Color> shrinkedImage,
            double objectSize,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel)
        {
            Debug.Assert(this.StatusReportRate > 0);
            Debug.Assert(this.MaxBfsIterationsInCombinedMode > 0);

            switch (BranchAndBoundType)
            {
                case BranchAndBoundType.DepthFirst:
                    return this.DepthFirstBranchAndBound(
                        shrinkedImage, objectSize, backgroundColorModel, objectColorModel);
                case BranchAndBoundType.BreadthFirst:
                    return this.BreadthFirstBranchAndBound(
                        shrinkedImage, objectSize, backgroundColorModel, objectColorModel);
                case BranchAndBoundType.Combined:
                    return this.CombinedBranchAndBound(
                        shrinkedImage, objectSize, backgroundColorModel, objectColorModel);
            }

            Debug.Fail("We should never get there");
            return null;
        }

        private Image2D<bool> BreadthFirstBranchAndBound(
            Image2D<Color> shrinkedImage,
            double objectSize,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel)
        {
            DateTime startTime = DateTime.Now;
            DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound started.");

            SortedSet<EnergyBound> front = this.BreadthFirstBranchAndBoundTraverse(
                shrinkedImage, objectSize, backgroundColorModel, objectColorModel, Int32.MaxValue);

            DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound finished in {0}.", DateTime.Now - startTime);
            DebugConfiguration.WriteImportantDebugText("Min energy value is {0}", front.Min.Bound);

            Debug.Assert(front.Min.Constraints.CheckIfSatisfied());
            return this.SegmentImageWithConstraints(front.Min.Constraints, shrinkedImage, backgroundColorModel, objectColorModel).SegmentationMask;
        }

        private Image2D<bool> CombinedBranchAndBound(
            Image2D<Color> shrinkedImage,
            double objectSize,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel)
        {
            DateTime startTime = DateTime.Now;
            DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound started.");

            SortedSet<EnergyBound> front = this.BreadthFirstBranchAndBoundTraverse(
                shrinkedImage, objectSize, backgroundColorModel, objectColorModel, this.MaxBfsIterationsInCombinedMode);

            DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound finished in {0}.", DateTime.Now - startTime);
            DebugConfiguration.WriteImportantDebugText("Best lower bound is {0}", front.Min.Bound);

            if (front.Min.Constraints.CheckIfSatisfied())
                return this.SegmentImageWithConstraints(front.Min.Constraints, shrinkedImage, backgroundColorModel, objectColorModel).SegmentationMask;

            // Sort front by depth
            List<EnergyBound> sortedFront = new List<EnergyBound>(front);
            sortedFront.Sort((item1, item2) => item1.Constraints.GetViolationSum() - item2.Constraints.GetViolationSum());

            startTime = DateTime.Now;
            DebugConfiguration.WriteImportantDebugText("Switching to depth-first branch-and-bound.");

            int lowerBoundsCalculated = 0;
            int upperBoundsCalculated = 0;
            int branchesTruncated = 0;
            int iteration = 1;
            int currentBound = 1;
            EnergyBound bestUpperBound = null;
            foreach (EnergyBound energyBound in sortedFront)
            {
                this.DepthFirstBranchAndBoundTraverse(
                    shrinkedImage,
                    objectSize,
                    backgroundColorModel,
                    objectColorModel,
                    energyBound.Constraints,
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
            double objectSize,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel)
        {
            ShapeConstraintsSet initialConstraints = ShapeConstraintsSet.ConstraintToImage(
                this.ShapeModel, shrinkedImage.Rectangle.Size);

            DateTime startTime = DateTime.Now;
            DebugConfiguration.WriteImportantDebugText("Depth-first branch-and-bound started.");

            EnergyBound bestUpperBound = null;
            int lowerBoundsCalculated = 0;
            int upperBoundsCalculated = 0;
            int branchesTruncated = 0;
            int iteration = 1;
            this.DepthFirstBranchAndBoundTraverse(
                shrinkedImage,
                objectSize,
                backgroundColorModel,
                objectColorModel,
                initialConstraints,
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
            double objectSize,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel,
            int maxIterations)
        {
            ShapeConstraintsSet initialConstraints = ShapeConstraintsSet.ConstraintToImage(
                this.ShapeModel, shrinkedImage.Rectangle.Size);
            SortedSet<EnergyBound> front = new SortedSet<EnergyBound>();
            front.Add(this.CalculateEnergyBound(
                initialConstraints, shrinkedImage, objectSize, backgroundColorModel, objectColorModel));

            int currentIteration = 1;
            DateTime startTime = DateTime.Now;
            DateTime lastOutputTime = startTime;
            int processedConstraintSets = 0;
            while (!front.Min.Constraints.CheckIfSatisfied() && currentIteration <= maxIterations)
            {
                EnergyBound parentLowerBound = front.Min;
                front.Remove(parentLowerBound);

                List<ShapeConstraintsSet> expandedConstraints = parentLowerBound.Constraints.SplitMostViolated();
                foreach (ShapeConstraintsSet constraintsSet in expandedConstraints)
                {
                    EnergyBound lowerBound = this.CalculateEnergyBound(
                        constraintsSet, shrinkedImage, objectSize, backgroundColorModel, objectColorModel);
                    lowerBound.CleanupSegmentationMask();
                    front.Add(lowerBound);

                    // TODO: strong lower bound check, remove it or comment
                    Tuple<double, double>[,] lowerBoundShapeTerm = new Tuple<double, double>[shrinkedImage.Width, shrinkedImage.Height];
                    for (int i = 0; i < shrinkedImage.Width; ++i)
                        for (int j = 0; j < shrinkedImage.Height; ++j)
                            lowerBoundShapeTerm[i, j] = CalculateShapeTerm(lowerBound.Constraints, new Point(i, j));
                    Tuple<double, double>[,] parentLowerBoundShapeTerm = new Tuple<double, double>[shrinkedImage.Width, shrinkedImage.Height];
                    for (int i = 0; i < shrinkedImage.Width; ++i)
                        for (int j = 0; j < shrinkedImage.Height; ++j)
                            parentLowerBoundShapeTerm[i, j] = CalculateShapeTerm(parentLowerBound.Constraints, new Point(i, j));
                    for (int i = 0; i < shrinkedImage.Width; ++i)
                        for (int j = 0; j < shrinkedImage.Height; ++j)
                        {
                            Debug.Assert(lowerBoundShapeTerm[i, j].Item1 >= parentLowerBoundShapeTerm[i, j].Item1);
                            Debug.Assert(lowerBoundShapeTerm[i, j].Item2 >= parentLowerBoundShapeTerm[i, j].Item2);
                            CalculateShapeTerm(lowerBound.Constraints, new Point(6, 17));
                            CalculateShapeTerm(parentLowerBound.Constraints, new Point(6, 17));
                        }

                    // Lower bound should not decrease
                    Debug.Assert(lowerBound.SegmentationEnergy >= parentLowerBound.SegmentationEnergy - 1e-6);
                    Debug.Assert(lowerBound.ShapeEnergy >= parentLowerBound.ShapeEnergy - 1e-6);

                    ++processedConstraintSets;
                }

                // TODO: remove me, this is for debug only
                if (currentIteration % 2000 == 0)
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
                    int maxRadiusConstraintViolation = 0, maxCoordConstraintViolation = 0;
                    for (int vertex = 0; vertex < this.ShapeModel.VertexCount; ++vertex)
                    {
                        VertexConstraints vertexConstraints = currentMin.Constraints.GetConstraintsForVertex(vertex);
                        maxRadiusConstraintViolation = Math.Max(
                            maxRadiusConstraintViolation, vertexConstraints.MaxRadiusExclusive - vertexConstraints.MinRadiusInclusive - 1);
                        maxCoordConstraintViolation = Math.Max(
                            maxCoordConstraintViolation, vertexConstraints.MaxCoordExclusive.X - vertexConstraints.MinCoordInclusive.X - 1);
                        maxCoordConstraintViolation = Math.Max(
                            maxCoordConstraintViolation, vertexConstraints.MaxCoordExclusive.Y - vertexConstraints.MinCoordInclusive.Y - 1);
                    }

                    DebugConfiguration.WriteDebugText("Current constraint violations: {0} (radius), {1} (coord)", maxRadiusConstraintViolation, maxCoordConstraintViolation);
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
            double objectSize,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel,
            ShapeConstraintsSet currentNode,
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
                DebugConfiguration.WriteDebugText("Lower bounds calculated: {0}", lowerBoundsCalculated);
                DebugConfiguration.WriteDebugText("Upper bounds calculated: {0}", upperBoundsCalculated);
                DebugConfiguration.WriteDebugText("Branches truncated: {0}", branchesTruncated);
                DebugConfiguration.WriteDebugText();

                // Report status
                this.ReportDepthFirstSearchStatus(shrinkedImage, currentNode, bestUpperBound);
            }

            List<ShapeConstraintsSet> children = currentNode.SplitMostViolated();

            // Traverse only subtrees with good lower bounds
            for (int i = 0; i < children.Count; ++i)
            {
                EnergyBound upperBound = this.CalculateEnergyBound(
                    children[i].GuessSolution(), shrinkedImage, objectSize, backgroundColorModel, objectColorModel);
                upperBoundsCalculated += 1;
                if (bestUpperBound == null || upperBound.Bound < bestUpperBound.Bound)
                {
                    bestUpperBound = upperBound;
                    DebugConfiguration.WriteImportantDebugText("Upper bound updated at iteration {0} to {1}.", iteration, bestUpperBound.Bound);
                }

                // Lower bound equals upper bound for leafs
                if (children[i].CheckIfSatisfied())
                    continue;

                EnergyBound lowerBound = this.CalculateEnergyBound(
                    children[i], shrinkedImage, objectSize, backgroundColorModel, objectColorModel);
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
                    objectSize,
                    backgroundColorModel,
                    objectColorModel,
                    children[i],
                    ref bestUpperBound,
                    ref lowerBoundsCalculated,
                    ref upperBoundsCalculated,
                    ref branchesTruncated,
                    ref iteration);
            }
        }

        private void ReportDepthFirstSearchStatus(Image2D<Color> shrinkedImage, ShapeConstraintsSet currentNode, EnergyBound upperBound)
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
            ShapeConstraintsSet constraintsSet,
            Image2D<Color> shrinkedImage,
            double objectSize,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel)
        {
            ImageSegmentationInfo boundInfo = this.SegmentImageWithConstraints(
                constraintsSet, shrinkedImage, backgroundColorModel, objectColorModel);

            double segmentationEnergy = boundInfo.Energy;
            double shapeEnergy = this.CalculateMinShapeEnergy(constraintsSet, objectSize);

            return new EnergyBound(constraintsSet, shapeEnergy, segmentationEnergy, this.ShapeEnergyWeight, boundInfo.SegmentationMask);
        }

        private ImageSegmentationInfo SegmentImageWithConstraints(
            ShapeConstraintsSet constraintsSet,
            Image2D<Color> shrinkedImage,
            Mixture<VectorGaussian> backgroundColorModel,
            Mixture<VectorGaussian> objectColorModel)
        {
            return this.SegmentImage(
                shrinkedImage,
                backgroundColorModel,
                objectColorModel,
                point => CalculateShapeTerm(constraintsSet, point),
                false);
        }

        /// <summary>
        /// Calculates shape-based unary potentials.
        /// </summary>
        /// <param name="constraintsSet"></param>
        /// <param name="point"></param>
        /// <returns>First item is penalty for being background, second is penalty for being object.</returns>
        // TODO: make this shit private
        public static Tuple<double, double> CalculateShapeTerm(ShapeConstraintsSet constraintsSet, Point point)
        {
            Vector pointAsVec = new Vector(point.X, point.Y);

            // Calculate weight to sink (min price for object label at (x, y))
            double minDistanceToEdgeSqr = Double.PositiveInfinity;
            bool inConvexHull = false;

            // First check if pixel is in convex hull for some edge
            for (int i = 0; i < constraintsSet.ShapeModel.Edges.Count; ++i)
            {
                ShapeEdge edge = constraintsSet.ShapeModel.Edges[i];
                Polygon convexHull = constraintsSet.GetConvexHullForVertexPair(edge.Index1, edge.Index2);
                if (convexHull.IsPointInside(pointAsVec))
                {
                    minDistanceToEdgeSqr = 0;
                    inConvexHull = true;
                    break;
                }
            }

            // If not, find closest edge possible
            if (!inConvexHull)
            {
                for (int i = 0; i < constraintsSet.ShapeModel.Edges.Count; ++i)
                {
                    ShapeEdge edge = constraintsSet.ShapeModel.Edges[i];
                    VertexConstraints constraints1 = constraintsSet.GetConstraintsForVertex(edge.Index1);
                    VertexConstraints constraints2 = constraintsSet.GetConstraintsForVertex(edge.Index2);

                    foreach (Vector point1 in constraints1.IterateCornersAndClosestPoint(point))
                    {
                        foreach (Vector point2 in constraints2.IterateCornersAndClosestPoint(point))
                        {
                            minDistanceToEdgeSqr = Math.Min(
                                minDistanceToEdgeSqr,
                                constraintsSet.ShapeModel.CalculateRelativeDistanceToEdgeSquared(
                                    pointAsVec,
                                    new Circle(point1, constraints1.MaxRadiusExclusive - 1),
                                    new Circle(point2, constraints2.MaxRadiusExclusive - 1)));
                        }
                    }
                }
            }

            double maxObjectPotential = constraintsSet.ShapeModel.RelativeDistanceToObjectPotential(
                Math.Sqrt(minDistanceToEdgeSqr));
            double toSink = -MathHelper.LogInf(maxObjectPotential);

            // Calculate weight to source (min price for background label at (x, y))
            double maxDistanceToEdgeSqr = Double.PositiveInfinity;
            for (int i = 0; i < constraintsSet.ShapeModel.Edges.Count; ++i)
            {
                ShapeEdge edge = constraintsSet.ShapeModel.Edges[i];
                VertexConstraints constraints1 = constraintsSet.GetConstraintsForVertex(edge.Index1);
                VertexConstraints constraints2 = constraintsSet.GetConstraintsForVertex(edge.Index2);

                // Solution will connect 2 corners (need to prove this fact)
                double maxDistanceToCurrentEdgeSqr = 0;
                foreach (Vector point1 in constraints1.IterateCorners())
                {
                    foreach (Vector point2 in constraints2.IterateCorners())
                    {
                        maxDistanceToCurrentEdgeSqr = Math.Max(
                            maxDistanceToCurrentEdgeSqr,
                            constraintsSet.ShapeModel.CalculateRelativeDistanceToEdgeSquared(
                                pointAsVec,
                                new Circle(point1, constraints1.MinRadiusInclusive),
                                new Circle(point2, constraints2.MinRadiusInclusive)));
                    }
                }

                maxDistanceToEdgeSqr = Math.Min(maxDistanceToEdgeSqr, maxDistanceToCurrentEdgeSqr);
            }

            double maxBackgroundPotential = 1 - constraintsSet.ShapeModel.RelativeDistanceToObjectPotential(
                Math.Sqrt(maxDistanceToEdgeSqr));
            double toSource = -MathHelper.LogInf(maxBackgroundPotential);

            return new Tuple<double, double>(toSource, toSink);
        }

        // TODO: make this shit private
        public double CalculateMinShapeEnergy(ShapeConstraintsSet constraintsSet, double objectSize)
        {
            // Here we use the fact that energy can be separated into vertex energy that depends on radii
            // and edge energy that depends on edge vertex positions

            double minVertexEnergy = 0;
            for (int vertexIndex = 0; vertexIndex < this.ShapeModel.VertexCount; ++vertexIndex)
                minVertexEnergy += this.ShapeModel.CalculateVertexEnergyTerm(
                    vertexIndex,
                    objectSize,
                    this.GetBestVertexRadius(vertexIndex, constraintsSet, objectSize));

            double minEdgeEnergy = 0;
            if (this.ShapeModel.PairwiseEdgeConstraintCount > 0)
            {
                double maxRatio1 = (from edgePair in this.ShapeModel.ConstrainedEdgePairs
                                    select this.ShapeModel.GetEdgeParams(edgePair.Item1, edgePair.Item2).LengthRatio).Max();
                double maxRatio2 = (from edgePair in this.ShapeModel.ConstrainedEdgePairs
                                    select 1.0 / this.ShapeModel.GetEdgeParams(edgePair.Item1, edgePair.Item2).LengthRatio).Max();
                double maxRatio = Math.Max(maxRatio1, maxRatio2);
                int lengthGridSize = (int)(Math.Round(objectSize * maxRatio * 2) + 1);

                List<GeneralizedDistanceTransform2D> childTransforms = new List<GeneralizedDistanceTransform2D>();
                foreach (int edgeIndex in this.ShapeModel.IterateNeighboringEdgeIndices(0))
                    childTransforms.Add(CalculateMinEnergiesForAllParentEdges(constraintsSet, 0, edgeIndex, lengthGridSize));

                int minPossibleLength, maxPossibleLength, minPossibleAngle, maxPossibleAngle;
                this.DetermineEdgeLimits(
                    0,
                    constraintsSet,
                    out minPossibleLength,
                    out maxPossibleLength,
                    out minPossibleAngle,
                    out maxPossibleAngle);
                Debug.Assert(maxPossibleLength >= minPossibleLength && maxPossibleAngle >= minPossibleAngle);

                minEdgeEnergy = Double.PositiveInfinity;
                for (int length = minPossibleLength; length <= maxPossibleLength; ++length)
                {
                    for (int angle = minPossibleAngle; angle <= maxPossibleAngle; ++angle)
                    {
                        double energySum = 0;
                        foreach (GeneralizedDistanceTransform2D childTransform in childTransforms)
                            energySum += childTransform[length, angle];

                        if (energySum < minEdgeEnergy)
                            minEdgeEnergy = energySum;
                    }
                }
            }

            return minVertexEnergy + minEdgeEnergy;
        }

        private void DetermineEdgeLimits(
            int edgeIndex,
            ShapeConstraintsSet constraintsSet,
            out int minLength,
            out int maxLength,
            out int minAngle,
            out int maxAngle)
        {
            minLength = Int32.MaxValue;
            maxLength = Int32.MinValue;
            minAngle = 180;
            maxAngle = -180;

            ShapeEdge edge = this.ShapeModel.Edges[edgeIndex];
            foreach (Vector point1 in constraintsSet.GetConstraintsForVertex(edge.Index1).IterateBorder())
                foreach (Vector point2 in constraintsSet.GetConstraintsForVertex(edge.Index2).IterateBorder())
                {
                    int length = (int)Math.Round((point1 - point2).Length);
                    minLength = Math.Min(minLength, length);
                    maxLength = Math.Max(maxLength, length);

                    if (point1 != point2)
                    {
                        double angle = Vector.AngleBetween(new Vector(1, 0), point2 - point1);
                        // Angle must be in [-180, 180]
                        if (angle > Math.PI)
                            angle -= Math.PI * 2;
                        int degrees = (int)Math.Round(MathHelper.ToDegrees(angle));
                        minAngle = Math.Min(minAngle, degrees);
                        maxAngle = Math.Max(maxAngle, degrees);
                    }
                }
        }

        private double GetBestVertexRadius(int vertexIndex, ShapeConstraintsSet constraintsSet, double objectSize)
        {
            VertexConstraints constraints = constraintsSet.GetConstraintsForVertex(vertexIndex);
            int bestRadius =
                (int)Math.Round(this.ShapeModel.GetVertexParams(vertexIndex).RadiusToObjectSizeRatio * objectSize);
            if (bestRadius < constraints.MinRadiusInclusive)
                return constraints.MinRadiusInclusive;
            if (bestRadius >= constraints.MaxRadiusExclusive)
                return constraints.MaxRadiusExclusive - 1;
            return bestRadius;
        }

        private GeneralizedDistanceTransform2D CalculateMinEnergiesForAllParentEdges(
            ShapeConstraintsSet constraintsSet,
            int parentEdgeIndex,
            int currentEdgeIndex,
            int lengthGridSize)
        {
            List<GeneralizedDistanceTransform2D> childDistanceTransforms = new List<GeneralizedDistanceTransform2D>();
            foreach (int neighborEdgeIndex in this.ShapeModel.IterateNeighboringEdgeIndices(currentEdgeIndex))
            {
                // Iterate only through children
                if (neighborEdgeIndex == parentEdgeIndex)
                    continue;

                GeneralizedDistanceTransform2D childTransform = CalculateMinEnergiesForAllParentEdges(
                    constraintsSet, currentEdgeIndex, neighborEdgeIndex, lengthGridSize);
                childDistanceTransforms.Add(childTransform);
            }

            int minPossibleLength, maxPossibleLength, minPossibleAngle, maxPossibleAngle;
            this.DetermineEdgeLimits(
                currentEdgeIndex,
                constraintsSet,
                out minPossibleLength,
                out maxPossibleLength,
                out minPossibleAngle,
                out maxPossibleAngle);

            ShapeEdgePairParams pairParams = this.ShapeModel.GetEdgeParams(parentEdgeIndex, currentEdgeIndex);
            Func<Point, double> penaltyFunction =
                (p) =>
                {
                    // Disallow invalid configurations
                    int length = (int)Math.Round(p.X / pairParams.LengthRatio);
                    int alpha = (int)Math.Round(p.Y + MathHelper.ToDegrees(pairParams.MeanAngle));
                    if (length < minPossibleLength || length > maxPossibleLength ||
                        alpha < minPossibleAngle || alpha > maxPossibleAngle)
                    {
                        return 1e+20; // Return something close to infinity
                    }

                    double sum = 0;
                    foreach (GeneralizedDistanceTransform2D childTransform in childDistanceTransforms)
                        sum += childTransform[length, alpha];
                    return sum;
                };

            return new GeneralizedDistanceTransform2D(
                new Point(0, -360),
                new Point(lengthGridSize, 360),
                1.0 / MathHelper.Sqr(pairParams.LengthDeviation),
                1.0 / MathHelper.Sqr(MathHelper.ToDegrees(pairParams.AngleDeviation * Math.PI)),
                penaltyFunction);
        }

        private class EnergyBound : IComparable<EnergyBound>
        {
            public ShapeConstraintsSet Constraints { get; private set; }

            public double Bound { get; private set; }

            public double ShapeEnergy { get; private set; }

            public double SegmentationEnergy { get; private set; }

            public Image2D<bool> SegmentationMask { get; private set; }

            private static long instanceCount;

            private readonly long instanceId;

            public EnergyBound(
                ShapeConstraintsSet constraints,
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
