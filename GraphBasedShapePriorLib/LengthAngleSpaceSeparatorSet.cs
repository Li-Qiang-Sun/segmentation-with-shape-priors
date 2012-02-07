using System;
using System.Collections.Generic;
using System.Linq;

namespace Research.GraphBasedShapePrior
{
    public class LengthAngleSpaceSeparatorSet
    {
        private readonly List<List<LengthAngleSpaceSeparator>> separatorLists = new List<List<LengthAngleSpaceSeparator>>();

        public LengthAngleSpaceSeparatorSet(VertexConstraint constraint1, VertexConstraint constraint2)
        {
            if (constraint1.CoordViolation < 1e-6 || constraint2.CoordViolation < 1e-6)
                throw new ArgumentException("Coord constraints should not be singular.");
            
            this.AddSeparatorsForPair(constraint1, constraint2, false);
            this.AddSeparatorsForPair(constraint2, constraint1, true);
        }

        public bool IsInside(double length, double angle)
        {
            if (length < 0)
                return false;

            for (int i = 0; i < this.separatorLists.Count; ++i)
            {
                bool all = true;
                for (int j = 0; j < separatorLists[i].Count; ++j)
                {
                    if (!separatorLists[i][j].IsInside(length, angle))
                    {
                        all = false;
                        break;
                    }
                }

                if (all)
                    return true;
            }

            return false;
        }

        private void AddSeparatorsForPair(VertexConstraint constraint1, VertexConstraint constraint2, bool swapDirection)
        {
            for (int i = 0; i < 4; ++i)
                this.AddSeparatorsForPoint(constraint1.Corners[i], constraint2, swapDirection);
        }

        private void AddSeparatorsForPoint(Vector point, VertexConstraint constraint2, bool swapDirection)
        {
            this.separatorLists.Add(new List<LengthAngleSpaceSeparator>());

            Vector middlePoint = 0.5 * (constraint2.Corners[0] + constraint2.Corners[2]);

            for (int j = 0; j < 4; ++j)
            {
                Vector segmentStart = constraint2.Corners[j];
                Vector segmentEnd = constraint2.Corners[(j + 1) % 4];
                Vector segmentMiddle = 0.5 * (segmentStart + segmentEnd);
                Vector allowedPoint = segmentMiddle + (middlePoint - segmentMiddle) * 0.01;
                Vector allowedVec = allowedPoint - point;
                double allowedLength = allowedVec.Length;
                double allowedAngle = Vector.AngleBetween(Vector.UnitX, allowedVec);

                this.separatorLists[this.separatorLists.Count - 1].Add(
                    new LengthAngleSpaceSeparator(segmentStart, segmentEnd, point, allowedLength, allowedAngle, swapDirection));
            }
        }
    }
}
