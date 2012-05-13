using System;
using System.Drawing;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Research.GraphBasedShapePrior.Tests
{
    [TestClass]
    public class DistanceTransformTests
    {
        [TestMethod]
        public void TestGrid1D()
        {
            const double left = -5, right = 1;
            const int gridSize = 7;

            GeneralizedDistanceTransform1D transform = new GeneralizedDistanceTransform1D(new Range(left, right), gridSize);
            for (int i = -5; i <= 1; ++i)
                Assert.AreEqual(i + 5, transform.CoordToGridIndex(i));
        }

        [TestMethod]
        public void TestGrid2D()
        {
            const double leftX = -5, rightX = 1;
            const double leftY = 1, rightY = 3;
            const int gridSizeX = 7, gridSizeY = 3;

            GeneralizedDistanceTransform2D transform = new GeneralizedDistanceTransform2D(
                new Range(leftX, rightX), new Range(leftY, rightY), new Size(gridSizeX, gridSizeY));
            for (int i = -5; i <= 1; ++i)
                Assert.AreEqual(i + 5, transform.CoordToGridIndexX(i));
            for (int i = 1; i <= 3; ++i)
                Assert.AreEqual(i - 1, transform.CoordToGridIndexY(i));
        }

        [TestMethod]
        public void TestNoIntervals()
        {
            const double left = -1, right = 1;
            const int gridSize = 3;

            GeneralizedDistanceTransform1D transform = new GeneralizedDistanceTransform1D(new Range(left, right), gridSize);
            double[] penalties = { 0, 3, 1.1 };
            transform.Compute(1, (x, r) => penalties[transform.CoordToGridIndex(x)]);

            for (int i = 0; i < 3; ++i) // To test reentrancy
            {
                Assert.AreEqual(0, transform.GetValueByCoord(-1));
                Assert.AreEqual(1, transform.GetValueByCoord(0));
                Assert.AreEqual(1.1, transform.GetValueByCoord(1));

                Assert.AreEqual(0, transform.GetBestIndexByCoord(-1));
                Assert.AreEqual(0, transform.GetBestIndexByCoord(0));
                Assert.AreEqual(2, transform.GetBestIndexByCoord(1));
            }
        }

        [TestMethod]
        public void TestFinitePenaltyIntervals()
        {
            const double left = -3, right = 3;
            const int gridSize = 7;

            GeneralizedDistanceTransform1D transform = new GeneralizedDistanceTransform1D(new Range(left, right), gridSize);
            transform.AddFinitePenaltyRange(new Range(-2, -1));
            transform.AddFinitePenaltyRange(new Range(1, 2));

            double[] penalties = { -2, 4, 3.1, 0, 2.1, -1, -10 };

            for (int i = 0; i < 3; ++i) // To test reentrancy
            {
                transform.Compute(1, (x, r) => penalties[transform.CoordToGridIndex(x)]);

                Assert.AreEqual(5, transform.GetValueByCoord(-3));
                Assert.AreEqual(4, transform.GetValueByCoord(-2));
                Assert.AreEqual(3.1, transform.GetValueByCoord(-1));
                Assert.AreEqual(3, transform.GetValueByCoord(0));
                Assert.AreEqual(0, transform.GetValueByCoord(1));
                Assert.AreEqual(-1, transform.GetValueByCoord(2));
                Assert.AreEqual(0, transform.GetValueByCoord(3));

                Assert.AreEqual(1, transform.GetBestIndexByCoord(-3));
                Assert.AreEqual(1, transform.GetBestIndexByCoord(-2));
                Assert.AreEqual(2, transform.GetBestIndexByCoord(-1));
                Assert.AreEqual(5, transform.GetBestIndexByCoord(0));
                Assert.AreEqual(5, transform.GetBestIndexByCoord(1));
                Assert.AreEqual(5, transform.GetBestIndexByCoord(2));
                Assert.AreEqual(5, transform.GetBestIndexByCoord(3));
            }
        }
    }
}
