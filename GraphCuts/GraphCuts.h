// GraphCuts.h

#pragma once

#include <vector>
#include "maxflow\graph.h"

using namespace System;
using namespace System::Diagnostics;
using namespace std;

namespace Research
{
	namespace GraphBasedShapePrior
	{
		namespace GraphCuts
		{
			public enum class Neighbor
			{
				Left = 0,
				LeftTop,
				Top,
				RightTop,
				Right,
				RightBottom,
				Bottom,
				LeftBottom
			};
			
			public ref class GraphCutCalculator : IDisposable
			{
			internal:
		
				typedef Graph<double, double, double> GraphType;
				GraphType *graph;

				unsigned char *neighborsSet;
				int *dx, *dy;
		
				int width, height;
				bool dirty;
				bool firstGraphCut;
		
				int CoordsToIndex(int x, int y)
				{
					return width * y + x;
				}

			public:
				GraphCutCalculator(int width, int height)
				{
					this->width = width;
					this->height = height;
			
					graph = new GraphType(width * height, width * height * 4);
					graph->add_node(width * height);

					neighborsSet = new unsigned char[width * height];
					std::fill(neighborsSet, neighborsSet + width * height, 0);

					dx = new int[8];
					dy = new int[8];
					dx[0] = -1; dx[1] = -1; dx[2] = 0; dx[3] = 1;
					dx[4] = 1; dx[5] = 1; dx[6] = 0; dx[7] = -1;
					dy[0] = 0; dy[1] = -1; dy[2] = -1; dy[3] = -1;
					dy[4] = 0; dy[5] = 1; dy[6] = 1; dy[7] = 1;
			
					dirty = true;
					firstGraphCut = true;
				}

				~GraphCutCalculator()
				{
					delete graph;
					delete neighborsSet;
				}

				void UpdateTerminalWeights(int x, int y, double toSourceOld, double toSinkOld, double toSource, double toSink)
				{
					if (x < 0 || x >= width)
						throw gcnew ArgumentOutOfRangeException("x");
					if (y < 0 && y >= height)
						throw gcnew ArgumentOutOfRangeException("y");
					if (firstGraphCut)
						throw gcnew InvalidOperationException("Use SetTerminalWeights on first iteration.");

					int node = CoordsToIndex(x, y);
					
					double oldCapacity = toSourceOld - toSinkOld;
					if (oldCapacity > 0)
					{
						toSink += oldCapacity - toSinkOld;
						toSource += -toSinkOld;
					}
					else
					{
						toSource += -oldCapacity - toSourceOld;
						toSink += -toSourceOld;
					}
					graph->add_tweights(node, toSource, toSink);
					graph->mark_node(node);

					dirty = true;
				}

				void SetTerminalWeights(int x, int y, double toSource, double toSink)
				{
					if (x < 0 || x >= width)
						throw gcnew ArgumentOutOfRangeException("x");
					if (y < 0 && y >= height)
						throw gcnew ArgumentOutOfRangeException("y");
					if (!firstGraphCut)
						throw gcnew InvalidOperationException("Use UpdateTerminalWeights on consequent iterations.");

					int node = CoordsToIndex(x, y);
					graph->add_tweights(node, toSource, toSink);
				}

				void SetNeighborWeights(int x, int y, Neighbor neighbor, double weight)
				{
					if (!firstGraphCut)
						throw gcnew NotSupportedException("Only terminal weight updates are currently supported.");

					int index = CoordsToIndex(x, y);
					if (index < 0 || index >= width * height)
						throw gcnew ArgumentException("coordinates are out of range");	
					int neighborIndex = CoordsToIndex(x + dx[(int) neighbor], y + dy[(int) neighbor]);
					if (neighborIndex < 0 || neighborIndex >= width * height)
						throw gcnew ArgumentException("neighbor coordinates are out of range");	
					if (neighborsSet[index] & (1 << (int) neighbor))
						throw gcnew InvalidOperationException("This edge has been set already.");

					graph->add_edge(index, neighborIndex, weight, weight);
					neighborsSet[index] |= (1 << (int) neighbor);
					neighborsSet[neighborIndex] |= (1 << (((int) neighbor + 4) % 8));
				}

				double Calculate()
				{
					double energy = graph->maxflow(!firstGraphCut);
					//assert(firstGraphCut || energy == graph->maxflow(false));

					dirty = false;
					firstGraphCut = false;
					return energy;
				}

				bool BelongsToSource(int x, int y)
				{
					if (dirty)
						throw gcnew InvalidOperationException("You should calculate maxflow first.");
					if (x < 0 && x >= width)
						throw gcnew ArgumentOutOfRangeException("x");
					if (y < 0 && y >= height)
						throw gcnew ArgumentOutOfRangeException("y");
			
					int index = CoordsToIndex(x, y);
					return graph->what_segment(index) == GraphType::SOURCE;
				}
			};
		}
	}
}
