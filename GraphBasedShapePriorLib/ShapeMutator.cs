using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Research.GraphBasedShapePrior.Util;
using Random = Research.GraphBasedShapePrior.Util.Random;

namespace Research.GraphBasedShapePrior
{
    public class ShapeMutator
    {
        private double vertexMutationProbability;
        private double vertexMutationRelativeDeviation;
        private double edgeMutationRelativeDeviation;
        private int maxMutationCount;

        public ShapeMutator()
        {
            this.vertexMutationProbability = 0.8;
            this.vertexMutationRelativeDeviation = 0.3;
            this.edgeMutationRelativeDeviation = 0.1;
            this.maxMutationCount = 3;
        }
        
        public double VertexMutationProbability
        {
            get { return vertexMutationProbability; }
            set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be in [0, 1] range.");
                vertexMutationProbability = value;
            }
        }
        
        public double VertexMutationRelativeDeviation
        {
            get { return vertexMutationRelativeDeviation; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                vertexMutationRelativeDeviation = value;
            }
        }

        public double EdgeMutationRelativeDeviation
        {
            get { return edgeMutationRelativeDeviation; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                edgeMutationRelativeDeviation = value;
            }
        }

        public int MaxMutationCount
        {
            get { return maxMutationCount; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                maxMutationCount = value;
            }
        }

        public Shape MutateShape(Shape shape, Size imageSize, double relativeTemperature)
        {
            Shape resultShape = shape.Clone();
            int mutations = Random.Int(1, this.MaxMutationCount + 1);
            for (int i = 0; i < mutations; ++i)
                ApplySingleMutation(resultShape, imageSize, relativeTemperature);
            return resultShape;
        }

        private void ApplySingleMutation(Shape shape, Size imageSize, double relativeTemperature)
        {
            if (Random.Double() < this.VertexMutationProbability)
            {
                int randomVertex = Random.Int(shape.VertexPositions.Count);
                double stdDevBase = this.VertexMutationRelativeDeviation * relativeTemperature;
                double stdDevX = stdDevBase * imageSize.Width;
                double stdDevY = stdDevBase * imageSize.Height;
                Vector shift = new Vector(Random.Normal(0, stdDevX), Random.Normal(0, stdDevY));
                shape.VertexPositions[randomVertex] = MathHelper.Trunc(
                    shape.VertexPositions[randomVertex] + shift,
                    Vector.Zero,
                    new Vector(imageSize.Width, imageSize.Height));
            }
            else
            {
                int randomEdge = Random.Int(shape.EdgeWidths.Count);
                double maxImageSideSize = Math.Max(imageSize.Width, imageSize.Height);
                double stdDev = maxImageSideSize * this.EdgeMutationRelativeDeviation * relativeTemperature;
                double shift = Random.Normal(0, stdDev, -shape.EdgeWidths[randomEdge]);
                shape.EdgeWidths[randomEdge] += shift;
            }
        }
    }
}
