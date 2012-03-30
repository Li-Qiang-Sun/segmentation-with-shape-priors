#include "CudaMath.h"
#include "Kernels.h"

__device__ float DistanceSqrToObjectPenalty(float distanceSqr, float edgeWidthSqr, float backgroundDistanceCoeff)
{
	return distanceSqr;
}

__device__ float DistanceSqrToBackgroundPenalty(float distanceSqr, float edgeWidthSqr, float backgroundDistanceCoeff)
{
	return max(edgeWidthSqr * (1 + backgroundDistanceCoeff) - backgroundDistanceCoeff * distanceSqr, 0.f);
}

// Clockwise order from bottom left (min) corner assumed
__device__ float2 ProjectToConstraints(float2 point, float2 corners[4])
{
	return trunc(point, corners[0], corners[2]);
}

#define BLOCK_DIM 16
#define INFINITY 1e+20f

__device__ __constant__ float2 EdgeConvexHull[8];
__device__ __constant__ float2 Corners1[4];
__device__ __constant__ float2 Corners2[4];

__global__ void CalcMinPenaltiesForEdgeKernel(
    int2 imageSize,
    int edgeConvexHullSize,
	float backgroundDistanceCoeff,
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

    float minDistanceSqr = INFINITY;
	float maxDistanceSqr = 0;

    if (PointInConvexHull(point, EdgeConvexHull, edgeConvexHullSize))
        minDistanceSqr = 0;
	
	for (int i = 0; i < 4; ++i)
	{
		for (int j = 0; j < 4; ++j)
		{
			float distanceSqr = DistanceToSegmentSqr(point, Corners1[i], Corners2[j]);
			minDistanceSqr = min(minDistanceSqr, distanceSqr);
			maxDistanceSqr = max(maxDistanceSqr, distanceSqr);
		}
	}
	
	float2 projection1 = ProjectToConstraints(point, Corners1);
	for (int i = 0; i < 4; ++i)
		minDistanceSqr = min(minDistanceSqr, DistanceToSegmentSqr(point, projection1, Corners2[i]));

	float2 projection2 = ProjectToConstraints(point, Corners2);
	for (int i = 0; i < 4; ++i)
		minDistanceSqr = min(minDistanceSqr, DistanceToSegmentSqr(point, Corners1[i], projection2));

	float minObjectPenalty = DistanceSqrToObjectPenalty(minDistanceSqr, minMaxWidthSqr.y, backgroundDistanceCoeff);
	float minBackgroundPenalty = DistanceSqrToBackgroundPenalty(maxDistanceSqr, minMaxWidthSqr.x, backgroundDistanceCoeff);

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
	float backgroundDistanceCoeff,
    int imageWidth,
    int imageHeight,
    float *objectPenalties,
    float *backgroundPenalties)
{
	int totalImageSize = imageWidth * imageHeight;
    
    // Prepare GPU storage
    float *objectPenaltiesGPU;
	float *backgroundPenaltiesGPU;
	int totalStorageSize = totalImageSize * sizeof(float);
    cudaMalloc((void**) &objectPenaltiesGPU, totalStorageSize);
	cudaMalloc((void**) &backgroundPenaltiesGPU, totalStorageSize);
    
    // Cleanup storage
	for (int i = 0; i < totalImageSize; ++i)
    {
		objectPenalties[i] = INFINITY;
		backgroundPenalties[i] = -INFINITY;
    }
    cudaMemcpy(objectPenaltiesGPU, objectPenalties, totalStorageSize, cudaMemcpyHostToDevice);
	cudaMemcpy(backgroundPenaltiesGPU, backgroundPenalties, totalStorageSize, cudaMemcpyHostToDevice);

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
			backgroundDistanceCoeff,
            make_float2(edgeWidthLimits[i].x * edgeWidthLimits[i].x, edgeWidthLimits[i].y * edgeWidthLimits[i].y),
            objectPenaltiesGPU,
			backgroundPenaltiesGPU);

		cudaThreadSynchronize();
    }

	// Save results
    cudaMemcpy(objectPenalties, objectPenaltiesGPU, totalStorageSize, cudaMemcpyDeviceToHost);
	cudaMemcpy(backgroundPenalties, backgroundPenaltiesGPU, totalStorageSize, cudaMemcpyDeviceToHost);
    
    cudaFree(objectPenaltiesGPU);
	cudaFree(backgroundPenaltiesGPU);
}