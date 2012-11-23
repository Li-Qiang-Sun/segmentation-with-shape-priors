using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using Research.GraphBasedShapePrior.Util;
using Random = Research.GraphBasedShapePrior.Util.Random;

namespace Research.GraphBasedShapePrior
{
    public class ShapeMutator
    {
        private double edgeWidthMutationWeight;
        private double edgeLengthMutationWeight;
        private double edgeAngleMutationWeight;
        private double shapeTranslationWeight;
        private double shapeScaleWeight;

        private double edgeWidthMutationPower;
        private double edgeLengthMutationPower;
        private double edgeAngleMutationPower;
        private double shapeTranslationPower;
        private double shapeScalePower;

        public ShapeMutator()
        {
            this.edgeWidthMutationWeight = 0.2;
            this.edgeLengthMutationWeight = 0.25;
            this.edgeAngleMutationWeight = 0.25;
            this.shapeTranslationWeight = 0.1;
            this.shapeScaleWeight = 0.1;

            this.edgeWidthMutationPower = 0.1;
            this.edgeLengthMutationPower = 0.3;
            this.edgeAngleMutationPower = Math.PI * 0.25;
            this.shapeTranslationPower = 0.1;
            this.shapeScalePower = 0.1;
        }
        
        public double EdgeWidthMutationWeight
        {
            get { return this.edgeWidthMutationWeight; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be non-negative.");
                this.edgeWidthMutationWeight = value;
            }
        }

        public double EdgeLengthMutationWeight
        {
            get { return this.edgeLengthMutationWeight; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be non-negative.");
                this.edgeLengthMutationWeight = value;
            }
        }

        public double EdgeAngleMutationWeight
        {
            get { return this.edgeAngleMutationWeight; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be non-negative.");
                this.edgeAngleMutationWeight = value;
            }
        }

        public double ShapeTranslationWeight
        {
            get { return this.shapeTranslationWeight; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be non-negative.");
                this.shapeTranslationWeight = value;
            }
        }

        public double ShapeScaleWeight
        {
            get { return this.shapeScaleWeight; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be non-negative.");
                this.shapeScaleWeight = value;
            }
        }

        public double EdgeWidthMutationPower
        {
            get { return this.edgeWidthMutationPower; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.edgeWidthMutationPower = value;
            }
        }

        public double EdgeLengthMutationPower
        {
            get { return this.edgeLengthMutationPower; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.edgeLengthMutationPower = value;
            }
        }

        public double EdgeAngleMutationPower
        {
            get { return this.edgeAngleMutationPower; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.edgeAngleMutationPower = value;
            }
        }

        public double ShapeTranslationPower
        {
            get { return this.shapeTranslationPower; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.shapeTranslationPower = value;
            }
        }

        public double ShapeScalePower
        {
            get { return this.shapeScalePower; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", "Value of this property should be positive.");
                this.shapeScalePower = value;
            }
        }

        public Shape MutateShape(Shape shape, ShapeModel shapeModel, Size imageSize, double normalizedTemperature)
        {
            if (shape == null)
                throw new ArgumentNullException("shape");
            if (shapeModel == null)
                throw new ArgumentNullException("shapeModel");

            double maxImageSideSize = Math.Max(imageSize.Width, imageSize.Height);
            Shape mutatedShape;

            double weightSum =
                this.edgeWidthMutationWeight +
                this.edgeLengthMutationWeight +
                this.edgeAngleMutationWeight +
                this.shapeTranslationWeight +
                this.shapeScaleWeight;
            if (weightSum <= 0)
                throw new InvalidOperationException("At least one type of mutation should have non-zero probability weight.");
            double rand = Random.Double(0, weightSum);

            // Shape part mutation
            if (rand < this.edgeWidthMutationWeight + this.edgeLengthMutationWeight + this.edgeAngleMutationWeight)
            {
                ShapeLengthAngleRepresentation representation = shape.GetLengthAngleRepresentation();
                int randomEdge = Random.Int(shape.Structure.Edges.Count);
                
                // Mutate edge width
                if (rand < this.edgeWidthMutationWeight)
                {
                    double widthShiftStdDev = maxImageSideSize * this.edgeWidthMutationPower * normalizedTemperature;
                    const double minWidth = 3;
                    double widthShift = Random.Normal(0, widthShiftStdDev, -shape.EdgeWidths[randomEdge] + minWidth);
                    representation.EdgeWidths[randomEdge] += widthShift;
                }
                // Mutate edge length
                else if (rand < this.edgeWidthMutationWeight + this.edgeLengthMutationWeight)
                {
                    double lengthShiftStdDev = maxImageSideSize * this.edgeLengthMutationPower * normalizedTemperature;
                    double lengthShift = Random.Normal(0, lengthShiftStdDev);
                    representation.EdgeLengths[randomEdge] += lengthShift;
                }
                // Mutate edge angle
                else
                {
                    double angleShiftStdDev = this.edgeAngleMutationPower * normalizedTemperature;
                    double angleShift = Random.Normal(0, angleShiftStdDev);
                    representation.EdgeAngles[randomEdge] += angleShift;
                } 

                mutatedShape = shapeModel.BuildShapeFromLengthAngleRepresentation(representation);
            }
            // Whole shape mutation
            else
            {
                rand -= this.edgeWidthMutationWeight + this.edgeLengthMutationWeight + this.edgeAngleMutationWeight;
                mutatedShape = shape.Clone();

                // Translate shape
                if (rand < this.shapeTranslationWeight)
                {
                    Vector maxTopLeftShift = new Vector(Double.NegativeInfinity, Double.NegativeInfinity);
                    Vector minBottomRightShift = new Vector(Double.PositiveInfinity, Double.PositiveInfinity);
                    for (int i  = 0; i < mutatedShape.VertexPositions.Count; ++i)
                    {
                        maxTopLeftShift.X = Math.Max(maxTopLeftShift.X, -mutatedShape.VertexPositions[i].X);
                        maxTopLeftShift.Y = Math.Max(maxTopLeftShift.Y, -mutatedShape.VertexPositions[i].Y);
                        minBottomRightShift.X = Math.Min(minBottomRightShift.X, imageSize.Width - mutatedShape.VertexPositions[i].X);
                        minBottomRightShift.Y = Math.Min(minBottomRightShift.Y, imageSize.Height - mutatedShape.VertexPositions[i].Y);
                    }

                    double translationStdDev = maxImageSideSize * this.shapeTranslationPower * normalizedTemperature;
                    Vector shift = new Vector(Random.Normal(0, translationStdDev), Random.Normal(0, translationStdDev));
                    shift = MathHelper.Trunc(shift, maxTopLeftShift, minBottomRightShift);

                    for (int i = 0; i < mutatedShape.VertexPositions.Count; ++i)
                        mutatedShape.VertexPositions[i] += shift;
                }
                // Scale shape
                else
                {
                    Vector shapeCenter = shape.VertexPositions.Aggregate(Vector.Zero, (a, c) => a + c) / shape.VertexPositions.Count;
                    double scaleStdDev = this.shapeScalePower * normalizedTemperature;
                    const double minScale = 0.1;
                    double scale = Random.Normal(1.0, scaleStdDev, minScale);
                    for (int i = 0; i < mutatedShape.VertexPositions.Count; ++i)
                        mutatedShape.VertexPositions[i] = shapeCenter + scale * (mutatedShape.VertexPositions[i] - shapeCenter);
                }
            }

            Debug.Assert(mutatedShape != null);
            return mutatedShape;
        }
    }
}
