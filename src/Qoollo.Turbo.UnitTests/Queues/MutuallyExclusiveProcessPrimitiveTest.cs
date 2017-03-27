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

                   // Assert.IsFalse(inst.Open());
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

        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void TestGateOpenAndEnter()
        {
            const int AttemptCount =500000;
            using (var inst = new MutuallyExclusiveProcessPrimitive())
            {
                inst.RequestGate2Open();

                using (var outerGuard = inst.EnterGate2(0, default(CancellationToken)))
                {
                    Assert.IsTrue(outerGuard.IsAcquired);

                    Barrier bar = new Barrier(2);
                    int entered = 0;
                    var task = Task.Run(() =>
                    {
                        bar.SignalAndWait();
                        for (int i = 0; i < AttemptCount; i++)
                        {
                            using (var guard = inst.OpenAndEnterGate1(60000, default(CancellationToken)))
                            {
                                Assert.IsTrue(guard.IsAcquired);
                                Interlocked.Increment(ref entered);
                            }
                        }
                    });

                    bar.SignalAndWait();
                    Thread.Sleep(100);

                    outerGuard.Dispose();
                    while (Volatile.Read(ref entered) < AttemptCount)
                        inst.RequestGate2Open();

                    Assert.AreEqual(AttemptCount, Volatile.Read(ref entered));

                    task.Wait();
                }
            }
        }



        // ===================

        private void RunComplexTest(MutuallyExclusiveProcessPrimitive inst, int workCount, int workSpin, int sleepProbability)
        {
            Barrier barrier = new Barrier(4);
            CancellationTokenSource globalCancellation = new CancellationTokenSource();
            ManualResetEventSlim alwaysNotSet = new ManualResetEventSlim(false);
            int workPerformGate1 = 0;
            int workPerformGate2 = 0;
            int workCompletedCount = 0;

            int gate1Executed = 0;
            int gate2Executed = 0;

            Action<Random, CancellationToken, int> doWork = (Random rnd, CancellationToken token, int gate) =>
            {
                try
                {
                    if (gate == 1)
                    {
                        Interlocked.Increment(ref workPerformGate1);
                        if (Volatile.Read(ref workPerformGate2) != 0)
                            throw new Exception("Mutual exclusive logic is broken");

                        Interlocked.Increment(ref gate1Executed);
                    }
                    else
                    {
                        Interlocked.Increment(ref workPerformGate2);
                        if (Volatile.Read(ref workPerformGate1) != 0)
                            throw new Exception("Mutual exclusive logic is broken");

                        Interlocked.Increment(ref gate2Executed);
                    }

                    if (rnd.Next(sleepProbability) == 0)
                    {
                        alwaysNotSet.Wait(1, token);
                    }
                    else
                    {
                        int spin = rnd.Next(0, workSpin);
                        Thread.SpinWait(spin);
                    }
                }
                finally
                {
                    if (gate == 1)
                        Interlocked.Decrement(ref workPerformGate1);
                    else
                        Interlocked.Decrement(ref workPerformGate2);
                }
            };


            Action<int> worker = (int gate) =>
            {
                var token = globalCancellation.Token;
                Random rnd = new Random(Environment.TickCount + Thread.CurrentThread.ManagedThreadId);

                barrier.SignalAndWait();
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        using (var guard = gate == 1 ? inst.EnterGate1(Timeout.Infinite, token) : inst.EnterGate2(Timeout.Infinite, token))
                        {
                            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(token, guard.Token))
                            {
                                doWork(rnd, token, gate);
                            }
                        }
                    }
                    catch (OperationCanceledException) { }

                    int localWorkCompl = Interlocked.Increment(ref workCompletedCount);
                    if (localWorkCompl > workCount)
                        globalCancellation.Cancel();
                }
            };

            Action switcher = () =>
            {
                var token = globalCancellation.Token;
                Random rnd = new Random(Environment.TickCount + Thread.CurrentThread.ManagedThreadId);

                barrier.SignalAndWait();
                while (!token.IsCancellationRequested)
                {
                    int sleep = rnd.Next(workSpin);

                    Thread.Sleep(sleep);
                    inst.RequestGate1Open();

                    sleep = rnd.Next(workSpin);
                    Thread.Sleep(sleep);
                    inst.RequestGate2Open();
                }
            };

            Task workerThread1 = Task.Factory.StartNew(() => worker(1), TaskCreationOptions.LongRunning);
            Task workerThread2 = Task.Factory.StartNew(() => worker(2), TaskCreationOptions.LongRunning);
            Task workerThread3 = Task.Factory.StartNew(() => worker(2), TaskCreationOptions.LongRunning);
            Task switcherThread = Task.Factory.StartNew(() => switcher(), TaskCreationOptions.LongRunning);

            Task.WaitAll(workerThread1, workerThread2, workerThread3, switcherThread);
        }


        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void ComplexTest()
        {
            MutuallyExclusiveProcessPrimitive inst = new MutuallyExclusiveProcessPrimitive();
            //for (int i = 0; i < 10; i++)
            {
                RunComplexTest(inst, 100000, 200, 50);
                RunComplexTest(inst, 200000, 100, 100);
                RunComplexTest(inst, 50000, 500, 50);
            }
        }
    }
}
