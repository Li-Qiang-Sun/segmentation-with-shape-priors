using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class Shape
    {
        private readonly List<Vector> vertexPositions;

        private readonly List<double> edgeWidths;

        public Shape(ShapeModel model, IEnumerable<Vector> vertexPositions, IEnumerable<double> edgeWidths)
        {
            if (model == null)
                throw new ArgumentNullException("model");
            if (vertexPositions == null)
                throw new ArgumentNullException("vertexPositions");
            if (edgeWidths == null)
                throw new ArgumentNullException("edgeWidths");
            
            this.Model = model;
            this.vertexPositions = new List<Vector>(vertexPositions);
            this.edgeWidths = new List<double>(edgeWidths);

            if (this.vertexPositions.Count != model.VertexCount)
                throw new ArgumentException("Wrong number of vertex positions given.", "vertexPositions");
            if (this.edgeWidths.Count != model.Edges.Count)
                throw new ArgumentException("Wrong number of edge widths given.", "edgeWidths");
        }

        public ShapeModel Model { get; private set; }

        public ReadOnlyCollection<Vector> VertexPositions
        {
            get { return this.vertexPositions.AsReadOnly(); }
        }

        public ReadOnlyCollection<double> EdgeWidths
        {
            get { return this.edgeWidths.AsReadOnly(); }
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
            double minPenalty = Double.PositiveInfinity;
            for (int i = 0; i < this.Model.Edges.Count; ++i)
            {
                ShapeEdge edge = this.Model.Edges[i];
                double penalty = this.Model.CalculateObjectPenaltyForEdge(
                    point, edgeWidths[i], this.vertexPositions[edge.Index1], this.vertexPositions[edge.Index2]);
                minPenalty = Math.Min(minPenalty, penalty);
            }
            return minPenalty;
        }

        public double GetBackgroundPenalty(Vector point)
        {
            double minPenalty = Double.PositiveInfinity;
            for (int i = 0; i < this.Model.Edges.Count; ++i)
            {
                ShapeEdge edge = this.Model.Edges[i];
                double penalty = this.Model.CalculateBackgroundPenaltyForEdge(
                    point, edgeWidths[i], this.vertexPositions[edge.Index1], this.vertexPositions[edge.Index2]);
                minPenalty = Math.Min(minPenalty, penalty);
            }
            return minPenalty;
        }

        public double CalculateEnergy()
        {
            double totalEnergy = 0;

            // Unary energy terms
            for (int i = 0; i < this.Model.Edges.Count; ++i)
            {
                ShapeEdge edge = this.Model.Edges[i];
                totalEnergy += this.Model.CalculateEdgeWidthEnergyTerm(
                    i, this.edgeWidths[i], this.VertexPositions[edge.Index1], this.VertexPositions[edge.Index2]);
            }

            // Pairwise energy terms
            foreach (Tuple<int, int> edgePair in this.Model.ConstrainedEdgePairs)
            {
                ShapeEdge edge1 = this.Model.Edges[edgePair.Item1];
                ShapeEdge edge2 = this.Model.Edges[edgePair.Item2];
                double edgePairEnergy = this.Model.CalculateEdgePairEnergyTerm(
                    edgePair.Item1,
                    edgePair.Item2,
                    this.vertexPositions[edge1.Index2] - this.vertexPositions[edge1.Index1],
                    this.vertexPositions[edge2.Index2] - this.vertexPositions[edge2.Index1]);
                totalEnergy += edgePairEnergy;
            }

            return totalEnergy;
        }
    }
}
