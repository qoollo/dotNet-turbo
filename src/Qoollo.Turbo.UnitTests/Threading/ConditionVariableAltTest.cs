using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Threading
{
    [TestClass]
    public class ConditionVariableAltTest
    {
        [TestMethod]
        public void TestAlwaysPositivePredicate()
        {
            object syncObj = new object();
            using (var testInst = new ConditionVariableAlt(syncObj))
            {
                using (var waiter = testInst.Enter(100000))
                {
                    int called = 0;
                    bool result = waiter.Wait((s) => { called++; return true; }, new object());
                    Assert.IsTrue(result);
                    Assert.AreEqual(1, called);
                }
            }
        }


        [TestMethod]
        public void TestStatePassedCorrectly()
        {
            object syncObj = new object();
            using (var testInst = new ConditionVariableAlt(syncObj))
            {
                using (var waiter = testInst.Enter(100000))
                {
                    object state = new object();
                    bool result = waiter.Wait((s) => { Assert.AreEqual(state, s); return true; }, state);
                    Assert.IsTrue(result);
                }
            }
        }


        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestExceptionPassedFromPredicate()
        {
            object syncObj = new object();
            using (var testInst = new ConditionVariableAlt(syncObj))
            {
                try
                {
                    using (var waiter = testInst.Enter(100000))
                    {
                        try
                        {
                            object state = new object();
                            bool result = waiter.Wait((s) => { throw new ApplicationException("test"); }, state);
                            Assert.IsTrue(result);
                        }
                        catch (Exception)
                        {
                            Assert.AreEqual(1, testInst.WaiterCount);
                            throw;
                        }
                    }
                }
                catch (Exception)
                {
                    Assert.AreEqual(0, testInst.WaiterCount);
                    throw;
                }
            }
        }


        [TestMethod]
        public void TestNotificationReceived()
        {
            object syncObj = new object();
            using (var testInst = new ConditionVariableAlt(syncObj))
            {
                int result = 0;
                var task = Task.Run(() =>
                {
                    using (var waiter = testInst.Enter(60000))
                    {
                        if (waiter.Wait())
                            Interlocked.Exchange(ref result, 1);
                        else
                            Interlocked.Exchange(ref result, 2);
                    }
                });

                TimingAssert.AreEqual(10000, 1, () => testInst.WaiterCount);
                Assert.AreEqual(0, Volatile.Read(ref result));

                lock (syncObj)
                {
                    testInst.Pulse();
                }
                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref result));
            }
        }



        [TestMethod]
        public void TestNotificationWithPredicate()
        {
            object syncObj = new object();
            using (var testInst = new ConditionVariableAlt(syncObj))
            {
                int result = 0;
                int state = 0;
                var task = Task.Run(() =>
                {
                    using (var waiter = testInst.Enter(60000))
                    {
                        if (waiter.Wait((s) => Volatile.Read(ref state) > 0, new object()))
                            Interlocked.Exchange(ref result, 1);
                        else
                            Interlocked.Exchange(ref result, 2);
                    }
                });

                Thread.Sleep(100);
                Assert.AreEqual(0, Volatile.Read(ref result));

                lock (syncObj)
                {
                    testInst.Pulse();
                }
                Thread.Sleep(100);
                Assert.AreEqual(0, Volatile.Read(ref result));

                Interlocked.Increment(ref state);
                lock (syncObj)
                {
                    testInst.Pulse();
                }
                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref result));
            }
        }



        [TestMethod]
        public void TestTimeoutWorks()
        {
            object syncObj = new object();
            using (var testInst = new ConditionVariableAlt(syncObj))
            {
                int result = 0;
                var task = Task.Run(() =>
                {
                    using (var waiter = testInst.Enter(500))
                    {
                        if (waiter.Wait(_ => false, (object)null))
                            Interlocked.Exchange(ref result, 1);
                        else
                            Interlocked.Exchange(ref result, 2);
                    }
                });

                lock (syncObj)
                {
                    testInst.Pulse();
                }
                TimingAssert.AreEqual(10000, 2, () => Volatile.Read(ref result));
            }
        }


        [TestMethod]
        public void TestCancellationWorks()
        {
            object syncObj = new object();
            using (var testInst = new ConditionVariableAlt(syncObj))
            {
                int result = 0;
                CancellationTokenSource tokenSrc = new CancellationTokenSource();
                var task = Task.Run(() =>
                {
                    try
                    {
                        using (var waiter = testInst.Enter(60000, tokenSrc.Token))
                        {
                            if (waiter.Wait(_ => false, (object)null))
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


                TimingAssert.AreEqual(10000, 1, () => testInst.WaiterCount);
                lock (syncObj)
                {
                    testInst.Pulse();
                }
                Assert.AreEqual(0, Volatile.Read(ref result));

                Thread.Sleep(100);
                tokenSrc.Cancel();
                TimingAssert.AreEqual(10000, 3, () => Volatile.Read(ref result));
            }
        }


        [TestMethod]
        public void TestLongPredicateEstimatesOnceWithSmallTimeout()
        {
            object syncObj = new object();
            using (var testInst = new ConditionVariableAlt(syncObj))
            {
                int result = 0;
                int estimCount = 0;
                var task = Task.Run(() =>
                {
                    using (var waiter = testInst.Enter(200))
                    {
                        if (waiter.Wait(_ => { Interlocked.Increment(ref estimCount); Thread.Sleep(500); return false; }, (object)null))
                            Interlocked.Exchange(ref result, 1);
                        else
                            Interlocked.Exchange(ref result, 2);
                    }
                });

                lock (syncObj)
                {
                    testInst.Pulse();
                }
                TimingAssert.AreEqual(10000, 2, () => Volatile.Read(ref result));
                Assert.AreEqual(1, Volatile.Read(ref estimCount));
            }
        }


        [TestMethod]
        public void TestSingleThreadWakeUpOnSignal()
        {
            object syncObj = new object();
            using (var testInst = new ConditionVariableAlt(syncObj))
            {
                int exitCount = 0;
                int state = 0;
                List<Task> tasks = new List<Task>();
                for (int i = 0; i < 6; i++)
                {
                    var task = Task.Run(() =>
                    {
                        using (var waiter = testInst.Enter())
                        {
                            waiter.Wait(_ => { return Volatile.Read(ref state) > 0; }, (object)null);
                            Interlocked.Increment(ref exitCount);
                        }
                    });
                    tasks.Add(task);
                }


                TimingAssert.AreEqual(10000, 6, () => testInst.WaiterCount);
                Interlocked.Increment(ref state);

                for (int i = 0; i < 6; i++)
                {
                    lock (syncObj)
                    {
                        testInst.Pulse();
                    }
                    TimingAssert.AreEqual(10000, 5 - i, () => testInst.WaiterCount);
                    Thread.Sleep(50);
                    TimingAssert.AreEqual(10000, i + 1, () => Volatile.Read(ref exitCount));
                }
            }
        }


        [TestMethod]
        public void TestAllThreadWakeUpOnSignalAll()
        {
            object syncObj = new object();
            using (var testInst = new ConditionVariableAlt(syncObj))
            {
                int exitCount = 0;
                int state = 0;
                List<Task> tasks = new List<Task>();
                for (int i = 0; i < 6; i++)
                {
                    var task = Task.Run(() =>
                    {
                        using (var waiter = testInst.Enter())
                        {
                            waiter.Wait(_ => { return Volatile.Read(ref state) > 0; }, (object)null);
                            Interlocked.Increment(ref exitCount);
                        }
                    });
                    tasks.Add(task);
                }


                TimingAssert.AreEqual(10000, 6, () => testInst.WaiterCount);
                Interlocked.Increment(ref state);

                lock (syncObj)
                {
                    testInst.PulseAll();
                }
                TimingAssert.AreEqual(10000, 0, () => testInst.WaiterCount);
                TimingAssert.AreEqual(10000, 6, () => Volatile.Read(ref exitCount));
            }
        }

        [TestMethod]
        public void TestInterruptOnDispose()
        {
            object syncObj = new object();
            using (var testInst = new ConditionVariableAlt(syncObj))
            {
                int exitCount = 0;
                var task = Task.Run(() =>
                {
                    try
                    {
                        using (var waiter = testInst.Enter())
                        {
                            waiter.Wait(_ => false, (object)null);
                        }
                    }
                    catch (OperationInterruptedException) { }
                    Interlocked.Increment(ref exitCount);
                });


                TimingAssert.AreEqual(10000, 1, () => testInst.WaiterCount);

                testInst.Dispose();
                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref exitCount));

                task.Wait();
            }
        }

        [TestMethod]
        public void TestPredicateCalledInsideLock()
        {
            object syncObj = new object();
            using (var testInst = new ConditionVariableAlt(syncObj))
            {
                int result = 0;
                int estimCount = 0;
                int inMonitorCount = 0;
                int stopEstim = 0;
                var task = Task.Run(() =>
                {
                    using (var waiter = testInst.Enter(60000))
                    {
                        if (waiter.Wait(_ => 
                        {
                            if (Monitor.IsEntered(syncObj))
                                Interlocked.Increment(ref inMonitorCount);
                            Interlocked.Increment(ref estimCount);
                            return Volatile.Read(ref stopEstim) != 0;
                        }, (object)null))
                            Interlocked.Exchange(ref result, 1);
                        else
                            Interlocked.Exchange(ref result, 2);
                    }
                });

                TimingAssert.AreEqual(10000, 1, () => testInst.WaiterCount);
                for (int i = 0; i < 20; i++)
                {
                    lock (syncObj)
                    {
                        testInst.Pulse();
                    }
                    Thread.Sleep(10);
                }
                Interlocked.Increment(ref stopEstim);
                lock (syncObj)
                {
                    testInst.Pulse();
                }

                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref result));
                Assert.IsTrue(Volatile.Read(ref estimCount) > 1);
                Assert.AreEqual(Volatile.Read(ref inMonitorCount), Volatile.Read(ref estimCount));
            }
        }

        [TestMethod]
        [ExpectedException(typeof(SynchronizationLockException))]
        public void TestWaitThrowsIfRecursiveEnter()
        {
            object syncObj = new object();
            using (var testInst = new ConditionVariableAlt(syncObj))
            {
                using (var waiter2 = testInst.Enter(10))
                {
                    using (var waiter = testInst.Enter(10))
                    {
                        waiter.Wait();
                    }
                }
            }
        }
        [TestMethod]
        [ExpectedException(typeof(SynchronizationLockException))]
        public void TestPulseThrowsIfExternalLockNotAcquired()
        {
            object syncObj = new object();
            using (var testInst = new ConditionVariableAlt(syncObj))
            {
                testInst.Pulse();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(SynchronizationLockException))]
        public void TestWaitThrowsIfExternalLockTakenRecursively()
        {
            object syncObj = new object();
            using (var testInst = new ConditionVariableAlt(syncObj))
            {
                lock (syncObj)
                {
                    using (var waiter = testInst.Enter(10))
                    {
                        waiter.Wait();
                    }
                }
            }
        }
        [TestMethod]
        [ExpectedException(typeof(SynchronizationLockException))]
        public void TestWaitWithPredicateThrowsIfExternalLockTakenRecursively()
        {
            object syncObj = new object();
            using (var testInst = new ConditionVariableAlt(syncObj))
            {
                lock (syncObj)
                {
                    using (var waiter = testInst.Enter(10))
                    {
                        waiter.Wait(_ => false, (object)null);
                    }
                }
            }
        }


        [TestMethod]
        public void TestPredicateCalledTwiceOnDelayedSuccess()
        {
            object syncObj = new object();
            using (var testInst = new ConditionVariableAlt(syncObj))
            {
                int result = 0;
                int state = 0;
                int called = 0;
                var task = Task.Run(() =>
                {
                    using (var waiter = testInst.Enter(60000))
                    {
                        if (waiter.Wait((s) => { Interlocked.Increment(ref called); return Volatile.Read(ref state) > 0; }, new object()))
                            Interlocked.Exchange(ref result, 1);
                        else
                            Interlocked.Exchange(ref result, 2);
                    }
                });

                TimingAssert.AreEqual(10000, 1, () => testInst.WaiterCount);
                Assert.AreEqual(0, Volatile.Read(ref result));
                Interlocked.Increment(ref state);
                lock (syncObj)
                {
                    testInst.Pulse();
                }
                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref result));
                TimingAssert.AreEqual(10000, 2, () => Volatile.Read(ref called));
            }
        }
        [TestMethod]
        public void TestPredicateCalledOnTimeout()
        {
            object syncObj = new object();
            using (var testInst = new ConditionVariableAlt(syncObj))
            {
                int result = 0;
                int state = 0;
                int called = 0;
                var task = Task.Run(() =>
                {
                    using (var waiter = testInst.Enter(100))
                    {
                        if (waiter.Wait((s) => { Interlocked.Increment(ref called); return Volatile.Read(ref state) > 0; }, new object()))
                            Interlocked.Exchange(ref result, 1);
                        else
                            Interlocked.Exchange(ref result, 2);
                    }
                });

                TimingAssert.AreEqual(10000, 1, () => testInst.WaiterCount);
                TimingAssert.AreEqual(10000, 2, () => Volatile.Read(ref result));
                TimingAssert.AreEqual(10000, 2, () => Volatile.Read(ref called));
            }
        }

    


        // ==========================

        private class ThreadSafeQueue<T>
        {
            public object SharedSyncObj = new object();
            public ConditionVariableAlt VarFull = null;
            public ConditionVariableAlt VarEmpty = null;
            public Queue<T> Queue = new Queue<T>();
            public int MaxCount = 1000;

            public ThreadSafeQueue()
            {
                VarFull = new ConditionVariableAlt(SharedSyncObj);
                VarEmpty = new ConditionVariableAlt(SharedSyncObj);
            }

            public bool TryAdd(T value, int timeout, CancellationToken token)
            {
                using (var waiter = VarFull.Enter(timeout, token))
                {
                    if (waiter.Wait(s => { Assert.IsTrue(Monitor.IsEntered(s.SharedSyncObj)); return s.Queue.Count < s.MaxCount; }, this))
                    {
                        Assert.IsTrue(Monitor.IsEntered(SharedSyncObj));
                        Queue.Enqueue(value);
                        VarEmpty.Pulse();
                        return true;
                    }

                    Assert.IsTrue(Monitor.IsEntered(SharedSyncObj));
                    return false;
                }
            }

            public bool TryTake(out T value, int timeout, CancellationToken token)
            {
                using (var waiter = VarEmpty.Enter(timeout, token))
                {
                    if (waiter.Wait(s => { Assert.IsTrue(Monitor.IsEntered(s.SharedSyncObj)); return s.Queue.Count > 0; }, this))
                    {
                        Assert.IsTrue(Monitor.IsEntered(SharedSyncObj));
                        value = Queue.Dequeue();
                        VarFull.Pulse();
                        return true;
                    }

                    Assert.IsTrue(Monitor.IsEntered(SharedSyncObj));
                    value = default(T);
                    return false;
                }
            }
        }



        private void RunComplexTest(ThreadSafeQueue<int> q, int elemCount, int thCount)
        {
            int atomicRandom = 0;

            int trackElemCount = elemCount;
            int addFinished = 0;

            Thread[] threadsTake = new Thread[thCount];
            Thread[] threadsAdd = new Thread[thCount];

            CancellationTokenSource tokSrc = new CancellationTokenSource();

            List<int> global = new List<int>(elemCount);

            Action addAction = () =>
            {
                Random rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * thCount * 2);

                while (true)
                {
                    int item = Interlocked.Decrement(ref trackElemCount);
                    if (item < 0)
                        break;

                    q.TryAdd(item, -1, default(CancellationToken));

                    int sleepTime = rnd.Next(100);

                    if (sleepTime > 0)
                        Thread.SpinWait(sleepTime);
                }

                Interlocked.Increment(ref addFinished);
            };


            Action takeAction = () =>
            {
                Random rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * thCount * 2);

                List<int> data = new List<int>();

                try
                {
                    while (Volatile.Read(ref addFinished) < thCount)
                    {
                        int tmp = 0;
                        if (q.TryTake(out tmp, -1, tokSrc.Token))
                            data.Add((int)tmp);

                        int sleepTime = rnd.Next(100);
                        if (sleepTime > 0)
                            Thread.SpinWait(sleepTime);
                    }
                }
                catch (OperationCanceledException) { }

                int tmp2;
                while (q.TryTake(out tmp2, 0, default(CancellationToken)))
                    data.Add((int)tmp2);

                lock (global)
                    global.AddRange(data);
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


            Assert.AreEqual(elemCount, global.Count);
            global.Sort();

            for (int i = 0; i < elemCount; i++)
                Assert.AreEqual(i, global[i]);
        }


        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void ComplexTest()
        {
            ThreadSafeQueue<int> q = new ThreadSafeQueue<int>();
            q.MaxCount = 2003;

            for (int i = 0; i < 10; i++)
                RunComplexTest(q, 500000, Math.Max(1, Environment.ProcessorCount / 2) + (i % 4));
        }


        //[TestMethod]
        public void ComplexTestSlow()
        {
            ThreadSafeQueue<int> q = new ThreadSafeQueue<int>();
            Random rnd = new Random();
            q.MaxCount = 2003;

            for (int i = 0; i < 10; i++)
            {
                q.MaxCount = rnd.Next(1000, 10000);
                RunComplexTest(q, int.MaxValue / 200, Math.Max(1, Environment.ProcessorCount / 2) + (i % 4) + rnd.Next(1));
            }
        }
    }
}
