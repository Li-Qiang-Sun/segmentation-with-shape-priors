using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Research.GraphBasedShapePrior.Util;

namespace Research.GraphBasedShapePrior.Tests
{
    /// <summary>
    /// Summary description for MathTests
    /// </summary>
    [TestClass]
    public class MathTests
    {
        [TestMethod]
        public void TestRange1()
        {
            const double eps = 1e-10;

            Range range1 = new Range(1, 2);
            Range range2 = new Range(1.5, 2.5);
            Assert.IsTrue(range1.IntersectsWith(range2));
            Assert.IsTrue(range2.IntersectsWith(range1));
            Assert.AreEqual(range1.Length, 1, eps);
            Assert.AreEqual(range2.Length, 1, eps);
        }

        [TestMethod]
        public void TestRange2()
        {
            const double eps = 1e-10;
            
            Range range1 = new Range(1, 2);
            Range range2 = new Range(2.1, 2.5);
            Assert.IsFalse(range1.IntersectsWith(range2));
            Assert.IsFalse(range2.IntersectsWith(range1));
            Assert.AreEqual(range1.Length, 1, eps);
            Assert.AreEqual(range2.Length, 0.4, eps);
        }

        [TestMethod]
        public void TestRange3()
        {
            const double eps = 1e-10;
            
            Range range1 = new Range(1, 2, true);
            Range range2 = new Range(2.1, 2.5, false);
            Assert.IsTrue(range1.IntersectsWith(range2));
            Assert.IsTrue(range2.IntersectsWith(range1));
            Assert.IsTrue(Double.IsPositiveInfinity(range1.Length));
            Assert.AreEqual(range2.Length, 0.4, eps);
        }

        [TestMethod]
        public void TestRange4()
        {
            const double eps = 1e-10;
            
            Range range1 = new Range(1, 2, true);
            Range range2 = new Range(1.1, 1.3, false);
            Assert.IsFalse(range1.IntersectsWith(range2));
            Assert.IsFalse(range2.IntersectsWith(range1));
            Assert.IsTrue(Double.IsPositiveInfinity(range1.Length));
            Assert.AreEqual(range2.Length, 0.2, eps);
        }

        [TestMethod]
        public void TestRange5()
        {
            Range range1 = new Range(1, 2, true);
            Range range2 = new Range(1.1, 1.3, true);
            Assert.IsTrue(range1.IntersectsWith(range2));
            Assert.IsTrue(range2.IntersectsWith(range1));
            Assert.IsTrue(Double.IsPositiveInfinity(range1.Length));
            Assert.IsTrue(Double.IsPositiveInfinity(range2.Length));
        }

        [TestMethod]
        public void TestRange6()
        {
            const double eps = 1e-10;
            
            Range range1 = new Range(1, 1);
            Range range2 = new Range(1, 2);
            Assert.IsTrue(range1.IntersectsWith(range2));
            Assert.IsTrue(range2.IntersectsWith(range1));
            Assert.AreEqual(range1.Length, 0, eps);
            Assert.AreEqual(range2.Length, 1, eps);
        }

        [TestMethod]
        public void TestRange7()
        {
            const double eps = 1e-10;
            
            Range range1 = new Range(1, 2);
            Range range2 = new Range(1.1, 1.1);
            Assert.IsTrue(range1.IntersectsWith(range2));
            Assert.IsTrue(range2.IntersectsWith(range1));
            Assert.AreEqual(range1.Length, 1, eps);
            Assert.AreEqual(range2.Length, 0, eps);
        }

        [TestMethod]
        public void TestRange8()
        {
            Range range1 = new Range(1, 2);
            Range range2 = new Range(1, 2);
            Assert.IsTrue(range1.IntersectsWith(range2));
            Assert.IsTrue(range2.IntersectsWith(range1));
        }

        [TestMethod]
        public void TestLineIntersection()
        {
            const double eps = 1e-10;
            
            Assert.AreEqual(null, MathHelper.LineIntersection(new Vector(0, 0), new Vector(1, 1), new Vector(2, 2), new Vector(-2, -2)));

            Tuple<double, double> intersection1 =
                MathHelper.LineIntersection(new Vector(0, 0), new Vector(1, 0), new Vector(1, -1), new Vector(0, 1));
            Assert.IsNotNull(intersection1);
            Assert.AreEqual(intersection1.Item1, 1, eps);
            Assert.AreEqual(intersection1.Item2, 1, eps);

            Tuple<double, double> intersection2 =
                MathHelper.LineIntersection(new Vector(0, 0), new Vector(1, 1).GetNormalized(), new Vector(2, 0), new Vector(-3, 3));
            Assert.IsNotNull(intersection2);
            Assert.AreEqual(intersection2.Item1, Math.Sqrt(2), eps);
            Assert.AreEqual(intersection2.Item2, 1.0 / 3.0, eps);
        }

        [TestMethod]
        public void TestAngleNormalization()
        {
            const double eps = 1e-10;
            
            Assert.AreEqual(Math.PI * 0.3, MathHelper.NormalizeAngle(Math.PI * 0.3), eps);
            Assert.AreEqual(Math.PI * 0.3, MathHelper.NormalizeAngle(Math.PI * 2.3), eps);
            Assert.AreEqual(-Math.PI * 0.8, MathHelper.NormalizeAngle(Math.PI * 1.2), eps);
            Assert.AreEqual(-Math.PI * 0.5, MathHelper.NormalizeAngle(-Math.PI * 0.5), eps);
            Assert.AreEqual(-Math.PI * 0.5, MathHelper.NormalizeAngle(-Math.PI * 2.5), eps);
            Assert.AreEqual(Math.PI * 0.3, MathHelper.NormalizeAngle(-Math.PI * 3.7), eps);
            Assert.AreEqual(Math.PI * 0.3, MathHelper.NormalizeAngle(-Math.PI * 7.7), eps);
        }

        [TestMethod]
        public void TestInterpolateAngle()
        {
            const double eps = 1e-10;
            
            Assert.AreEqual(0, MathHelper.InterpolateAngle(Math.PI * 0.25, -Math.PI * 0.25, 0.5), eps);
            Assert.AreEqual(0, MathHelper.InterpolateAngle(-Math.PI * 0.25, Math.PI * 0.25, 0.5), eps);
            Assert.AreEqual(Math.PI * 0.25 * 0.75, MathHelper.InterpolateAngle(Math.PI * 0.25, 0, 0.25), eps);
            Assert.AreEqual(Math.PI * 0.25 * 0.75, MathHelper.InterpolateAngle(0, Math.PI * 0.25, 0.75), eps);
            Assert.AreEqual(Math.PI * 0.25, MathHelper.InterpolateAngle(0, Math.PI, 0.25), eps);
            Assert.AreEqual(Math.PI * 0.25, MathHelper.InterpolateAngle(Math.PI, 0, 0.75), eps);
            Assert.AreEqual(-Math.PI * 0.25, MathHelper.InterpolateAngle(0, -Math.PI, 0.25), eps);
            Assert.AreEqual(-Math.PI * 0.25, MathHelper.InterpolateAngle(-Math.PI, 0, 0.75), eps);
            Assert.AreEqual(Math.PI * 0.875, MathHelper.InterpolateAngle(Math.PI * 0.75, -Math.PI * 0.75, 0.25), eps);
            Assert.AreEqual(Math.PI * 0.875, MathHelper.InterpolateAngle(-Math.PI * 0.75, Math.PI * 0.75, 0.75), eps);
            Assert.AreEqual(-Math.PI * 0.875, MathHelper.InterpolateAngle(Math.PI * 0.75, -Math.PI * 0.75, 0.75), eps);
            Assert.AreEqual(-Math.PI * 0.875, MathHelper.InterpolateAngle(-Math.PI * 0.75, Math.PI * 0.75, 0.25), eps);
        }

        [TestMethod]
        public void TestAngleAbsDifference()
        {
            const double eps = 1e-10;

            Assert.AreEqual(0, MathHelper.AngleAbsDifference(0, 0), eps);
            Assert.AreEqual(0, MathHelper.AngleAbsDifference(Math.PI, -Math.PI), eps);
            Assert.AreEqual(Math.PI * 0.5, MathHelper.AngleAbsDifference(Math.PI * 0.75, Math.PI * 0.25), eps);
            Assert.AreEqual(Math.PI * 0.5, MathHelper.AngleAbsDifference(-Math.PI * 0.75, -Math.PI * 0.25), eps);
            Assert.AreEqual(Math.PI * 0.5, MathHelper.AngleAbsDifference(Math.PI * 0.75, -Math.PI * 0.75), eps);
            Assert.AreEqual(Math.PI * 0.5, MathHelper.AngleAbsDifference(-Math.PI * 0.75, Math.PI * 0.75), eps);
        }
    }
}
