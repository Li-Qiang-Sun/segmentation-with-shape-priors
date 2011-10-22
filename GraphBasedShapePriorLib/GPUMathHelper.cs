using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TidePowerd.DeviceMethods;
using TidePowerd.DeviceMethods.Vectors;

namespace Research.GraphBasedShapePrior
{
    public static class GPUMathHelper
    {
        [Function]
        public static float DotProduct(SingleVector2 vec1, SingleVector2 vec2)
        {
            return vec1.X * vec2.X + vec1.Y * vec2.Y;
        }

        [Function]
        public static int DotProduct(Int16Vector2 vec1, Int16Vector2 vec2)
        {
            return (int)vec1.X * vec2.X + (int)vec1.Y * vec2.Y;
        }

        [Function]
        public static float CrossProduct(SingleVector2 vec1, SingleVector2 vec2)
        {
            return vec1.X * vec2.Y - vec1.Y * vec2.X;
        }

        [Function]
        public static int CrossProduct(Int16Vector2 vec1, Int16Vector2 vec2)
        {
            return (int)vec1.X * vec2.Y - (int)vec1.Y * vec2.X;
        }

        [Function]
        public static Int16Vector2 VectorSub(Int16Vector2 left, Int16Vector2 right)
        {
            return new Int16Vector2((short)(left.X - right.X), (short)(left.Y - right.Y));
        }

        [Function]
        public static SingleVector2 VectorSub(SingleVector2 left, SingleVector2 right)
        {
            return new SingleVector2(left.X - right.X, left.Y - right.Y);
        }

        [Function]
        public static Int16Vector2 VectorAdd(Int16Vector2 left, Int16Vector2 right)
        {
            return new Int16Vector2((short)(left.X + right.X), (short)(left.Y + right.Y));
        }

        [Function]
        public static SingleVector2 VectorAdd(SingleVector2 left, SingleVector2 right)
        {
            return new SingleVector2(left.X + right.X, left.Y + right.Y);
        }

        [Function]
        public static float Length(Int16Vector2 vec)
        {
            return DeviceMath.Sqrt(LengthSqr(vec));
        }

        [Function]
        public static float Length(SingleVector2 vec)
        {
            return DeviceMath.Sqrt(LengthSqr(vec));
        }

        [Function]
        public static int LengthSqr(Int16Vector2 vec)
        {
            return DotProduct(vec, vec);
        }

        [Function]
        public static float LengthSqr(SingleVector2 vec)
        {
            return DotProduct(vec, vec);
        }

        [Function]
        public static bool CircleInCircle(
            Int16Vector2 centerOuter, int radiusOuter, Int16Vector2 centerInner, int radiusInner)
        {
            int distanceSqr = LengthSqr(VectorSub(centerInner, centerOuter));
            int radiusDiff = radiusOuter - radiusInner;
            return radiusOuter >= radiusInner && distanceSqr <= radiusDiff * radiusDiff;
        }

        public static SingleVector2 ToSingle2(Int16Vector2 vec)
        {
            return new SingleVector2(vec.X, vec.Y);
        }

        [Function]
        public static float DistanceToSegment(
            SingleVector2 point,
            SingleVector2 segmentStart,
            SingleVector2 segmentEnd)
        {
            SingleVector2 v = VectorSub(segmentEnd, segmentStart);
            SingleVector2 p = VectorSub(point, segmentStart);

            float alpha = DotProduct(v, p) / (float)LengthSqr(v);
            if (alpha < 0)
                return Length(p);
            if (alpha > 0)
                return Length(VectorSub(point, segmentEnd));

            SingleVector2 scaledV = new SingleVector2(v.X * alpha, v.Y * alpha);
            return Length(VectorSub(VectorAdd(segmentStart, scaledV), point));
        }
    }
}
