using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using Research.GraphBasedShapePrior.Util;
using Vector = Research.GraphBasedShapePrior.Util.Vector;

namespace Research.GraphBasedShapePrior
{
    public class BranchAndBoundSegmentationAlgorithm : SegmentationAlgorithmBase
    {
        private const int DefaultLengthGridSize = 201;

        private const int DefaultAngleGridSize = 201;

        private IShapeEnergyLowerBoundCalculator shapeEnergyLowerBoundCalculator = new ShapeEnergyLowerBoundCalculator(DefaultLengthGridSize, DefaultAngleGridSize);

        private int progressReportRate = 50;

        private double minEdgeWidth = 3;

        private double maxEdgeWidth = 20;

        private double maxCoordFreedom = 1;

        private double maxWidthFreedom = 1;

        private Image2D<ObjectBackgroundTerm> shapeUnaryTerms;

        private IShapeTermsLowerBoundCalculator shapeTermsCalculator = new CpuShapeTermsLowerBoundCalculator();

        private DateTime startTime;

        private ShapeConstraints startConstraints;

        public event EventHandler<BranchAndBoundProgressEventArgs> BreadthFirstBranchAndBoundProgress;

        public event EventHandler BranchAndBoundStarted;

        public event EventHandler<BranchAndBoundCompletedEventArgs> BranchAndBoundCompleted;

        public int ProgressReportRate
        {
            get { return this.progressReportRate; }
            set
            {
                if (value < 1)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.progressReportRate = value;
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
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.maxWidthFreedom = value;
            }
        }

        public IShapeTermsLowerBoundCalculator ShapeTermCalculator
        {
            get { return this.shapeTermsCalculator; }
            set
            {
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
                if (this.IsRunning)
                    throw new InvalidOperationException("You can't change the value of this property while segmentation is running.");
                this.startConstraints = value;
            }
        }

        protected override SegmentationSolution SegmentCurrentImage()
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

            this.startTime = DateTime.Now;
            DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound started.");

            SortedSet<EnergyBound> front = this.BreadthFirstBranchAndBoundTraverse(constraints);
            ReportBranchAndBoundCompletion(front.Min);

            if (front.Min.Constraints.CheckIfSatisfied(this.maxCoordFreedom, this.maxWidthFreedom))
            {
                DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound finished in {0}.",
                                                           DateTime.Now - this.startTime);
                DebugConfiguration.WriteImportantDebugText("Best lower bound is {0:0.0000}", front.Min.Bound);
            }
            else
            {
                DebugConfiguration.WriteImportantDebugText("Breadth-first branch-and-bound forced to stop after {0}.", DateTime.Now - this.startTime);
                DebugConfiguration.WriteImportantDebugText("Min energy value achieved is {0:0.0000}", front.Min.Bound);
            }

            EnergyBound collapsedBfsSolution = this.CalculateEnergyBound(front.Min.Constraints.Collapse());
            Shape resultShape = front.Min.Constraints.CollapseToShape();

            DebugConfiguration.WriteImportantDebugText(
                "Collapsed solution energy value is {0:0.0000} ({1:0.0000} + {2:0.0000})",
                collapsedBfsSolution.Bound,
                collapsedBfsSolution.SegmentationEnergy,
                collapsedBfsSolution.ShapeEnergy * this.ShapeEnergyWeight);
            return new SegmentationSolution(resultShape, this.ImageSegmentator.GetLastSegmentationMask(), collapsedBfsSolution.Bound);
        }

        private SortedSet<EnergyBound> BreadthFirstBranchAndBoundTraverse(ShapeConstraints constraints)
        {
            SortedSet<EnergyBound> front = new SortedSet<EnergyBound> { this.CalculateEnergyBound(constraints) };

            int currentIteration = 1;
            DateTime lastOutputTime = startTime;
            int processedConstraintSets = 0;
            while (!front.Min.Constraints.CheckIfSatisfied(this.maxCoordFreedom, this.maxWidthFreedom) && !this.IsStopping)
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

                    this.ReportBranchAndBoundProgress(front);

                    lastOutputTime = currentTime;
                    processedConstraintSets = 0;
                }

                currentIteration += 1;
            }

            return front;
        }

        private void ReportBranchAndBoundProgress(SortedSet<EnergyBound> front)
        {
            EnergyBound currentMin = front.Min;

            // In order to report various masks we need to segment image again
            this.SegmentImageWithConstraints(currentMin.Constraints);

            // Raise status report event
            BranchAndBoundProgressEventArgs args = new BranchAndBoundProgressEventArgs(
                front.Min.Bound, this.ImageSegmentator.GetLastSegmentationMask(), this.ImageSegmentator.GetLastUnaryTerms(), this.ImageSegmentator.GetLastShapeTerms(), currentMin.Constraints);
            if (this.BreadthFirstBranchAndBoundProgress != null)
                this.BreadthFirstBranchAndBoundProgress.Invoke(this, args);
        }

        private void ReportBranchAndBoundCompletion(EnergyBound result)
        {
            // In order to report various masks we need to segment image again
            this.SegmentImageWithConstraints(result.Constraints.Collapse());

            BranchAndBoundCompletedEventArgs args = new BranchAndBoundCompletedEventArgs(
                this.ImageSegmentator.GetLastSegmentationMask(), this.ImageSegmentator.GetLastUnaryTerms(), this.ImageSegmentator.GetLastShapeTerms(), result.Constraints, result.Bound);
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
