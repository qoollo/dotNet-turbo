using Qoollo.Turbo.Threading.ThreadPools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.ThreadPools
{
    [TestClass]
    public class StaticThreadPoolNewTest
    {
        [ClassInitialize]
        public static void Init(TestContext context)
        {
            TestLoggingHelper.Subscribe(context, false);
        }
        [ClassCleanup]
        public static void Cleanup()
        {
            TestLoggingHelper.Unsubscribe();
        }


        public TestContext TestContext { get; set; }

        //=============================



        [TestMethod]
        public void TestSimpleProcessWork()
        {
            using (StaticThreadPool testInst = new StaticThreadPool(Environment.ProcessorCount, 100, "name"))
            {
                Assert.AreEqual(Environment.ProcessorCount, testInst.ThreadCount);
                Assert.AreEqual(100, testInst.QueueCapacity);
                Assert.IsTrue(testInst.IsWork);

                int expectedWork = 100000;
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

                testInst.CompleteAdding();
                testInst.Dispose();
            }
        }

        [TestMethod]
        public void TestLongProcessWork()
        {
            using (StaticThreadPool testInst = new StaticThreadPool(Environment.ProcessorCount, 100, "name"))
            {
                Assert.AreEqual(Environment.ProcessorCount, testInst.ThreadCount);
                Assert.AreEqual(100, testInst.QueueCapacity);

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
        }

        [TestMethod]
        public void TestQueueCapacityBound()
        {
            using (StaticThreadPool testInst = new StaticThreadPool(Environment.ProcessorCount, 10, "name"))
            {
                Assert.AreEqual(10, testInst.QueueCapacity);

                int tryRunWorkItem = 100 * testInst.QueueCapacity;
                int runWorkCount = 0;
                int executedWork = 0;

                for (int i = 0; i < tryRunWorkItem; i++)
                {
                    if (testInst.TryRun(() =>
                                            {
                                                Thread.Sleep(500);
                                                Interlocked.Increment(ref executedWork);
                                            }))
                    {
                        runWorkCount++;
                    }
                }

                testInst.Dispose(true, true, true);
                Assert.IsTrue(runWorkCount > 0);
                Assert.IsTrue(runWorkCount < tryRunWorkItem);
                Assert.AreEqual(runWorkCount, executedWork);
            }
        }


        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void TestQueueCapacityBoundExtends()
        {
            using (StaticThreadPool testInst = new StaticThreadPool(Environment.ProcessorCount, 10, "name"))
            {

                int expectedWork = 25;
                int executedWork = 0;

                ManualResetEventSlim tracker = new ManualResetEventSlim();

                for (int i = 0; i < expectedWork; i++)
                {
                    testInst.Run(() =>
                        {
                            tracker.Wait();
                            Interlocked.Increment(ref executedWork);
                        });
                }

                tracker.Set();


                testInst.Dispose(true, true, true);
                Assert.AreEqual(expectedWork, executedWork);
            }
        }


        [TestMethod]
        public void StopNoFinishProcessWorkCorrect()
        {
            using (StaticThreadPool testInst = new StaticThreadPool(Environment.ProcessorCount, -1, "name"))
            {
                Assert.AreEqual(-1, testInst.QueueCapacity);

                int expectedWork = 1000;
                int executedWork = 0;

                for (int i = 0; i < expectedWork; i++)
                {
                    testInst.Run(() =>
                    {
                        Thread.Sleep(1000);
                        Interlocked.Increment(ref executedWork);
                    });
                }

                testInst.Dispose(true, false, false);

                Assert.IsTrue(testInst.State == ThreadPoolState.Stopped);
                Assert.IsFalse(testInst.IsWork);
                Assert.IsTrue(executedWork < expectedWork);
            }
        }


        [TestMethod]
        public void WaitUntilStopWorkCorrect()
        {
            using (StaticThreadPool testInst = new StaticThreadPool(Environment.ProcessorCount, -1, "name"))
            {
                Assert.AreEqual(-1, testInst.QueueCapacity);

                int expectedWork = 100;
                int executedWork = 0;

                for (int i = 0; i < expectedWork; i++)
                {
                    testInst.Run(() =>
                    {
                        Thread.Sleep(200);
                        Interlocked.Increment(ref executedWork);
                    });
                }

                testInst.Dispose(false, true, false);
                Assert.IsTrue(executedWork < expectedWork);
                Assert.IsTrue(testInst.State == ThreadPoolState.StopRequested);

                testInst.WaitUntilStop();
                Assert.IsTrue(testInst.State == ThreadPoolState.Stopped);
                Assert.AreEqual(expectedWork, executedWork);
            }
        }


        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void TestNotAddAfterDispose()
        {
            using (StaticThreadPool testInst = new StaticThreadPool(Environment.ProcessorCount, -1, "name"))
            {
                Assert.IsTrue(testInst.TryRun(() => { }));

                testInst.Dispose();

                Assert.IsFalse(testInst.TryRun(() => { }));

                testInst.Run(() => { });
            }
        }


        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestCantAddAfterCompleteAdding()
        {
            using (StaticThreadPool testInst = new StaticThreadPool(Environment.ProcessorCount, -1, "name"))
            {
                Assert.IsTrue(testInst.TryRun(() => { }));

                testInst.CompleteAdding();

                Assert.IsFalse(testInst.TryRun(() => { }));

                testInst.Run(() => { });
            }
        }



        private async Task TestSwitchToPoolWorkInner(StaticThreadPool testInst)
        {
            Assert.IsFalse(testInst.IsThreadPoolThread);
            Assert.IsNull(SynchronizationContext.Current);

            await testInst.SwitchToPool();

            Assert.IsTrue(testInst.IsThreadPoolThread);
            Assert.IsNotNull(SynchronizationContext.Current);
        }

        [TestMethod]
        public void TestSwitchToPoolWork()
        {
            using (StaticThreadPool testInst = new StaticThreadPool(Environment.ProcessorCount, -1, "name"))
            {
                var task = TestSwitchToPoolWorkInner(testInst);
                task.Wait();
                Assert.IsFalse(task.IsFaulted);
            }
        }




        [TestMethod]
        public void StaticThreadPoolChangeThreadCount()
        {
            using (StaticThreadPool testInst = new StaticThreadPool(0, -1, "name"))
            {
                int expectedWork = 100;
                int executedWork = 0;

                for (int i = 0; i < expectedWork; i++)
                {
                    if (i == 30)
                        testInst.AddThreads(10);
                    else if (i == 50)
                        testInst.RemoveThreads(5);
                    else if (i == 80)
                        testInst.SetThreadCount(2);

                    testInst.Run(() =>
                    {
                        Thread.Sleep(200);
                        Interlocked.Increment(ref executedWork);
                    });

                    Thread.Sleep(10);
                }

                SpinWait.SpinUntil(() => executedWork == expectedWork);

                Assert.AreEqual(2, testInst.ThreadCount);

                testInst.Dispose(true, true, true);

                Assert.AreEqual(0, testInst.ThreadCount);
                Assert.IsFalse(testInst.IsWork);
                Assert.AreEqual(expectedWork, executedWork);
            }
        }


        [TestMethod]
        public void TestTaskSchedulerWork()
        {
            using (StaticThreadPool testInst = new StaticThreadPool(4, -1, "name", false, 2000, 2000, 256, true, true, false))
            {
                bool firstTaskInPool = false;
                bool secondTaskInPool = false;

                var task = testInst.RunAsTask(() =>
                    {
                        firstTaskInPool = testInst.IsThreadPoolThread;
                    }).ContinueWith(t =>
                    {
                        secondTaskInPool = testInst.IsThreadPoolThread;
                    }, testInst.TaskScheduler);

                task.Wait();
                Assert.IsTrue(firstTaskInPool);
                Assert.IsTrue(secondTaskInPool);

                testInst.Dispose();
            }
        }


        private void RunTestOnPool(StaticThreadPool pool, int totalTaskCount, int taskSpinCount, int spawnThreadCount, int spawnSpinTime, bool spawnFromPool)
        {
            Random rndGenerator = new Random();

            int executedTaskCounter = 0;
            int completedTaskCount = 0;


            Action taskAction = null;
            taskAction = () =>
            {
                int curTaskSpinCount = taskSpinCount;
                lock (rndGenerator)
                    curTaskSpinCount = rndGenerator.Next(taskSpinCount);

                Thread.SpinWait(curTaskSpinCount);

                if (spawnFromPool)
                {
                    if (Interlocked.Increment(ref executedTaskCounter) <= totalTaskCount)
                        pool.Run(taskAction);
                }

                Interlocked.Increment(ref completedTaskCount);
            };

            Barrier bar = new Barrier(spawnThreadCount + 1);

            Random spawnRndGenerator = new Random();
            Thread[] spawnThreads = new Thread[spawnThreadCount];
            ThreadStart spawnAction = () =>
            {
                bar.SignalAndWait();
                while (Interlocked.Increment(ref executedTaskCounter) <= totalTaskCount)
                {
                    pool.Run(taskAction);

                    int curSpawnSpinCount = spawnSpinTime;
                    lock (spawnRndGenerator)
                        curSpawnSpinCount = spawnRndGenerator.Next(spawnSpinTime);

                    Thread.SpinWait(curSpawnSpinCount);
                }
            };


            for (int i = 0; i < spawnThreads.Length; i++)
                spawnThreads[i] = new Thread(spawnAction);

            for (int i = 0; i < spawnThreads.Length; i++)
                spawnThreads[i].Start();

            bar.SignalAndWait();

            TimingAssert.AreEqual(60 * 1000, totalTaskCount, () => Volatile.Read(ref completedTaskCount));
        }


        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void RunComplexTest()
        {
            using (StaticThreadPool testInst = new StaticThreadPool(2 * Environment.ProcessorCount, 1000, "name"))
            {
                RunTestOnPool(testInst, 4000000, 1000, 2, 10, false);
                RunTestOnPool(testInst, 4000000, 10, 2, 1000, false);
                RunTestOnPool(testInst, 4000000, 0, 2, 0, true);
                RunTestOnPool(testInst, 4000000, 0, Environment.ProcessorCount, 0, false);
            }
        }
    }
}
