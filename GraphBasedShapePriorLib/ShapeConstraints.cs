using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Research.GraphBasedShapePrior
{
    public class ShapeConstraints
    {
        private List<VertexConstraints> vertexConstraints;
        private List<EdgeConstraints> edgeConstraints;

        private ShapeConstraints()
        {
        }

        private ShapeConstraints(ShapeConstraints other)
            : this()
        {
            this.vertexConstraints = new List<VertexConstraints>(other.vertexConstraints);
            this.edgeConstraints = new List<EdgeConstraints>(other.edgeConstraints);
            this.ShapeStructure = other.ShapeStructure;
        }

        public ShapeStructure ShapeStructure { get; private set; }

        public static ShapeConstraints CreateFromConstraints(
            ShapeStructure structure,
            IEnumerable<VertexConstraints> vertexConstraints,
            IEnumerable<EdgeConstraints> edgeConstraints)
        {
            if (structure == null)
                throw new ArgumentNullException("structure");
            if (vertexConstraints == null)
                throw new ArgumentNullException("vertexConstraints");
            if (edgeConstraints == null)
                throw new ArgumentNullException("edgeConstraints");
            
            ShapeConstraints result = new ShapeConstraints();
            result.ShapeStructure = structure;
            result.vertexConstraints = new List<VertexConstraints>(vertexConstraints);
            result.edgeConstraints = new List<EdgeConstraints>(edgeConstraints);

            if (result.vertexConstraints.Count != result.ShapeStructure.VertexCount)
                throw new ArgumentException("Vertex constraint should be given for every vertex (and for every vertex only).", "vertexConstraints");
            if (result.edgeConstraints.Count != result.ShapeStructure.Edges.Count)
                throw new ArgumentException("Edge constraint should be given for every edge (and for every vertex only).", "edgeConstraints");

            return result;
        }

        public static ShapeConstraints CreateFromShape(Shape shape)
        {
            IEnumerable<VertexConstraints> vertexConstraints =
                shape.VertexPositions.Select(vertex => new VertexConstraints(vertex));
            IEnumerable<EdgeConstraints> edgeConstraints =
                shape.EdgeWidths.Select(width => new EdgeConstraints(width));
            return CreateFromConstraints(shape.Structure, vertexConstraints, edgeConstraints);
        }

        public static ShapeConstraints CreateFromBounds(
            ShapeStructure structure,
            Vector coordMin,
            Vector coordMax,
            double minEdgeWidth,
            double maxEdgeWidth)
        {
            ShapeConstraints result = new ShapeConstraints();
            result.ShapeStructure = structure;
            result.vertexConstraints = new List<VertexConstraints>();
            result.edgeConstraints = new List<EdgeConstraints>();

            for (int i = 0; i < structure.VertexCount; ++i)
                result.vertexConstraints.Add(new VertexConstraints(coordMin, coordMax));

            for (int i = 0; i < structure.Edges.Count; ++i)
                result.edgeConstraints.Add(new EdgeConstraints(minEdgeWidth, maxEdgeWidth));

            return result;
        }

        public List<ShapeConstraints> SplitMostFree(double maxCoordFreedom, double maxWidthFreedom)
        {
            Debug.Assert(!this.CheckIfSatisfied(maxCoordFreedom, maxWidthFreedom));

            // Most violated vertex constraint
            int mostFreeVertexConstraint = -1;
            double curMaxCoordFreedom = 0;
            for (int i = 0; i < vertexConstraints.Count; ++i)
            {
                if (vertexConstraints[i].Freedom > maxCoordFreedom &&
                    (mostFreeVertexConstraint == -1 || vertexConstraints[i].Freedom > curMaxCoordFreedom))
                {
                    mostFreeVertexConstraint = i;
                    curMaxCoordFreedom = vertexConstraints[i].Freedom;
                }
            }
            
            // Most violated edge constraint
            int mostFreeEdgeConstraint = -1;
            double curMaxWidthFreedom = 0;
            for (int i = 0; i < edgeConstraints.Count; ++i)
            {
                if (edgeConstraints[i].Freedom > maxWidthFreedom &&
                    (mostFreeEdgeConstraint == -1 || edgeConstraints[i].Freedom > curMaxWidthFreedom))
                {
                    mostFreeEdgeConstraint = i;
                    curMaxWidthFreedom = edgeConstraints[i].Freedom;
                }
            }

            bool splitEdgeConstraint = curMaxWidthFreedom > curMaxCoordFreedom;
            List<ShapeConstraints> result = new List<ShapeConstraints>();
            if (splitEdgeConstraint)
            {
                List<EdgeConstraints> splittedEdgeConstraints =
                    this.edgeConstraints[mostFreeEdgeConstraint].Split();
                for (int i = 0; i < splittedEdgeConstraints.Count; ++i)
                {
                    ShapeConstraints newSet = new ShapeConstraints(this);
                    newSet.edgeConstraints[mostFreeEdgeConstraint] = splittedEdgeConstraints[i];
                    result.Add(newSet);
                }    
            }
            else
            {
                List<VertexConstraints> splittedVertexConstraints =
                    this.vertexConstraints[mostFreeVertexConstraint].Split();
                for (int i = 0; i < splittedVertexConstraints.Count; ++i)
                {
                    ShapeConstraints newSet = new ShapeConstraints(this);
                    newSet.vertexConstraints[mostFreeVertexConstraint] = splittedVertexConstraints[i];
                    result.Add(newSet);
                }    
            }

            return result;
        }

        public Polygon GetConvexHullForVertexPair(int vertex1, int vertex2)
        {
            List<Vector> points = new List<Vector>();
            points.AddRange(this.VertexConstraints[vertex1].Corners);
            points.AddRange(this.VertexConstraints[vertex2].Corners);
            Polygon convexHull = Polygon.ConvexHull(points);

            return convexHull;
        }

        public bool CheckIfSatisfied(double maxCoordFreedom, double maxWidthFreedom)
        {
            for (int i = 0; i < vertexConstraints.Count; ++i)
                if (vertexConstraints[i].Freedom > maxCoordFreedom)
                    return false;
            
            for (int i = 0; i < edgeConstraints.Count; ++i)
                if (edgeConstraints[i].Freedom > maxWidthFreedom)
                    return false;
            
            return true;
        }

        public double GetMaxFreedom()
        {
            double maxViolation = vertexConstraints.Max(c => c.Freedom);
            maxViolation = Math.Max(maxViolation, edgeConstraints.Max(c => c.Freedom));
            return maxViolation;
        }

        public double GetFreedomSum()
        {
            double sum = 0;
            sum += vertexConstraints.Sum(c => c.Freedom);
            sum += edgeConstraints.Sum(c => c.Freedom);
            return sum;
        }

        public ReadOnlyCollection<VertexConstraints> VertexConstraints
        {
            get { return this.vertexConstraints.AsReadOnly(); }
        }

        public ReadOnlyCollection<EdgeConstraints> EdgeConstraints
        {
            get { return this.edgeConstraints.AsReadOnly(); }
        }

        public ShapeConstraints Collapse()
        {
            List<VertexConstraints> collapsedVertexConstraints = this.vertexConstraints.Select(c => c.Collapse()).ToList();
            List<EdgeConstraints> collapsedEdgeConstraints = this.edgeConstraints.Select(c => c.Collapse()).ToList();
            return CreateFromConstraints(this.ShapeStructure, collapsedVertexConstraints, collapsedEdgeConstraints);
        }

        public ShapeConstraints CollapseRandomly()
        {
            List<VertexConstraints> collapsedVertexConstraints = this.vertexConstraints.Select(c => c.CollapseRandomly()).ToList();
            List<EdgeConstraints> collapsedEdgeConstraints = this.edgeConstraints.Select(c => c.CollapseRandomly()).ToList();
            return CreateFromConstraints(this.ShapeStructure, collapsedVertexConstraints, collapsedEdgeConstraints);
        }
    }
}