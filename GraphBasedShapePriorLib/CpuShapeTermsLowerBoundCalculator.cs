using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior
{
    public class CpuShapeTermsLowerBoundCalculator : IShapeTermsLowerBoundCalculator
    {
        private const int CacheCapacity = 500;

        private ShapeModel shapeModel;

        private Size imageSize;

        private LinkedList<Image2D<ObjectBackgroundTerm>> freeTermImages;

        private LruCache<EdgeDescription, Image2D<ObjectBackgroundTerm>> cachedEdgeTerms;

        public void CalculateShapeTerms(ShapeModel model, ShapeConstraints constraintsSet, Image2D<ObjectBackgroundTerm> result)
        {
            if (model == null)
                throw new ArgumentNullException("model");
            if (constraintsSet == null)
                throw new ArgumentNullException("constraintsSet");
            if (result == null)
                throw new ArgumentNullException("result");
            if (model.Structure != constraintsSet.ShapeStructure)
                throw new ArgumentException("Shape model and shape constraints correspond to different shape structures.");

            if (model != this.shapeModel || result.Rectangle.Size != this.imageSize)
                this.SetTarget(model, result.Rectangle.Size);

            for (int x = 0; x < imageSize.Width; ++x)
                for (int y = 0; y < imageSize.Height; ++y)
                    result[x, y] = new ObjectBackgroundTerm(Double.PositiveInfinity, 0);

            for (int edgeIndex = 0; edgeIndex < this.shapeModel.Structure.Edges.Count; ++edgeIndex)
            {
                ShapeEdge edge = this.shapeModel.Structure.Edges[edgeIndex];
                VertexConstraints vertexConstraints1 = constraintsSet.VertexConstraints[edge.Index1];
                VertexConstraints vertexConstraints2 = constraintsSet.VertexConstraints[edge.Index2];
                EdgeConstraints edgeConstraints = constraintsSet.EdgeConstraints[edgeIndex];

                Image2D<ObjectBackgroundTerm> edgeTerms;
                EdgeDescription edgeDescription = new EdgeDescription(
                    vertexConstraints1, vertexConstraints2, edgeConstraints);
                if (!this.cachedEdgeTerms.TryGetValue(edgeDescription, out edgeTerms))
                {
                    edgeTerms = this.AllocateImage();
                    this.cachedEdgeTerms.Add(edgeDescription, edgeTerms);

                    Polygon convexHull = constraintsSet.GetConvexHullForVertexPair(edge.Index1, edge.Index2);

                    Parallel.For(
                        0,
                        imageSize.Width,
                        x =>
                        {
                            for (int y = 0; y < imageSize.Height; ++y)
                            {
                                Vector pointAsVec = new Vector(x, y);
                                double minDistanceSqr, maxDistanceSqr;
                                MinMaxDistanceForEdge(
                                    pointAsVec,
                                    convexHull,
                                    vertexConstraints1,
                                    vertexConstraints2,
                                    out minDistanceSqr,
                                    out maxDistanceSqr);

                                edgeTerms[x, y] = new ObjectBackgroundTerm(
                                    this.shapeModel.CalculateObjectPenaltyForEdge(
                                        minDistanceSqr, edgeConstraints.MaxWidth),
                                    this.shapeModel.CalculateBackgroundPenaltyForEdge(
                                        maxDistanceSqr, edgeConstraints.MinWidth));
                            }
                        });
                }

                for (int x = 0; x < imageSize.Width; ++x)
                    for (int y = 0; y < imageSize.Height; ++y)
                        result[x, y] = new ObjectBackgroundTerm(
                            Math.Min(result[x, y].ObjectTerm, edgeTerms[x, y].ObjectTerm),
                            Math.Max(result[x, y].BackgroundTerm, edgeTerms[x, y].BackgroundTerm));
            }
        }

        private void SetTarget(ShapeModel newShapeModel, Size newImageSize)
        {
            if (newShapeModel.Structure.Edges.Count > CacheCapacity)
                throw new InvalidOperationException("Edge count is bigger than cache size. Such shape models are not currently supported.");

            this.freeTermImages = new LinkedList<Image2D<ObjectBackgroundTerm>>();
            this.cachedEdgeTerms = new LruCache<EdgeDescription, Image2D<ObjectBackgroundTerm>>(CacheCapacity);
            this.cachedEdgeTerms.CacheItemDiscarded += (sender, args) => this.DeallocateImage(args.DiscardedValue);
            this.shapeModel = newShapeModel;
            this.imageSize = newImageSize;
        }

        private Image2D<ObjectBackgroundTerm> AllocateImage()
        {
            Debug.Assert(shapeModel != null);

            Image2D<ObjectBackgroundTerm> result;
            if (freeTermImages.Count > 0)
            {
                result = freeTermImages.Last.Value;
                freeTermImages.RemoveLast();
            }
            else
                result = new Image2D<ObjectBackgroundTerm>(imageSize.Width, imageSize.Height);

            return result;
        }

        private void DeallocateImage(Image2D<ObjectBackgroundTerm> image)
        {
            freeTermImages.AddLast(image);
        }

        private static void MinMaxDistanceForEdge(
            Vector point,
            Polygon convexHull,
            VertexConstraints constraints1,
            VertexConstraints constraints2,
            out double minDistanceSqr,
            out double maxDistanceSqr)
        {
            if (convexHull.IsPointInside(point))
                minDistanceSqr = 0;
            else
            {
                minDistanceSqr = Double.PositiveInfinity;
                for (int i = 0; i < convexHull.Vertices.Count; ++i)
                {
                    double distanceSqr = point.DistanceToSegmentSquared(
                        convexHull.Vertices[i],
                        convexHull.Vertices[(i + 1) % convexHull.Vertices.Count]);
                    minDistanceSqr = Math.Min(minDistanceSqr, distanceSqr);
                }
            }
            
            maxDistanceSqr = 0;
            foreach (Vector vertex1 in constraints1.Corners)
            {
                foreach (Vector vertex2 in constraints2.Corners)
                {
                    double distanceSqr = point.DistanceToSegmentSquared(vertex1, vertex2);
                    maxDistanceSqr = Math.Max(maxDistanceSqr, distanceSqr);
                }
            }
        }

        private class EdgeDescription
        {
            public VertexConstraints VertexConstraints1 { get; private set; }

            public VertexConstraints VertexConstraints2 { get; private set; }

            public EdgeConstraints EdgeConstraints { get; private set; }

            public EdgeDescription(
                VertexConstraints vertexConstraints1,
                VertexConstraints vertexConstraints2,
                EdgeConstraints edgeConstraints)
            {
                this.VertexConstraints1 = vertexConstraints1;
                this.VertexConstraints2 = vertexConstraints2;
                this.EdgeConstraints = edgeConstraints;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return false;

                EdgeDescription objCasted = (EdgeDescription)obj;
                return
                    objCasted.VertexConstraints1 == this.VertexConstraints1 &&
                    objCasted.VertexConstraints2 == this.VertexConstraints2 &&
                    objCasted.EdgeConstraints == this.EdgeConstraints;
            }

            public override int GetHashCode()
            {
                return
                    this.VertexConstraints1.GetHashCode() ^
                    this.VertexConstraints2.GetHashCode() ^
                    this.EdgeConstraints.GetHashCode();
            }
        }
    }
}
