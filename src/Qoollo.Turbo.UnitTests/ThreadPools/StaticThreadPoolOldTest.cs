using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Threading.OldThreadPools;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.ThreadPools
{
    [TestClass]
    public class StaticThreadPoolOldTest
    {
        [TestMethod]
        public void StaticThreadPoolExecuteSomeWork()
        {
            StaticThreadPool testInst = new StaticThreadPool(4);

            int expectedWork = 10000;
            int executedWork = 0;

            for (int i = 0; i < expectedWork; i++)
            {
                testInst.Run(() =>
                    {
                        Interlocked.Increment(ref executedWork);
                    });
            }

            testInst.Dispose(true, true, true);

            Assert.AreEqual(0, testInst.ThreadCount);
            Assert.IsFalse(testInst.IsWork);
            Assert.AreEqual(expectedWork, executedWork);
        }


        [TestMethod]
        public void StaticThreadPoolExecuteLongProcessWork()
        {
            StaticThreadPool testInst = new StaticThreadPool(4);

            int expectedWork = 100;
            int executedWork = 0;

            for (int i = 0; i < expectedWork; i++)
            {
                testInst.Run(() =>
                {
                    Thread.Sleep(300);
                    Interlocked.Increment(ref executedWork);
                });
            }

            testInst.Dispose(true, true, true);

            Assert.AreEqual(0, testInst.ThreadCount);
            Assert.IsFalse(testInst.IsWork);
            Assert.AreEqual(expectedWork, executedWork);
        }



        [TestMethod]
        public void StaticThreadPoolChangeThreadCount()
        {
            StaticThreadPool testInst = new StaticThreadPool(0);

            int expectedWork = 100;
            int executedWork = 0;

            for (int i = 0; i < expectedWork; i++)
            {
                if (i == 30)
                    testInst.AddThreads(10);
                else if (i == 50)
                    testInst.RemoveThreads(5);
                else if (i == 80)
                    testInst.FillPoolUpTo(2);

                testInst.Run(() =>
                {
                    Thread.Sleep(200);
                    Interlocked.Increment(ref executedWork);
                });

                Thread.Sleep(1);
            }

            SpinWait.SpinUntil(() => executedWork == expectedWork);

            Assert.AreEqual(2, testInst.ThreadCount);

            testInst.Dispose(true, true, true);

            Assert.AreEqual(0, testInst.ThreadCount);
            Assert.IsFalse(testInst.IsWork);
            Assert.AreEqual(expectedWork, executedWork);
        }


        [TestMethod]
        [Ignore]
        public void StaticThreadPoolOverheadTest()
        {
            int expectedWork = 100000;
            int executedWork = 0;

            Action act = () =>
                {
                    Interlocked.Increment(ref executedWork);
                };

            Stopwatch swSerial = Stopwatch.StartNew();

            for (int i = 0; i < expectedWork; i++)
                act();

            swSerial.Stop();


            StaticThreadPool testInst = new StaticThreadPool(1);

            Stopwatch swPool = Stopwatch.StartNew();

            for (int i = 0; i < expectedWork; i++)
                testInst.Run(act);

            testInst.Dispose(true, true, true);
            swPool.Stop();


            Assert.IsTrue(Math.Abs(swPool.Elapsed.TotalMilliseconds - swSerial.Elapsed.TotalMilliseconds) < expectedWork * 0.001);
        }

        [TestMethod]
        [Ignore]
        public void StaticThreadPoolPerformanceInCompareToSystem()
        {
            int expectedWork = 100000;
            int executedWork = 0;

            Action act = () =>
            {
                Interlocked.Increment(ref executedWork);
            };


            SystemThreadPool sysThreadPool = new SystemThreadPool();

            Stopwatch swSystem = Stopwatch.StartNew();

            for (int i = 0; i < expectedWork; i++)
                sysThreadPool.Run(act);

            SpinWait.SpinUntil(() => executedWork >= expectedWork);
            swSystem.Stop();


            StaticThreadPool testInst = new StaticThreadPool(1);

            Stopwatch swPool = Stopwatch.StartNew();

            for (int i = 0; i < expectedWork; i++)
                testInst.Run(act);

            testInst.Dispose(true, true, true);
            swPool.Stop();


            Assert.IsTrue(Math.Abs(swPool.Elapsed.TotalMilliseconds - swSystem.Elapsed.TotalMilliseconds) < expectedWork * 0.001);
        }
    }
}
