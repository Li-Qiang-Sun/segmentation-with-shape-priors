#pragma once

#include <cutil_math.h>

#include "Kernels.h"

using namespace System;

template<class T>
void FreeArray2D(T **arr, int size)
{
	for (int i = 0; i < size; ++i)
		delete[] arr[i];
	delete[] arr;
}

namespace Research
{
	namespace GraphBasedShapePrior
	{
		public ref class GpuBranchAndBoundShapeTermsCalculator : public IBranchAndBoundShapeTermsCalculator
		{
		private:

			float2 VectorToFloat2(Vector vec)
			{
				return make_float2(static_cast<float>(vec.X), static_cast<float>(vec.Y));
			}

		public:

			virtual void CalculateShapeTerms(ShapeConstraints ^shapeConstraints, Image2D<ObjectBackgroundTerm> ^result)
			{
				int edgeCount = shapeConstraints->ShapeModel->Edges->Count;
				
				// Allocate unmanaged storage
				// TODO: memory leak is possible here
				float2 **convexHulls = new float2*[edgeCount];
				int *convexHullSizes = new int[edgeCount];
				float2 *edgeWidthLimits = new float2[edgeCount];
				float2 **corners1 = new float2*[edgeCount];
				float2 **corners2 = new float2*[edgeCount];
				float *objectPenalties = new float[result->Width * result->Height];
				float *backgroundPenalties = new float[result->Width * result->Height];

				// Copy params
				for (int edgeIndex = 0; edgeIndex < edgeCount; ++edgeIndex)
				{
					ShapeEdge edge = shapeConstraints->ShapeModel->Edges[edgeIndex];
					EdgeConstraints ^edgeConstraints = shapeConstraints->EdgeConstraints[edgeIndex];
					VertexConstraints ^vertexConstraints1 = shapeConstraints->VertexConstraints[edge.Index1];
					VertexConstraints ^vertexConstraints2 = shapeConstraints->VertexConstraints[edge.Index2];
					
					edgeWidthLimits[edgeIndex] = make_float2(static_cast<float>(edgeConstraints->MinWidth), static_cast<float>(edgeConstraints->MaxWidth));
					
					corners1[edgeIndex] = new float2[4];
					corners2[edgeIndex] = new float2[4];
					for (int i = 0; i < 4; ++i)
					{
						corners1[edgeIndex][i] = VectorToFloat2(vertexConstraints1->Corners[i]);
						corners2[edgeIndex][i] = VectorToFloat2(vertexConstraints2->Corners[i]);
					}
					
					Polygon ^convexHull = shapeConstraints->GetConvexHullForVertexPair(edge.Index1, edge.Index2);
					convexHullSizes[edgeIndex] = convexHull->Vertices->Count;
					convexHulls[edgeIndex] = new float2[convexHull->Vertices->Count];
					for (int i = 0; i < convexHull->Vertices->Count; ++i)
						convexHulls[edgeIndex][i] = make_float2(static_cast<float>(convexHull->Vertices[i].X), static_cast<float>(convexHull->Vertices[i].Y));
				}
				
				// Call CUDA code wrapper
				CalculateShapeUnaryTerms(
					edgeCount,
					convexHulls,
					convexHullSizes,
					corners1,
					corners2,
					edgeWidthLimits,
					static_cast<float>(shapeConstraints->ShapeModel->BackgroundDistanceCoeff),
					result->Width,
					result->Height,
					objectPenalties,
					backgroundPenalties);

				// Copy results
				for (int x = 0; x < result->Width; ++x)
				{
					for (int y = 0; y < result->Height; ++y)
					{
						int index = y * result->Width + x;
						result[x, y] = ObjectBackgroundTerm(objectPenalties[index], backgroundPenalties[index]);
					}
				}

				// Free unmanaged storage
				FreeArray2D(convexHulls, edgeCount);
				FreeArray2D(corners1, edgeCount);
				FreeArray2D(corners2, edgeCount);
				delete[] convexHullSizes;
				delete[] edgeWidthLimits;
				delete[] objectPenalties;
				delete[] backgroundPenalties;
			}
		};
	}
}
