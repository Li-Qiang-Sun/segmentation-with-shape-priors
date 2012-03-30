using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Research.GraphBasedShapePrior
{
    public class AllowedLengthAngleChecker
    {
        private readonly Image2D<byte> lengthAngleStatus;

        private readonly GeneralizedDistanceTransform2D checkingTransform;
        
        public AllowedLengthAngleChecker(
            VertexConstraints constraint1,
            VertexConstraints constraint2,
            GeneralizedDistanceTransform2D checkingTransform,
            double lengthRatio,
            double meanAngle)
        {
            this.lengthAngleStatus = new Image2D<byte>(checkingTransform.GridSize.Width, checkingTransform.GridSize.Height);
            LengthAngleSpaceSeparatorSet separator = new LengthAngleSpaceSeparatorSet(constraint1, constraint2);

            this.checkingTransform = checkingTransform;
            
            // Initial fill
            for (int i = 0; i < checkingTransform.GridSize.Width; ++i)
            {
                double scaledLength = checkingTransform.GridIndexToCoordX(i);
                double length = scaledLength / lengthRatio;

                for (int j = 0; j < checkingTransform.GridSize.Height; ++j)
                {
                    double shiftedAngle = checkingTransform.GridIndexToCoordY(j);
                    double angle = shiftedAngle + meanAngle;

                    if (separator.IsInside(length, angle))
                        this.lengthAngleStatus[i, j] = 2;
                    else if (i == 0 || j == 0 || this.lengthAngleStatus[i - 1, j] == 1 || this.lengthAngleStatus[i, j - 1] == 1)
                        this.lengthAngleStatus[i, j] = 1;
                }
            }

            // Fill holes
            for (int i = 0; i < checkingTransform.GridSize.Width; ++i)
            {
                for (int j = 0; j < checkingTransform.GridSize.Height; ++j)
                {
                    if (lengthAngleStatus[i, j] == 0)
                        lengthAngleStatus[i, j] = 2;
                }
            }
        }

        public bool IsAllowed(double scaledLength, double shiftedAngle)
        {
            int lengthIndex = this.checkingTransform.CoordToGridIndexX(scaledLength);
            int angleIndex = this.checkingTransform.CoordToGridIndexY(shiftedAngle);
            return this.lengthAngleStatus[lengthIndex, angleIndex] == 2;
        }
    }
}
