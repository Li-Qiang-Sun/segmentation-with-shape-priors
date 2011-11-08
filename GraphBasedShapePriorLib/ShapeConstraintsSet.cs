using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace Research.GraphBasedShapePrior
{
    public class ShapeConstraintsSet
    {
        private List<VertexConstraints> vertexConstraints;

        private Polygon[,] convexHullsForEdges;
        private Polygon[,,,] minRadiusPulleysForEdges;
        private Polygon[,,,] maxRadiusPulleysForEdges;

        public ShapeModel ShapeModel { get; private set; }

        private ShapeConstraintsSet()
        {
        }

        private ShapeConstraintsSet(ShapeConstraintsSet other)
            : this()
        {
            this.vertexConstraints = new List<VertexConstraints>(other.vertexConstraints);
            this.ShapeModel = other.ShapeModel;
        }

        public static ShapeConstraintsSet Create(ShapeModel model, IEnumerable<VertexConstraints> vertexConstraints)
        {
            ShapeConstraintsSet result = new ShapeConstraintsSet();
            result.ShapeModel = model;
            result.vertexConstraints = new List<VertexConstraints>(vertexConstraints);

            if (result.vertexConstraints.Count != result.ShapeModel.VertexCount)
                throw new ArgumentException("Vertex constraint should be given for every vertex (and for every vertex only).", "vertexConstraints");

            return result;
        }

        public static ShapeConstraintsSet CreateFromShape(Shape shape)
        {
            List<VertexConstraints> vertexConstraints = new List<VertexConstraints>();
            foreach (Circle vertex in shape.Vertices)
            {
                Point roundedPos = new Point((int) Math.Round(vertex.Center.X), (int) Math.Round(vertex.Center.Y));
                int roundedRadius = (int) Math.Round(vertex.Radius);
                vertexConstraints.Add(new VertexConstraints(
                    roundedPos, new Point(roundedPos.X + 1, roundedPos.Y + 1), roundedRadius, roundedRadius + 1));
            }

            return Create(shape.Model, vertexConstraints);
        }

        public static ShapeConstraintsSet ConstraintToImage(ShapeModel model, Size imageSize)
        {
            ShapeConstraintsSet result = new ShapeConstraintsSet();
            result.ShapeModel = model;
            result.vertexConstraints = new List<VertexConstraints>();

            // TODO: move this to configuration
            int minRadius = 2; // We don't want singular radii
            int maxRadius = Math.Min(imageSize.Width, imageSize.Height) / 6; // Max circle will constitute to 1/3 of image
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

            bool splitByRadius = radiusViolation * 2 > coordViolation; // We multiply here by 2 because diameter is a better measure for violation scale
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

        public Polygon GetPulleyPointsForVertexPair(int vertex1, int vertex2, int corner1, int corner2, bool maximizeRadius)
        {
            if (this.minRadiusPulleysForEdges == null)
                this.minRadiusPulleysForEdges = new Polygon[this.vertexConstraints.Count, this.vertexConstraints.Count, 4, 4];
            if (this.maxRadiusPulleysForEdges == null)
                this.maxRadiusPulleysForEdges = new Polygon[this.vertexConstraints.Count, this.vertexConstraints.Count, 4, 4];
            Polygon[,,,] storage = maximizeRadius ? this.maxRadiusPulleysForEdges : this.minRadiusPulleysForEdges;
            
            // Pulley points are order-invariant
            if (vertex1 > vertex2)
                Helper.Swap(ref vertex1, ref vertex2);

            // Do some caching
            if (storage[vertex1, vertex2, corner1, corner2] != null)
                return storage[vertex1, vertex2, corner1, corner2];

            // Make pulley parts
            Circle circle1 = new Circle(
                vertexConstraints[vertex1].Corners[corner1],
                maximizeRadius ? vertexConstraints[vertex1].MaxRadiusExclusive - 1 : vertexConstraints[vertex1].MinRadiusInclusive);
            Circle circle2 = new Circle(
                vertexConstraints[vertex2].Corners[corner2],
                maximizeRadius ? vertexConstraints[vertex2].MaxRadiusExclusive - 1 : vertexConstraints[vertex2].MinRadiusInclusive);

            // Is it valid pulley
            if (!circle1.Contains(circle2) && !circle2.Contains(circle1))
            {
                Polygon pulleyPoints = MathHelper.SolvePulleyProblem(circle1, circle2);
                storage[vertex1, vertex2, corner1, corner2] = pulleyPoints;
                return pulleyPoints;    
            }

            return null;
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
            points.AddRange(this.GetConstraintsForVertex(vertex1).Corners);
            points.AddRange(this.GetConstraintsForVertex(vertex2).Corners);
            Polygon convexHull = Polygon.ConvexHull(points);

            // Store result in cache
            this.convexHullsForEdges[vertex1, vertex2] = convexHull;

            return convexHull;
        }

        public void ClearCaches()
        {
            this.convexHullsForEdges = null;
            this.minRadiusPulleysForEdges = null;
            this.maxRadiusPulleysForEdges = null;
        }

        public bool CheckIfSatisfied()
        {
            for (int i = 0; i < vertexConstraints.Count; ++i)
                if (!vertexConstraints[i].CoordSatisfied || !vertexConstraints[i].RadiusSatisfied)
                    return false;
            return true;
        }

        public int GetMaxViolation()
        {
            int maxViolation = 0;
            for (int i = 0; i < vertexConstraints.Count; ++i)
            {
                maxViolation = Math.Max(maxViolation, vertexConstraints[i].CoordViolation);
                maxViolation = Math.Max(maxViolation, vertexConstraints[i].RadiusViolation);
            }

            return maxViolation;
        }

        public int GetViolationSum()
        {
            int sum = 0;
            for (int i = 0; i < vertexConstraints.Count; ++i)
            {
                sum += vertexConstraints[i].CoordViolation;
                sum += vertexConstraints[i].RadiusViolation;
            }

            return sum;
        }

        public VertexConstraints GetConstraintsForVertex(int index)
        {
            return this.vertexConstraints[index];
        }

        public void Draw(Graphics graphics)
        {
            foreach (VertexConstraints vertexConstraint in vertexConstraints)
            {
                PointF center = GetRectangleCenter(vertexConstraint.CoordRectangle);
                graphics.DrawRectangle(Pens.Green, vertexConstraint.CoordRectangle);
                graphics.DrawEllipse(Pens.DeepPink, new RectangleF(
                    center.X - vertexConstraint.MinRadiusInclusive,
                    center.Y - vertexConstraint.MinRadiusInclusive,
                    vertexConstraint.MinRadiusInclusive * 2,
                    vertexConstraint.MinRadiusInclusive * 2));
                graphics.DrawEllipse(Pens.DeepPink, new RectangleF(
                    center.X - vertexConstraint.MaxRadiusExclusive,
                    center.Y - vertexConstraint.MaxRadiusExclusive,
                    vertexConstraint.MaxRadiusExclusive * 2,
                    vertexConstraint.MaxRadiusExclusive * 2));
            }

            foreach (ShapeEdge edge in this.ShapeModel.Edges)
            {
                PointF point1 = GetRectangleCenter(this.vertexConstraints[edge.Index1].CoordRectangle);
                PointF point2 = GetRectangleCenter(this.vertexConstraints[edge.Index2].CoordRectangle);
                graphics.DrawLine(Pens.Orange, point1, point2);
            }
        }

        public ShapeConstraintsSet GuessSolution()
        {
            List<VertexConstraints> collapsedConstraints = new List<VertexConstraints>(this.ShapeModel.VertexCount);
            for (int i = 0; i < this.vertexConstraints.Count; ++i)
                collapsedConstraints.Add(this.vertexConstraints[i].Collapse());
            return Create(this.ShapeModel, collapsedConstraints);
        }

        private static PointF GetRectangleCenter(Rectangle rect)
        {
            return new PointF(rect.Left + rect.Width * 0.5f, rect.Top + rect.Height * 0.5f);
        }
    }
}