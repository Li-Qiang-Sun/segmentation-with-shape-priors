using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Research.GraphBasedShapePrior
{
    public interface ILengthAngleConstraints
    {
        Range LengthBoundary { get; }

        Range AngleBoundary { get; }
        
        bool InRange(double length, double lengthTolerance, double angle, double angleTolerance);
    }

    public class BoxSetLengthAngleConstraints : ILengthAngleConstraints
    {
        private readonly List<BoxLengthAngleConstraints> childConstraints;

        private BoxSetLengthAngleConstraints(IEnumerable<BoxLengthAngleConstraints> constraints, BoxLengthAngleConstraints overallRange)
        {
            this.childConstraints = new List<BoxLengthAngleConstraints>(constraints);
            this.OverallRange = overallRange;
        }

        public ReadOnlyCollection<BoxLengthAngleConstraints> ChildConstraints
        {
            get { return this.childConstraints.AsReadOnly(); }
        }

        public BoxLengthAngleConstraints OverallRange { get; private set; }

        public Range LengthBoundary
        {
            get { return this.OverallRange.LengthBoundary; }
        }

        public Range AngleBoundary
        {
            get { return this.OverallRange.AngleBoundary; }
        }

        public bool InRange(double length, double lengthTolerance, double angle, double angleTolerance)
        {
            if (!this.OverallRange.InRange(length, lengthTolerance, angle, angleTolerance))
                return false;

            for (int i = 0; i < this.childConstraints.Count; ++i)
                if (this.childConstraints[i].InRange(length, lengthTolerance, angle, angleTolerance))
                    return true;

            return false;
        }

        public static BoxSetLengthAngleConstraints FromVertexConstraints(
            VertexConstraints vertexConstraints1, VertexConstraints vertexConstraints2,
            int maxSplitDepth,
            double nonSplittableArea)
        {
            BoxLengthAngleConstraints overallRange = BoxLengthAngleConstraints.FromVertexConstraints(
                vertexConstraints1, vertexConstraints2);

            IEnumerable<VertexConstraints> split1 = GenerateSplit(vertexConstraints1, maxSplitDepth, nonSplittableArea);
            IEnumerable<VertexConstraints> split2 = GenerateSplit(vertexConstraints2, maxSplitDepth, nonSplittableArea);
            List<BoxLengthAngleConstraints> childConstraints = new List<BoxLengthAngleConstraints>();
            foreach (VertexConstraints childVertexConstraints1 in split1)
                foreach (VertexConstraints childVertexConstraints2 in split2)
                    childConstraints.Add(BoxLengthAngleConstraints.FromVertexConstraints(childVertexConstraints1, childVertexConstraints2));

            return new BoxSetLengthAngleConstraints(childConstraints, overallRange);
        }

        private static IEnumerable<VertexConstraints> GenerateSplit(
            VertexConstraints constraints,
            int maxSplitDepth,
            double nonSplittableArea)
        {
            List<VertexConstraints> split = new List<VertexConstraints> { constraints };
            int iteration = 0;
            int splitIndex = 0;
            while (split[iteration].Area > nonSplittableArea && iteration < maxSplitDepth)
            {
                int length = split.Count;
                for (int i = splitIndex; i < length; ++i)
                    split.AddRange(split[i].Split());
                splitIndex = length;
                ++iteration;
            }

            return split.Skip(splitIndex);
        }
    }

    public class BoxLengthAngleConstraints : ILengthAngleConstraints
    {
        public Range LengthBoundary { get; private set; }

        public Range AngleBoundary { get; private set; }

        private BoxLengthAngleConstraints(Range lengthBoundary, Range angleBoundary)
        {
            Debug.Assert(!lengthBoundary.Outside);

            this.LengthBoundary = lengthBoundary;
            this.AngleBoundary = angleBoundary;
        }

        public bool InRange(double length, double lengthTolerance, double angle, double angleTolerance)
        {
            return
                this.LengthBoundary.IntersectsWith(new Range(length - lengthTolerance * 0.5, length + lengthTolerance * 0.5)) &&
                this.AngleBoundary.IntersectsWith(new Range(angle - angleTolerance * 0.5, angle + angleTolerance * 0.5));
        }

        public static BoxLengthAngleConstraints FromVertexConstraints(VertexConstraints constraints1, VertexConstraints constraints2)
        {
            Range angleRange;

            Range xRange1 = new Range(constraints1.MinCoord.X, constraints1.MaxCoord.X);
            Range yRange1 = new Range(constraints1.MinCoord.Y, constraints1.MaxCoord.Y);
            Range xRange2 = new Range(constraints2.MinCoord.X, constraints2.MaxCoord.X);
            Range yRange2 = new Range(constraints2.MinCoord.Y, constraints2.MaxCoord.Y);

            bool xIntersection = xRange1.IntersectsWith(xRange2);
            bool yIntersection = yRange1.IntersectsWith(yRange2);

            double minLength = Double.PositiveInfinity, maxLength = 0;

            if (xIntersection && yIntersection)
            {
                // Special case: intersecting rectangles
                angleRange = new Range(-Math.PI, Math.PI);
                minLength = 0;
            }
            else
            {
                // Angle changes from PI to -PI when second constraint is to the left of the first one
                bool angleSignChanges = constraints1.MinCoord.X > constraints2.MaxCoord.X && yIntersection;

                double minAngle = angleSignChanges ? -Math.PI : Math.PI;
                double maxAngle = angleSignChanges ? Math.PI : -Math.PI;
                foreach (Vector point1 in constraints1.Corners)
                {
                    foreach (Vector point2 in constraints2.Corners)
                    {
                        double angle = Vector.AngleBetween(new Vector(1, 0), point2 - point1);
                        if (angleSignChanges)
                        {
                            if (angle < 0)
                                minAngle = Math.Max(minAngle, angle);
                            else
                                maxAngle = Math.Min(maxAngle, angle);
                        }
                        else
                        {
                            minAngle = Math.Min(minAngle, angle);
                            maxAngle = Math.Max(maxAngle, angle);
                        }
                    }
                }
                angleRange = new Range(minAngle, maxAngle, angleSignChanges);

                // One constraint is on top or on bottom of another
                if (xIntersection)
                {
                    // 1 on top of 2
                    if (constraints1.MinCoord.Y > constraints2.MaxCoord.Y)
                        minLength = Math.Min(minLength, constraints1.MinCoord.Y - constraints2.MaxCoord.Y);
                    // 2 on top of 1
                    else
                        minLength = Math.Min(minLength, constraints2.MinCoord.Y - constraints1.MaxCoord.Y);
                }
                else if (yIntersection)
                {
                    // 1 to the left of 2
                    if (constraints1.MaxCoord.X < constraints2.MinCoord.X)
                        minLength = Math.Min(minLength, constraints2.MinCoord.X - constraints1.MaxCoord.X);
                    // 2 to the left of 1
                    else
                        minLength = Math.Min(minLength, constraints1.MinCoord.X - constraints2.MaxCoord.X);
                }
            }

            foreach (Vector point1 in constraints1.Corners)
            {
                foreach (Vector point2 in constraints2.Corners)
                {
                    double length = (point1 - point2).Length;
                    minLength = Math.Min(minLength, length);
                    maxLength = Math.Max(maxLength, length);
                }
            }

            Range lengthRange = new Range(minLength, maxLength);

            return new BoxLengthAngleConstraints(lengthRange, angleRange);
        }
    }
}
