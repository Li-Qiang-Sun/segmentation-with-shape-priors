// OptimizedComputations.h

#pragma once

#include <cmath>
#include <algorithm>

using namespace System;
using namespace std;

namespace Research
{
	namespace GraphBasedShapePrior
	{
		public ref class OptimizedComputations abstract sealed
		{
		private:

			static double DistanceToObjectPotential(double distance, double width, double constantProbRate, double cutoff)
			{
				double result = max(distance - constantProbRate * width, 0.0);
				result /= (1 - constantProbRate) * width;
				result = exp(-cutoff * result * result);

				return result;
			}

			static void PointToSegmentDistance(double px, double py, double v1x, double v1y, double v2x, double v2y, double *distance, double *alpha)
			{
				double vx = v2x - v1x;
				double vy = v2y - v1y;
				double pvx = px - v1x;
				double pvy = py - v1y;

				*alpha = (vx * pvx + vy * pvy) / (vx * vx + vy * vy);
				if (*alpha >= 0 && *alpha <= 1)
				{
					double dx = v1x + *alpha * vx - px;
					double dy = v1y + *alpha * vy - py;
					*distance = dx * dx + dy * dy;
				}
				else if (*alpha < 0)
					*distance = pvx * pvx + pvy * pvy;
				else
				{
					double dx = px - v2x;
					double dy = py - v2y;
					*distance = dx * dx + dy * dy;
				}

				*distance = sqrt(*distance);
			}

			static double PointToPointDistance(double p1x, double p1y, double p2x, double p2y)
			{
				double dx = p1x - p2x, dy = p1y - p2y;
				return sqrt(dx * dx + dy * dy);
			}
	
		public:

			static double CalculateObjectPotentialForEdge(double px, double py, double v1x, double v1y, double v1r, double v2x, double v2y, double v2r, double constantProbRate, double cutoff)
			{
				double width, distance;
				if (v1x == v2x && v1y == v2y)
				{
					distance = PointToPointDistance(px, py, v1x, v1y);
					width = min(v1r, v2r); // For consistency
				}
				else
				{
					double alpha;
					PointToSegmentDistance(px, py, v1x, v1y, v2x, v2y, &distance, &alpha);
					alpha = min(max(alpha, 0.0), 1.0);
					width = v1r + (v2r - v1r) * alpha;
				}

				return DistanceToObjectPotential(distance, width, constantProbRate, cutoff);
			}
		};
	}
}
