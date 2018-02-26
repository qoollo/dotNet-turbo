using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Common
{
    [TestClass]
    public class StringDistanceTest : TestClassBase
    {
        [TestMethod]
        public void HammingStringDistance()
        {
            var dist1 = StringDistance.GetHammingDistance("abcd", "accd");
            Assert.AreEqual(1, dist1);
        }

        [TestMethod]
        public void LevenshteinStringDistance()
        {
            var dist1 = StringDistance.GetLevenshteinDistance("Test string one", "Test string one");
            Assert.AreEqual(0, dist1);

            var dist2 = StringDistance.GetLevenshteinDistance("Test string one", "string one");
            Assert.AreEqual(5, dist2);

            var dist3 = StringDistance.GetLevenshteinDistance("Test string one", "Tesd");
            Assert.AreEqual(12, dist3);

            var dist4 = StringDistance.GetLevenshteinDistance("String 1 test", "Absolutly different val");
            Assert.AreEqual(20, dist4);

            var dist5 = StringDistance.GetLevenshteinDistance("141154342", "141,154,342");
            Assert.AreEqual(2, dist5);

            var dist6 = StringDistance.GetLevenshteinDistance("-141154342", "(141,154,342)");
            Assert.AreEqual(4, dist6);
        }

        [TestMethod]
        public void DamerauLevenshteinStringDistance()
        {
            var dist1 = StringDistance.GetLevenshteinDistance("string", "strnig", true);
            Assert.AreEqual(1, dist1);
        }
    }
}
