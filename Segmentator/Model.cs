using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Research.GraphBasedShapePrior;

namespace Segmentator
{
    public class Model
    {
        public ShapeModel ShapeModel { get; private set; }

        public Rectangle ObjectRectangle { get; private set; }

        public Image2D<Color> ImageToSegment { get; private set; }

        public Image2D<Color> ImageToLearnColors { get; private set; }

        public static Model CreateOneEdge()
        {
            const double scale = 0.2;
            Model model = new Model();
            model.ObjectRectangle = ScaleRectangle(new Rectangle(153, 124, 796, 480), scale);
            model.ShapeModel = CreateSimpleShapeModel1();
            model.ImageToSegment = model.ImageToLearnColors = Image2D.LoadFromFile("./images/simple_1.png", scale);
            return model;
        }

        public static Model CreateTwoEdges()
        {
            const double scale = 0.2;
            Model model = new Model();
            model.ObjectRectangle = ScaleRectangle(new Rectangle(249, 22, 391, 495), scale);
            model.ShapeModel = CreateSimpleShapeModel2();
            model.ImageToSegment = model.ImageToLearnColors = Image2D.LoadFromFile("./images/simple_3.png", scale);
            return model;
        }

        public static Model CreateLetter1()
        {
            const double scale = 0.2;
            Model model = new Model();
            model.ObjectRectangle = ScaleRectangle(new Rectangle(68, 70, 203, 359), scale);
            model.ShapeModel = CreateLetterShapeModel();
            model.ImageToSegment = model.ImageToLearnColors = Image2D.LoadFromFile("./images/letter_1.jpg", scale);
            return model;
        }

        public static Model CreateLetter2()
        {
            const double scale = 0.4;
            Model model = new Model();
            model.ObjectRectangle = ScaleRectangle(new Rectangle(126, 35, 148, 188), scale);
            model.ShapeModel = CreateLetterShapeModel();
            model.ImageToSegment = model.ImageToLearnColors = Image2D.LoadFromFile("./images/letter_2.jpg", scale);
            return model;
        }

        public static Model CreateLetter3()
        {
            const double scale = 1;
            Model model = new Model();
            model.ObjectRectangle = ScaleRectangle(new Rectangle(9, 34, 114, 70), scale);
            model.ShapeModel = CreateLetterShapeModel();
            model.ImageToSegment = Image2D.LoadFromFile("./images/letter_3.png", scale);
            model.ImageToLearnColors = Image2D.LoadFromFile("./images/letter_3_original.png", scale);
            return model;
        }

        private static Rectangle ScaleRectangle(Rectangle rectangle, double scale)
        {
            return new Rectangle(
                (int)(rectangle.X * scale),
                (int)(rectangle.Y * scale),
                (int)(rectangle.Width * scale),
                (int)(rectangle.Height * scale));
        }
        
        private static ShapeModel CreateSimpleShapeModel1()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));

            List<ShapeEdgeParams> edgeParams = new List<ShapeEdgeParams>();
            edgeParams.Add(new ShapeEdgeParams(0.1, 0.1));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();

            return ShapeModel.Create(edges, edgeParams, edgePairParams);
        }

        private static ShapeModel CreateSimpleShapeModel2()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));
            edges.Add(new ShapeEdge(1, 2));

            List<ShapeEdgeParams> edgeParams = new List<ShapeEdgeParams>();
            edgeParams.Add(new ShapeEdgeParams(0.1, 0.1));
            edgeParams.Add(new ShapeEdgeParams(0.1, 0.1));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(Math.PI * 0.5, 1, Math.PI * 0.1, 5)); // TODO: we need edge length deviations to be relative

            return ShapeModel.Create(edges, edgeParams, edgePairParams);
        }

        private static ShapeModel CreateLetterShapeModel()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));
            edges.Add(new ShapeEdge(0, 2));
            edges.Add(new ShapeEdge(2, 3));
            edges.Add(new ShapeEdge(2, 4));
            edges.Add(new ShapeEdge(4, 5));

            List<ShapeEdgeParams> edgeParams = new List<ShapeEdgeParams>();
            edgeParams.Add(new ShapeEdgeParams(0.2, 0.05));
            edgeParams.Add(new ShapeEdgeParams(0.2, 0.05));
            edgeParams.Add(new ShapeEdgeParams(0.2, 0.05));
            edgeParams.Add(new ShapeEdgeParams(0.2, 0.05));
            edgeParams.Add(new ShapeEdgeParams(0.2, 0.05));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams = new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(-Math.PI * 0.5, 1.3, Math.PI * 0.01, 1)); // TODO: we need edge length deviations to be relative
            edgePairParams.Add(new Tuple<int, int>(1, 2), new ShapeEdgePairParams(Math.PI * 0.5, 1, Math.PI * 0.01, 1));
            edgePairParams.Add(new Tuple<int, int>(2, 3), new ShapeEdgePairParams(-Math.PI * 0.5, 1, Math.PI * 0.01, 1));
            edgePairParams.Add(new Tuple<int, int>(3, 4), new ShapeEdgePairParams(Math.PI * 0.5, 0.77, Math.PI * 0.01, 1));

            return ShapeModel.Create(edges, edgeParams, edgePairParams);
        }
    }
}
