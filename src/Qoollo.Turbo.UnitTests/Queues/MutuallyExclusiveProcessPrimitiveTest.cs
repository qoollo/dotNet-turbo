using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Queues.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Queues
{
    [TestClass]
    public class MutuallyExclusiveProcessPrimitiveTest
    {
        [TestMethod]
        public void TestGateOpeningClosing()
        {
            int closeNotified = 0;
            using (var inst = new MutuallyExclusiveProcessGate(true, () => Interlocked.Increment(ref closeNotified)))
            {
                Assert.IsTrue(inst.IsOpened);
                Assert.IsFalse(inst.IsFullyClosed);
                Assert.IsFalse(inst.Token.IsCancellationRequested);
                Assert.AreEqual(0, inst.CurrentCount);

                using (var guard = inst.EnterClient(0, default(CancellationToken)))
                {
                    Assert.IsTrue(guard.IsAcquired);
                    Assert.AreEqual(1, inst.CurrentCount);

                    inst.Close();
                    Assert.AreEqual(0, closeNotified);

                    Assert.IsFalse(inst.IsOpened);
                    Assert.IsFalse(inst.IsFullyClosed);
                    Assert.IsTrue(inst.Token.IsCancellationRequested);

                    Assert.IsFalse(inst.Open());
                }

                Assert.AreEqual(0, inst.CurrentCount);
                Assert.IsFalse(inst.IsOpened);
                Assert.IsTrue(inst.IsFullyClosed);
                Assert.IsTrue(inst.Token.IsCancellationRequested);

                Assert.AreEqual(1, closeNotified);

                using (var guard = inst.EnterClient(0, default(CancellationToken)))
                {
                    Assert.IsFalse(guard.IsAcquired);
                }

                Assert.IsTrue(inst.Open());

                Assert.IsTrue(inst.IsOpened);
                Assert.IsFalse(inst.IsFullyClosed);
                Assert.IsFalse(inst.Token.IsCancellationRequested);
                Assert.AreEqual(0, inst.CurrentCount);

                using (var guard = inst.EnterClient(0, default(CancellationToken)))
                {
                    Assert.IsTrue(guard.IsAcquired);
                }
            }
        }


        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void TestDisposeInterruptWaiting()
        {
            using (var inst = new MutuallyExclusiveProcessGate(false, null))
            {
                var task = Task.Run(() =>
                {
                    Thread.Sleep(50);
                    inst.Dispose();
                });


                try
                {
                    using (var guard = inst.EnterClient(60000, default(CancellationToken)))
                    {
                    }
                }
                finally
                {
                    task.Wait();
                }
            }
        }

        [TestMethod]
        public void TestWaitingPassed()
        {
            using (var inst = new MutuallyExclusiveProcessGate(false, null))
            {
                Barrier bar = new Barrier(2);
                int result = 0;
                var task = Task.Run(() =>
                {
                    bar.SignalAndWait();
                    using (var guard = inst.EnterClient(60000, default(CancellationToken)))
                    {
                        if (guard.IsAcquired)
                            Interlocked.Exchange(ref result, 1);
                        else
                            Interlocked.Exchange(ref result, 2);
                    }
                });


                bar.SignalAndWait();
                Thread.Sleep(20);
                inst.Open();

                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref result));


                task.Wait();
            }
        }


        [TestMethod]
        public void TestCancellationWorks()
        {
            using (var inst = new MutuallyExclusiveProcessGate(false, null))
            {
                CancellationTokenSource tokSource = new CancellationTokenSource();
                Barrier bar = new Barrier(2);
                int result = 0;
                var task = Task.Run(() =>
                {
                    bar.SignalAndWait();
                    try
                    {
                        using (var guard = inst.EnterClient(60000, tokSource.Token))
                        {
                            if (guard.IsAcquired)
                                Interlocked.Exchange(ref result, 1);
                            else
                                Interlocked.Exchange(ref result, 2);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Interlocked.Exchange(ref result, 3);
                    }
                });


                bar.SignalAndWait();
                Thread.Sleep(20);
                tokSource.Cancel();

                TimingAssert.AreEqual(10000, 3, () => Volatile.Read(ref result));


                task.Wait();
            }
        }



        [TestMethod]
        public void TestMutuallExclusive()
        {
            using (var inst = new MutuallyExclusiveProcessPrimitive())
            {
                using (var guard = inst.EnterGate1(0, default(CancellationToken)))
                {
                    Assert.IsTrue(guard.IsAcquired);
                }
                using (var guard = inst.EnterGate2(0, default(CancellationToken)))
                {
                    Assert.IsFalse(guard.IsAcquired);
                }

                inst.RequestGate2Open();

                using (var guard = inst.EnterGate1(0, default(CancellationToken)))
                {
                    Assert.IsFalse(guard.IsAcquired);
                }
                using (var guard = inst.EnterGate2(0, default(CancellationToken)))
                {
                    Assert.IsTrue(guard.IsAcquired);
                }

                inst.RequestGate1Open();

                using (var guard = inst.EnterGate1(0, default(CancellationToken)))
                {
                    Assert.IsTrue(guard.IsAcquired);
                }
                using (var guard = inst.EnterGate2(0, default(CancellationToken)))
                {
                    Assert.IsFalse(guard.IsAcquired);
                }
            }
        }

        [TestMethod]
        public void TestMutuallExclusiveWaits()
        {
            using (var inst = new MutuallyExclusiveProcessPrimitive())
            {
                using (var guard = inst.EnterGate1(0, default(CancellationToken)))
                {
                    Assert.IsTrue(guard.IsAcquired);
                    inst.RequestGate2Open();

                    using (var guard2 = inst.EnterGate2(0, default(CancellationToken)))
                    {
                        Assert.IsFalse(guard2.IsAcquired);
                    }

                    var task = Task.Run(() =>
                    {
                        using (var guard2 = inst.EnterGate2(60000, default(CancellationToken)))
                        {
                            Assert.IsTrue(guard2.IsAcquired);
                        }
                    });


                    guard.Dispose();

                    task.Wait();
                }
            }
        }



        // ===================

        private void RunComplexTest(MutuallyExclusiveProcessPrimitive inst, int workCount, int workSpin)
        {
            int workPerformCount = 0;
            Random rnd = new Random();

            Action doWork = () =>
            {
                try
                {
                    int curWorkPerformCount = Interlocked.Increment(ref workPerformCount);
                    if (curWorkPerformCount != 1)
                        throw new Exception("Mutual exclusive is broken");

                    int spin = workSpin;
                    lock (rnd)
                        spin = rnd.Next(0, workSpin);

                    Thread.SpinWait(spin);
                }
                finally
                {
                    Interlocked.Decrement(ref workPerformCount);
                }
            };

        }
    }
}
