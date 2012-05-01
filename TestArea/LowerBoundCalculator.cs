using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Research.GraphBasedShapePrior;

namespace TestArea
{
    struct Segment
    {
        public Segment(Vector point1, Vector point2)
            : this()
        {
            this.Point1 = point1;
            this.Point2 = point2;
        }

        public Vector Point1 { get; set; }

        public Vector Point2 { get; set; }
    }

    class StatsCalculator
    {
        private readonly List<double> values = new List<double>();

        public void Add(double value)
        {
            values.Add(value);
        }

        public void PrintStats(string name)
        {
            values.Sort();
            double sum = values.Sum();

            Console.WriteLine();
            Console.WriteLine("Stats for {0}:", name);
            Console.WriteLine("Sum: {0}", sum);
            Console.WriteLine("Median: {0}", values[values.Count / 2]);
            Console.WriteLine("Mean: {0}", sum / values.Count);
            Console.WriteLine("Min: {0}", values.Min());
            Console.WriteLine("Max: {0}", values.Max());
        }
    }

    class LowerBoundCalculator
    {
        private readonly ImageSegmentator segmentator;

        public LowerBoundCalculator(ImageSegmentator segmentator)
        {
            if (segmentator == null)
                throw new ArgumentNullException("segmentator");

            this.segmentator = segmentator;
        }

        public void CalculateLowerBound(ShapeConstraints constraints)
        {
            if (constraints == null)
                throw new ArgumentNullException("constraints");
            if (constraints.ShapeModel.Edges.Count != 1)
                throw new NotSupportedException("Only 1-edged models are currently supported");

            double lambdaScale = 1.0 / (segmentator.UnaryTermWeight * segmentator.ShapeUnaryTermWeight);

            Size imageSize = segmentator.ImageSize;
            Segment[,] bestObjectSegments = new Segment[imageSize.Width, imageSize.Height];
            Segment[,] bestBackgroundSegments = new Segment[imageSize.Width, imageSize.Height];
            Segment[,] lambdas = new Segment[imageSize.Width, imageSize.Height];
            double[,] backgroundPenalties = new double[imageSize.Width, imageSize.Height];
            Image2D<ObjectBackgroundTerm> shapeTerms = new Image2D<ObjectBackgroundTerm>(imageSize.Width, imageSize.Height);
            Segment masterSegment = new Segment();

            VertexConstraints vertex1Constraints = constraints.VertexConstraints[0];
            VertexConstraints vertex2Constraints = constraints.VertexConstraints[1];
            EdgeConstraints edgeConstraints = constraints.EdgeConstraints[0];

            // First, calculate background terms. It should be done only once.
            for (int x = 0; x < imageSize.Width; ++x)
            {
                for (int y = 0; y < imageSize.Height; ++y)
                {
                    Vector point = new Vector(x, y);
                    double maxDistanceToEdgeSqr = 0;
                    foreach (Vector corner1 in vertex1Constraints.Corners)
                    {
                        foreach (Vector corner2 in vertex2Constraints.Corners)
                        {
                            maxDistanceToEdgeSqr = Math.Max(
                                maxDistanceToEdgeSqr,
                                point.DistanceToSegmentSquared(corner1, corner2));
                        }
                    }

                    backgroundPenalties[x, y] = constraints.ShapeModel.CalculateBackgroundPenaltyForEdge(
                        maxDistanceToEdgeSqr, edgeConstraints.MinWidth);
                }
            }

            // Now iteratively recalculate object penalties and improve lower bound
            const double subgradientAscentStep = 0.00001;
            double firstLowerBound = 0, maxLowerBound = Double.NegativeInfinity;
            for (int iteration = 1; iteration <= 50; ++iteration)
            {
                // Calculate background-type segments
                for (int x = 0; x < imageSize.Width; ++x)
                {
                    for (int y = 0; y < imageSize.Height; ++y)
                    {
                        Segment bestSegment = new Segment();
                        double bestPenalty = Double.PositiveInfinity;
                        foreach (Vector corner1 in vertex1Constraints.Corners)
                            foreach (Vector corner2 in vertex2Constraints.Corners)
                            {
                                double penalty =
                                    Vector.DotProduct(corner1, lambdas[x, y].Point1) +
                                    Vector.DotProduct(corner2, lambdas[x, y].Point2);
                                if (penalty < bestPenalty)
                                {
                                    bestPenalty = penalty;
                                    bestSegment = new Segment(corner1, corner2);
                                }
                            }

                        shapeTerms[x, y] = new ObjectBackgroundTerm(0, lambdaScale * bestPenalty);
                        bestBackgroundSegments[x, y] = bestSegment;
                    }
                }

                // Calculate object-type segments
                for (int x = 0; x < imageSize.Width; ++x)
                {
                    for (int y = 0; y < imageSize.Height; ++y)
                    {
                        Vector point = new Vector(x, y);

                        double termLowerBound;
                        double bestTermLowerBound = Double.PositiveInfinity;
                        Vector distancePoint, penaltyPoint;
                        Vector bestDistancePoint = Vector.Zero, bestPenaltyPoint = Vector.Zero, bestFixedPoint = Vector.Zero;
                        bool needSwap = false;

                        foreach (Vector corner1 in vertex1Constraints.Corners)
                        {
                            LowerBoundForFixedCorner(
                                point, corner1, vertex2Constraints, lambdas[x, y].Point1, lambdas[x, y].Point2, lambdaScale, out termLowerBound, out distancePoint, out penaltyPoint);
                            if (termLowerBound < bestTermLowerBound)
                            {
                                bestTermLowerBound = termLowerBound;
                                bestDistancePoint = distancePoint;
                                bestPenaltyPoint = penaltyPoint;
                                bestFixedPoint = corner1;
                            }
                        }

                        foreach (Vector corner2 in vertex2Constraints.Corners)
                        {
                            LowerBoundForFixedCorner(
                                point, corner2, vertex1Constraints, lambdas[x, y].Point2, lambdas[x, y].Point1, lambdaScale, out termLowerBound, out distancePoint, out penaltyPoint);
                            if (termLowerBound < bestTermLowerBound)
                            {
                                bestTermLowerBound = termLowerBound;
                                bestDistancePoint = distancePoint;
                                bestPenaltyPoint = penaltyPoint;
                                bestFixedPoint = corner2;
                                needSwap = true;
                            }
                        }

                        Segment bestSegment = new Segment(bestFixedPoint, 0.5 * (bestDistancePoint + bestPenaltyPoint));
                        if (needSwap)
                            bestSegment = new Segment(bestSegment.Point2, bestSegment.Point1);

                        bestObjectSegments[x, y] = bestSegment;
                        shapeTerms[x, y] = new ObjectBackgroundTerm(bestTermLowerBound, shapeTerms[x, y].BackgroundTerm + backgroundPenalties[x, y]);
                    }
                }

                // Find lambdas for master segment
                Vector masterLambdas1 = Vector.Zero, masterLambdas2 = Vector.Zero;
                for (int x = 0; x < imageSize.Width; ++x)
                {
                    for (int y = 0; y < imageSize.Height; ++y)
                    {
                        masterLambdas1 -= lambdas[x, y].Point1;
                        masterLambdas2 -= lambdas[x, y].Point2;
                    }
                }

                // Find best master segment
                double bestMasterSegmentPenalty = Double.PositiveInfinity;
                foreach (Vector corner1 in vertex1Constraints.Corners)
                {
                    foreach (Vector corner2 in vertex2Constraints.Corners)
                    {
                        double penalty =
                            Vector.DotProduct(corner1, masterLambdas1) +
                            Vector.DotProduct(corner2, masterLambdas2);
                        if (penalty < bestMasterSegmentPenalty)
                        {
                            bestMasterSegmentPenalty = penalty;
                            masterSegment = new Segment(corner1, corner2);
                        }
                    }
                }

                // Segment image
                double lowerBound = this.segmentator.SegmentImageWithShapeTerms(p => shapeTerms[p.X, p.Y]) + bestMasterSegmentPenalty;
                Image2D<bool> mask = this.segmentator.GetLastSegmentationMask();
                if (iteration == 1)
                    firstLowerBound = lowerBound;
                maxLowerBound = Math.Max(maxLowerBound, lowerBound);

                // Output some info
                Console.WriteLine();
                Console.WriteLine("After iteration {0} lower bound is {1:0.0000}", iteration, lowerBound);
                Console.WriteLine("Master segment: ({0}), ({1})", masterSegment.Point1, masterSegment.Point2);
                Console.WriteLine("Master segment lambdas: ({0}), ({1})", masterLambdas1, masterLambdas2);
                Console.WriteLine("Master segment penalty: {0}", bestMasterSegmentPenalty);
                using (Image image = Image2D.ToRegularImage(this.segmentator.GetLastUnaryTerms(), -20, 20))
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    constraints.Draw(graphics);
                    graphics.DrawLine(new Pen(Color.Red, 3), (float)masterSegment.Point1.X, (float)masterSegment.Point1.Y, (float)masterSegment.Point2.X, (float)masterSegment.Point2.Y);
                    image.Save(String.Format("../../unary_terms_{0}.png", iteration));
                }
                Image2D.SaveToFile(this.segmentator.GetLastShapeTerms(), -20, 20, String.Format("../../shape_terms_{0}.png", iteration));
                Image2D.SaveToFile(mask, String.Format("../../mask_{0}.png", iteration));
                DrawSegments(String.Format("../../bg_segments_{0}.png", iteration), constraints, bestBackgroundSegments);
                DrawSegments(String.Format("../../obj_segments_{0}.png", iteration), constraints, bestObjectSegments);

                // Recalculate lambdas
                for (int x = 0; x < imageSize.Width; ++x)
                {
                    for (int y = 0; y < imageSize.Height; ++y)
                    {
                        Segment selectedSegment = mask[x, y] ? bestObjectSegments[x, y] : bestBackgroundSegments[x, y];
                        lambdas[x, y] = new Segment(
                            lambdas[x, y].Point1 + subgradientAscentStep * (selectedSegment.Point1 - masterSegment.Point1),
                            lambdas[x, y].Point2 + subgradientAscentStep * (selectedSegment.Point2 - masterSegment.Point2));
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("Max lower bound is {0}", maxLowerBound);
            Console.WriteLine("It is {0} first lower bound", maxLowerBound == firstLowerBound ? "THE" : "NOT");
        }

        private void DrawSegments(string fileName, ShapeConstraints constraints, Segment[,] segments)
        {
            using (Image image = new Bitmap(this.segmentator.ImageSize.Width, this.segmentator.ImageSize.Height))
            using (Graphics graphics = Graphics.FromImage(image))
            {
                for (int x = 0; x < image.Width; ++x)
                {
                    for (int y = 0; y < image.Height; ++y)
                    {
                        graphics.DrawLine(
                            Pens.LightYellow,
                            (float) segments[x, y].Point1.X,
                            (float) segments[x, y].Point1.Y,
                            (float) segments[x, y].Point2.X,
                            (float) segments[x, y].Point2.Y);
                    }
                }

                constraints.Draw(graphics);

                image.Save(fileName);
            }
        }

        private static void LowerBoundForFixedCorner(
            Vector point,
            Vector fixedCorner,
            VertexConstraints freeCornerConstraints,
            Vector fixedCornerLambdas,
            Vector freeCornerLambdas,
            double lambdaScale,
            out double lowerBound,
            out Vector minDistancePoint,
            out Vector minPenaltyPoint)
        {
            // First try best point inside
            {
                Vector shiftedPoint = point - freeCornerLambdas * 0.5;
                Vector closestPoint = new Vector(
                    MathHelper.Trunc(shiftedPoint.X, freeCornerConstraints.MinCoord.X, freeCornerConstraints.MaxCoord.X),
                    MathHelper.Trunc(shiftedPoint.Y, freeCornerConstraints.MinCoord.Y, freeCornerConstraints.MaxCoord.Y));
                minDistancePoint = minPenaltyPoint = closestPoint;
                lowerBound = lambdaScale * Vector.DotProduct(closestPoint, freeCornerLambdas) + point.DistanceToSegmentSquared(fixedCorner, closestPoint);
            }

            // Now try lower bounds for each segment
            for (int i = 0; i < 4; ++i)
            {
                Vector corner1 = freeCornerConstraints.Corners[i];
                Vector corner2 = freeCornerConstraints.Corners[(i + 1) % 4];
                double penalty1 = Vector.DotProduct(corner1, freeCornerLambdas);
                double penalty2 = Vector.DotProduct(corner2, freeCornerLambdas);
                Vector closestPoint;
                Tuple<double, double> intersection =
                    MathHelper.LineIntersection(fixedCorner, point - fixedCorner, corner1, corner2 - corner1);
                if (intersection != null && intersection.Item1 >= 1 && intersection.Item2 >= 0 && intersection.Item2 <= 1)
                    closestPoint = corner1 + intersection.Item2 * (corner2 - corner1);
                else
                    closestPoint = new Vector(
                        MathHelper.Trunc(point.X, Math.Min(corner1.X, corner2.X), Math.Max(corner1.X, corner2.X)),
                        MathHelper.Trunc(point.Y, Math.Min(corner1.Y, corner2.Y), Math.Max(corner1.Y, corner2.Y)));

                Vector penaltyPoint = penalty1 < penalty2 ? corner1 : corner2;
                double possibleLowerBound = point.DistanceToSegmentSquared(fixedCorner, closestPoint) + Math.Min(penalty1, penalty2) * lambdaScale;
                if (possibleLowerBound < lowerBound)
                {
                    lowerBound = possibleLowerBound;
                    minDistancePoint = closestPoint;
                    minPenaltyPoint = penaltyPoint;
                }
            }

            double lowerBoundBase = lambdaScale * Vector.DotProduct(fixedCorner, fixedCornerLambdas);
            lowerBound += lowerBoundBase;
        }
    }
}
