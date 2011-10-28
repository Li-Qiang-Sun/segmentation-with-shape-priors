﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Research.GraphBasedShapePrior
{
    public class ShapeModel
    {
        public double Cutoff { get; private set; }

        private List<ShapeVertexParams> shapeVertexParams;

        private Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams;

        private List<ShapeEdge> edges;

        private List<LinkedList<int>> edgeTree;

        private ShapeModel()
        {
            // TODO: this parameter should probably be made relative to object size somehow
            this.Cutoff = 0.1;
        }

        public static ShapeModel Create(
            IList<ShapeEdge> edges,
            IList<ShapeVertexParams> vertexParams,
            IDictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams)
        {
            Debug.Assert(edges != null);
            Debug.Assert(vertexParams != null);
            Debug.Assert(edgePairParams != null);

            if (edges.Count() == 0)
                throw new ArgumentException("Model should contain at least one edge.", "edges");

            // Check if all the edge vertex indices are valid
            if (edges.Any(
                shapeEdge => shapeEdge.Index1 < 0 ||
                shapeEdge.Index1 >= vertexParams.Count ||
                shapeEdge.Index2 < 0 ||
                shapeEdge.Index2 >= vertexParams.Count))
            {
                throw new ArgumentException("One of the given edges has invalid edge indices.", "edges");
            }

            // Check edge pair constraints
            foreach (Tuple<int, int> edgePair in edgePairParams.Keys)
            {
                if (edgePair.Item1 == edgePair.Item2)
                    throw new ArgumentException("Edge pair constraint can't be specified for the single edge.", "edgePairParams");
                if (edgePairParams.ContainsKey(new Tuple<int, int>(edgePair.Item2, edgePair.Item1)))
                    throw new ArgumentException("Duplicate pairwise constraints specified for some pair of edges.", "edgePairParams");

                ShapeEdge edge1 = edges[edgePair.Item1];
                ShapeEdge edge2 = edges[edgePair.Item2];
                if (edge1.Index1 != edge2.Index1 && edge1.Index2 != edge2.Index1 &&
                    edge1.Index1 != edge2.Index2 && edge1.Index2 != edge2.Index2)
                {
                    throw new ArgumentException("Constrained edge pairs should be connected.", "edgePairParams");
                }
            }

            // Set
            ShapeModel result = new ShapeModel();
            result.edges = new List<ShapeEdge>(edges);
            result.shapeVertexParams = new List<ShapeVertexParams>(vertexParams);
            result.edgePairParams = new Dictionary<Tuple<int, int>, ShapeEdgePairParams>(edgePairParams);
            result.BuildEdgeTree();

            return result;
        }

        public int VertexCount
        {
            get { return this.shapeVertexParams.Count; }
        }

        public ReadOnlyCollection<ShapeEdge> Edges
        {
            get { return this.edges.AsReadOnly(); }
        }

        public IEnumerable<Tuple<int, int>> ConstrainedEdgePairs
        {
            get { return this.edgePairParams.Keys; }
        }

        public int PairwiseEdgeConstraintCount
        {
            get { return this.edgePairParams.Count; }
        }

        public ShapeVertexParams GetVertexParams(int vertexIndex)
        {
            return this.shapeVertexParams[vertexIndex];
        }

        public ShapeEdgePairParams GetEdgeParams(int edgeIndex1, int edgeIndex2)
        {
            bool shouldSwap = false;
            if (edgeIndex1 > edgeIndex2)
            {
                Helper.Swap(ref edgeIndex1, ref edgeIndex2);
                shouldSwap = true;
            }

            Tuple<int, int> key = new Tuple<int, int>(edgeIndex1, edgeIndex2);
            ShapeEdgePairParams edgeParams;
            if (!this.edgePairParams.TryGetValue(key, out edgeParams))
                throw new ArgumentException("Given edges do not have common parameters.");

            return shouldSwap ? edgeParams.Swap() : edgeParams;
        }

        public IEnumerable<int> IterateNeighboringEdgeIndices(int edge)
        {
            return this.edgeTree[edge];
        }

        public double CalculateVertexEnergyTerm(int vertex, double bodyLength, double radius)
        {
            double diff = radius - bodyLength * this.shapeVertexParams[vertex].RadiusToObjectSizeRatio;
            double stddev = bodyLength * this.shapeVertexParams[vertex].RadiusRelativeDeviation;
            return diff * diff / (stddev * stddev);
        }

        public double CalculateEdgePairEnergyTerm(int edge1, int edge2, Vector edge1Vector, Vector edge2Vector)
        {
            Tuple<int, int> pair = new Tuple<int, int>(edge1, edge2);
            ShapeEdgePairParams pairParams;
            if (!this.edgePairParams.TryGetValue(pair, out pairParams))
                throw new ArgumentException("Given edge pair has no common pairwise constraints.");

            double lengthDiff = edge1Vector.Length - edge2Vector.Length * pairParams.LengthRatio;
            double lengthTerm = lengthDiff * lengthDiff / (pairParams.LengthDeviation * pairParams.LengthDeviation);
            double angleDiff = Vector.AngleBetween(edge1Vector, edge2Vector) - pairParams.MeanAngle;
            double angleTerm = angleDiff * angleDiff / (pairParams.AngleDeviation * pairParams.AngleDeviation);
            return lengthTerm + angleTerm;
        }

        public double CalculateDistanceToEdge(Vector point, Circle edgePoint1, Circle edgePoint2, Polygon preCalculatedPulleyPoints = null)
        {
            double distance = point.DistanceToCircleOut(edgePoint1);
            distance = Math.Min(distance, point.DistanceToCircleOut(edgePoint2));

            if (edgePoint1.Contains(edgePoint2) || edgePoint2.Contains(edgePoint1))
                return distance;

            if (preCalculatedPulleyPoints == null)
                preCalculatedPulleyPoints = MathHelper.SolvePulleyProblem(edgePoint1, edgePoint2);
            Debug.Assert(preCalculatedPulleyPoints.Vertices.Count == 4);

            if (preCalculatedPulleyPoints.IsPointInside(point))
                return 0;

            distance = Math.Min(distance, point.DistanceToSegment(preCalculatedPulleyPoints.Vertices[0], preCalculatedPulleyPoints.Vertices[1]));
            distance = Math.Min(distance, point.DistanceToSegment(preCalculatedPulleyPoints.Vertices[2], preCalculatedPulleyPoints.Vertices[3]));
            
            return distance;
        }

        public double CalculateObjectPenaltyForEdge(Vector point, Circle edgePoint1, Circle edgePoint2)
        {
            double distance = CalculateDistanceToEdge(point, edgePoint1, edgePoint2);
            return CalculateObjectPenaltyFromDistance(distance);
        }

        public double CalculateBackgroundPenaltyForEdge(Vector point, Circle edgePoint1, Circle edgePoint2)
        {
            double distance = CalculateDistanceToEdge(point, edgePoint1, edgePoint2);
            return CalculateBackgroundPenaltyFromDistance(distance);
        }

        public double CalculateObjectPenaltyFromDistance(double distance)
        {
            Debug.Assert(distance >= 0);
            return this.Cutoff * MathHelper.Sqr(distance);
        }

        public double CalculateBackgroundPenaltyFromDistance(double distance)
        {
            Debug.Assert(distance >= 0);
            return -MathHelper.LogInf(1 - Math.Exp(-this.Cutoff * MathHelper.Sqr(distance)));
        }

        private void BuildEdgeTree()
        {
            List<LinkedList<int>> graphStructure = new List<LinkedList<int>>();
            for (int i = 0; i < this.edges.Count; ++i)
                graphStructure.Add(new LinkedList<int>());

            foreach (Tuple<int, int> edgePair in this.edgePairParams.Keys)
            {
                graphStructure[edgePair.Item1].AddLast(edgePair.Item2);
                graphStructure[edgePair.Item2].AddLast(edgePair.Item1);
            }

            if (!CheckIfTree(graphStructure))
                throw new InvalidOperationException("Pairwise edge constraint graph should be a tree.");

            this.edgeTree = graphStructure;
        }

        private static bool CheckIfTree(List<LinkedList<int>> graphStructure)
        {
            bool[] visited = new bool[graphStructure.Count];
            bool hasCycles = DetectCyclesDfs(-1, 0, visited, graphStructure);
            if (hasCycles)
                return false;

            return visited.All(x => x);
        }

        private static bool DetectCyclesDfs(int parent, int vertex, bool[] visited, List<LinkedList<int>> graphStructure)
        {
            visited[vertex] = true;

            foreach (int neighbor in graphStructure[vertex])
            {
                if (neighbor == parent)
                    continue;
                if (visited[neighbor] || DetectCyclesDfs(vertex, neighbor, visited, graphStructure))
                    return true;
            }

            return false;
        }
    }
}