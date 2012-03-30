#pragma once

void CalculateShapeUnaryTerms(
	int edgeCount,
	float2 **convexHulls,
	int *convexHullSizes,
	float2 **ñorners1,
	float2 **corners2,
	float2 *edgeWidthLimits,
	float backgroundDistanceCoeff,
    int imageWidth,
    int imageHeight,
    float *objectPenalties,
    float *backgroundPenalties);