using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Common
{
    [TestClass]
    public class ConcurrentDictionaryExtensionsTests : TestClassBase
    {
#pragma warning disable CS0618 // Type or member is obsolete
        [TestMethod]
        public void EstimateCountCalculates()
        {
            var dict = new ConcurrentDictionary<int, int>();

            int estimateCount = dict.GetEstimateCount();
            int realCount = dict.Count;

            Assert.AreEqual(realCount, estimateCount);

            for (int i = 0; i < 10; i++)
                dict.TryAdd(i, i);

            Assert.AreEqual(dict.Count, dict.GetEstimateCount());
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
