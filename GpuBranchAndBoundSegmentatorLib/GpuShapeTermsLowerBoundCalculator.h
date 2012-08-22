#pragma once

#include <cutil_math.h>

#include "Kernels.h"

using namespace System;
using namespace System::Diagnostics;
using namespace System::Drawing;
using namespace Research::GraphBasedShapePrior::Util;

namespace Research
{
	namespace GraphBasedShapePrior
	{
		public ref class GpuShapeTermsLowerBoundCalculator : public IShapeTermsLowerBoundCalculator
		{
		private:
			static const int maxConvexHullSize = 8;
			static const float infinity = 1e+20f;

			float2 **convexHulls;
			int *convexHullSizes;
			float2 *edgeWidthLimits;
			float2 **corners1;
			float2 **corners2;
			
			float *objectPenaltiesCpu;
			float *backgroundPenaltiesCpu;
			float *objectPenaltiesGpu;
			float *backgroundPenaltiesGpu;

			int lastEdgeCount;
			Size lastImageSize;

			float2 VectorToFloat2(Vector vec)
			{
				return make_float2(static_cast<float>(vec.X), static_cast<float>(vec.Y));
			}

			template<class T>
			void FreeArray2D(T **arr, int size)
			{
				if (arr == NULL)
					return;
				for (int i = 0; i < size; ++i)
					delete[] arr[i];
				delete[] arr;
			}

			void Allocate()
			{
				convexHulls = new float2*[lastEdgeCount];
				corners1 = new float2*[lastEdgeCount];
				corners2 = new float2*[lastEdgeCount];
				for (int i = 0; i < lastEdgeCount; ++i)
				{
					convexHulls[i] = new float2[maxConvexHullSize];
					corners1[i] = new float2[4];
					corners2[i] = new float2[4];
				}

				convexHullSizes = new int[lastEdgeCount];
				edgeWidthLimits = new float2[lastEdgeCount];

				size_t totalPixels = lastImageSize.Width * lastImageSize.Height;
				objectPenaltiesCpu = new float[totalPixels];
				backgroundPenaltiesCpu = new float[totalPixels];

				pin_ptr<float*> pinnedObjectPenaltiesGpu = &objectPenaltiesGpu;
				cudaMalloc((void**) pinnedObjectPenaltiesGpu, totalPixels * sizeof(float));
				pin_ptr<float*> pinnedBackgroundPenaltiesGpu = &backgroundPenaltiesGpu;
				cudaMalloc((void**) pinnedBackgroundPenaltiesGpu, totalPixels * sizeof(float));
			}

			void Deallocate()
			{
				FreeArray2D(convexHulls, lastEdgeCount);
				FreeArray2D(corners1, lastEdgeCount);
				FreeArray2D(corners2, lastEdgeCount);
				delete[] convexHullSizes;
				delete[] edgeWidthLimits;

				delete[] objectPenaltiesCpu;
				delete[] backgroundPenaltiesCpu;
				
				cudaFree(objectPenaltiesGpu);
				cudaFree(backgroundPenaltiesGpu);
			}

		public:
			GpuShapeTermsLowerBoundCalculator()
				: convexHulls(NULL)
				, convexHullSizes(NULL)
				, edgeWidthLimits(NULL)
				, corners1(NULL)
				, corners2(NULL)
				, objectPenaltiesCpu(NULL)
				, backgroundPenaltiesCpu(NULL)
				, objectPenaltiesGpu(NULL)
				, backgroundPenaltiesGpu(NULL)
				, lastEdgeCount(-1)
				, lastImageSize(0, 0)
			{
			}

			~GpuShapeTermsLowerBoundCalculator()
			{
				this->!GpuShapeTermsLowerBoundCalculator();
			}

			!GpuShapeTermsLowerBoundCalculator()
			{
				Deallocate();
			}

			virtual void CalculateShapeTerms(ShapeModel ^shapeModel, ShapeConstraints ^shapeConstraints, Image2D<ObjectBackgroundTerm> ^result)
			{
				int edgeCount = shapeConstraints->ShapeStructure->Edges->Count;
				Size imageSize = Size(result->Width, result->Height);

				if (edgeCount != lastEdgeCount || imageSize != lastImageSize)
				{
					Deallocate();
					lastEdgeCount = edgeCount;
					lastImageSize = imageSize;
					Allocate();
				}

				// Copy params
				for (int edgeIndex = 0; edgeIndex < edgeCount; ++edgeIndex)
				{
					ShapeEdge edge = shapeConstraints->ShapeStructure->Edges[edgeIndex];
					EdgeConstraints ^edgeConstraints = shapeConstraints->EdgeConstraints[edgeIndex];
					VertexConstraints ^vertexConstraints1 = shapeConstraints->VertexConstraints[edge.Index1];
					VertexConstraints ^vertexConstraints2 = shapeConstraints->VertexConstraints[edge.Index2];
					
					edgeWidthLimits[edgeIndex] = make_float2(static_cast<float>(edgeConstraints->MinWidth), static_cast<float>(edgeConstraints->MaxWidth));
					
					for (int i = 0; i < 4; ++i)
					{
						corners1[edgeIndex][i] = VectorToFloat2(vertexConstraints1->Corners[i]);
						corners2[edgeIndex][i] = VectorToFloat2(vertexConstraints2->Corners[i]);
					}
					
					Polygon ^convexHull = shapeConstraints->GetConvexHullForVertexPair(edge.Index1, edge.Index2);
					Debug::Assert(convexHull->Vertices->Count <= maxConvexHullSize);
					convexHullSizes[edgeIndex] = convexHull->Vertices->Count;
					for (int i = 0; i < convexHull->Vertices->Count; ++i)
						convexHulls[edgeIndex][i] = make_float2(static_cast<float>(convexHull->Vertices[i].X), static_cast<float>(convexHull->Vertices[i].Y));
				}

				size_t totalImageSize = lastImageSize.Width * lastImageSize.Height;
				size_t totalImageByteSize = totalImageSize * sizeof(float);
				
				// Cleanup object/background penalties
				for (size_t i = 0; i < totalImageSize; ++i)
				{
					objectPenaltiesCpu[i] = infinity;
					backgroundPenaltiesCpu[i] = -infinity;
				}
				cudaMemcpy(objectPenaltiesGpu, objectPenaltiesCpu, totalImageByteSize, cudaMemcpyHostToDevice);
				cudaMemcpy(backgroundPenaltiesGpu, backgroundPenaltiesCpu, totalImageByteSize, cudaMemcpyHostToDevice);

				// Call CUDA code wrapper
				CalculateShapeUnaryTerms(
					edgeCount,
					convexHulls,
					convexHullSizes,
					corners1,
					corners2,
					edgeWidthLimits,
					static_cast<float>(shapeModel->BackgroundDistanceCoeff),
					result->Width,
					result->Height,
					objectPenaltiesGpu,
					backgroundPenaltiesGpu);

				// Copy results
				cudaMemcpy(objectPenaltiesCpu, objectPenaltiesGpu, totalImageByteSize, cudaMemcpyDeviceToHost);
				cudaMemcpy(backgroundPenaltiesCpu, backgroundPenaltiesGpu, totalImageByteSize, cudaMemcpyDeviceToHost);
				for (int x = 0; x < result->Width; ++x)
				{
					for (int y = 0; y < result->Height; ++y)
					{
						int index = y * result->Width + x;
						result[x, y] = ObjectBackgroundTerm(objectPenaltiesCpu[index], backgroundPenaltiesCpu[index]);
					}
				}
			}
		};
	}
}
