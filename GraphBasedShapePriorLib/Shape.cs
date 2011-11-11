using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace Research.GraphBasedShapePrior
{
    public class Shape
    {
        private readonly List<Circle> vertices;

        public Shape(ShapeModel model, IEnumerable<Circle> vertices)
        {
            this.Model = model;
            this.vertices = new List<Circle>(vertices);

            if (this.vertices.Count != model.VertexCount)
                throw new ArgumentException("Wrong number of vertices given.", "vertices");
        }

        public ShapeModel Model { get; private set; }

        public ReadOnlyCollection<Circle> Vertices
        {
            get { return this.vertices.AsReadOnly(); }
        }

        public ReadOnlyCollection<ShapeEdge> Edges
        {
            get { return this.Model.Edges; }
        }

        public double GetObjectPenalty(Point point)
        {
            return GetObjectPenalty(new Vector(point.X, point.Y));
        }

        public double GetBackgroundPenalty(Point point)
        {
            return GetBackgroundPenalty(new Vector(point.X, point.Y));
        }

        public double GetObjectPenalty(Vector point)
        {
            return this.Edges.Select(
                edge => this.Model.CalculateObjectPenaltyForEdge(point, this.vertices[edge.Index1], this.vertices[edge.Index2])).Aggregate(Double.PositiveInfinity, Math.Min);
        }

        public double GetBackgroundPenalty(Vector point)
        {
            return this.Edges.Select(
                edge => this.Model.CalculateBackgroundPenaltyForEdge(point, this.vertices[edge.Index1], this.vertices[edge.Index2])).Aggregate(Double.PositiveInfinity, Math.Min);
        }

        public double CalculateEnergy(double bodyLength)
        {
            double totalEnergy = 0;

            for (int i = 0; i < this.Model.VertexCount; ++i)
            {
                double vertexEnergy = this.Model.CalculateVertexEnergyTerm(i, bodyLength, this.vertices[i].Radius);
                totalEnergy += vertexEnergy;
            }

            foreach (Tuple<int, int> edgePair in this.Model.ConstrainedEdgePairs)
            {
                double edgePairEnergy = this.Model.CalculateEdgePairEnergyTerm(
                    edgePair.Item1,
                    edgePair.Item2,
                    this.vertices[this.Model.Edges[edgePair.Item1].Index2].Center - this.vertices[this.Model.Edges[edgePair.Item1].Index1].Center,
                    this.vertices[this.Model.Edges[edgePair.Item2].Index2].Center - this.vertices[this.Model.Edges[edgePair.Item2].Index1].Center);
                totalEnergy += edgePairEnergy;
            }

            return totalEnergy;
        }
    }
}
