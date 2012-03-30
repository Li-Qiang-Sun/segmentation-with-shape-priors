#pragma once

#include <cutil_math.h>

__device__ float cross(float2 left, float2 right)
{
	return left.x * right.y - left.y * right.x;
}

__device__ float length_sqr(float2 vec)
{
	return dot(vec, vec);
}

__device__ float2 min(float2 value1, float2 value2)
{
	return make_float2(min(value1.x, value2.x), min(value1.y, value2.y));
}

__device__ float2 max(float2 value1, float2 value2)
{
	return make_float2(max(value1.x, value2.x), max(value1.y, value2.y));
}

__device__ float2 trunc(float2 value, float2 minValue, float2 maxValue)
{
	return max(min(value, maxValue), minValue);
}

__device__ float log_inf(float x)
{
	const float threshold = 1e-15;
	return log(x < threshold ? threshold : x);
}

template<class T>
__device__ void device_swap(T &a, T &b)
{
	T temp = a;
	a = b;
	b = temp;
}

__device__ bool PointInConvexHull(float2 point, float2 *convexHullPoints, int convexHullPointCount)
{
    // Assumes that points are in clockwise order
    bool inside = true;
    for (int i = 0; i < convexHullPointCount; ++i)
    {
        float2 diff1 = point - convexHullPoints[i];
        float2 diff2 = convexHullPoints[(i + 1) % convexHullPointCount] - convexHullPoints[i];
        inside &= cross(diff1, diff2) >= 0;
    }

    return inside;
}

__device__ float DistanceToSegmentSqr(
    float2 point,
    float2 segmentStart,
    float2 segmentEnd)
{
    float2 v = segmentEnd - segmentStart;
    float2 p = point - segmentStart;

    float alpha = dot(v, p) / dot(v, v);
    if (alpha < 0)
        return length_sqr(p);
    if (alpha > 1)
        return length_sqr(point - segmentEnd);
    return length_sqr(segmentStart + v * alpha - point);
}