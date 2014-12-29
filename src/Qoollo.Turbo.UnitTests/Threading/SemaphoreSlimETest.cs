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
    public class SemaphoreSlimETest
    {
        [TestMethod]
        public void TestReleaseWait()
        {
            SemaphoreSlimE inst = new SemaphoreSlimE(0);
            Assert.AreEqual(0, inst.CurrentCount);

            inst.Release();
            Assert.AreEqual(1, inst.CurrentCount);

            Assert.IsTrue(inst.Wait(0));
            Assert.AreEqual(0, inst.CurrentCount);
        }

        [TestMethod]
        public void TestManyReleaseWait()
        {
            SemaphoreSlimE inst = new SemaphoreSlimE(0);

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
            SemaphoreSlimE inst = new SemaphoreSlimE(0);

            Assert.IsFalse(inst.Wait(1000));
            Assert.AreEqual(0, inst.CurrentCount);
        }


        [TestMethod]
        public void TestWakeUpOnRelease()
        {
            SemaphoreSlimE inst = new SemaphoreSlimE(0);
            bool wakeUp = false;
            int startedFlag = 0;

            Task.Run(() =>
                {
                    Interlocked.Exchange(ref startedFlag, 1);
                    inst.Wait();
                    wakeUp = true;
                });


            TimingAssert.IsTrue(5000, () => Volatile.Read(ref startedFlag) == 1);
            Thread.Sleep(10);
            Assert.IsFalse(wakeUp);

            inst.Release();
            TimingAssert.IsTrue(5000, () => Volatile.Read(ref wakeUp));

            Assert.AreEqual(0, inst.CurrentCount);
        }


        [TestMethod]
        public void TestCancellation()
        {
            SemaphoreSlimE inst = new SemaphoreSlimE(0);
            CancellationTokenSource tokenSrc = new CancellationTokenSource();
            bool cancelled = false;
            int startedFlag = 0;

            Task.Run(() =>
            {
                try
                {
                    Interlocked.Exchange(ref startedFlag, 1);
                    inst.Wait(tokenSrc.Token);
                }
                catch (OperationCanceledException)
                {
                    Volatile.Write(ref cancelled, true);
                    Thread.MemoryBarrier();
                }
            });

            TimingAssert.IsTrue(5000, () => Volatile.Read(ref startedFlag) == 1);
            Thread.Sleep(10);
            Assert.IsFalse(cancelled);

            tokenSrc.Cancel();
            TimingAssert.IsTrue(5000, () => Volatile.Read(ref cancelled));

            Assert.AreEqual(0, inst.CurrentCount);
        }



        private void RunComplexTest(SemaphoreSlimE sem, int elemCount, int thCount)
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


            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i].Join();
            tokSrc.Cancel();
            for (int i = 0; i < threadsTake.Length; i++)
                threadsTake[i].Join();


            Assert.AreEqual(elemCount, waitedTimesCount);
        }


        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void ComplexTest()
        {
            SemaphoreSlimE sem = new SemaphoreSlimE(0);

            for (int i = 0; i < 5; i++)
            {
                RunComplexTest(sem, 5000000, Math.Max(1, Environment.ProcessorCount / 2));
                Assert.AreEqual(0, sem.CurrentCount);
            }
        }
    }
}
