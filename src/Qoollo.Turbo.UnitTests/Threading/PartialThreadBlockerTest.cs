using Qoollo.Turbo.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Turbo.Threading.ServiceStuff;

namespace Qoollo.Turbo.UnitTests.Threading
{
    [TestClass]
    public class PartialThreadBlockerTest : TestClassBase
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


        [TestMethod]
        public void TestNoBlockWithZeroWaiterCount()
        {
            PartialThreadBlocker inst = new PartialThreadBlocker();

            Assert.IsTrue(inst.Wait(1));
            Assert.IsTrue(inst.Wait(1));
        }


        [TestMethod]
        public void TestBlockRequiredCount()
        {
            PartialThreadBlocker inst = new PartialThreadBlocker(4);
            Barrier startBar = new Barrier(8 + 1);
            int exitedCount = 0;
            

            for (int i = 0; i < 8; i++)
            {
                Task.Run(() =>
                    {
                        startBar.SignalAndWait();
                        inst.Wait();
                        Interlocked.Increment(ref exitedCount);
                    });
            }

            startBar.SignalAndWait();
            Assert.IsTrue(SpinWait.SpinUntil(() => Volatile.Read(ref exitedCount) >= 4 && inst.RealWaiterCount >= 4, 4000));

            TimingAssert.AreEqual(5000, 8 - 4, () => Volatile.Read(ref exitedCount), "exitedCount != 8");
            TimingAssert.AreEqual(5000, 4, () => inst.ExpectedWaiterCount, "ExpectedWaiterCount != 4");
            TimingAssert.AreEqual(5000, 4, () => inst.RealWaiterCount, "RealWaiterCount != 4");

            inst.SetExpectedWaiterCount(0);

            TimingAssert.AreEqual(5000, 8, () => Volatile.Read(ref exitedCount), "exitedCount != 8");
            TimingAssert.AreEqual(5000, 0, () => inst.ExpectedWaiterCount, "ExpectedWaiterCount != 0");
            TimingAssert.AreEqual(5000, 0, () => inst.RealWaiterCount, "RealWaiterCount != 0");
        }


        [TestMethod]
        public void TestCycledBlockRequiredCount()
        {
            PartialThreadBlocker inst = new PartialThreadBlocker(4);
            Barrier startBar = new Barrier(8 + 1);
            int exitedCount = 0;
            int somethingWork = 0;
            CancellationTokenSource tokenSrc = new CancellationTokenSource();

            for (int i = 0; i < 8; i++)
            {
                Task.Run(() =>
                {
                    startBar.SignalAndWait();
                    while (!tokenSrc.IsCancellationRequested)
                    {
                        inst.Wait();
                        Interlocked.Increment(ref somethingWork);
                        Thread.Sleep(10);
                    }
                    Interlocked.Increment(ref exitedCount);
                });
            }

            startBar.SignalAndWait();

            TimingAssert.AreEqual(5000, 4, () => inst.ExpectedWaiterCount);
            TimingAssert.AreEqual(5000, 4, () => inst.RealWaiterCount);
            TimingAssert.IsTrue(5000, () => Volatile.Read(ref somethingWork) > 0);

            tokenSrc.Cancel();
            inst.SetExpectedWaiterCount(0);

            TimingAssert.AreEqual(5000, 8, () => Volatile.Read(ref exitedCount));
            TimingAssert.AreEqual(5000, 0, () => inst.ExpectedWaiterCount);
            TimingAssert.AreEqual(5000, 0, () => inst.RealWaiterCount);
        }


        [TestMethod]
        public void TestSingleDecreaseWork()
        {
            PartialThreadBlocker inst = new PartialThreadBlocker(4);
            Barrier startBar = new Barrier(1 + 1);
            int exitedCount = 0;

            Task.Run(() =>
            {
                startBar.SignalAndWait();
                inst.Wait();
                Interlocked.Increment(ref exitedCount);
            });

            startBar.SignalAndWait();
 
            TimingAssert.AreEqual(5000, 4, () => inst.ExpectedWaiterCount);
            TimingAssert.AreEqual(5000, 1, () => inst.RealWaiterCount, "RealWaiterCount != 1");

            for (int i = 1; i <= 4; i++)
            {
                inst.SubstractExpectedWaiterCount(1);
                TimingAssert.AreEqual(5000, 4 - i, () => inst.ExpectedWaiterCount);
                TimingAssert.AreEqual(5000, i == 4 ? 0 : 1, () => inst.RealWaiterCount);
            }

            TimingAssert.AreEqual(5000, 1, () => Volatile.Read(ref exitedCount), "exitedCount != 1");
        }

        [TestMethod]
        public void TestIncreaseWaiters()
        {
            PartialThreadBlocker inst = new PartialThreadBlocker(4);
            Barrier startBar = new Barrier(8 + 1);
            int exitedCount = 0;
            int somethingWork = 0;
            CancellationTokenSource tokenSrc = new CancellationTokenSource();

            for (int i = 0; i < 8; i++)
            {
                Task.Run(() =>
                {
                    startBar.SignalAndWait();
                    while (!tokenSrc.IsCancellationRequested)
                    {
                        inst.Wait();
                        Interlocked.Increment(ref somethingWork);
                        Thread.Sleep(10);
                    }
                    Interlocked.Increment(ref exitedCount);
                });
            }

            startBar.SignalAndWait();

            TimingAssert.AreEqual(5000, 4, () => inst.ExpectedWaiterCount);
            TimingAssert.AreEqual(5000, 4, () => inst.RealWaiterCount, "Real waiter count != 4 (can be caused by slow processing)");
            TimingAssert.IsTrue(5000, () => Volatile.Read(ref somethingWork) > 0);

            inst.SetExpectedWaiterCount(8);
            Assert.AreEqual(8, inst.ExpectedWaiterCount);

            TimingAssert.AreEqual(5000, 8, () => inst.RealWaiterCount);
            Interlocked.Exchange(ref somethingWork, 0);

            TimingAssert.IsTrue(5000, () => Volatile.Read(ref somethingWork) == 0);

            tokenSrc.Cancel();
            inst.SetExpectedWaiterCount(0);

            TimingAssert.AreEqual(5000, 8, () => Volatile.Read(ref exitedCount));
            TimingAssert.AreEqual(5000, 0, () => inst.ExpectedWaiterCount);
            TimingAssert.AreEqual(500, 0, () => inst.RealWaiterCount);
        }


        [TestMethod]
        public void TestCancellationWork()
        {
            PartialThreadBlocker inst = new PartialThreadBlocker(4);
            CancellationTokenSource tokSrc = new CancellationTokenSource();
            int exitedCount = 0;

            Task.Run(() =>
            {
                try
                {
                    inst.Wait(tokSrc.Token);
                }
                catch (OperationCanceledException)
                {
                }
                Interlocked.Increment(ref exitedCount);
            });


            TimingAssert.AreEqual(5000, 4, () => inst.ExpectedWaiterCount);
            TimingAssert.AreEqual(5000, 1, () => inst.RealWaiterCount);

            tokSrc.Cancel();

            TimingAssert.AreEqual(5000, 4, () => inst.ExpectedWaiterCount);
            TimingAssert.AreEqual(5000, 0, () => inst.RealWaiterCount);
            TimingAssert.AreEqual(5000, 1, () => Volatile.Read(ref exitedCount));
        }


        [TestMethod]
        public void TestTimeoutWork()
        {
            PartialThreadBlocker inst = new PartialThreadBlocker(4);
            int exitedCount = 0;
            bool exitByTimeout = false;

            Task.Run(() =>
            {
                Volatile.Write(ref exitByTimeout, !inst.Wait(1500));
                Interlocked.Increment(ref exitedCount);
            });


            TimingAssert.AreEqual(5000, 4, () => inst.ExpectedWaiterCount);
            TimingAssert.AreEqual(5000, 1, () => inst.RealWaiterCount);


            TimingAssert.AreEqual(5000, 4, () => inst.ExpectedWaiterCount);
            TimingAssert.AreEqual(5000, 0, () => inst.RealWaiterCount);
            TimingAssert.AreEqual(5000, 1, () => Volatile.Read(ref exitedCount));
            TimingAssert.IsTrue(5000, () => Volatile.Read(ref exitByTimeout));
        }


        [TestMethod]
        public void TestFullCycle()
        {
            PartialThreadBlocker inst = new PartialThreadBlocker(0);
            Barrier startBar = new Barrier(8 + 1);
            int exitedCount = 0;
            CancellationTokenSource tokenSrc = new CancellationTokenSource();
            Thread[] threads = new Thread[8];

            Func<int> sleepThCount = () =>
                {
                    int result = 0;
                    foreach (var th in threads)
                        if ((th.ThreadState & ThreadState.WaitSleepJoin) != 0)
                            result++;
                    return result;
                };

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() =>
                {
                    startBar.SignalAndWait();
                    while (!tokenSrc.IsCancellationRequested)
                    {
                        inst.Wait();
                        SpinWaitHelper.SpinWait(1200);
                        Thread.Yield();
                    }
                    Interlocked.Increment(ref exitedCount);
                });

                threads[i].Start();
            }

            startBar.SignalAndWait();

            for (int i = 0; i < threads.Length; i++)
            {
                TimingAssert.AreEqual(5000, i, sleepThCount, "UP. Sleeping thread count != i");
                TimingAssert.AreEqual(5000, i, () => inst.RealWaiterCount, "UP. RealWaiterCount != i");
                TimingAssert.AreEqual(5000, i, () => inst.ExpectedWaiterCount, "UP. ExpectedWaiterCount != i");
                inst.AddExpectedWaiterCount(1);
                Thread.Sleep(10);
            }

            TimingAssert.AreEqual(5000, threads.Length, sleepThCount);
            TimingAssert.AreEqual(5000, threads.Length, () => inst.RealWaiterCount);
            TimingAssert.AreEqual(5000, threads.Length, () => inst.ExpectedWaiterCount);


            for (int i = threads.Length; i > 0; i--)
            {
                TimingAssert.AreEqual(5000, i, sleepThCount);
                TimingAssert.AreEqual(5000, i, () => inst.RealWaiterCount);
                TimingAssert.AreEqual(5000, i, () => inst.ExpectedWaiterCount);
                inst.SubstractExpectedWaiterCount(1);
                Thread.Sleep(10);
            }

            TimingAssert.AreEqual(5000, 0, sleepThCount, "Sleeping thread count != 0");
            TimingAssert.AreEqual(5000, 0, () => inst.RealWaiterCount, "RealWaiterCount != 0");
            TimingAssert.AreEqual(5000, 0, () => inst.ExpectedWaiterCount, "ExpectedWaiterCount != 0");


            tokenSrc.Cancel();

            TimingAssert.AreEqual(5000, threads.Length, () => Volatile.Read(ref exitedCount));
        }
    }
}
