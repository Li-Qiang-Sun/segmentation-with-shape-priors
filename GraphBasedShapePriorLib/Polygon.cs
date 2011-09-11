using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Research.GraphBasedShapePrior
{
    public class Polygon
    {
        private readonly List<Vector> vertices = new List<Vector>();

        private Polygon()
        {
        }

        /// <summary>
        /// Checks if point is inside the polygon. Polygon is assumed to be "simple".
        /// </summary>
        /// <param name="point">Point to check.</param>
        /// <returns>True if point is inside, false otherwise.</returns>
        public bool IsPointInside(Vector point)
        {
            int windingNumber = this.WindingNumber(point);
            return windingNumber == 1 || windingNumber == -1;
        }

        public int WindingNumber(Vector point)
        {
            int winding = 0;
            Vector prev = this.vertices[this.vertices.Count - 1];
            for (int i = 0; i < this.vertices.Count; ++i)
            {
                Vector cur = this.vertices[i];
                if (prev.Y <= point.Y)
                {
                    if (cur.Y > point.Y && Vector.CrossProduct(prev - point, cur - point) > 0)
                        ++winding;
                }
                else
                {
                    if (cur.Y <= point.Y && Vector.CrossProduct(prev - point, cur - point) < 0)
                        --winding;
                }
                prev = cur;
            }

            return winding;
        }

        /// <summary>
        /// Implements gift wrapping algorithm.
        /// </summary>
        /// <param name="points">Set of points.</param>
        /// <returns>Polygon representing convex hull.</returns>
        public static Polygon ConvexHull(IList<Vector> points)
        {
            Debug.Assert(points != null);
            Debug.Assert(points.Count >= 3);
            
            Vector hullStart = points[0];
            for (int i = 1; i < points.Count; ++i)
            {
                Vector point = points[i];
                if (point.X < hullStart.X || point.Y < hullStart.Y)
                    hullStart = point;
            }

            Vector convexHullPoint = hullStart;
            Polygon result = new Polygon();
            do
            {
                result.vertices.Add(convexHullPoint);
                Vector endpoint = points[0];
                for (int i = 1; i < points.Count; ++i)
                {
                    if (endpoint == convexHullPoint ||
                        Vector.CrossProduct(points[i] - convexHullPoint, endpoint - convexHullPoint) < 0)
                    {
                        endpoint = points[i];
                    }
                }
                convexHullPoint = endpoint;
            } while (convexHullPoint != hullStart);

            return result;
        }
    }
}
