using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Threading.OldThreadPools;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.ThreadPools
{
    [TestClass]
    public class DynamicThreadPoolOldTest
    {
        [TestMethod]
        public void DynamicThreadPoolExecuteSomeWork()
        {
            DynamicThreadPool testInst = new DynamicThreadPool(4);

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
        public void DynamicThreadPoolExecuteLongProcessWork()
        {
            DynamicThreadPool testInst = new DynamicThreadPool(4);

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
        [Ignore]
        [Timeout(2 * 60 * 1000)]
        public void DynamicThreadPoolRescueThreadsWorkOk()
        {
            DynamicThreadPool tp = new DynamicThreadPool(4, 20, -1, 100, 500, false, "pool", true, true, false);
            object lobj = new object();

            int finished = 0;
            Stopwatch sw = Stopwatch.StartNew();

            ManualResetEventSlim ev = new ManualResetEventSlim();

            for (int i = 0; i < 10; i++)
            {
                tp.Run(() =>
                    {
                        lock (lobj)
                        {
                            Thread.Sleep(10);
                            for (int j = 0; j < 10; j++)
                            {
                                tp.Run(() =>
                                    {
                                        ev.Wait();
                                        Thread.Sleep(10);
                                        Interlocked.Increment(ref finished);
                                    });
                            }
                        }
                    });
            }

            var finalTask = tp.RunAsTask(() =>
            {
                Thread.Sleep(1000);
                ev.Set();
                Assert.IsTrue(tp.ThreadCount > tp.MaxThreadCount);
            });

            finalTask.Wait();
            Assert.IsFalse(finalTask.IsFaulted);


            Console.WriteLine("For finished in: " + sw.ElapsedMilliseconds.ToString());

            for (int i = 0; i < 1000; i++)
                tp.Run(() => { });

            SpinWait.SpinUntil(() => Volatile.Read(ref finished) >= 10 * 10);

            Assert.IsTrue(tp.ThreadCount <= tp.MaxThreadCount);

            tp.Dispose(true, true, true);

            sw.Stop();
            Console.WriteLine("Full finished in: " + sw.ElapsedMilliseconds.ToString());
        }
    }
}
