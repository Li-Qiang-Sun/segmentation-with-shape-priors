using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Research.GraphBasedShapePrior
{
    public class AllowedLengthAngleChecker
    {
        private readonly Image2D<byte> lengthAngleStatus;
        
        public AllowedLengthAngleChecker(
            VertexConstraint constraint1, VertexConstraint constraint2, GeneralizedDistanceTransform2D checkingTransform)
        {
            this.lengthAngleStatus = new Image2D<byte>(checkingTransform.GridSize.Width, checkingTransform.GridSize.Height);
            LengthAngleSpaceSeparatorSet separator = new LengthAngleSpaceSeparatorSet(constraint1, constraint2);
            
            // Initial fill
            for (int i = 0; i < checkingTransform.GridSize.Width; ++i)
            {
                double length = checkingTransform.GridIndexToCoordX(i);
                for (int j = 0; j < checkingTransform.GridSize.Height; ++j)
                {
                    double angle = checkingTransform.GridIndexToCoordY(j);
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

        public bool IsAllowed(int lengthGridIndex, int angleGridIndex)
        {
            return this.lengthAngleStatus[lengthGridIndex, angleGridIndex] == 2;
        }
    }
}
