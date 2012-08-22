using System;
using System.Diagnostics;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior
{
    public class LengthAngleSpaceSeparator
    {
        private readonly double distanceSqr;

        private readonly double angleOffset;

        private readonly double sign;

        private readonly bool swapDirection;

        private readonly bool singularMode;

        private readonly Range singularModeAngleRange;

        public LengthAngleSpaceSeparator(
            Vector segmentStart, Vector segmentEnd, Vector point, double allowedLength, double allowedAngle, bool swapDirection)
        {
            if (allowedAngle < -Math.PI || allowedAngle > Math.PI)
                throw new ArgumentOutOfRangeException("allowedAngle", "Allowed angle should be in [-pi, pi] range.");
            if (allowedLength < 0)
                throw new ArgumentOutOfRangeException("allowedLength", "Allowed length should be positive.");
            if ((segmentStart - segmentEnd).LengthSquared < 1e-6)
                throw new ArgumentException("Given segment must have non-zero length!");
            
            double distanceToSegmentSqr, alpha;
            point.DistanceToSegmentSquared(segmentStart, segmentEnd, out distanceToSegmentSqr, out alpha);

            Vector zeroAngleVec = segmentStart + (segmentEnd - segmentStart) * alpha - point;
            this.distanceSqr = zeroAngleVec.LengthSquared;

            if (this.distanceSqr < 1e-6)
            {
                this.singularMode = true;
                
                double angle = Vector.AngleBetween(Vector.UnitX, segmentEnd - segmentStart);
                double minAngle, maxAngle;
                if (angle < 0)
                {
                    minAngle = angle;
                    maxAngle = angle + Math.PI;
                }
                else
                {
                    maxAngle = angle;
                    minAngle = angle - Math.PI;
                }

                singularModeAngleRange = new Range(minAngle, maxAngle);
                if (!singularModeAngleRange.Contains(allowedAngle))
                    singularModeAngleRange = singularModeAngleRange.Invert();
            }
            else
            {
                this.singularMode = false;

                this.angleOffset = -Vector.AngleBetween(Vector.UnitX, zeroAngleVec);
                double offsetedAllowedAngle = this.OffsetAngle(allowedAngle);
                if (offsetedAllowedAngle < -Math.PI * 0.5 || offsetedAllowedAngle > Math.PI * 0.5)
                    throw new ArgumentOutOfRangeException("allowedAngle", "After translation to the coordinate system of the given segment, allowed angle must be in [-pi/2, pi/2] range.");
                this.sign = Math.Sign(this.SeparationValue(allowedLength, offsetedAllowedAngle));
            }

            this.swapDirection = swapDirection;
        }

        public bool IsInside(double length, double angle)
        {
            if (length < 0)
                return false;

            angle = MathHelper.NormalizeAngle(angle);

            if (this.swapDirection)
            {
                if (angle > 0)
                    angle -= Math.PI;
                else
                    angle += Math.PI;
            }
            
            if (this.singularMode)
                return this.singularModeAngleRange.Contains(angle);
            
            double offsetedAngle = this.OffsetAngle(angle);
            if (offsetedAngle >= -Math.PI * 0.5 && offsetedAngle <= Math.PI * 0.5 && this.SeparationValue(length, offsetedAngle) * this.sign > 0)
                return true;

            return this.sign < 0 && (offsetedAngle >= Math.PI * 0.5 || offsetedAngle <= -Math.PI * 0.5);
        }

        private double OffsetAngle(double angle)
        {
            double result = MathHelper.NormalizeAngle(this.angleOffset + angle);
            return result;
        }

        private double SeparationValue(double length, double offsetedAngle)
        {
            Debug.Assert(offsetedAngle >= -Math.PI * 0.5 && offsetedAngle <= Math.PI * 0.5);
            double tanAngle = Math.Tan(offsetedAngle);
            return length * length - this.distanceSqr * (1 + tanAngle * tanAngle);
        }
    }
}