using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Threading
{
    [TestClass]
    public class MonitorWaiterTest
    {
        [TestMethod]
        public void TestLockEnterExit()
        {
            using (var inst = new MonitorWaiter())
            {
                Assert.AreEqual(0, inst.WaiterCount);
                using (var wait = inst.Enter())
                {
                    Assert.AreEqual(1, inst.WaiterCount);
                    Assert.IsTrue(Monitor.IsEntered(inst));
                }
                Assert.AreEqual(0, inst.WaiterCount);
            }
        }


        [TestMethod]
        public void TestNotificationReceived()
        {
            using (var testInst = new MonitorWaiter())
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

                testInst.Pulse();
                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref result));

                task.Wait();
            }
        }


        [TestMethod]
        public void TestNotificationWithPredicate()
        {
            using (var testInst = new MonitorWaiter())
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

                TimingAssert.AreEqual(10000, 1, () => testInst.WaiterCount);
                Assert.AreEqual(0, Volatile.Read(ref result));

                testInst.Pulse();
                Thread.Sleep(100);
                Assert.AreEqual(0, Volatile.Read(ref result));

                Interlocked.Increment(ref state);
                testInst.Pulse();
                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref result));

                task.Wait();
            }
        }


        [TestMethod]
        public void TestNotificationWithPredicateRef()
        {
            using (var testInst = new MonitorWaiter())
            {
                int result = 0;
                int state = 0;
                var task = Task.Run(() =>
                {
                    using (var waiter = testInst.Enter(60000))
                    {
                        if (waiter.Wait((ref int s) => Volatile.Read(ref s) > 0, ref state))
                            Interlocked.Exchange(ref result, 1);
                        else
                            Interlocked.Exchange(ref result, 2);
                    }
                });

                TimingAssert.AreEqual(10000, 1, () => testInst.WaiterCount);
                Assert.AreEqual(0, Volatile.Read(ref result));

                testInst.Pulse();
                Thread.Sleep(100);
                Assert.AreEqual(0, Volatile.Read(ref result));

                Interlocked.Increment(ref state);
                testInst.Pulse();
                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref result));

                task.Wait();
            }
        }



        [TestMethod]
        public void TestExceptionFromPredicatePassed()
        {
            using (var testInst = new MonitorWaiter())
            {
                int result = 0;
                int state = 0;
                var task = Task.Run(() =>
                {
                    using (var waiter = testInst.Enter(60000))
                    {
                        if (waiter.Wait((s) => { if (Volatile.Read(ref state) > 0) throw new ApplicationException(); return false; }, new object()))
                            Interlocked.Exchange(ref result, 1);
                        else
                            Interlocked.Exchange(ref result, 2);
                    }
                });

                TimingAssert.AreEqual(10000, 1, () => testInst.WaiterCount);
                Assert.AreEqual(0, Volatile.Read(ref result));

                testInst.Pulse();
                Thread.Sleep(1);
                Assert.AreEqual(0, Volatile.Read(ref result));

                Interlocked.Increment(ref state);
                testInst.Pulse();
                TimingAssert.AreEqual(10000, 0, () => testInst.WaiterCount);
                Assert.AreEqual(0, result);

                try
                {
                    task.Wait();
                }
                catch (AggregateException aE)
                {
                    if (aE.InnerExceptions.Count != 1 || !(aE.InnerExceptions[0] is ApplicationException))
                        throw;
                }
            }
        }


        [TestMethod]
        public void TestTimeoutWorks()
        {
            using (var testInst = new MonitorWaiter())
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

                TimingAssert.AreEqual(10000, 1, () => testInst.WaiterCount);
                testInst.Pulse();
                TimingAssert.AreEqual(10000, 2, () => Volatile.Read(ref result));

                task.Wait();
            }
        }

        [TestMethod]
        public void TestCustomTimeoutWorks()
        {
            using (var testInst = new MonitorWaiter())
            {
                int result = 0;
                var task = Task.Run(() =>
                {
                    using (var waiter = testInst.Enter(60000))
                    {
                        if (waiter.Wait(100))
                            Interlocked.Exchange(ref result, 1);
                        else
                            Interlocked.Exchange(ref result, 2);
                    }
                });

                TimingAssert.AreEqual(10000, 1, () => testInst.WaiterCount);
                TimingAssert.AreEqual(10000, 2, () => Volatile.Read(ref result));

                task.Wait();
            }
        }


        [TestMethod]
        public void TestCancellationWorks()
        {
            using (var testInst = new MonitorWaiter())
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

                testInst.Pulse();

                Thread.Sleep(100);
                Assert.AreEqual(0, Volatile.Read(ref result));

                tokenSrc.Cancel();
                TimingAssert.AreEqual(10000, 3, () => Volatile.Read(ref result));
            }
        }


        [TestMethod]
        public void TestCancellationWorksInWaitNoParam()
        {
            using (var testInst = new MonitorWaiter())
            {
                int result = 0;
                CancellationTokenSource tokenSrc = new CancellationTokenSource();
                var task = Task.Run(() =>
                {
                    try
                    {
                        using (var waiter = testInst.Enter(60000, tokenSrc.Token))
                        {
                            while (!waiter.Wait()) { }
                            Interlocked.Exchange(ref result, 1);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        Interlocked.Exchange(ref result, 3);
                    }
                });

                testInst.Pulse();

                Thread.Sleep(100);
                Assert.AreEqual(0, Volatile.Read(ref result));

                tokenSrc.Cancel();
                TimingAssert.AreEqual(10000, 3, () => Volatile.Read(ref result));
            }
        }


        [TestMethod]
        public void TestInterruptOnDispose()
        {
            using (var testInst = new MonitorWaiter())
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
        public void TestSingleThreadWakeUpOnSignal()
        {
            using (var testInst = new MonitorWaiter())
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
                    testInst.Pulse();
                    TimingAssert.AreEqual(10000, 5 - i, () => testInst.WaiterCount);
                    Thread.Sleep(50);
                    TimingAssert.AreEqual(10000, i + 1, () => Volatile.Read(ref exitCount));
                }

                Task.WaitAll(tasks.ToArray());
            }
        }


        [TestMethod]
        public void TestAllThreadWakeUpOnSignalAll()
        {
            using (var testInst = new MonitorWaiter())
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

                testInst.PulseAll();
                TimingAssert.AreEqual(10000, 0, () => testInst.WaiterCount);
                TimingAssert.AreEqual(10000, 6, () => Volatile.Read(ref exitCount));

                Task.WaitAll(tasks.ToArray());
            }
        }


        // ==========================

        private class ThreadSafeQueue<T>
        {
            public MonitorWaiter WaiterFull = new MonitorWaiter("WaiterFull");
            public MonitorWaiter WaiterEmpty = new MonitorWaiter("WaiterEmpty");
            public ConcurrentQueue<T> Queue = new ConcurrentQueue<T>();
            public int ItemCount = 0;
            public int MaxCount = 1000;


            public bool TryAdd(T value, int timeout, CancellationToken token)
            {
                bool result = false;
                using (var waiter = WaiterFull.Enter(timeout, token))
                {
                    if (waiter.Wait(s => { Assert.IsTrue(Monitor.IsEntered(s.WaiterFull)); return s.ItemCount < s.MaxCount; }, this))
                    {
                        Assert.IsTrue(Monitor.IsEntered(WaiterFull));
                        Queue.Enqueue(value);
                        Interlocked.Increment(ref ItemCount);
                        result = true;
                    }
                    else
                    {
                        Assert.IsTrue(Monitor.IsEntered(WaiterFull));
                        result = false;
                    }
                }

                if (result)
                    WaiterEmpty.Pulse();


                return result;
            }

            public bool TryTake(out T value, int timeout, CancellationToken token)
            {
                bool result = false;
                using (var waiter = WaiterEmpty.Enter(timeout, token))
                {
                    if (waiter.Wait(s => { Assert.IsTrue(Monitor.IsEntered(s.WaiterEmpty)); return s.ItemCount > 0; }, this))
                    {
                        Assert.IsTrue(Monitor.IsEntered(WaiterEmpty));
                        bool dRes = Queue.TryDequeue(out value);
                        Assert.IsTrue(dRes);
                        Interlocked.Decrement(ref ItemCount);
                        result = true;
                    }
                    else
                    {
                        Assert.IsTrue(Monitor.IsEntered(WaiterEmpty));
                        value = default(T);
                        result = false;
                    }
                }

                if (result)
                    WaiterFull.Pulse();

                return result;
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

    }
}
