using Qoollo.Turbo.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Threading
{
    [TestClass]
    public class LinearSpinWaitTest
    {
        [TestMethod]
        public void YieldThresholdCalculateCorrectly()
        {
            int val = LinearSpinWait.CalculateYieldThreshold(LinearSpinWait.DefaultSingleSpinCount);
            Assert.AreEqual(LinearSpinWait.DefaultYieldThreshold, val);
        }

        [TestMethod]
        public void TestYieldThresholdWorkCorrectly()
        {
            LinearSpinWait sw = new LinearSpinWait();

            if (Environment.ProcessorCount > 1)
            {
                for (int i = 0; i < LinearSpinWait.DefaultYieldThreshold; i++)
                {
                    Assert.IsFalse(sw.NextSpinWillYield);
                    sw.SpinOnce();
                }
            }
            Assert.IsTrue(sw.NextSpinWillYield);
        }

        [TestMethod]
        public void TestCustomYieldThresholdWorkCorrectly()
        {
            LinearSpinWait sw = new LinearSpinWait(10, 20);
            if (Environment.ProcessorCount > 1)
            {
                for (int i = 0; i < 20; i++)
                {
                    Assert.IsFalse(sw.NextSpinWillYield);
                    sw.SpinOnce();
                }
            }

            Assert.IsTrue(sw.NextSpinWillYield);
        }


        [TestMethod]
        public void TestLongIteration()
        {
            LinearSpinWait sw = new LinearSpinWait();

            for (int i = 0; i < 2000; i++)
                sw.SpinOnce();
        }

    }
}
