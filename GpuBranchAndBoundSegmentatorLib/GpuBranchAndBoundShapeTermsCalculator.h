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
		public:

			virtual void CalculateShapeTerms(VertexConstraintSet ^constraintSet, Image2D<ObjectBackgroundTerm> ^result)
			{
				int edgeCount = constraintSet->ShapeModel->Edges->Count;
				
				// Allocate unmanaged storage
				float2 **corners1 = new float2*[edgeCount];
				float2 **corners2 = new float2*[edgeCount];
				float2 **convexHulls = new float2*[edgeCount];
				int* convexHullSizes = new int[edgeCount];
				float2 *minRadii = new float2[edgeCount];
				float2 *maxRadii = new float2[edgeCount];
				float *objectPenalties = new float[result->Width * result->Height];
				float *backgroundPenalties = new float[result->Width * result->Height];

				// Copy params
				for (int i = 0; i < edgeCount; ++i)
				{
					ShapeEdge edge = constraintSet->ShapeModel->Edges[i];
					VertexConstraint ^vertex1 = constraintSet->GetConstraintsForVertex(edge.Index1);
					VertexConstraint ^vertex2 = constraintSet->GetConstraintsForVertex(edge.Index2);
					
					minRadii[i] = make_float2(static_cast<float>(vertex1->MinRadius), static_cast<float>(vertex2->MinRadius));
					maxRadii[i] = make_float2(static_cast<float>(vertex1->MaxRadius), static_cast<float>(vertex2->MaxRadius));

					corners1[i] = new float2[4];
					corners2[i] = new float2[4];
					for (int j = 0; j < 4; ++j)
					{
						corners1[i][j] = make_float2(static_cast<float>(vertex1->Corners[j].X), static_cast<float>(vertex1->Corners[j].Y));
						corners2[i][j] = make_float2(static_cast<float>(vertex2->Corners[j].X), static_cast<float>(vertex2->Corners[j].Y));
					}

					Polygon ^convexHull = constraintSet->GetConvexHullForVertexPair(edge.Index1, edge.Index2);
					convexHullSizes[i] = convexHull->Vertices->Count;
					convexHulls[i] = new float2[convexHullSizes[i]];
					for (int j = 0; j < convexHullSizes[i]; ++j)
						convexHulls[i][j] = make_float2(static_cast<float>(convexHull->Vertices[j].X), static_cast<float>(convexHull->Vertices[j].Y));
				}
				
				// Call CUDA code wrapper
				CalculateShapeUnaryTerms(edgeCount, corners1, corners2, convexHulls, convexHullSizes, minRadii, maxRadii, result->Width, result->Height,
					static_cast<float>(constraintSet->ShapeModel->Cutoff), objectPenalties, backgroundPenalties);

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
				FreeArray2D(corners1, edgeCount);
				FreeArray2D(corners2, edgeCount);
				FreeArray2D(convexHulls, edgeCount);
				delete[] convexHullSizes;
				delete[] minRadii;
				delete[] maxRadii;
				delete[] objectPenalties;
				delete[] backgroundPenalties;
			}
		};
	}
}
