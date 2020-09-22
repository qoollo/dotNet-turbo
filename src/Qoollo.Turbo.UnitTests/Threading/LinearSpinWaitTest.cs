using Qoollo.Turbo.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace Qoollo.Turbo.UnitTests.Threading
{
    [TestClass]
    public class LinearSpinWaitTest : TestClassBase
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


        [TestMethod]
        public void TestThreadYieldPeriod()
        {
            const int yieldTimes = 4000;

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < yieldTimes; i++)
            {
                if (i % 2 == 0)
                    Thread.Yield();
                else
                    Thread.SpinWait(8);
            }

            sw.Stop();
            TestContext.WriteLine($"Yield time: {sw.ElapsedMilliseconds}ms. For {yieldTimes} yields");
        }

        [TestMethod]
        public void TestThreadSleep0Period()
        {
            const int sleepTimes = 4000;

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < sleepTimes; i++)
            {
                if (i % 2 == 0)
                    Thread.Sleep(0);
                else
                    Thread.SpinWait(8);
            }

            sw.Stop();
            TestContext.WriteLine($"Sleep(0) time: {sw.ElapsedMilliseconds}ms. For {sleepTimes} sleeps");
        }
    }
}
