using System.Collections.Generic;
using System.Linq;

namespace Research.GraphBasedShapePrior
{
    public class LengthAngleSpaceSeparatorSet
    {
        private readonly List<List<LengthAngleSpaceSeparator>> separatorLists = new List<List<LengthAngleSpaceSeparator>>();

        public LengthAngleSpaceSeparatorSet(VertexConstraint constraint1, VertexConstraint constraint2)
        {
            this.AddSeparatorsForPair(constraint1, constraint2, false);
            this.AddSeparatorsForPair(constraint2, constraint1, true);
        }

        public bool IsInside(double length, double angle)
        {
            return length >= 0 && separatorLists.Any(list => list.All(separator => separator.IsInside(length, angle)));
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
