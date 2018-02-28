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
    public class DynamicThreadPoolNewTest : TestClassBase
    {
        [ClassInitialize]
        public static void Init(TestContext context)
        {
            SubscribeToUnhandledExceptions(context, false);
        }
        [ClassCleanup]
        public static void Cleanup()
        {
            UnsubscribeFromUnhandledExceptions();
        }


        //=============================


        private void RunSimpleProcessWorkTest(int minThreadCount, int maxThreadCount, int queueCapacity, int workCount = 100000)
        {
            using (DynamicThreadPool testInst = new DynamicThreadPool(minThreadCount, maxThreadCount, queueCapacity, "name"))
            {
                Assert.IsTrue(testInst.ThreadCount >= minThreadCount);
                Assert.IsTrue(testInst.ActiveThreadCount >= minThreadCount);
                Assert.AreEqual(queueCapacity, testInst.QueueCapacity);
                Assert.IsTrue(testInst.IsWork);

                int expectedWork = workCount;
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
            }
        }


        [TestMethod]
        public void TestSimpleProcessWork()
        {
            RunSimpleProcessWorkTest(0, Environment.ProcessorCount, 100);
        }

        [TestMethod]
        public void TestSimpleProcessWorkWithSmallThreadCount()
        {
            RunSimpleProcessWorkTest(0, 1, 100);
        }

        [TestMethod]
        public void TestSimpleProcessWorkWithLargeThreadCount()
        {
            RunSimpleProcessWorkTest(0, 100, 100);
        }

        [TestMethod]
        public void TestSimpleProcessWorkWithLargeMinThreadCount()
        {
            RunSimpleProcessWorkTest(100, 100, 100, 25000);
        }

        [TestMethod]
        public void TestLongProcessWork()
        {
            using (DynamicThreadPool testInst = new DynamicThreadPool(0, Environment.ProcessorCount, 100, "name"))
            {
                Assert.AreEqual(100, testInst.QueueCapacity);

                int expectedWork = 100;
                int executedWork = 0;

                for (int i = 0; i < expectedWork; i++)
                {
                    testInst.Run(() =>
                    {
                        Thread.Sleep(250);
                        Interlocked.Increment(ref executedWork);
                    });
                }

                testInst.Dispose(true, true, true);

                Assert.AreEqual(0, testInst.ThreadCount);
                Assert.AreEqual(0, testInst.ActiveThreadCount);
                Assert.IsFalse(testInst.IsWork);
                Assert.AreEqual(expectedWork, executedWork);
            }
        }

        [TestMethod]
        public void TestTryRunWork()
        {
            using (DynamicThreadPool testInst = new DynamicThreadPool(0, Environment.ProcessorCount, 100, "name"))
            {
                int wasExecuted = 0;
                bool wasRunned = testInst.TryRun(() =>
                {
                    Interlocked.Exchange(ref wasExecuted, 1);
                });

                Assert.IsTrue(wasRunned);
                TimingAssert.AreEqual(5000, 1, () => Volatile.Read(ref wasExecuted));
            }
        }

        [TestMethod]
        public void TestQueueCapacityBound()
        {
            using (DynamicThreadPool testInst = new DynamicThreadPool(0, Environment.ProcessorCount, 10, "name"))
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
            using (DynamicThreadPool testInst = new DynamicThreadPool(0, Environment.ProcessorCount, 10, "name"))
            {
                int expectedWork = 25 + Environment.ProcessorCount;
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
            using (DynamicThreadPool testInst = new DynamicThreadPool(0, Environment.ProcessorCount, -1, "name"))
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
            using (DynamicThreadPool testInst = new DynamicThreadPool(0, Environment.ProcessorCount, -1, "name"))
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
            using (DynamicThreadPool testInst = new DynamicThreadPool(0, Environment.ProcessorCount, -1, "name"))
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
            using (DynamicThreadPool testInst = new DynamicThreadPool(0, Environment.ProcessorCount, -1, "name"))
            {
                Assert.IsTrue(testInst.TryRun(() => { }));

                testInst.CompleteAdding();

                Assert.IsFalse(testInst.TryRun(() => { }));

                testInst.Run(() => { });
            }
        }



        private async Task TestSwitchToPoolWorkInner(DynamicThreadPool testInst)
        {
            Assert.IsFalse(testInst.IsThreadPoolThread);
            //Assert.IsNull(SynchronizationContext.Current);

            await testInst.SwitchToPool();

            Assert.IsTrue(testInst.IsThreadPoolThread);
            //Assert.IsNotNull(SynchronizationContext.Current);
        }

        [TestMethod]
        public void TestSwitchToPoolWork()
        {
            using (DynamicThreadPool testInst = new DynamicThreadPool(0, Environment.ProcessorCount, -1, "name"))
            {
                var task = TestSwitchToPoolWorkInner(testInst);
                task.Wait();
                Assert.IsFalse(task.IsFaulted);
            }
        }




        [TestMethod]
        public void TestTaskSchedulerWork()
        {
            using (DynamicThreadPool testInst = new DynamicThreadPool(0, Environment.ProcessorCount, -1, "name", false, new DynamicThreadPoolOptions() { UseOwnTaskScheduler = true, UseOwnSyncContext = true }))
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
            }
        }

        [TestMethod]
        public void TestTaskSchedulerSetted()
        {
            using (DynamicThreadPool testInst = new DynamicThreadPool(0, Environment.ProcessorCount, -1, "name", false, new DynamicThreadPoolOptions() { UseOwnTaskScheduler = true, UseOwnSyncContext = true }))
            {
                AtomicBool isPropperSceduller = new AtomicBool(false);

                testInst.Run(() =>
                {
                    isPropperSceduller.Value = TaskScheduler.Current == testInst.TaskScheduler;
                });

                TimingAssert.IsTrue(10000, isPropperSceduller, "isPropperSceduller");
                testInst.Dispose(true, true, false);
            }
        }

        [TestMethod]
        public void TestSyncContextSetted()
        {
            using (DynamicThreadPool testInst = new DynamicThreadPool(0, Environment.ProcessorCount, -1, "name", false, new DynamicThreadPoolOptions() { UseOwnTaskScheduler = true, UseOwnSyncContext = true }))
            {
                AtomicBool isPropperSyncContext = new AtomicBool(false);

                testInst.Run(() =>
                {
                    if (SynchronizationContext.Current != null)
                    {
                        SynchronizationContext.Current.Post((st) =>
                        {
                            isPropperSyncContext.Value = testInst.IsThreadPoolThread;
                        }, null);
                    }
                });

                TimingAssert.IsTrue(10000, isPropperSyncContext, "isPropperSyncContext");
                testInst.Dispose(true, true, false);
            }
        }

        [TestMethod]
        public void TestNoSyncContextAndTaskSchedullerWhenNotConfigurated()
        {
            using (DynamicThreadPool testInst = new DynamicThreadPool(0, Environment.ProcessorCount, -1, "name", false, new DynamicThreadPoolOptions() { UseOwnTaskScheduler = false, UseOwnSyncContext = false }))
            {
                var defSyncContext = SynchronizationContext.Current;
                var defTaskScheduller = TaskScheduler.Current;

                AtomicBool isDefaultSyncContext = new AtomicBool(false);
                AtomicBool isDefaultTaskScheduller = new AtomicBool(false);

                testInst.Run(() =>
                {
                    isDefaultSyncContext.Value = SynchronizationContext.Current == defSyncContext;
                    isDefaultTaskScheduller.Value = TaskScheduler.Current == defTaskScheduller;
                });

                TimingAssert.IsTrue(10000, isDefaultSyncContext, "isDefaultSyncContext");
                TimingAssert.IsTrue(10000, isDefaultTaskScheduller, "isDefaultTaskScheduller");
                testInst.Dispose(true, true, false);
            }
        }


        private async Task AwaitThroughSyncContext()
        {
            await Task.Delay(100);
        }

        [TestMethod]
        public void TestAwaitThroughSyncContext()
        {
            using (DynamicThreadPool testInst = new DynamicThreadPool(0, 2 * Environment.ProcessorCount, -1, "name", false, new DynamicThreadPoolOptions() { UseOwnTaskScheduler = true, UseOwnSyncContext = true }))
            {
                bool isNotFailed = false;
                bool isThreadPool = false;

                testInst.Run(() =>
                    {
                        var task = AwaitThroughSyncContext();
                        task.ContinueWith(t =>
                            {
                                Volatile.Write(ref isNotFailed, !t.IsFaulted);
                                Volatile.Write(ref isThreadPool, testInst.IsThreadPoolThread);
                            }, TaskContinuationOptions.ExecuteSynchronously).Wait();
                    });

                testInst.Dispose(true, true, false);

                Assert.IsTrue(isNotFailed, "isNotFailed");
                Assert.IsTrue(isThreadPool, "isThreadPool");
            }
        }


        private void RunTestOnPool(DynamicThreadPool pool, int totalTaskCount, int taskSpinCount, int spawnThreadCount, int spawnSpinTime, bool spawnFromPool)
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
            using (DynamicThreadPool testInst = new DynamicThreadPool(0, 4 * Environment.ProcessorCount, 1000, "name"))
            {
                RunTestOnPool(testInst, 4000000, 1000, 2, 10, false);
                RunTestOnPool(testInst, 4000000, 10, 2, 1000, false);
                RunTestOnPool(testInst, 4000000, 0, 2, 0, true);
                RunTestOnPool(testInst, 4000000, 0, Environment.ProcessorCount, 0, false);
            }
        }


        [TestMethod]
        public void DynamicThreadPoolFillUpToWork()
        {
            TestContext.WriteLine("Test trace message");

            using (DynamicThreadPool testInst = new DynamicThreadPool(0, 4 * Environment.ProcessorCount, 1000, "name"))
            {
                Assert.AreEqual(0, testInst.ActiveThreadCount);

                testInst.FillPoolUpTo(Environment.ProcessorCount);
                Assert.AreEqual(Environment.ProcessorCount, testInst.ActiveThreadCount);
                Assert.AreEqual(Environment.ProcessorCount, testInst.ThreadCount);

                testInst.FillPoolUpTo(4 * Environment.ProcessorCount);
                Assert.AreEqual(4 * Environment.ProcessorCount, testInst.ThreadCount);
            }
        }


        [TestMethod]
        [Timeout(4 * 60 * 1000)]
        public void DynamicThreadPoolPerformBalancing()
        {
            using (DynamicThreadPool testInst = new DynamicThreadPool(0, 4 * Environment.ProcessorCount, 1000, "name"))
            {
                Assert.AreEqual(0, testInst.ActiveThreadCount);

                // ========== Проверяем, что число потоков увеличивается автоматически от нуля ===========

                int executedTaskCount = 0;

                for (int i = 0; i < 1000; i++)
                {
                    testInst.Run(() =>
                        {
                            Interlocked.Increment(ref executedTaskCount);
                        });
                }

                TimingAssert.AreEqual(5000, 1000, () => Volatile.Read(ref executedTaskCount));
                Assert.IsTrue(testInst.ActiveThreadCount > 0, "1. testInst.ActiveThreadCount > 0");

                // ======== Проверяем, что на большом числе задач он рано или поздно дойдёт до числа потоков равного числу ядер ===========

                executedTaskCount = 0;
                for (int i = 0; i < testInst.MaxThreadCount * testInst.QueueCapacity; i++)
                {
                    testInst.Run(() =>
                    {
                        Thread.Sleep(1);
                        Interlocked.Increment(ref executedTaskCount);
                    });
                }


                TimingAssert.AreEqual(15000, testInst.MaxThreadCount * testInst.QueueCapacity, () => Volatile.Read(ref executedTaskCount));
                Assert.IsTrue(testInst.ActiveThreadCount >= Environment.ProcessorCount, "2. testInst.ActiveThreadCount >= Environment.ProcessorCount");


                // ======== Проверяем, что на долгих задачах число потоков может стать больше числа ядер ===========

                executedTaskCount = 0;
                for (int i = 0; i < 1000; i++)
                {
                    testInst.Run(() =>
                    {
                        Thread.Sleep(20);
                        Interlocked.Increment(ref executedTaskCount);
                    });
                }


                TimingAssert.IsTrue(30000, () => Volatile.Read(ref executedTaskCount) >= 500);

                Assert.IsTrue(testInst.ActiveThreadCount > Environment.ProcessorCount, "3. testInst.ActiveThreadCount > Environment.ProcessorCount");
                Assert.IsTrue(testInst.ActiveThreadCount <= testInst.MaxThreadCount, "3. testInst.ActiveThreadCount <= testInst.MaxThreadCount");

                TimingAssert.AreEqual(30000, 1000, () => Volatile.Read(ref executedTaskCount));

                TimingAssert.IsTrue(5000, () => testInst.ActiveThreadCount <= Environment.ProcessorCount, "4. testInst.ActiveThreadCount <= Environment.ProcessorCount");
            }
        }
    }
}
