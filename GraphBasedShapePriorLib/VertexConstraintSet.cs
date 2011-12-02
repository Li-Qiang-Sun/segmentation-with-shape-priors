using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace Research.GraphBasedShapePrior
{
    public class VertexConstraintSet
    {
        private List<VertexConstraint> vertexConstraints;

        private Polygon[,] convexHullsForEdges;
        private Polygon[, , ,] minRadiusPulleysForEdges;
        private Polygon[, , ,] maxRadiusPulleysForEdges;

        public ShapeModel ShapeModel { get; private set; }

        private VertexConstraintSet()
        {
        }

        private VertexConstraintSet(VertexConstraintSet other)
            : this()
        {
            this.vertexConstraints = new List<VertexConstraint>(other.vertexConstraints);
            this.ShapeModel = other.ShapeModel;
        }

        public static VertexConstraintSet CreateFromConstraints(ShapeModel model, IEnumerable<VertexConstraint> vertexConstraints)
        {
            VertexConstraintSet result = new VertexConstraintSet();
            result.ShapeModel = model;
            result.vertexConstraints = new List<VertexConstraint>(vertexConstraints);

            if (result.vertexConstraints.Count != result.ShapeModel.VertexCount)
                throw new ArgumentException("Vertex constraint should be given for every vertex (and for every vertex only).", "vertexConstraints");

            return result;
        }

        public static VertexConstraintSet CreateFromShape(Shape shape)
        {
            IEnumerable<VertexConstraint> vertexConstraints =
                shape.Vertices.Select(vertex => new VertexConstraint(vertex.Center, vertex.Radius));
            return CreateFromConstraints(shape.Model, vertexConstraints);
        }

        public static VertexConstraintSet CreateFromBounds(ShapeModel model, Vector coordMin, Vector coordMax, double radiusMin, double radiusMax)
        {
            VertexConstraintSet result = new VertexConstraintSet();
            result.ShapeModel = model;
            result.vertexConstraints = new List<VertexConstraint>();

            for (int i = 0; i < model.VertexCount; ++i)
                result.vertexConstraints.Add(new VertexConstraint(coordMin, coordMax, radiusMin, radiusMax));

            return result;
        }

        public List<VertexConstraintSet> SplitMostViolated()
        {
            Debug.Assert(!this.CheckIfSatisfied());

            // Build list of constraints that aren't satisfied
            int mostViolatedRadiusConstraint = -1, mostViolatedCoordConstraint = -1;
            double radiusViolation = 0, coordViolation = 0;
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
            List<VertexConstraint> splittedVertexConstraints =
                splitByRadius ? this.vertexConstraints[vertex].SplitByRadius() : this.vertexConstraints[vertex].SplitByCoords();

            List<VertexConstraintSet> result = new List<VertexConstraintSet>();
            for (int i = 0; i < splittedVertexConstraints.Count; ++i)
            {
                VertexConstraintSet newSet = new VertexConstraintSet(this);
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
            Polygon[, , ,] storage = maximizeRadius ? this.maxRadiusPulleysForEdges : this.minRadiusPulleysForEdges;

            // Pulley points are order-invariant
            if (vertex1 > vertex2)
                Helper.Swap(ref vertex1, ref vertex2);

            // Do some caching
            if (storage[vertex1, vertex2, corner1, corner2] != null)
                return storage[vertex1, vertex2, corner1, corner2];

            // Make pulley parts
            Circle circle1 = new Circle(
                vertexConstraints[vertex1].Corners[corner1],
                maximizeRadius ? vertexConstraints[vertex1].MaxRadius : vertexConstraints[vertex1].MinRadius);
            Circle circle2 = new Circle(
                vertexConstraints[vertex2].Corners[corner2],
                maximizeRadius ? vertexConstraints[vertex2].MaxRadius : vertexConstraints[vertex2].MinRadius);

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

        public void DetermineEdgeLimits(
           int edgeIndex,
           out Range lengthRange,
           out Range angleRange)
        {
            ShapeEdge edge = this.ShapeModel.Edges[edgeIndex];
            VertexConstraint constraint1 = this.GetConstraintsForVertex(edge.Index1);
            VertexConstraint constraint2 = this.GetConstraintsForVertex(edge.Index2);

            Range xRange1 = new Range(constraint1.MinCoord.X, constraint1.MaxCoord.X, false);
            Range yRange1 = new Range(constraint1.MinCoord.Y, constraint1.MaxCoord.Y, false);
            Range xRange2 = new Range(constraint2.MinCoord.X, constraint2.MaxCoord.X, false);
            Range yRange2 = new Range(constraint2.MinCoord.Y, constraint2.MaxCoord.Y, false);

            bool xIntersection = xRange1.IntersectsWith(xRange2);
            bool yIntersection = yRange1.IntersectsWith(yRange2);

            double minLength = Double.PositiveInfinity, maxLength = 0;

            if (xIntersection && yIntersection)
            {
                // Special case: intersecting rectangles
                angleRange = new Range(-Math.PI, Math.PI, false);
                minLength = 0;
            }
            else
            {
                // Angle changes from PI to -PI when second constraint is to the left of the first one
                bool angleSignChanges = constraint1.MinCoord.X > constraint2.MaxCoord.X && yIntersection;
                
                double minAngle = angleSignChanges ? -Math.PI : Math.PI;
                double maxAngle = angleSignChanges ? Math.PI : -Math.PI;
                foreach (Vector point1 in constraint1.Corners)
                {
                    foreach (Vector point2 in constraint2.Corners)
                    {
                        double angle = Vector.AngleBetween(new Vector(1, 0), point2 - point1);
                        if (angleSignChanges)
                        {
                            if (angle < 0)
                                minAngle = Math.Max(minAngle, angle);
                            else
                                maxAngle = Math.Min(maxAngle, angle);
                        }
                        else
                        {
                            minAngle = Math.Min(minAngle, angle);
                            maxAngle = Math.Max(maxAngle, angle);    
                        }
                    }
                }                
                angleRange = new Range(minAngle, maxAngle, angleSignChanges);

                // One constraint is on top or on bottom of another
                if (xIntersection)
                {
                    // 1 on top of 2
                    if (constraint1.MinCoord.Y > constraint2.MaxCoord.Y)
                        minLength = Math.Min(minLength, constraint1.MinCoord.Y - constraint2.MaxCoord.Y);
                    // 2 on top of 1
                    else
                        minLength = Math.Min(minLength, constraint2.MinCoord.Y - constraint1.MaxCoord.Y);
                }
                else if (yIntersection)
                {
                    // 1 to the left of 2
                    if (constraint1.MaxCoord.X < constraint2.MinCoord.X)
                        minLength = Math.Min(minLength, constraint2.MinCoord.X - constraint1.MaxCoord.X);
                    // 2 to the left of 1
                    else
                        minLength = Math.Min(minLength, constraint1.MinCoord.X - constraint2.MaxCoord.X);
                }
            }

            foreach (Vector point1 in constraint1.Corners)
            {
                foreach (Vector point2 in constraint2.Corners)
                {
                    double length = (point1 - point2).Length;
                    minLength = Math.Min(minLength, length);
                    maxLength = Math.Max(maxLength, length);
                }
            }

            lengthRange = new Range(minLength, maxLength, false);
        }

        public bool CheckIfSatisfied()
        {
            for (int i = 0; i < vertexConstraints.Count; ++i)
                if (!vertexConstraints[i].CoordSatisfied || !vertexConstraints[i].RadiusSatisfied)
                    return false;
            return true;
        }

        public double GetMaxViolation()
        {
            double maxViolation = 0;
            for (int i = 0; i < vertexConstraints.Count; ++i)
            {
                maxViolation = Math.Max(maxViolation, vertexConstraints[i].CoordViolation);
                maxViolation = Math.Max(maxViolation, vertexConstraints[i].RadiusViolation);
            }

            return maxViolation;
        }

        public double GetViolationSum()
        {
            double sum = 0;
            for (int i = 0; i < vertexConstraints.Count; ++i)
            {
                sum += vertexConstraints[i].CoordViolation;
                sum += vertexConstraints[i].RadiusViolation;
            }

            return sum;
        }

        public VertexConstraint GetConstraintsForVertex(int index)
        {
            return this.vertexConstraints[index];
        }

        public void Draw(Graphics graphics)
        {
            foreach (VertexConstraint vertexConstraint in vertexConstraints)
            {
                Vector center = vertexConstraint.MiddleCoord;
                graphics.DrawRectangle(
                    Pens.Green,
                    (float)vertexConstraint.MinCoord.X,
                    (float)vertexConstraint.MinCoord.Y,
                    (float)(vertexConstraint.MaxCoord.X - vertexConstraint.MinCoord.X),
                    (float)(vertexConstraint.MaxCoord.Y - vertexConstraint.MinCoord.Y));
                graphics.DrawEllipse(Pens.DeepPink, new RectangleF(
                    (float)(center.X - vertexConstraint.MinRadius),
                    (float)(center.Y - vertexConstraint.MinRadius),
                    (float)(vertexConstraint.MinRadius * 2),
                    (float)(vertexConstraint.MinRadius * 2)));
                graphics.DrawEllipse(Pens.DeepPink, new RectangleF(
                    (float)(center.X - vertexConstraint.MaxRadius),
                    (float)(center.Y - vertexConstraint.MaxRadius),
                    (float)(vertexConstraint.MaxRadius * 2),
                    (float)(vertexConstraint.MaxRadius * 2)));
            }

            foreach (ShapeEdge edge in this.ShapeModel.Edges)
            {
                Vector point1 = this.vertexConstraints[edge.Index1].MiddleCoord;
                Vector point2 = this.vertexConstraints[edge.Index2].MiddleCoord;
                graphics.DrawLine(Pens.Orange, new PointF((float)point1.X, (float)point1.Y), new PointF((float)point2.X, (float)point2.Y));
            }
        }

        public VertexConstraintSet GuessSolution()
        {
            List<VertexConstraint> collapsedConstraints = new List<VertexConstraint>(this.ShapeModel.VertexCount);
            for (int i = 0; i < this.vertexConstraints.Count; ++i)
                collapsedConstraints.Add(this.vertexConstraints[i].Collapse());
            return CreateFromConstraints(this.ShapeModel, collapsedConstraints);
        }
    }
}