using Qoollo.Turbo.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Threading
{
    [TestClass]
    public class SemaphoreLightTest
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
        public void TestReleaseWait()
        {
            SemaphoreLight inst = new SemaphoreLight(0);
            Assert.AreEqual(0, inst.CurrentCount);

            inst.Release();
            Assert.AreEqual(1, inst.CurrentCount);

            Assert.IsTrue(inst.Wait(0));
            Assert.AreEqual(0, inst.CurrentCount);
        }

        [TestMethod]
        public void TestManyReleaseWait()
        {
            SemaphoreLight inst = new SemaphoreLight(0);

            for (int i = 0; i < 100; i++)
            {  
                Assert.AreEqual(i, inst.CurrentCount);
                inst.Release();
            }

            for (int i = 100; i > 0; i--)
            {
                Assert.AreEqual(i, inst.CurrentCount);
                Assert.IsTrue(inst.Wait(10));
            }

            Assert.AreEqual(0, inst.CurrentCount);
        }


        [TestMethod]
        [Timeout(5 * 1000)]
        public void TestTimeoutWork()
        {
            SemaphoreLight inst = new SemaphoreLight(0);

            Assert.IsFalse(inst.Wait(1000));
            Assert.AreEqual(0, inst.CurrentCount);
        }


        [TestMethod]
        public void TestWakeUpOnRelease()
        {
            SemaphoreLight inst = new SemaphoreLight(0);
            bool wakeUp = false;

            Task.Run(() =>
                {
                    inst.Wait();
                    wakeUp = true;
                });

            TimingAssert.IsTrue(5000, () => inst.WaiterCount > 0);
            Assert.IsFalse(wakeUp);

            inst.Release();
            TimingAssert.IsTrue(5000, () => Volatile.Read(ref wakeUp));

            Assert.AreEqual(0, inst.CurrentCount);
            Assert.AreEqual(0, inst.WaiterCount);
        }


        [TestMethod]
        public void TestCancellation()
        {
            SemaphoreLight inst = new SemaphoreLight(0);
            CancellationTokenSource tokenSrc = new CancellationTokenSource();
            bool cancelled = false;

            Task.Run(() =>
            {
                try
                {
                    inst.Wait(tokenSrc.Token);
                }
                catch (OperationCanceledException)
                {
                    Volatile.Write(ref cancelled, true);
                    Thread.MemoryBarrier();
                }
            });

            TimingAssert.IsTrue(5000, () => inst.WaiterCount > 0);
            Assert.IsFalse(Volatile.Read(ref cancelled));

            tokenSrc.Cancel();
            TimingAssert.IsTrue(5000, () => Volatile.Read(ref cancelled));

            Assert.AreEqual(0, inst.CurrentCount);
            Assert.AreEqual(0, inst.WaiterCount);
        }

        [TestMethod]
        public void TestSilentCancellation()
        {
            SemaphoreLight inst = new SemaphoreLight(0);
            CancellationTokenSource tokenSrc = new CancellationTokenSource();
            bool cancelled = false;

            Task.Run(() =>
            {
                inst.Wait(-1, tokenSrc.Token, false);
                Volatile.Write(ref cancelled, true);
                Thread.MemoryBarrier();
            });

            TimingAssert.IsTrue(5000, () => inst.WaiterCount > 0);
            Assert.IsFalse(Volatile.Read(ref cancelled));

            tokenSrc.Cancel();
            TimingAssert.IsTrue(5000, () => Volatile.Read(ref cancelled));

            Assert.AreEqual(0, inst.CurrentCount);
            Assert.AreEqual(0, inst.WaiterCount);
        }



        [TestMethod]
        public void TestProducerConsumer()
        {
            const int NumIters = 500;
            SemaphoreLight inst = new SemaphoreLight(0);

            var task1 = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < NumIters; i++)
                    Assert.IsTrue(inst.Wait(30000));
                Assert.IsFalse(inst.Wait(0));
            }, TaskCreationOptions.LongRunning);

            var task2 = Task.Factory.StartNew(() =>
            {
                for (int i = 0; i < NumIters; i++)
                    inst.Release();
            }, TaskCreationOptions.LongRunning);


            Task.WaitAll(task1, task2);
        }

        [TestMethod]
        public void OverflowTest()
        {
            SemaphoreLight inst = new SemaphoreLight(0);

            inst.Release(int.MaxValue - 100);
            Assert.AreEqual(int.MaxValue - 100, inst.CurrentCount);

            inst.Release(100);
            Assert.AreEqual(int.MaxValue, inst.CurrentCount);

            try
            {
                inst.Release();
                Assert.Fail("SemaphoreFullException expected");
            }
            catch (SemaphoreFullException)
            {
            }

            bool waitResult = inst.Wait(0);
            Assert.IsTrue(waitResult);
        }



        private void RunComplexTest(SemaphoreLight sem, int elemCount, int thCount)
        {
            int atomicRandom = 0;

            int trackElemCount = elemCount;
            int waitedTimesCount = 0;
            int addFinished = 0;

            Thread[] threadsTake = new Thread[thCount];
            Thread[] threadsAdd = new Thread[thCount];

            CancellationTokenSource tokSrc = new CancellationTokenSource();

            Action addAction = () =>
            {
                Random rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * thCount * 2);

                while (true)
                {
                    int item = Interlocked.Decrement(ref trackElemCount);
                    if (item < 0)
                        break;

                    sem.Release();

                    int sleepTime = rnd.Next(100);
                    if (sleepTime > 0)
                        Thread.SpinWait(sleepTime);
                }

                Interlocked.Increment(ref addFinished);
            };


            Action takeAction = () =>
            {
                Random rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * thCount * 2);

                try
                {
                    while (Volatile.Read(ref addFinished) < thCount)
                    {
                        sem.Wait(tokSrc.Token);
                        Interlocked.Increment(ref waitedTimesCount);

                        int sleepTime = rnd.Next(100);
                        if (sleepTime > 0)
                            Thread.SpinWait(sleepTime);
                    }
                }
                catch (OperationCanceledException) { }

                TestContext.WriteLine("One take thread exit main cycle");

                while (sem.Wait(0))
                    Interlocked.Increment(ref waitedTimesCount);
            };


            for (int i = 0; i < threadsTake.Length; i++)
                threadsTake[i] = new Thread(new ThreadStart(takeAction));
            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i] = new Thread(new ThreadStart(addAction));


            for (int i = 0; i < threadsTake.Length; i++)
                threadsTake[i].Start();
            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i].Start();

            TestContext.WriteLine("All threads started");

            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i].Join();

            TestContext.WriteLine("All add threads stopped");

            tokSrc.Cancel();

            TestContext.WriteLine("Cancell called");

            for (int i = 0; i < threadsTake.Length; i++)
                threadsTake[i].Join();

            TestContext.WriteLine("All take threads stopped");


            Assert.AreEqual(elemCount, waitedTimesCount);
        }


        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void ComplexTest()
        {
            SemaphoreLight sem = new SemaphoreLight(0);

            for (int i = 0; i < 5; i++)
            {
                RunComplexTest(sem, 5000000, Math.Max(1, Environment.ProcessorCount / 2));
                Assert.AreEqual(0, sem.CurrentCount);
                TestContext.WriteLine("=========== One pass ===========");
            }
        }
    }
}
