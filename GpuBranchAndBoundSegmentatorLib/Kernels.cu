#include "CudaMath.h"
#include "Kernels.h"

__device__ float DistanceToObjectPenalty(float distance, float cutoff)
{
    return -log_inf(exp(-cutoff * distance * distance));
}

__device__ float DistanceToBackgroundPenalty(float distance, float cutoff)
{
    return -log_inf(1 - exp(-cutoff * distance * distance));
}

__device__ float2 GetClosestPoint(float2 point, float2 corners[4])
{
    // Clockwise order from bottom left (min) corner assumed
    float2 min = corners[0];
    float2 max = corners[2];
    
    if (point.x >= min.x && point.x <= max.x)
    {
        if (point.y <= min.y)
            return make_float2(point.x, min.y);
        if (point.y >= max.y)
            return make_float2(point.x, max.y);
    }

    if (point.y >= min.y && point.y <= max.y)
    {
        if (point.x <= min.x)
            return make_float2(min.x, point.y);
        if (point.x >= max.x)
            return make_float2(max.x, point.y);
    }

    // Just because we have to return some point
    return min;
}

#define BLOCK_DIM 16
#define INFINITY 1e+20f

__device__ __constant__ float2 VertexCorners1[4];
__device__ __constant__ float2 VertexCorners2[4];
__device__ __constant__ float2 EdgeConvexHull[8];

__global__ void CalcMinObjectPenaltyKernel(
    int2 imageSize,
    int edgeConvexHullSize,
    float2 maxRadii,
    float distanceCutoff,
    float *minObjectPenalties)
{
    int2 pointInt;
    pointInt.x = blockIdx.x * BLOCK_DIM + threadIdx.x;
    pointInt.y = blockIdx.y * BLOCK_DIM + threadIdx.y;
    if (pointInt.x >= imageSize.x || pointInt.y >= imageSize.y)
        return;
    int index = pointInt.x + pointInt.y * imageSize.x;
    float2 point = make_float2(pointInt.x, pointInt.y);

    float distance = INFINITY;
    if (PointInConvexHull(point, EdgeConvexHull, edgeConvexHullSize))
    {
        distance = 0;
    }
    else
    {
		for (int corner1 = 0; corner1 < 4; ++corner1)
            for (int corner2 = 0; corner2 < 4; ++corner2)
            {
                float distanceToEdge = DistanceToPulleyArea(
                    point,
                    VertexCorners1[corner1],
                    maxRadii.x,
                    VertexCorners2[corner2],
                    maxRadii.y);
                distance = min(distance, distanceToEdge);
            }

        float2 closestPoint1 = GetClosestPoint(point, VertexCorners1);
        float2 closestPoint2 = GetClosestPoint(point, VertexCorners2);

        for (int corner = 0; corner < 4; ++corner)
        {
            float distanceToEdge1 = DistanceToPulleyArea(
                point,
                closestPoint1,
                maxRadii.x,
                VertexCorners2[corner],
                maxRadii.y);
            distance = min(distance, distanceToEdge1);

            float distanceToEdge2 = DistanceToPulleyArea(
                point,
                VertexCorners1[corner], 
                maxRadii.x,
                closestPoint2,
                maxRadii.y);
            distance = min(distance, distanceToEdge2);
        }

        float distanceBetweenClosestPoints = DistanceToPulleyArea(
                point,
                closestPoint1,
                maxRadii.x,
                closestPoint2,
                maxRadii.y);
        distance = min(distance, distanceBetweenClosestPoints);
    }

    float penalty = DistanceToObjectPenalty(distance, distanceCutoff);
    minObjectPenalties[index] = min(minObjectPenalties[index], penalty);
}

__global__ void CalcMaxBackgroundPenaltyKernel(
    int2 imageSize,
    int edgeConvexHullSize,
    float2 minRadii,
    float distanceCutoff,
    float *maxBackgroundPenalties)
{
    int2 pointInt;
    pointInt.x = blockIdx.x * BLOCK_DIM + threadIdx.x;
    pointInt.y = blockIdx.y * BLOCK_DIM + threadIdx.y;
    if (pointInt.x >= imageSize.x || pointInt.y >= imageSize.y)
        return;
    int index = pointInt.x + pointInt.y * imageSize.x;
    float2 point = make_float2(pointInt.x, pointInt.y);

    float distance = 0;
	for (int corner1 = 0; corner1 < 4; ++corner1)
        for (int corner2 = 0; corner2 < 4; ++corner2)
        {
            float distanceToEdge = DistanceToPulleyArea(
                point,
                VertexCorners1[corner1],
                minRadii.x,
                VertexCorners2[corner2],
                minRadii.y);
            distance = max(distance, distanceToEdge);
        }

    float penalty = DistanceToBackgroundPenalty(distance, distanceCutoff);
    maxBackgroundPenalties[index] = max(maxBackgroundPenalties[index], penalty);
}

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

    // Calculate min object penalty
    for (int i = 0; i < edgeCount; ++i)
    {
        // Prepare corners and convex hull
        cudaMemcpyToSymbol("VertexCorners1", corners1[i], 4 * sizeof(float2));
		cudaMemcpyToSymbol("VertexCorners2", corners2[i], 4 * sizeof(float2));
		cudaMemcpyToSymbol("EdgeConvexHull", convexHulls[i], convexHullSizes[i] * sizeof(float2));
        
        // Prepare GPU grid
        dim3 blockDim(BLOCK_DIM, BLOCK_DIM, 1);
        dim3 gridDim((imageWidth + blockDim.x - 1) / blockDim.x, (imageHeight + blockDim.y - 1) / blockDim.y, 1);
        
        // Run kernel to update penalty storage
        CalcMinObjectPenaltyKernel<<<gridDim, blockDim, 0>>>(
            make_int2(imageWidth, imageHeight),
            convexHullSizes[i],
            maxRadii[i],
            distanceCutoff,
            objectPenaltiesGPU);
		CalcMaxBackgroundPenaltyKernel<<<gridDim, blockDim, 0>>>(
            make_int2(imageWidth, imageHeight),
            convexHullSizes[i],
            minRadii[i],
            distanceCutoff,
            backgroundPenaltiesGPU);

		cudaThreadSynchronize();
    }

	// Save results
    cudaMemcpy(objectPenalties, objectPenaltiesGPU, totalStorageSize, cudaMemcpyDeviceToHost);
	cudaMemcpy(backgroundPenalties, backgroundPenaltiesGPU, totalStorageSize, cudaMemcpyDeviceToHost);
    
    cudaFree(objectPenaltiesGPU);
	cudaFree(backgroundPenaltiesGPU);
}