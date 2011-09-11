using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace Research.GraphBasedShapePrior
{
    public class ShapeConstraintsSet
    {
        private List<VertexConstraints> vertexConstraints;

        private Polygon[,] convexHullsForEdges;

        private ShapeConstraintsSet()
        {
        }

        public ShapeModel ShapeModel { get; private set; }

        private ShapeConstraintsSet(ShapeConstraintsSet other)
        {
            this.vertexConstraints = new List<VertexConstraints>(other.vertexConstraints);
            this.ShapeModel = other.ShapeModel;
        }

        public static ShapeConstraintsSet Create(ShapeModel model, IEnumerable<VertexConstraints> vertexConstraints)
        {
            ShapeConstraintsSet result = new ShapeConstraintsSet();
            result.ShapeModel = model;

            if (result.vertexConstraints.Count != result.ShapeModel.VertexCount)
                throw new ArgumentException("Vertex constraint should be given for every vertex (and for every vertex only).", "vertexConstraints");

            return result;
        }

        public static ShapeConstraintsSet ConstraintToImage(ShapeModel model, Size imageSize)
        {
            ShapeConstraintsSet result = new ShapeConstraintsSet();
            result.ShapeModel = model;
            result.vertexConstraints = new List<VertexConstraints>();

            int minRadius = 1; // We don't want singular radii
            int maxRadius = Math.Min(imageSize.Width, imageSize.Height) / 2;
            for (int i = 0; i < model.VertexCount; ++i)
            {
                result.vertexConstraints.Add(
                    new VertexConstraints(Point.Empty, new Point(imageSize), minRadius, maxRadius));
            }

            return result;
        }

        public List<ShapeConstraintsSet> SplitMostViolated()
        {
            Debug.Assert(!this.CheckIfSatisfied());

            // Build list of constraints that aren't satisfied
            int mostViolatedRadiusConstraint = -1, mostViolatedCoordConstraint = -1;
            int radiusViolation = 0, coordViolation = 0;
            for (int i = 0; i < vertexConstraints.Count; ++i)
            {
                if (!vertexConstraints[i].RadiusSatisfied &&
                    (mostViolatedRadiusConstraint == -1 || vertexConstraints[i].RadiusViolation > radiusViolation))
                {
                    mostViolatedRadiusConstraint = i;
                    radiusViolation = vertexConstraints[i].RadiusViolation;
                }
                if (!vertexConstraints[i].CoordSatisfied &&
                    (mostViolatedCoordConstraint == -1 || vertexConstraints[i].CoordViolation > coordViolation))
                {
                    mostViolatedCoordConstraint = i;
                    coordViolation = vertexConstraints[i].CoordViolation;
                }
            }

            bool splitByRadius = radiusViolation > coordViolation;
            int vertex = splitByRadius ? mostViolatedRadiusConstraint : mostViolatedCoordConstraint;
            List<VertexConstraints> splittedVertexConstraints =
                splitByRadius ? this.vertexConstraints[vertex].SplitByRadius() : this.vertexConstraints[vertex].SplitByCoords();

            List<ShapeConstraintsSet> result = new List<ShapeConstraintsSet>();
            for (int i = 0; i < splittedVertexConstraints.Count; ++i)
            {
                ShapeConstraintsSet newSet = new ShapeConstraintsSet(this);
                newSet.vertexConstraints[vertex] = splittedVertexConstraints[i];
                result.Add(newSet);
            }

            return result;
        }

        public Polygon GetConvexHullForVertexPair(int vertex1, int vertex2)
        {
            // Convex hull is order-invariant
            if (vertex1 > vertex2)
                Helper.Swap(ref vertex1, ref vertex2);
            
            // Do some caching
            if (this.convexHullsForEdges == null)
                this.convexHullsForEdges = new Polygon[this.vertexConstraints.Count, this.vertexConstraints.Count]; 
            if (this.convexHullsForEdges[vertex1, vertex2] != null)
                return this.convexHullsForEdges[vertex1, vertex2];
            
            // Calculate convex hull
            List<Vector> points = new List<Vector>();
            points.AddRange(this.GetConstraintsForVertex(vertex1).IterateCorners());
            points.AddRange(this.GetConstraintsForVertex(vertex2).IterateCorners());
            Polygon convexHull = Polygon.ConvexHull(points);

            // Store result in cache
            this.convexHullsForEdges[vertex1, vertex2] = convexHull;

            return convexHull;
        }

        public void ClearConvexHullCache()
        {
            this.convexHullsForEdges = null;
        }

        public bool CheckIfSatisfied()
        {
            for (int i = 0; i < vertexConstraints.Count; ++i)
                if (!vertexConstraints[i].CoordSatisfied || !vertexConstraints[i].RadiusSatisfied)
                    return false;
            return true;
        }

        public VertexConstraints GetConstraintsForVertex(int index)
        {
            return this.vertexConstraints[index];
        }
    }
}