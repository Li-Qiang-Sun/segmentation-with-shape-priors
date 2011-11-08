using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using MicrosoftResearch.Infer.Maths;
using Research.GraphBasedShapePrior;
using Vector = Research.GraphBasedShapePrior.Vector;

namespace TestArea
{
    class Program
    {
        static ShapeModel CreateSimpleShapeModel1()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));

            List<ShapeVertexParams> vertexParams = new List<ShapeVertexParams>();
            vertexParams.Add(new ShapeVertexParams(0.3, 0.1));
            vertexParams.Add(new ShapeVertexParams(0.3, 0.1));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            
            return ShapeModel.Create(edges, vertexParams, edgePairParams);
        }

        private static ShapeModel CreateSimpleShapeModel2()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));
            edges.Add(new ShapeEdge(1, 2));

            List<ShapeVertexParams> vertexParams = new List<ShapeVertexParams>();
            vertexParams.Add(new ShapeVertexParams(0.15, 0.1));
            vertexParams.Add(new ShapeVertexParams(0.15, 0.1));
            vertexParams.Add(new ShapeVertexParams(0.15, 0.1));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams =
                new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(Math.PI * 0.5, 1, 0.1, 10)); // TODO: we need deviations to be relative

            return ShapeModel.Create(edges, vertexParams, edgePairParams);
        }

        private static ShapeModel CreateLetterShapeModel()
        {
            List<ShapeEdge> edges = new List<ShapeEdge>();
            edges.Add(new ShapeEdge(0, 1));
            edges.Add(new ShapeEdge(0, 2));
            edges.Add(new ShapeEdge(2, 3));
            edges.Add(new ShapeEdge(2, 4));
            edges.Add(new ShapeEdge(4, 5));

            List<ShapeVertexParams> vertexParams = new List<ShapeVertexParams>();
            vertexParams.Add(new ShapeVertexParams(0.07, 0.05));
            vertexParams.Add(new ShapeVertexParams(0.07, 0.05));
            vertexParams.Add(new ShapeVertexParams(0.07, 0.05));
            vertexParams.Add(new ShapeVertexParams(0.07, 0.05));
            vertexParams.Add(new ShapeVertexParams(0.07, 0.05));
            vertexParams.Add(new ShapeVertexParams(0.07, 0.05));

            Dictionary<Tuple<int, int>, ShapeEdgePairParams> edgePairParams = new Dictionary<Tuple<int, int>, ShapeEdgePairParams>();
            edgePairParams.Add(new Tuple<int, int>(0, 1), new ShapeEdgePairParams(-Math.PI * 0.5, 1.3, Math.PI * 0.1, 5)); // TODO: we need edge length deviations to be relative
            edgePairParams.Add(new Tuple<int, int>(1, 2), new ShapeEdgePairParams(Math.PI * 0.5, 1, Math.PI * 0.1, 5));
            edgePairParams.Add(new Tuple<int, int>(2, 3), new ShapeEdgePairParams(-Math.PI * 0.5, 1, Math.PI * 0.1, 5));
            edgePairParams.Add(new Tuple<int, int>(3, 4), new ShapeEdgePairParams(Math.PI * 0.5, 0.77, Math.PI * 0.1, 5));

            return ShapeModel.Create(edges, vertexParams, edgePairParams);
        }

        static void MainForUnaryPotentialsCheck()
        {
            Image2D<Color> result = new Image2D<Color>(320, 240);

            VertexConstraints constraint1 = new VertexConstraints(new Point(40, 40), new Point(41, 41), 10, 11);
            VertexConstraints constraint2 = new VertexConstraints(new Point(240, 170), new Point(241, 171), 40, 41);
            ShapeConstraintsSet constraintsSet = ShapeConstraintsSet.Create(CreateSimpleShapeModel1(), new []{constraint2, constraint1});

            BranchAndBoundSegmentatorBase segmentator = new BranchAndBoundSegmentatorCpu();
            Image2D<Tuple<double, double>> shapeTerms = new Image2D<Tuple<double, double>>(result.Width, result.Height);
            segmentator.PrepareShapeUnaryPotentials(constraintsSet, shapeTerms);

            for (int x = 0; x < result.Width; ++x)
                for (int y = 0; y < result.Height; ++y)
                {
                    //double diff = potentials.Item1 - potentials.Item2;
                    //int redColor = diff < 0 ? 0 : (int) Math.Min(diff * 200, 255);
                    //int blueColor = diff > 0 ? 0 : (int) Math.Min(-diff * 200, 255);
                    //result[x, y] = Color.FromArgb(redColor, 0, blueColor);
                    int color = (int)(Math.Exp(-shapeTerms[x, y].Item2) * 255.0);
                    result[x, y] = Color.FromArgb(color, color, color);
                }
            Image2D.SaveToFile(result, "../../potentials.png");
        }

        static void MainForSegmentation()
        {
            BranchAndBoundSegmentatorBase segmentator = new BranchAndBoundSegmentatorCpu();
            segmentator.ShapeModel = CreateSimpleShapeModel1();

            DebugConfiguration.VerbosityLevel = VerbosityLevel.Everything;

            const double scale = 0.15;
            Image2D<Color> image = Image2D.LoadFromFile("../../../Images/simple_1.png", scale);
            Rectangle bigLocation = new Rectangle(153, 124, 796, 480);
            Rectangle location = new Rectangle(
                (int)(bigLocation.X * scale),
                (int)(bigLocation.Y * scale),
                (int)(bigLocation.Width * scale),
                (int)(bigLocation.Height * scale));

            Image2D<bool> mask = segmentator.SegmentImage(image, location);
            Image2D.SaveToFile(mask, "../../result.png");
        }

        static void MainForConvexHull()
        {
            List<Vector> points = new List<Vector>();
            points.Add(new Vector(0, 0));
            points.Add(new Vector(1, 2));
            points.Add(new Vector(2, 1));
            points.Add(new Vector(3, 2));
            points.Add(new Vector(4, 2));
            points.Add(new Vector(3, -1));

            Polygon p = Polygon.ConvexHull(points);
            Console.WriteLine(p.IsPointInside(new Vector(2, 1)));
            Console.WriteLine(p.IsPointInside(new Vector(2, -1)));
            Console.WriteLine(p.IsPointInside(new Vector(0, -1)));
            Console.WriteLine(p.IsPointInside(new Vector(-1, 0)));
            Console.WriteLine(p.IsPointInside(new Vector(2, 0)));
            Console.WriteLine(p.IsPointInside(new Vector(3, 1)));
        }

        static void DrawLengthAngleDependence()
        {
            VertexConstraints constraints1 = new VertexConstraints(new Point(20, 20), new Point(50, 70), 1, 10);
            VertexConstraints constraints2 = new VertexConstraints(new Point(20, 20), new Point(40, 50), 1, 10);

            Random random = new Random();
            using (StreamWriter writer = new StreamWriter("./length_angle3.txt"))
            {
                for (int i = 0; i < 20000; ++i)
                {
                    double randomX1 = constraints1.MinCoordInclusive.X + random.NextDouble() * (constraints1.MaxCoordExclusive.X - constraints1.MinCoordInclusive.X);
                    double randomY1 = constraints1.MinCoordInclusive.X + random.NextDouble() * (constraints1.MaxCoordExclusive.X - constraints1.MinCoordInclusive.X);
                    double randomX2 = constraints2.MinCoordInclusive.X + random.NextDouble() * (constraints2.MaxCoordExclusive.X - constraints2.MinCoordInclusive.X);
                    double randomY2 = constraints2.MinCoordInclusive.X + random.NextDouble() * (constraints2.MaxCoordExclusive.X - constraints2.MinCoordInclusive.X);    
                    Vector vector1 = new Vector(randomX1, randomY1);
                    Vector vector2 = new Vector(randomX2, randomY2);
                    double length = (vector1 - vector2).Length;
                    double angle = vector1 == vector2 ? 0 : Vector.AngleBetween(vector1 - vector2, new Vector(1, 0));
                    writer.WriteLine("{0} {1}", length, angle);
                }
            }
        }

        private static void GenerateMeanShape()
        {
            ShapeModel shapeModel = CreateLetterShapeModel();
            Shape shape = shapeModel.BuildMeanShape(new Size(100, 200));
        }

        static void Main()
        {
            Rand.Restart(666);

            GenerateMeanShape();
            //DrawLengthAngleDependence();
            //MainForUnaryPotentialsCheck();
            //MainForSegmentation();
            //MainForConvexHull();
            //MainForShapeEnergyCheck();
        }
    }
}
