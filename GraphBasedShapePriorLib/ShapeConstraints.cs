using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace Research.GraphBasedShapePrior
{
    public class ShapeConstraints
    {
        private List<VertexConstraints> vertexConstraints;
        private List<EdgeConstraints> edgeConstraints;

        public ShapeModel ShapeModel { get; private set; }

        private ShapeConstraints()
        {
        }

        private ShapeConstraints(ShapeConstraints other)
            : this()
        {
            this.vertexConstraints = new List<VertexConstraints>(other.vertexConstraints);
            this.edgeConstraints = new List<EdgeConstraints>(other.edgeConstraints);
            this.ShapeModel = other.ShapeModel;
            this.MaxCoordFreedom = other.MaxCoordFreedom;
            this.MaxWidthFreedom = other.MaxWidthFreedom;
        }

        public static ShapeConstraints CreateFromConstraints(
            ShapeModel model,
            IEnumerable<VertexConstraints> vertexConstraints,
            IEnumerable<EdgeConstraints> edgeConstraints,
            double maxCoordFreedom,
            double maxWidthFreedom)
        {
            if (model == null)
                throw new ArgumentNullException("model");
            if (vertexConstraints == null)
                throw new ArgumentNullException("vertexConstraints");
            if (edgeConstraints == null)
                throw new ArgumentNullException("edgeConstraints");
            if (maxCoordFreedom <= 0)
                throw new ArgumentOutOfRangeException("maxCoordFreedom", "Parameter value should be positive");
            if (maxWidthFreedom <= 0)
                throw new ArgumentOutOfRangeException("maxWidthFreedom", "Parameter value should be positive");
            
            ShapeConstraints result = new ShapeConstraints();
            result.ShapeModel = model;
            result.vertexConstraints = new List<VertexConstraints>(vertexConstraints);
            result.edgeConstraints = new List<EdgeConstraints>(edgeConstraints);
            result.MaxCoordFreedom = maxCoordFreedom;
            result.MaxWidthFreedom = maxWidthFreedom;

            if (result.vertexConstraints.Count != result.ShapeModel.VertexCount)
                throw new ArgumentException("Vertex constraint should be given for every vertex (and for every vertex only).", "vertexConstraints");
            if (result.edgeConstraints.Count != result.ShapeModel.Edges.Count)
                throw new ArgumentException("Edge constraint should be given for every edge (and for every vertex only).", "edgeConstraints");

            return result;
        }

        public static ShapeConstraints CreateFromShape(Shape shape)
        {
            IEnumerable<VertexConstraints> vertexConstraints =
                shape.VertexPositions.Select(vertex => new VertexConstraints(vertex));
            IEnumerable<EdgeConstraints> edgeConstraints =
                shape.EdgeWidths.Select(width => new EdgeConstraints(width));
            return CreateFromConstraints(shape.Model, vertexConstraints, edgeConstraints, 1e-6, 1e-6);
        }

        public static ShapeConstraints CreateFromBounds(
            ShapeModel model,
            Vector coordMin,
            Vector coordMax,
            double minEdgeWidth,
            double maxEdgeWidth,
            double maxCoordFreedom,
            double maxWidthFreedom)
        {
            ShapeConstraints result = new ShapeConstraints();
            result.ShapeModel = model;
            result.vertexConstraints = new List<VertexConstraints>();
            result.edgeConstraints = new List<EdgeConstraints>();
            result.MaxCoordFreedom = maxCoordFreedom;
            result.MaxWidthFreedom = maxWidthFreedom;

            for (int i = 0; i < model.VertexCount; ++i)
                result.vertexConstraints.Add(new VertexConstraints(coordMin, coordMax));

            for (int i = 0; i < model.Edges.Count; ++i)
                result.edgeConstraints.Add(new EdgeConstraints(minEdgeWidth, maxEdgeWidth));

            return result;
        }

        public double MaxCoordFreedom { get; private set; }

        public double MaxWidthFreedom { get; private set; }

        public List<ShapeConstraints> SplitMostFree()
        {
            Debug.Assert(!this.CheckIfSatisfied());

            // Most violated vertex constraint
            int mostFreeVertexConstraint = -1;
            double maxVertexFreedom = 0;
            for (int i = 0; i < vertexConstraints.Count; ++i)
            {
                if (vertexConstraints[i].Freedom > this.MaxCoordFreedom &&
                    (mostFreeVertexConstraint == -1 || vertexConstraints[i].Freedom > maxVertexFreedom))
                {
                    mostFreeVertexConstraint = i;
                    maxVertexFreedom = vertexConstraints[i].Freedom;
                }
            }
            
            // Most violated edge constraint
            int mostFreeEdgeConstraint = -1;
            double maxEdgeFreedom = 0;
            for (int i = 0; i < edgeConstraints.Count; ++i)
            {
                if (edgeConstraints[i].Freedom > this.MaxWidthFreedom &&
                    (mostFreeEdgeConstraint == -1 || edgeConstraints[i].Freedom > maxEdgeFreedom))
                {
                    mostFreeEdgeConstraint = i;
                    maxEdgeFreedom = edgeConstraints[i].Freedom;
                }
            }

            bool splitEdgeConstraint = maxEdgeFreedom > maxVertexFreedom;
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

        public bool CheckIfSatisfied()
        {
            for (int i = 0; i < vertexConstraints.Count; ++i)
                if (vertexConstraints[i].Freedom > this.MaxCoordFreedom)
                    return false;
            
            for (int i = 0; i < edgeConstraints.Count; ++i)
                if (edgeConstraints[i].Freedom > this.MaxWidthFreedom)
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

        public void Draw(Graphics graphics)
        {
            foreach (VertexConstraints vertexConstraint in vertexConstraints)
            {
                graphics.DrawRectangle(
                    Pens.Green,
                    (float)vertexConstraint.MinCoord.X,
                    (float)vertexConstraint.MinCoord.Y,
                    (float)(vertexConstraint.MaxCoord.X - vertexConstraint.MinCoord.X),
                    (float)(vertexConstraint.MaxCoord.Y - vertexConstraint.MinCoord.Y));
            }

            for (int i = 0; i < this.ShapeModel.Edges.Count; ++i)
            {
                ShapeEdge edge = this.ShapeModel.Edges[i];
                Vector point1 = this.vertexConstraints[edge.Index1].MiddleCoord;
                Vector point2 = this.vertexConstraints[edge.Index2].MiddleCoord;
                graphics.DrawLine(Pens.Orange, MathHelper.VecToPointF(point1), MathHelper.VecToPointF(point2));

                EdgeConstraints edgeConstraint = this.edgeConstraints[i];
                Vector diff = point2 - point1;
                Vector edgeNormal = (new Vector(diff.Y, -diff.X)).GetNormalized();
                Vector middle = point1 + 0.5 * diff;
                graphics.DrawLine(
                    Pens.Cyan,
                    MathHelper.VecToPointF(middle - edgeNormal * edgeConstraint.MaxWidth * 0.5),
                    MathHelper.VecToPointF(middle + edgeNormal * edgeConstraint.MaxWidth * 0.5));
                graphics.DrawLine(
                    Pens.Red,
                    MathHelper.VecToPointF(middle - edgeNormal * edgeConstraint.MinWidth * 0.5),
                    MathHelper.VecToPointF(middle + edgeNormal * edgeConstraint.MinWidth * 0.5));
            }
        }

        public ShapeConstraints Collapse()
        {
            List<VertexConstraints> collapsedVertexConstraints = this.vertexConstraints.Select(c => c.Collapse()).ToList();
            List<EdgeConstraints> collapsedEdgeConstraints = this.edgeConstraints.Select(c => c.Collapse()).ToList();
            return CreateFromConstraints(this.ShapeModel, collapsedVertexConstraints, collapsedEdgeConstraints, 1e-6, 1e-6);
        }

        public ShapeConstraints CollapseRandomly()
        {
            List<VertexConstraints> collapsedVertexConstraints = this.vertexConstraints.Select(c => c.CollapseRandomly()).ToList();
            List<EdgeConstraints> collapsedEdgeConstraints = this.edgeConstraints.Select(c => c.CollapseRandomly()).ToList();
            return CreateFromConstraints(this.ShapeModel, collapsedVertexConstraints, collapsedEdgeConstraints, 1e-6, 1e-6);
        }
    }
}