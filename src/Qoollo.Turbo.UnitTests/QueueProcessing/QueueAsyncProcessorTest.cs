using Qoollo.Turbo.Threading.QueueProcessing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.QueueProcessing
{
    [TestClass]
    public class QueueAsyncProcessorTest
    {
        [ClassInitialize]
        public static void Init(TestContext context)
        {
            TestLoggingHelper.Subscribe(context, true);
        }
        [ClassCleanup]
        public static void Cleanup()
        {
            TestLoggingHelper.Unsubscribe();
        }


        public TestContext TestContext { get; set; }

        //=============================


        [TestMethod]
        public void TestSimpleProcess()
        {
            int processed = 0;

            using (DeleageQueueAsyncProcessor<int> proc = new DeleageQueueAsyncProcessor<int>(Environment.ProcessorCount, 1000, "name", (elem, token) =>
            {
                Interlocked.Increment(ref processed);
            }))
            {
                Assert.AreEqual(0, proc.ElementCount);
                Assert.AreEqual(Environment.ProcessorCount, proc.ThreadCount);
                Assert.AreEqual(false, proc.IsAddingCompleted);
                Assert.AreEqual(false, proc.IsBackground);
                Assert.AreEqual(false, proc.IsWork);
                Assert.AreEqual(1000, proc.QueueCapacity);
                Assert.IsTrue(proc.State == QueueAsyncProcessorState.Created);

                proc.Start();

                Assert.IsTrue(proc.State == QueueAsyncProcessorState.Running);
                Assert.IsTrue(proc.IsWork);

                for (int i = 0; i < 10000; i++)
                    proc.Add(i);

                proc.Stop(true, true, true);

                Assert.IsTrue(proc.State == QueueAsyncProcessorState.Stopped);
                Assert.IsFalse(proc.IsWork);

                Assert.AreEqual(10000, processed);
            }
        }

        [TestMethod]
        public void CanStopNotStartedQueueProcessor()
        {
            int processed = 0;

            DeleageQueueAsyncProcessor<int> proc = new DeleageQueueAsyncProcessor<int>(Environment.ProcessorCount, 1000, "name", (elem, token) =>
            {
                Interlocked.Increment(ref processed);
            });

            proc.Stop();
        }


        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void TestCantAddAfterDispose()
        {
            int processed = 0;

            using (DeleageQueueAsyncProcessor<int> proc = new DeleageQueueAsyncProcessor<int>(Environment.ProcessorCount, 1000, "name", (elem, token) =>
            {
                Interlocked.Increment(ref processed);
            }))
            {
                Assert.IsTrue(proc.TryAdd(10));

                proc.Start();

                Assert.IsTrue(proc.TryAdd(20));

                proc.Dispose();

                proc.Add(-1);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestCantAddAfterCompleteAdding()
        {
            int processed = 0;

            using (DeleageQueueAsyncProcessor<int> proc = new DeleageQueueAsyncProcessor<int>(Environment.ProcessorCount, 1000, "name", (elem, token) =>
            {
                Interlocked.Increment(ref processed);
            }))
            {

                Assert.IsTrue(proc.TryAdd(10));

                proc.Start();

                Assert.IsTrue(proc.TryAdd(20));

                proc.CompleteAdding();

                Assert.IsTrue(proc.IsAddingCompleted);

                proc.Add(-1);
            }
        }


        [TestMethod]
        public void TestHardStopWork()
        {
            int processed = 0;
            int startedTask = 0;
            ManualResetEventSlim waiter = new ManualResetEventSlim(false);

            using (DeleageQueueAsyncProcessor<int> proc = new DeleageQueueAsyncProcessor<int>(Environment.ProcessorCount, 1000, "name", (elem, token) =>
            {
                try
                {
                    Interlocked.Increment(ref startedTask);
                    waiter.Wait(token);
                }
                finally
                {
                    Interlocked.Increment(ref processed);
                }
            }))
            {

                proc.Start();

                for (int i = 0; i < 5 * Environment.ProcessorCount; i++)
                    proc.Add(i);

                Assert.IsTrue(proc.ThreadCount > 0, "proc.ThreadCount > 0");
                Assert.IsTrue(proc.ThreadCount == Environment.ProcessorCount, "proc.ThreadCount == Environment.ProcessorCount");

                TimingAssert.IsTrue(10000, () => proc.ActiveThreadCount >= 0, "FAILED: wait while thread activated");
                TimingAssert.IsTrue(10000, () => proc.ActiveThreadCount == proc.ThreadCount, "FAILED: wait while all threads activated");

                TimingAssert.IsTrue(10000, () => Volatile.Read(ref startedTask) >= 0, "FAILED: wait while first thread blocked");
                TimingAssert.IsTrue(10000, () => Volatile.Read(ref startedTask) == proc.ThreadCount, () => "FAILED: wait while all thread blocked. Currently blocked = " + Volatile.Read(ref startedTask).ToString() + ", expected = " + proc.ThreadCount.ToString());
                proc.Stop(true, false, true);

                Assert.IsTrue(proc.State == QueueAsyncProcessorState.Stopped, "proc.State == QueueAsyncProcessorState.Stopped");
                Assert.IsTrue(processed > 0, "processed > 0");
            }
        }


        [TestMethod]
        public void TestUnlockWhenItemReceive()
        {
            int processed = 0;
            ManualResetEventSlim waiter = new ManualResetEventSlim(false);

            using (DeleageQueueAsyncProcessor<int> proc = new DeleageQueueAsyncProcessor<int>(Environment.ProcessorCount, 1000, "name", (elem, token) =>
            {
                Interlocked.Increment(ref processed);
                waiter.Wait(token);
            }))
            {
                proc.Start();

                for (int i = 0; i < proc.ThreadCount; i++)
                    proc.Add(i);

                //if (!SpinWait.SpinUntil(() => Volatile.Read(ref processed) == proc.ThreadCount, 5000))
                //{
                //    System.Diagnostics.Debugger.Launch();
                //    throw new Exception("Proc: " + Volatile.Read(ref processed).ToString() + ", threads: " + proc.ActiveThreadCount.ToString() +
                //                        ", tasks: " + proc.ElementCount.ToString());
                //}

                TimingAssert.IsTrue(15000, () => Volatile.Read(ref processed) == proc.ThreadCount, "FAILED: wait for all thread start");

                waiter.Set();
                proc.Stop(true, true, true);

                Assert.IsTrue(proc.State == QueueAsyncProcessorState.Stopped, "proc.State == QueueAsyncProcessorState.Stopped");
            }
        }

        [TestMethod]
        public void RunManyTestUnlockWhenItemReceive()
        {
            for (int i = 0; i < 1000; i++)
                TestUnlockWhenItemReceive();
        }


        [TestMethod]
        public void TestWaitUntilStop()
        {
            int processed = 0;

            using (DeleageQueueAsyncProcessor<int> proc = new DeleageQueueAsyncProcessor<int>(Environment.ProcessorCount, 1000, "name", (elem, token) =>
            {
                Thread.Sleep(1000);
                Interlocked.Increment(ref processed);
            }).ThenStart())
            {
                for (int i = 0; i < 10; i++)
                    proc.Add(i);

                proc.Stop(false, true, false);

                Assert.IsTrue(proc.State == QueueAsyncProcessorState.StopRequested);

                proc.WaitUntilStop();

                Assert.IsTrue(proc.State == QueueAsyncProcessorState.Stopped);
                Assert.IsTrue(processed == 10);
            }
        }




        private void RunComplexTest(int threadCount, int queueSize, int testElemCount, int addThreadCount, int procSpinWaitCount, int addSleepMs)
        {
            List<int> processedItems = new List<int>(testElemCount + 1);
            int currentItem = 0;
            Random rnd = new Random();

            using (DeleageQueueAsyncProcessor<int> proc = new DeleageQueueAsyncProcessor<int>(threadCount, queueSize, "name", (elem, token) =>
            {
                int curSpinCount = 0;
                lock (rnd)
                    curSpinCount = rnd.Next(procSpinWaitCount);

                Thread.SpinWait(curSpinCount);

                lock (processedItems)
                    processedItems.Add(elem);
            }))
            {
                Action addAction = () =>
                    {
                        while (true)
                        {
                            int curVal = Interlocked.Increment(ref currentItem);
                            if (curVal > testElemCount)
                                break;

                            proc.Add(curVal - 1);

                            if (addSleepMs > 0)
                                Thread.Sleep(addSleepMs);
                        }
                    };


                Thread[] addThreads = new Thread[addThreadCount];

                for (int i = 0; i < addThreads.Length; i++)
                    addThreads[i] = new Thread(new ThreadStart(addAction));

                for (int i = 0; i < addThreads.Length; i++)
                    addThreads[i].Start();

                proc.Start();

                Assert.IsTrue(proc.IsWork);

                for (int i = 0; i < addThreads.Length; i++)
                    addThreads[i].Join();


                proc.Stop(true, true, true);

                Assert.AreEqual(testElemCount, processedItems.Count);

                processedItems.Sort();
                for (int i = 0; i < processedItems.Count; i++)
                    Assert.AreEqual(i, processedItems[i]);
            }
        }


        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void ComplexTest()
        {
            RunComplexTest(Environment.ProcessorCount, 1000, 1000000, Environment.ProcessorCount, 0, 0);
            RunComplexTest(1, -1, 10000, Environment.ProcessorCount, 0, 1);
            RunComplexTest(2 * Environment.ProcessorCount, 1000, 1000000, Environment.ProcessorCount, 100, 0);
            RunComplexTest(Environment.ProcessorCount, 100, 20000, Environment.ProcessorCount, 100, 1);
        }
    }
}
