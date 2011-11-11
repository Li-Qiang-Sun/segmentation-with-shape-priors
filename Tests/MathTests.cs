using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            Range range1 = new Range(1, 2, false);
            Range range2 = new Range(1.5, 2.5, false);
            Assert.IsTrue(range1.IntersectsWith(range2));
            Assert.IsTrue(range2.IntersectsWith(range1));
        }

        [TestMethod]
        public void TestRange2()
        {
            Range range1 = new Range(1, 2, false);
            Range range2 = new Range(2.1, 2.5, false);
            Assert.IsFalse(range1.IntersectsWith(range2));
            Assert.IsFalse(range2.IntersectsWith(range1));
        }

        [TestMethod]
        public void TestRange3()
        {
            Range range1 = new Range(1, 2, true);
            Range range2 = new Range(2.1, 2.5, false);
            Assert.IsTrue(range1.IntersectsWith(range2));
            Assert.IsTrue(range2.IntersectsWith(range1));
        }

        [TestMethod]
        public void TestRange4()
        {
            Range range1 = new Range(1, 2, true);
            Range range2 = new Range(1.1, 1.3, false);
            Assert.IsFalse(range1.IntersectsWith(range2));
            Assert.IsFalse(range2.IntersectsWith(range1));
        }

        [TestMethod]
        public void TestRange5()
        {
            Range range1 = new Range(1, 2, true);
            Range range2 = new Range(1.1, 1.3, true);
            Assert.IsTrue(range1.IntersectsWith(range2));
            Assert.IsTrue(range2.IntersectsWith(range1));
        }

        [TestMethod]
        public void TestRange6()
        {
            Range range1 = new Range(1, 1, false);
            Range range2 = new Range(1, 2, false);
            Assert.IsTrue(range1.IntersectsWith(range2));
            Assert.IsTrue(range2.IntersectsWith(range1));
        }

        [TestMethod]
        public void TestRange7()
        {
            Range range1 = new Range(1, 2, false);
            Range range2 = new Range(1.1, 1.1, false);
            Assert.IsTrue(range1.IntersectsWith(range2));
            Assert.IsTrue(range2.IntersectsWith(range1));
        }
    }
}
