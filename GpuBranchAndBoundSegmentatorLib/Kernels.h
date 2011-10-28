#pragma once

void CalculateShapeUnaryTerms(
	int edgeCount,
	float2 **corners1,
	float2 **corners2,
	float2 **convexHulls,
	int *convexHullSizes,
	float2 *minRadii,
	float2 *maxRadii,
    int imageWidth,
    int imageHeight,
    float distanceCutoff,
    float *objectPenalties,
    float *backgroundPenalties);