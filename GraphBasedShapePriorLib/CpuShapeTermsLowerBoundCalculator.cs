using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace Research.GraphBasedShapePrior
{
    public class CpuShapeTermsLowerBoundCalculator : IShapeTermsLowerBoundCalculator
    {
        private const int CacheCapacity = 500;

        private ShapeModel shapeModel;

        private Size imageSize;

        private LinkedList<Image2D<ObjectBackgroundTerm>> freeTermImages;

        private LruCache<EdgeDescription, Image2D<ObjectBackgroundTerm>> cachedEdgeTerms;

        public void CalculateShapeTerms(ShapeConstraints constraintsSet, Image2D<ObjectBackgroundTerm> result)
        {
            if (constraintsSet == null)
                throw new ArgumentNullException("constraintsSet");
            if (result == null)
                throw new ArgumentNullException("result");

            if (constraintsSet.ShapeModel != this.shapeModel || result.Rectangle.Size != this.imageSize)
                this.SetTarget(constraintsSet.ShapeModel, result.Rectangle.Size);

            for (int x = 0; x < imageSize.Width; ++x)
                for (int y = 0; y < imageSize.Height; ++y)
                    result[x, y] = new ObjectBackgroundTerm(Double.PositiveInfinity, 0);

            for (int edgeIndex = 0; edgeIndex < constraintsSet.ShapeModel.Edges.Count; ++edgeIndex)
            {
                ShapeEdge edge = constraintsSet.ShapeModel.Edges[edgeIndex];
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

                    for (int x = 0; x < imageSize.Width; ++x)
                    {
                        for (int y = 0; y < imageSize.Height; ++y)
                        {
                            Vector pointAsVec = new Vector(x, y);
                            double minDistanceSqr, maxDistanceSqr;
                            MinDistanceForEdge(
                                pointAsVec,
                                vertexConstraints1,
                                vertexConstraints2,
                                out minDistanceSqr,
                                out maxDistanceSqr);

                            // If point is inside convex hull than min distance is 0
                            if (convexHull.IsPointInside(pointAsVec))
                                minDistanceSqr = 0;

                            edgeTerms[x, y] = new ObjectBackgroundTerm(
                                constraintsSet.ShapeModel.CalculateObjectPenaltyForEdge(minDistanceSqr, edgeConstraints.MaxWidth),
                                constraintsSet.ShapeModel.CalculateBackgroundPenaltyForEdge(maxDistanceSqr, edgeConstraints.MinWidth));
                        }
                    }
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
            if (newShapeModel.Edges.Count > CacheCapacity)
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

        private static void MinDistanceForEdge(Vector point, VertexConstraints constraints1, VertexConstraints constraints2, out double minDistanceSqr, out double maxDistanceSqr)
        {
            minDistanceSqr = Double.PositiveInfinity;
            maxDistanceSqr = 0;
            foreach (Vector vertex1 in constraints1.Corners)
            {
                foreach (Vector vertex2 in constraints2.Corners)
                {
                    double distanceSqr = point.DistanceToSegmentSquared(vertex1, vertex2);
                    minDistanceSqr = Math.Min(minDistanceSqr, distanceSqr);
                    maxDistanceSqr = Math.Max(maxDistanceSqr, distanceSqr);
                }
            }

            Vector? projection1 = constraints1.GetClosestPoint(point);
            if (projection1.HasValue)
            {
                foreach (Vector vertex2 in constraints2.Corners)
                    minDistanceSqr = Math.Min(minDistanceSqr, point.DistanceToSegmentSquared(projection1.Value, vertex2));
            }

            Vector? projection2 = constraints2.GetClosestPoint(point);
            if (projection2.HasValue)
            {
                foreach (Vector vertex1 in constraints1.Corners)
                    minDistanceSqr = Math.Min(minDistanceSqr, point.DistanceToSegmentSquared(vertex1, projection2.Value));
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
