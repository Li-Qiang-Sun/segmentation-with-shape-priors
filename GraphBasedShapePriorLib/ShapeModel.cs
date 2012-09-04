using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior
{
    [DataContract]
    public class ShapeModel
    {
        [DataMember]
        private List<ShapeEdgeParams> edgeParams;

        [DataMember]
        private Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams;

        [DataMember]
        private List<LinkedList<int>> edgeConstraintTree;

        [DataMember]
        private double backgroundDistanceCoeff = 1;

        private ShapeModel()
        {
        }

        public static ShapeModel Create(
            ShapeStructure structure,
            IList<ShapeEdgeParams> edgeParams,
            IDictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams)
        {
            if (structure == null)
                throw new ArgumentNullException("structure");
            if (edgeParams == null)
                throw new ArgumentNullException("edgeParams");
            if (edgePairParams == null)
                throw new ArgumentNullException("edgePairParams");

            if (edgeParams.Count != structure.Edges.Count)
                throw new ArgumentException("Edge params count is not equal to edge count.");

            // Check edge pair constraints
            foreach (Tuple<int, int> edgePair in edgePairParams.Keys)
            {
                if (edgePair.Item1 < 0 || edgePair.Item1 >= structure.Edges.Count || edgePair.Item2 < 0 || edgePair.Item2 >= structure.Edges.Count)
                    throw new ArgumentOutOfRangeException("edgePairParams", "Invalid edge index given.");
                if (edgePair.Item1 == edgePair.Item2)
                    throw new ArgumentException("Edge pair constraint can't be specified for the single edge.", "edgePairParams");
                if (edgePairParams.ContainsKey(new Tuple<int, int>(edgePair.Item2, edgePair.Item1)))
                    throw new ArgumentException("Duplicate pairwise constraints specified for some pair of edges.", "edgePairParams");

                ShapeEdge edge1 = structure.Edges[edgePair.Item1];
                ShapeEdge edge2 = structure.Edges[edgePair.Item2];
                if (edge1.Index1 != edge2.Index1 && edge1.Index2 != edge2.Index1 &&
                    edge1.Index1 != edge2.Index2 && edge1.Index2 != edge2.Index2)
                {
                    throw new ArgumentException("Constrained edge pairs should be connected.", "edgePairParams");
                }
            }

            // Set
            ShapeModel result = new ShapeModel();
            result.Structure = structure;
            result.edgeParams = new List<ShapeEdgeParams>(edgeParams);
            result.edgePairParams = new Dictionary<Tuple<int, int>, ShapeEdgePairParams>(edgePairParams);
            result.BuildEdgeTree();

            return result;
        }

        public static ShapeModel Learn(IEnumerable<Shape> shapes, IEnumerable<Tuple<int, int>> constrainedEdgePairs)
        {
            if (shapes == null)
                throw new ArgumentNullException("shapes");
            if (constrainedEdgePairs == null)
                throw new ArgumentNullException("constrainedEdgePairs");
            int shapeCount = shapes.Count();
            if (shapeCount == 0)
                throw new ArgumentException("Can't learn from no shapes.", "shapes");
            ShapeStructure structure = shapes.First().Structure;
            if (!shapes.All(s => s.Structure == structure))
                throw new ArgumentException("All the shapes should have the same structure.", "shapes");

            // Learn edge params
            List<ShapeEdgeParams> shapeEdgeParams = new List<ShapeEdgeParams>();
            for (int i = 0; i < structure.Edges.Count; ++i)
            {
                double sum = 0, sumSqr = 0;
                foreach (Shape shape in shapes)
                {
                    double edgeLength = shape.GetEdgeVector(i).Length;
                    double ratio = shape.EdgeWidths[i] / edgeLength;
                    sum += ratio;
                    sumSqr += ratio * ratio;
                }

                double meanWidthToLengthRatio = sum / shapeCount;
                double widthToLengthRatioDeviation = Math.Sqrt(sumSqr / shapeCount - meanWidthToLengthRatio * meanWidthToLengthRatio);
                shapeEdgeParams.Add(new ShapeEdgeParams(meanWidthToLengthRatio, widthToLengthRatioDeviation));
            }

            // Learn edge pair params
            Dictionary<Tuple<int, int>, ShapeEdgePairParams> shapeEdgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            foreach (Tuple<int, int> edgePair in constrainedEdgePairs)
            {
                if (edgePair.Item1 < 0 || edgePair.Item1 >= structure.Edges.Count || edgePair.Item2 < 0 || edgePair.Item2 >= structure.Edges.Count)
                    throw new ArgumentOutOfRangeException("constrainedEdgePairs", "Set of constrained edge pairs contains invalid edge index.");
                ShapeEdge edge1 = structure.Edges[edgePair.Item1];
                ShapeEdge edge2 = structure.Edges[edgePair.Item2];
                if (edge1.Index1 != edge2.Index1 && edge1.Index2 != edge2.Index1 &&
                    edge1.Index1 != edge2.Index2 && edge1.Index2 != edge2.Index2)
                {
                    throw new ArgumentException("Constrained edge pairs should be connected.", "constrainedEdgePairs");
                }

                double lengthProdSum = 0, lengthSqrSum = 0;
                double angleSum = 0, angleSumSqr = 0; // TODO: angle estimation is suboptimal when angles are big, fix it!
                foreach (Shape shape in shapes)
                {
                    Vector edge1Vec = shape.GetEdgeVector(edgePair.Item1);
                    Vector edge2Vec = shape.GetEdgeVector(edgePair.Item2);

                    double length1 = edge1Vec.Length;
                    double length2 = edge2Vec.Length;
                    lengthProdSum += length1 * length2;
                    lengthSqrSum += length2 * length2;

                    double angle = Vector.AngleBetween(edge1Vec, edge2Vec);
                    angleSum += angle;
                    angleSumSqr += angle * angle;
                }

                double meanAngle = angleSum / shapeCount;
                double angleDeviation = Math.Sqrt(angleSumSqr / shapeCount - meanAngle * meanAngle);

                double meanLengthRatio = lengthProdSum / lengthSqrSum;
                double lengthDiffSqrSum = 0;
                foreach (Shape shape in shapes)
                {
                    Vector edge1Vec = shape.GetEdgeVector(edgePair.Item1);
                    Vector edge2Vec = shape.GetEdgeVector(edgePair.Item2);

                    double length1 = edge1Vec.Length;
                    double length2 = edge2Vec.Length;
                    double diff = length1 - meanLengthRatio * length2;
                    lengthDiffSqrSum  += diff * diff;
                }

                double lengthDiffDeviation = Math.Sqrt(lengthDiffSqrSum / shapeCount);

                if (shapeEdgePairParams.ContainsKey(edgePair) || shapeEdgePairParams.ContainsKey(new Tuple<int, int>(edgePair.Item2, edgePair.Item1)))
                    throw new ArgumentException("Same pair of edges is constrained more than once.", "constrainedEdgePairs");
                shapeEdgePairParams.Add(edgePair, new ShapeEdgePairParams(meanAngle, meanLengthRatio, angleDeviation, lengthDiffDeviation));
            }

            return Create(structure, shapeEdgeParams, shapeEdgePairParams);
        }

        [DataMember]
        public ShapeStructure Structure { get; private set; }

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

        public double CalculateEdgeWidthEnergyTerm(int edgeIndex, double width, Vector edge1Vector, Vector edge2Vector)
        {
            return CalculateEdgeWidthEnergyTerm(edgeIndex, width, (edge1Vector - edge2Vector).Length);
        }

        public double CalculateEdgeWidthEnergyTerm(int edgeIndex, double width, double edgeLength)
        {
            ShapeEdgeParams @params = this.edgeParams[edgeIndex];
            double diff = width / edgeLength - @params.WidthToEdgeLengthRatio;
            return diff * diff / (2 * @params.WidthToEdgeLengthRatioDeviation * @params.WidthToEdgeLengthRatioDeviation);
        }

        public double CalculateEdgePairEnergyTerm(int edgeIndex1, int edgeIndex2, Vector edge1Vector, Vector edge2Vector)
        {
            Tuple<int, int> pair = new Tuple<int, int>(edgeIndex1, edgeIndex2);
            ShapeEdgePairParams pairParams;
            if (!this.edgePairParams.TryGetValue(pair, out pairParams))
                throw new ArgumentException("Given edge pair has no common pairwise constraints.");

            double lengthDiff = edge1Vector.Length - edge2Vector.Length * pairParams.MeanLengthRatio;
            double lengthTerm = lengthDiff * lengthDiff / (2 * pairParams.LengthDiffDeviation * pairParams.LengthDiffDeviation);
            double angle = Vector.AngleBetween(edge1Vector, edge2Vector);
            double angleDiff1 = Math.Abs(angle - pairParams.MeanAngle);
            double angleDiff2 = Math.Abs(angle - pairParams.MeanAngle + (angle < 0 ? Math.PI * 2 : -Math.PI * 2));
            double angleDiff = Math.Min(angleDiff1, angleDiff2);
            double angleTerm = angleDiff * angleDiff / (2 * pairParams.AngleDeviation * pairParams.AngleDeviation);
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
            double shift = 0.25 * edgeWidth * edgeWidth * (1 + this.backgroundDistanceCoeff);
            return Math.Max(shift - this.backgroundDistanceCoeff * distanceSqr, 0);
        }

        public IEnumerable<int> IterateNeighboringEdgeIndices(int edge)
        {
            return this.edgeConstraintTree[edge];
        }

        public static ShapeModel LoadFromFile(string fileName)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Open))
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(ShapeModel));
                return (ShapeModel)serializer.ReadObject(stream);
            }
        }

        public void SaveToFile(string fileName)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Create))
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(ShapeModel));
                serializer.WriteObject(stream, this);
            }
        }

        public Shape FitMeanShape(int width, int height)
        {
            // Build tree, ignore scale           
            Vector[] vertices = new Vector[this.Structure.VertexCount];
            vertices[this.Structure.Edges[0].Index1] = new Vector(0, 0);
            vertices[this.Structure.Edges[0].Index2] = new Vector(1, 0);
            foreach (int childEdgeIndex in this.IterateNeighboringEdgeIndices(0))
            {
                BuildMeanShapeDfs(vertices, childEdgeIndex, 0, vertices[this.Structure.Edges[0].Index1], vertices[this.Structure.Edges[0].Index2]);
            }

            // Determine axis-aligned bounding box for the generated shapes
            Vector min = new Vector(Double.PositiveInfinity, Double.PositiveInfinity);
            Vector max = new Vector(Double.NegativeInfinity, Double.NegativeInfinity);
            for (int i = 0; i < this.Structure.VertexCount; ++i)
            {
                min.X = Math.Min(min.X, vertices[i].X);
                min.Y = Math.Min(min.Y, vertices[i].Y);
                max.X = Math.Max(max.X, vertices[i].X);
                max.Y = Math.Max(max.Y, vertices[i].Y);
            }

            // Scale & shift vertices, leaving some place near the borders
            double scale = Math.Min(width / (max.X - min.X), height / (max.Y - min.Y));
            for (int i = 0; i < this.Structure.VertexCount; ++i)
            {
                Vector pos = vertices[i];
                pos -= 0.5 * (max + min);
                pos *= scale * 0.8;
                pos += new Vector(width * 0.5, height * 0.5);
                vertices[i] = pos;
            }

            // Generate best possible edge widths
            List<double> edgeWidths = new List<double>();
            for (int i = 0; i < this.edgeParams.Count; ++i)
            {
                ShapeEdge edge = this.Structure.Edges[i];
                double length = (vertices[edge.Index1] - vertices[edge.Index2]).Length;
                edgeWidths.Add(length * this.edgeParams[i].WidthToEdgeLengthRatio);
            }

            return new Shape(this.Structure, vertices, edgeWidths);
        }

        public double CalculateObjectPenalty(Shape shape, Vector point)
        {
            if (shape == null)
                throw new ArgumentNullException("shape");
            if (shape.Structure != this.Structure)
                throw new ArgumentException("Shape and model have different structures.", "shape");

            double minPenalty = Double.PositiveInfinity;
            for (int i = 0; i < this.Structure.Edges.Count; ++i)
            {
                ShapeEdge edge = this.Structure.Edges[i];
                double penalty = this.CalculateObjectPenaltyForEdge(
                    point, shape.EdgeWidths[i], shape.VertexPositions[edge.Index1], shape.VertexPositions[edge.Index2]);
                minPenalty = Math.Min(minPenalty, penalty);
            }
            return minPenalty;
        }

        public double CalculateBackgroundPenalty(Shape shape, Vector point)
        {
            if (shape == null)
                throw new ArgumentNullException("shape");
            if (shape.Structure != this.Structure)
                throw new ArgumentException("Shape and model have different structures.", "shape");

            double maxPenalty = Double.NegativeInfinity;
            for (int i = 0; i < this.Structure.Edges.Count; ++i)
            {
                ShapeEdge edge = this.Structure.Edges[i];
                double penalty = this.CalculateBackgroundPenaltyForEdge(
                    point, shape.EdgeWidths[i], shape.VertexPositions[edge.Index1], shape.VertexPositions[edge.Index2]);
                maxPenalty = Math.Max(maxPenalty, penalty);
            }
            return maxPenalty;
        }

        public ObjectBackgroundTerm CalculatePenalties(Shape shape, Vector point)
        {
            return new ObjectBackgroundTerm(
                this.CalculateObjectPenalty(shape, point), this.CalculateBackgroundPenalty(shape, point));
        }

        public double CalculateEnergy(Shape shape)
        {
            if (shape == null)
                throw new ArgumentNullException("shape");
            if (shape.Structure != this.Structure)
                throw new ArgumentException("Shape and model have different structures.", "shape");

            double totalEnergy = 0;

            // Unary energy terms
            for (int i = 0; i < this.Structure.Edges.Count; ++i)
            {
                ShapeEdge edge = this.Structure.Edges[i];
                totalEnergy += this.CalculateEdgeWidthEnergyTerm(
                    i, shape.EdgeWidths[i], shape.VertexPositions[edge.Index1], shape.VertexPositions[edge.Index2]);
            }

            // Pairwise energy terms
            foreach (Tuple<int, int> edgePair in this.ConstrainedEdgePairs)
            {
                double edgePairEnergy = this.CalculateEdgePairEnergyTerm(
                    edgePair.Item1,
                    edgePair.Item2,
                    shape.GetEdgeVector(edgePair.Item1),
                    shape.GetEdgeVector(edgePair.Item2));
                totalEnergy += edgePairEnergy;
            }

            return totalEnergy;
        }

        private void BuildEdgeTree()
        {
            List<LinkedList<int>> graphStructure = new List<LinkedList<int>>();
            for (int i = 0; i < this.Structure.Edges.Count; ++i)
                graphStructure.Add(new LinkedList<int>());

            foreach (Tuple<int, int> edgePair in this.edgePairParams.Keys)
            {
                graphStructure[edgePair.Item1].AddLast(edgePair.Item2);
                graphStructure[edgePair.Item2].AddLast(edgePair.Item1);
            }

            if (!CheckIfTree(graphStructure))
                throw new InvalidOperationException("Pairwise edge constraint graph should be a fully-connected tree.");

            this.edgeConstraintTree = graphStructure;
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

        private void BuildMeanShapeDfs(Vector[] vertices, int currentEdgeIndex, int parentEdgeIndex, Vector parentEdgePoint1, Vector parentEdgePoint2)
        {
            ShapeEdgePairParams @params = this.GetEdgePairParams(parentEdgeIndex, currentEdgeIndex);
            ShapeEdge currentEdge = this.Structure.Edges[currentEdgeIndex];
            ShapeEdge parentEdge = this.Structure.Edges[parentEdgeIndex];

            // Determine edge direction and length
            Vector parentEdgeVec = parentEdgePoint2 - parentEdgePoint1;
            double length = parentEdgeVec.Length / @params.MeanLengthRatio;
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
            vertices[this.Structure.Edges[currentEdgeIndex].Index1] = edgePoint1;
            vertices[this.Structure.Edges[currentEdgeIndex].Index2] = edgePoint2;

            foreach (int childEdgeIndex in this.IterateNeighboringEdgeIndices(currentEdgeIndex))
            {
                if (childEdgeIndex != parentEdgeIndex)
                    BuildMeanShapeDfs(vertices, childEdgeIndex, currentEdgeIndex, edgePoint1, edgePoint2);
            }
        }
    }
}