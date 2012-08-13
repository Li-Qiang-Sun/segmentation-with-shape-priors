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
            model.ShapeModel = CreateOneEdgeShapeModel();
            model.ImageToSegment = model.ImageToLearnColors = Image2D.LoadFromFile("./images/simple_1.png", scale);
            return model;
        }

        public static Model CreateTwoEdges()
        {
            const double scale = 0.2;
            Model model = new Model();
            model.ObjectRectangle = ScaleRectangle(new Rectangle(249, 22, 391, 495), scale);
            model.ShapeModel = CreateTwoEdgesShapeModel();
            model.ImageToSegment = model.ImageToLearnColors = Image2D.LoadFromFile("./images/simple_3.png", scale);
            return model;
        }

        public static Model CreateLetter1()
        {
            const double scale = 0.22;
            Model model = new Model();
            model.ObjectRectangle = ScaleRectangle(new Rectangle(68, 70, 203, 359), scale);
            model.ShapeModel = CreateLetterShapeModel();
            model.ImageToSegment = model.ImageToLearnColors = Image2D.LoadFromFile("./images/letter_1.jpg", scale);
            return model;
        }

        public static Model CreateLetter2()
        {
            const double scale = 0.24;
            Model model = new Model();
            model.ObjectRectangle = ScaleRectangle(new Rectangle(21, 47, 325, 265), scale);
            model.ShapeModel = CreateLetterShapeModel();
            model.ImageToSegment = model.ImageToLearnColors = Image2D.LoadFromFile("./images/letter_2.png", scale);
            return model;
        }

        public static Model CreateLetter3()
        {
            const double scale = 0.27;
            Model model = new Model();
            model.ObjectRectangle = ScaleRectangle(new Rectangle(30, 71, 289, 176), scale);
            model.ShapeModel = CreateLetterShapeModel();
            model.ImageToSegment = model.ImageToLearnColors = Image2D.LoadFromFile("./images/letter_3.png", scale);
            return model;
        }

        public static Model CreateLetter4()
        {
            const double scale = 0.49;
            Model model = new Model();
            model.ObjectRectangle = ScaleRectangle(new Rectangle(28, 40, 161, 125), scale);
            model.ShapeModel = CreateLetterShapeModel();
            model.ImageToSegment = model.ImageToLearnColors = Image2D.LoadFromFile("./images/letter_4.jpeg", scale);
            return model;
        }

        public static Model CreateLetter5()
        {
            const double scale = 0.24;
            Model model = new Model();
            model.ObjectRectangle = ScaleRectangle(new Rectangle(165, 20, 190, 320), scale);
            model.ShapeModel = CreateLetterShapeModel();
            model.ImageToSegment = model.ImageToLearnColors = Image2D.LoadFromFile("./images/letter_5.png", scale);
            return model;
        }

        public static Model CreateCow1()
        {
            const double scale = 0.17;
            Model model = new Model();
            model.ObjectRectangle = ScaleRectangle(new Rectangle(203, 136, 544, 386), scale);
            model.ShapeModel = CreateCowShapeModel();
            model.ImageToSegment = model.ImageToLearnColors = Image2D.LoadFromFile("./images/cow_1.jpeg", scale);
            return model;
        }

        public static Model CreateCow2()
        {
            const double scale = 0.22;
            Model model = new Model();
            model.ObjectRectangle = ScaleRectangle(new Rectangle(22, 34, 541, 319), scale);
            model.ShapeModel = CreateCowShapeModel();
            model.ImageToSegment = model.ImageToLearnColors = Image2D.LoadFromFile("./images/cow_2.jpeg", scale);
            return model;
        }

        public static Model CreateCow3()
        {
            const double scale = 0.22;
            Model model = new Model();
            model.ObjectRectangle = ScaleRectangle(new Rectangle(37, 10, 365, 267), scale);
            model.ShapeModel = CreateCowShapeModel();
            model.ImageToSegment = model.ImageToLearnColors = Image2D.LoadFromFile("./images/cow_3.jpeg", scale);
            return model;
        }

        public static Model CreateGiraffe1()
        {
            const double scale = 0.22;
            Model model = new Model();
            model.ObjectRectangle = ScaleRectangle(new Rectangle(130, 104, 205, 355), scale);
            model.ShapeModel = CreateGiraffeShapeModel();
            model.ImageToSegment = model.ImageToLearnColors = Image2D.LoadFromFile("./images/giraffe_1.jpg", scale);
            return model;
        }

        public static Model CreateGiraffe2()
        {
            const double scale = 0.19;
            Model model = new Model();
            model.ObjectRectangle = ScaleRectangle(new Rectangle(45, 65, 257, 401), scale);
            model.ShapeModel = CreateGiraffeShapeModel();
            model.ImageToSegment = model.ImageToLearnColors = Image2D.LoadFromFile("./images/giraffe_2.jpg", scale);
            return model;
        }

        public static Model CreateGiraffe3()
        {
            const double scale = 0.33;
            Model model = new Model();
            model.ObjectRectangle = ScaleRectangle(new Rectangle(172, 68, 239, 292), scale);
            model.ShapeModel = CreateGiraffeShapeModel();
            model.ImageToSegment = model.ImageToLearnColors = Image2D.LoadFromFile("./images/giraffe_3.jpg", scale);
            return model;
        }

        public static Model CreateGiraffe4()
        {
            const double scale = 0.2;
            Model model = new Model();
            model.ObjectRectangle = ScaleRectangle(new Rectangle(60, 35, 219, 450), scale);
            model.ShapeModel = CreateGiraffeShapeModel();
            model.ImageToSegment = model.ImageToLearnColors = Image2D.LoadFromFile("./images/giraffe_4.jpg", scale);
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
        
        private static ShapeModel CreateOneEdgeShapeModel()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));

            List<ShapeEdgeParams> edgeParams = new List<ShapeEdgeParams>();
            edgeParams.Add(new ShapeEdgeParams(0.1, 0.1));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();

            return ShapeModel.Create(new ShapeStructure(edges), edgeParams, edgePairParams);
        }

        private static ShapeModel CreateTwoEdgesShapeModel()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));
            edges.Add(new ShapeEdge(1, 2));

            List<ShapeEdgeParams> edgeParams = new List<ShapeEdgeParams>();
            edgeParams.Add(new ShapeEdgeParams(0.1, 0.1));
            edgeParams.Add(new ShapeEdgeParams(0.1, 0.1));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(Math.PI * 0.5, 1, Math.PI * 0.1, 5));

            return ShapeModel.Create(new ShapeStructure(edges), edgeParams, edgePairParams);
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
            edgeParams.Add(new ShapeEdgeParams(0.2, 0.2));
            edgeParams.Add(new ShapeEdgeParams(0.2, 0.2));
            edgeParams.Add(new ShapeEdgeParams(0.2, 0.2));
            edgeParams.Add(new ShapeEdgeParams(0.2, 0.2));
            edgeParams.Add(new ShapeEdgeParams(0.2, 0.2));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams = new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(-Math.PI * 0.5, 1.3, Math.PI * 0.04, 5));
            edgePairParams.Add(new Tuple<int, int>(1, 2), new ShapeEdgePairParams(Math.PI * 0.5, 1, Math.PI * 0.04, 5));
            edgePairParams.Add(new Tuple<int, int>(2, 3), new ShapeEdgePairParams(-Math.PI * 0.5, 1, Math.PI * 0.04, 5));
            edgePairParams.Add(new Tuple<int, int>(3, 4), new ShapeEdgePairParams(Math.PI * 0.5, 0.77, Math.PI * 0.04, 5));

            return ShapeModel.Create(new ShapeStructure(edges), edgeParams, edgePairParams);
        }

        private static ShapeModel CreateCowShapeModel()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));
            edges.Add(new ShapeEdge(0, 2));
            edges.Add(new ShapeEdge(1, 3));
            edges.Add(new ShapeEdge(0, 4));
            edges.Add(new ShapeEdge(4, 5));

            List<ShapeEdgeParams> edgeParams = new List<ShapeEdgeParams>();
            edgeParams.Add(new ShapeEdgeParams(0.65, 0.2));
            edgeParams.Add(new ShapeEdgeParams(0.2, 0.1));
            edgeParams.Add(new ShapeEdgeParams(0.2, 0.1));
            edgeParams.Add(new ShapeEdgeParams(0.8, 0.2));
            edgeParams.Add(new ShapeEdgeParams(0.9, 0.2));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams = new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(Math.PI * 0.5, 1.6, Math.PI * 0.04, 3));
            edgePairParams.Add(new Tuple<int, int>(0, 2), new ShapeEdgePairParams(Math.PI * 0.5, 1.6, Math.PI * 0.04, 3));
            edgePairParams.Add(new Tuple<int, int>(0, 3), new ShapeEdgePairParams(-Math.PI * 0.8, 3, Math.PI * 0.04, 2));
            edgePairParams.Add(new Tuple<int, int>(3, 4), new ShapeEdgePairParams(-Math.PI * 0.4, 1.5, Math.PI * 0.1, 2));

            return ShapeModel.Create(new ShapeStructure(edges), edgeParams, edgePairParams);
        }

        private static ShapeModel CreateGiraffeShapeModel()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));
            edges.Add(new ShapeEdge(0, 2));
            edges.Add(new ShapeEdge(1, 3));
            edges.Add(new ShapeEdge(0, 4));
            edges.Add(new ShapeEdge(4, 5));

            List<ShapeEdgeParams> edgeParams = new List<ShapeEdgeParams>();
            edgeParams.Add(new ShapeEdgeParams(0.65, 0.2));
            edgeParams.Add(new ShapeEdgeParams(0.1, 0.1));
            edgeParams.Add(new ShapeEdgeParams(0.1, 0.1));
            edgeParams.Add(new ShapeEdgeParams(0.1, 0.2));
            edgeParams.Add(new ShapeEdgeParams(0.5, 0.2));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams = new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(Math.PI * 0.5, 0.7, Math.PI * 0.04, 3));
            edgePairParams.Add(new Tuple<int, int>(0, 2), new ShapeEdgePairParams(Math.PI * 0.5, 0.7, Math.PI * 0.04, 3));
            edgePairParams.Add(new Tuple<int, int>(0, 3), new ShapeEdgePairParams(-Math.PI * 0.6, 0.7, Math.PI * 0.04, 4));
            edgePairParams.Add(new Tuple<int, int>(3, 4), new ShapeEdgePairParams(-Math.PI * 0.5, 4, Math.PI * 0.04, 2));

            return ShapeModel.Create(new ShapeStructure(edges), edgeParams, edgePairParams);
        }
    }
}
