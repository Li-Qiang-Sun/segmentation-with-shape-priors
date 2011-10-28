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

__device__ float log_inf(float value)
{
    const float threshold = 1e-15f;
    if (value < threshold)
        return log(threshold);
    return log(value);
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

__device__ float DistToCircleOuter(float2 point, float2 center, float radius)
{
    return max(length(point - center) - radius, 0.f);
}

__device__ bool CircleInCircle(
	float2 centerOuter, float radiusOuter, float2 centerInner, float radiusInner)
{
    float distanceSqr = length_sqr(centerInner - centerOuter);
    float radiusDiff = radiusOuter - 2 * radiusInner;
    return radiusOuter >= radiusInner && distanceSqr <= radiusDiff * radiusDiff;
}

__device__ float DistanceToSegment(
    float2 point,
    float2 segmentStart,
    float2 segmentEnd)
{
    float2 v = segmentEnd - segmentStart;
    float2 p = point - segmentStart;

    float alpha = dot(v, p) / dot(v, v);
    if (alpha < 0)
        return length(p);
    if (alpha > 1)
        return length(point - segmentEnd);
    return length(segmentStart + v * alpha - point);
}

__device__ float DistanceToPulley(
    float2 point,
    float2 pulleyPoint1,
    float pulleyRadius1,
    float2 pulleyPoint2,
    float pulleyRadius2)
{
    // First circle should always be bigger
    if (pulleyRadius1 < pulleyRadius2)
    {
        device_swap(pulleyPoint1, pulleyPoint2);
        device_swap(pulleyRadius1, pulleyRadius2);
    }

    // Dist to large circle
    float distance = DistToCircleOuter(point, pulleyPoint1, pulleyRadius1);
    
    // Singular pulley
    if (CircleInCircle(pulleyPoint1, pulleyRadius1, pulleyPoint2, pulleyRadius2))
		return distance;

    // Dist to small circle
    distance = min(distance, DistToCircleOuter(point, pulleyPoint2, pulleyRadius2));
    
    // Inside one of the circles
    if (distance == 0)
        return 0;

    float edgeLength = length(pulleyPoint1 - pulleyPoint2);
    float cosAngle = (pulleyRadius1 - pulleyRadius2) / edgeLength;
    float angle = acos(cosAngle);
    float lineAngle = atan2(pulleyPoint2.y - pulleyPoint1.y, pulleyPoint2.x - pulleyPoint1.x);
    float cosPlusPlus = cos(lineAngle + angle);
    float sinPlusPlus = sin(lineAngle + angle);
    float cosPlusMinus = cos(lineAngle - angle);
    float sinPlusMinus = sin(lineAngle - angle);

    // Find pulley points
    float2 pulleyPoints[4];
    pulleyPoints[0] = make_float2(	// Line 2 point 1
        pulleyPoint1.x + pulleyRadius1 * cosPlusMinus,
        pulleyPoint1.y + pulleyRadius1 * sinPlusMinus);
	pulleyPoints[1] = make_float2(	// Line 1 point 1
        pulleyPoint1.x + pulleyRadius1 * cosPlusPlus,
        pulleyPoint1.y + pulleyRadius1 * sinPlusPlus);
    pulleyPoints[2] = make_float2(	// Line 1 point 2
        pulleyPoint2.x + pulleyRadius2 * cosPlusPlus,
        pulleyPoint2.y + pulleyRadius2 * sinPlusPlus);
    pulleyPoints[3] = make_float2(	// Line 2 point 2
        pulleyPoint2.x + pulleyRadius2 * cosPlusMinus,
        pulleyPoint2.y + pulleyRadius2 * sinPlusMinus);
    
    // Check if point is inside pulley
    if (PointInConvexHull(point, pulleyPoints, 4))
		return 0;

    distance = min(distance, DistanceToSegment(point, pulleyPoints[0], pulleyPoints[3]));
    distance = min(distance, DistanceToSegment(point, pulleyPoints[1], pulleyPoints[2]));
    return distance;
}