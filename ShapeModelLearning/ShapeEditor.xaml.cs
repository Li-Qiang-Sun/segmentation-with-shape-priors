using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Research.GraphBasedShapePrior.ShapeModelLearning
{
    /// <summary>
    /// Interaction logic for ShapeEditor.xaml
    /// </summary>
    public partial class ShapeEditor
    {
        private Shape shape;

        private readonly Dictionary<System.Windows.Shapes.Shape, int> vertexShapeToVertexIndex =
            new Dictionary<System.Windows.Shapes.Shape, int>();

        private readonly Dictionary<System.Windows.Shapes.Shape, int> edgeShapeToEdgeIndex =
            new Dictionary<System.Windows.Shapes.Shape, int>();

        private int? controlledVertexIndex;

        private int? controlledEdgeIndex;

        private Vector mouseOffset;

        private double initialEdgeWidth;

        private double initialDistanceFromEdgeLine;

        public ShapeEditor()
        {
            InitializeComponent();
        }

        public Shape Shape
        {
            get { return this.shape; }
            set
            {
                this.shape = value;
                this.ResetShape();
            }
        }

        private void ResetShape()
        {
            this.shapeCanvas.Children.Clear();
            this.vertexShapeToVertexIndex.Clear();
            this.edgeShapeToEdgeIndex.Clear();

            if (this.shape == null)
                return;

            int zIndex = 0;

            for (int i = 0; i < this.shape.Structure.Edges.Count; ++i)
            {
                Rectangle edgeRectangle = (Rectangle)this.FindResource("shapeEdge");
                this.shapeCanvas.Children.Add(edgeRectangle);
                Panel.SetZIndex(edgeRectangle, zIndex++);
                Canvas.SetLeft(edgeRectangle, 0);
                Canvas.SetTop(edgeRectangle, 0);
                this.edgeShapeToEdgeIndex.Add(edgeRectangle, i);
            }

            for (int i = 0; i < this.shape.Structure.VertexCount; ++i)
            {
                Ellipse ellipse = (Ellipse)this.FindResource("shapeVertex");
                this.shapeCanvas.Children.Add(ellipse);
                Panel.SetZIndex(ellipse, zIndex++);
                this.vertexShapeToVertexIndex.Add(ellipse, i);
            }

            this.UpdateShapeControls();
        }

        private void UpdateShapeControls()
        {
            foreach (var shapeVertexIndexPair in this.vertexShapeToVertexIndex)
            {
                Vector vertexPos = this.shape.VertexPositions[shapeVertexIndexPair.Value];
                var ellipse = shapeVertexIndexPair.Key;
                Point ellipsePosition = new Point(vertexPos.X, vertexPos.Y);
                ellipsePosition.Offset(-ellipse.Width * 0.5, -ellipse.Height * 0.5);
                Canvas.SetLeft(ellipse, ellipsePosition.X);
                Canvas.SetTop(ellipse, ellipsePosition.Y);
            }
                
            foreach (var shapeEdgeIndexPair in this.edgeShapeToEdgeIndex)
                shapeEdgeIndexPair.Key.RenderTransform = CalcEdgeTransform(shapeEdgeIndexPair.Value);
        }

        private Transform CalcEdgeTransform(int edgeIndex)
        {
            ShapeEdge edge = this.shape.Structure.Edges[edgeIndex];
            Vector pos1 = this.shape.VertexPositions[edge.Index1];
            Vector pos2 = this.shape.VertexPositions[edge.Index2];
            Vector middlePos = 0.5 * (pos1 + pos2);
            double edgeLength = pos1.DistanceToPoint(pos2);
            double angle = Vector.AngleBetween(Vector.UnitX, pos2 - pos1);

            TransformGroup result = new TransformGroup();
            result.Children.Add(new TranslateTransform(-0.5, -0.5));
            result.Children.Add(new ScaleTransform(edgeLength, this.shape.EdgeWidths[edgeIndex]));
            result.Children.Add(new RotateTransform(MathHelper.ToDegrees(angle)));
            result.Children.Add(new TranslateTransform(middlePos.X, middlePos.Y));

            return result;
        }

        private void OnShapeVertexMouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Shapes.Shape vertexShape = (System.Windows.Shapes.Shape) sender;
            this.controlledVertexIndex = this.vertexShapeToVertexIndex[vertexShape];
            Point relativeMousePos = e.GetPosition(vertexShape);
            this.mouseOffset = new Vector(relativeMousePos.X - vertexShape.Width * 0.5, relativeMousePos.Y - vertexShape.Height * 0.5);
        }

        private void OnShapeEdgeMouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Shapes.Shape edgeShape = (System.Windows.Shapes.Shape)sender;
            this.controlledEdgeIndex = this.edgeShapeToEdgeIndex[edgeShape];
            ShapeEdge edge = this.shape.Structure.Edges[this.controlledEdgeIndex.Value];
            this.initialEdgeWidth = this.shape.EdgeWidths[this.controlledEdgeIndex.Value];

            Point mousePos = e.GetPosition(this.shapeCanvas);
            this.initialDistanceFromEdgeLine = new Vector(mousePos.X, mousePos.Y).DistanceToLine(
                this.shape.VertexPositions[edge.Index1], this.shape.VertexPositions[edge.Index2]);
        }

        private void OnShapeCanvasMouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(this.shapeCanvas);
            
            if (this.controlledVertexIndex.HasValue)
            {
                this.shape.VertexPositions[this.controlledVertexIndex.Value] = new Vector(mousePos.X, mousePos.Y) - this.mouseOffset;
            }
            else if (this.controlledEdgeIndex.HasValue)
            {
                ShapeEdge edge = this.shape.Structure.Edges[this.controlledEdgeIndex.Value];
                double distanceFromEdgeLine = new Vector(mousePos.X, mousePos.Y).DistanceToLine(
                    this.shape.VertexPositions[edge.Index1], this.shape.VertexPositions[edge.Index2]);
                this.shape.EdgeWidths[this.controlledEdgeIndex.Value] =
                    Math.Max(this.initialEdgeWidth + 2 * (distanceFromEdgeLine - this.initialDistanceFromEdgeLine), 5);
            }

            if (this.controlledVertexIndex.HasValue || this.controlledEdgeIndex.HasValue)
                this.UpdateShapeControls();
        }

        private void OnShapeCanvasMouseUp(object sender, MouseButtonEventArgs e)
        {
            this.controlledVertexIndex = null;
            this.controlledEdgeIndex = null;
        }
    }
}
