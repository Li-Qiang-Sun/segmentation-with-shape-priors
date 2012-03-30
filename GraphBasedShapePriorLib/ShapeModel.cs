using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace Research.GraphBasedShapePrior
{
    public class ShapeModel
    {
        private List<ShapeEdgeParams> edgeParams;

        private Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams;

        private List<ShapeEdge> edges;

        private List<LinkedList<int>> edgeTree;

        private double backgroundDistanceCoeff = 1;

        private ShapeModel()
        {
        }

        public static ShapeModel Create(
            IList<ShapeEdge> edges,
            IList<ShapeEdgeParams> edgeParams,
            IDictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams)
        {
            if (edges == null)
                throw new ArgumentNullException("edges");
            if (edgeParams == null)
                throw new ArgumentNullException("edgeParams");
            if (edgePairParams == null)
                throw new ArgumentNullException("edgePairParams");

            if (edges.Count == 0)
                throw new ArgumentException("Model should contain at least one edge.", "edges");

            if (edgeParams.Count != edges.Count)
                throw new ArgumentException("Edge params count is not equal to edge count.");

            // Vertex count is implicitly specified by the maximum index
            int vertexCount = edges.Max(e => Math.Max(e.Index1, e.Index2)) + 1;

            // Check if all the edge vertex indices are valid
            if (edges.Any(shapeEdge => shapeEdge.Index1 < 0 || shapeEdge.Index2 < 0))
                throw new ArgumentException("Some of the edges have negative vertex indices.", "edges");

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
            result.edgeParams = new List<ShapeEdgeParams>(edgeParams);
            result.edgePairParams = new Dictionary<Tuple<int, int>, ShapeEdgePairParams>(edgePairParams);
            result.VertexCount = vertexCount;
            result.BuildEdgeTree();

            return result;
        }

        public int VertexCount { get; private set; }

        public double BackgroundDistanceCoeff
        {
            get { return this.backgroundDistanceCoeff; }   
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.backgroundDistanceCoeff = value;
            }
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

        public ShapeEdgeParams GetEdgeParams(int edgeIndex)
        {
            return this.edgeParams[edgeIndex];
        }

        public ShapeEdgePairParams GetEdgePairParams(int edgeIndex1, int edgeIndex2)
        {
            bool shouldSwap = false;
            if (edgeIndex1 > edgeIndex2)
            {
                Helper.Swap(ref edgeIndex1, ref edgeIndex2);
                shouldSwap = true;
            }

            Tuple<int, int> key = new Tuple<int, int>(edgeIndex1, edgeIndex2);
            ShapeEdgePairParams @params;
            if (!this.edgePairParams.TryGetValue(key, out @params))
                throw new ArgumentException("Given edges do not have common parameters.");

            return shouldSwap ? @params.Swap() : @params;
        }

        public IEnumerable<int> IterateNeighboringEdgeIndices(int edge)
        {
            return this.edgeTree[edge];
        }

        public double CalculateEdgeWidthEnergyTerm(int edgeIndex, double width, Vector edge1Vector, Vector edge2Vector)
        {
            return CalculateEdgeWidthEnergyTerm(edgeIndex, width, (edge1Vector - edge2Vector).Length);
        }

        public double CalculateEdgeWidthEnergyTerm(int edgeIndex, double width, double edgeLength)
        {
            ShapeEdgeParams @params = this.edgeParams[edgeIndex];
            double diff = edgeLength * @params.WidthToEdgeLengthRatio - width;
            double stddev = @params.RelativeWidthDeviation * edgeLength;
            return diff * diff / (stddev * stddev);
        }

        public double CalculateEdgePairEnergyTerm(int edgeIndex1, int edgeIndex2, Vector edge1Vector, Vector edge2Vector)
        {
            Tuple<int, int> pair = new Tuple<int, int>(edgeIndex1, edgeIndex2);
            ShapeEdgePairParams pairParams;
            if (!this.edgePairParams.TryGetValue(pair, out pairParams))
                throw new ArgumentException("Given edge pair has no common pairwise constraints.");

            double lengthDiff = edge1Vector.Length - edge2Vector.Length * pairParams.LengthRatio;
            double lengthTerm = lengthDiff * lengthDiff / (pairParams.LengthDeviation * pairParams.LengthDeviation);
            double angle = Vector.AngleBetween(edge1Vector, edge2Vector);
            double angleDiff1 = Math.Abs(angle - pairParams.MeanAngle);
            double angleDiff2 = Math.Abs(angle - pairParams.MeanAngle + (angle < 0 ? Math.PI * 2 : -Math.PI * 2));
            double angleDiff = Math.Min(angleDiff1, angleDiff2);
            double angleTerm = angleDiff * angleDiff / (pairParams.AngleDeviation * pairParams.AngleDeviation);
            return lengthTerm + angleTerm;
        }

        public double CalculateObjectPenaltyForEdge(Vector point, double edgeWidth, Vector edgePoint1, Vector edgePoint2)
        {
            double distanceSqr = point.DistanceToSegmentSquared(edgePoint1, edgePoint2);
            return CalculateObjectPenaltyForEdge(distanceSqr, edgeWidth);
        }

        public double CalculateBackgroundPenaltyForEdge(Vector point, double edgeWidth, Vector edgePoint1, Vector edgePoint2)
        {
            double distanceSqr = point.DistanceToSegmentSquared(edgePoint1, edgePoint2);
            return CalculateBackgroundPenaltyForEdge(distanceSqr, edgeWidth);
        }

        public double CalculateObjectPenaltyForEdge(double distanceSqr, double edgeWidth)
        {
            return distanceSqr;
        }

        public double CalculateBackgroundPenaltyForEdge(double distanceSqr, double edgeWidth)
        {
            return Math.Max(
                edgeWidth * edgeWidth * (1 + this.backgroundDistanceCoeff) - this.backgroundDistanceCoeff * distanceSqr,
                0);
        }

        public Shape FitMeanShape(Size imageSize)
        {
            // Build tree, ignore scale           
            Vector[] vertices = new Vector[this.VertexCount];
            vertices[this.edges[0].Index1] = new Vector(0, 0);
            vertices[this.edges[0].Index2] = new Vector(1, 0);
            foreach (int childEdgeIndex in this.IterateNeighboringEdgeIndices(0))
            {
                BuildMeanShapeDfs(vertices, childEdgeIndex, 0, vertices[this.edges[0].Index1], vertices[this.edges[0].Index2]);
            }
            
            // Determine axis-aligned bounding box for the generated shapes
            Vector min = new Vector(Double.PositiveInfinity, Double.PositiveInfinity);
            Vector max = new Vector(Double.NegativeInfinity, Double.NegativeInfinity);
            for (int i = 0; i < this.VertexCount; ++i)
            {
                min.X = Math.Min(min.X, vertices[i].X);
                min.Y = Math.Min(min.Y, vertices[i].Y);
                max.X = Math.Max(max.X, vertices[i].X);
                max.Y = Math.Max(max.Y, vertices[i].Y);
            }
            
            // Scale & shift vertices, leaving some place near the borders
            double scale = Math.Min(imageSize.Width / (max.X - min.X), imageSize.Height / (max.Y - min.Y));
            for (int i = 0; i < this.VertexCount; ++i)
            {
                Vector pos = vertices[i];
                pos -= 0.5 * (max + min);
                pos *= scale * 0.8;
                pos += new Vector(imageSize.Width * 0.5, imageSize.Height * 0.5);
                vertices[i] = pos;
            }

            // Generate best possible edge widths
            List<double> edgeWidths = new List<double>();
            for (int i = 0; i < this.edgeParams.Count; ++i)
            {
                ShapeEdge edge = this.edges[i];
                double length = (vertices[edge.Index1] - vertices[edge.Index2]).Length;
                edgeWidths.Add(length * this.edgeParams[i].WidthToEdgeLengthRatio);
            }

            return new Shape(this, vertices, edgeWidths);
        }

        private void BuildMeanShapeDfs(Vector[] vertices, int currentEdgeIndex, int parentEdgeIndex, Vector parentEdgePoint1, Vector parentEdgePoint2)
        {
            ShapeEdgePairParams @params = this.GetEdgePairParams(parentEdgeIndex, currentEdgeIndex);
            ShapeEdge currentEdge = this.edges[currentEdgeIndex];
            ShapeEdge parentEdge = this.edges[parentEdgeIndex];
            
            // Determine edge direction and length
            Vector parentEdgeVec = parentEdgePoint2 - parentEdgePoint1;
            double length = parentEdgeVec.Length / @params.LengthRatio;
            double angle = Vector.AngleBetween(new Vector(1, 0), parentEdgeVec) + @params.MeanAngle;
            Vector edgeVec = new Vector(Math.Cos(angle), Math.Sin(angle)) * length;
            
            // Choose correct start/end points
            Vector edgePoint1, edgePoint2;
            if (currentEdge.Index1 == parentEdge.Index1)
            {
                edgePoint1 = parentEdgePoint1;
                edgePoint2 = edgePoint1 + edgeVec;
            }
            else if (currentEdge.Index1 == parentEdge.Index2)
            {
                edgePoint1 = parentEdgePoint2;
                edgePoint2 = edgePoint1 + edgeVec;
            }
            else if (currentEdge.Index2 == parentEdge.Index1)
            {
                edgePoint2 = parentEdgePoint1;
                edgePoint1 = edgePoint2 - edgeVec;
            }
            else
            {
                Debug.Assert(currentEdge.Index2 == parentEdge.Index2);
                edgePoint2 = parentEdgePoint2;
                edgePoint1 = edgePoint2 - edgeVec;
            }

            // Setup vertices (some vertex already was placed, but who cares?)
            vertices[this.edges[currentEdgeIndex].Index1] = edgePoint1;
            vertices[this.edges[currentEdgeIndex].Index2] = edgePoint2;

            foreach (int childEdgeIndex in this.IterateNeighboringEdgeIndices(currentEdgeIndex))
            {
                if (childEdgeIndex != parentEdgeIndex)
                    BuildMeanShapeDfs(vertices, childEdgeIndex, currentEdgeIndex, edgePoint1, edgePoint2);
            }
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
                throw new InvalidOperationException("Pairwise edge constraint graph should be a fully-connected tree.");

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