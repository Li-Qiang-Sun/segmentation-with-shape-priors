#include "CudaMath.h"
#include "Kernels.h"

#define BLOCK_DIM 16
#define INFINITY 1e+20f
#define LOG_2 0.69314718

__device__ float DistanceSqrToObjectPenalty(float distanceSqr, float edgeWidthSqr)
{
	return 4 * LOG_2 * distanceSqr / (edgeWidthSqr + 1e-6);
}

__device__ float DistanceSqrToBackgroundPenalty(float distanceSqr, float edgeWidthSqr)
{
	return -log(1 + 1e-6 - exp(-DistanceSqrToObjectPenalty(distanceSqr, edgeWidthSqr)));
}

__device__ __constant__ float2 EdgeConvexHull[8];
__device__ __constant__ float2 Corners1[4]; 
__device__ __constant__ float2 Corners2[4];

__global__ void CalcMinPenaltiesForEdgeKernel(
    int2 imageSize,
    int edgeConvexHullSize,
    float2 minMaxWidthSqr,
    float *objectPenalties,
	float *backgroundPenalties)
{
    int2 pointInt;
    pointInt.x = blockIdx.x * BLOCK_DIM + threadIdx.x;
    pointInt.y = blockIdx.y * BLOCK_DIM + threadIdx.y;
    if (pointInt.x >= imageSize.x || pointInt.y >= imageSize.y)
        return;
    int index = pointInt.x + pointInt.y * imageSize.x;
    float2 point = make_float2(pointInt.x, pointInt.y);

	float minDistanceSqr;
	if (PointInConvexHull(point, EdgeConvexHull, edgeConvexHullSize))
        minDistanceSqr = 0;
	else
	{
		minDistanceSqr = INFINITY;
		for (int i = 0; i < edgeConvexHullSize; ++i)
		{
			float distanceSqr = DistanceToSegmentSqr(point, EdgeConvexHull[i], EdgeConvexHull[(i + 1) % edgeConvexHullSize]);
			minDistanceSqr = min(minDistanceSqr, distanceSqr);
		}
	}
	
	float maxDistanceSqr = 0;
	for (int i = 0; i < 4; ++i)
	{
		for (int j = 0; j < 4; ++j)
		{
			float distanceSqr = DistanceToSegmentSqr(point, Corners1[i], Corners2[j]);
			maxDistanceSqr = max(maxDistanceSqr, distanceSqr);
		}
	}
	
	float minObjectPenalty = DistanceSqrToObjectPenalty(minDistanceSqr, minMaxWidthSqr.y);
	float minBackgroundPenalty = DistanceSqrToBackgroundPenalty(maxDistanceSqr, minMaxWidthSqr.x);

    objectPenalties[index] = min(objectPenalties[index], minObjectPenalty);
	backgroundPenalties[index] = max(backgroundPenalties[index], minBackgroundPenalty);
}

void CalculateShapeUnaryTerms(
	int edgeCount,
	float2 **convexHulls,
	int *convexHullSizes,
	float2 **corners1,
	float2 **corners2,
	float2 *edgeWidthLimits,
    int imageWidth,
    int imageHeight,
    float *objectPenalties,
    float *backgroundPenalties)
{
    for (int i = 0; i < edgeCount; ++i)
    {
        // Setup convex hull for the current edge
        cudaMemcpyToSymbol("EdgeConvexHull", convexHulls[i], convexHullSizes[i] * sizeof(float2));
		cudaMemcpyToSymbol("Corners1", corners1[i], 4 * sizeof(float2));
		cudaMemcpyToSymbol("Corners2", corners2[i], 4 * sizeof(float2));
        
        // Prepare GPU grid
        dim3 blockDim(BLOCK_DIM, BLOCK_DIM, 1);
        dim3 gridDim((imageWidth + blockDim.x - 1) / blockDim.x, (imageHeight + blockDim.y - 1) / blockDim.y, 1);
        
        // Run kernel to update penalty storage
        CalcMinPenaltiesForEdgeKernel<<<gridDim, blockDim, 0>>>(
            make_int2(imageWidth, imageHeight),
            convexHullSizes[i],
            make_float2(edgeWidthLimits[i].x * edgeWidthLimits[i].x, edgeWidthLimits[i].y * edgeWidthLimits[i].y),
            objectPenalties,
			backgroundPenalties);

		cudaThreadSynchronize();
    }
}