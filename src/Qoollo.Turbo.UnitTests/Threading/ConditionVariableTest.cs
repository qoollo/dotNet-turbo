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
    public class ConditionVariableTest
    {
        [TestMethod]
        public void TestAlwaysPositivePredicate()
        {
            using (var testInst = new ConditionVariableBad())
            {
                int called = 0;
                bool result = testInst.Wait(() => { called++; return true; }, 100000);
                Assert.IsTrue(result);
                Assert.AreEqual(1, called);

                called = 0;
                result = testInst.Wait((s) => { called++; return true; }, new object(), 100000);
                Assert.IsTrue(result);
                Assert.AreEqual(1, called);

                called = 0;
                result = testInst.Wait((ref int s) => { s++; return true; }, ref called, 100000);
                Assert.IsTrue(result);
                Assert.AreEqual(1, called);
            }
        }


        [TestMethod]
        public void TestStatePassedCorrectly()
        {
            using (var testInst = new ConditionVariableBad())
            {
                object state = new object();
                bool result = testInst.Wait((s) => { Assert.AreEqual(state, s);  return true; }, state, 100000);
                Assert.IsTrue(result);

                result = testInst.Wait((ref object s) => { Assert.AreEqual(state, s); return true; }, ref state, 100000);
                Assert.IsTrue(result);
            }
        }


        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestExceptionPassedFromPredicate()
        {
            using (var testInst = new ConditionVariableBad())
            {
                try
                {
                    object state = new object();
                    bool result = testInst.Wait((s) => { throw new ApplicationException("test"); }, state, 100000);
                    Assert.IsTrue(result);
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
            using (var testInst = new ConditionVariableBad())
            {
                int result = 0;
                var task = Task.Run(() =>
                {
                    if (testInst.Wait(60000))
                        Interlocked.Exchange(ref result, 1);
                    else
                        Interlocked.Exchange(ref result, 2);
                });

                Thread.Sleep(100);
                Assert.AreEqual(0, Volatile.Read(ref result));

                testInst.Signal();
                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref result));
            }
        }


        [TestMethod]
        public void TestNotificationWithPredicateOverload1()
        {
            using (var testInst = new ConditionVariableBad())
            {
                int result = 0;
                int state = 0;
                var task = Task.Run(() =>
                {
                    if (testInst.Wait(() => Volatile.Read(ref state) > 0, 60000))
                        Interlocked.Exchange(ref result, 1);
                    else
                        Interlocked.Exchange(ref result, 2);
                });

                Thread.Sleep(100);
                Assert.AreEqual(0, Volatile.Read(ref result));

                testInst.Signal();
                Thread.Sleep(100);
                Assert.AreEqual(0, Volatile.Read(ref result));

                Interlocked.Increment(ref state);
                testInst.Signal();
                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref result));
            }
        }

        [TestMethod]
        public void TestNotificationWithPredicateOverload2()
        {
            using (var testInst = new ConditionVariableBad())
            {
                int result = 0;
                int state = 0;
                var task = Task.Run(() =>
                {
                    if (testInst.Wait((s) => Volatile.Read(ref state) > 0, new object(), 60000))
                        Interlocked.Exchange(ref result, 1);
                    else
                        Interlocked.Exchange(ref result, 2);
                });

                Thread.Sleep(100);
                Assert.AreEqual(0, Volatile.Read(ref result));

                testInst.Signal();
                Thread.Sleep(100);
                Assert.AreEqual(0, Volatile.Read(ref result));

                Interlocked.Increment(ref state);
                testInst.Signal();
                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref result));
            }
        }


        [TestMethod]
        public void TestNotificationWithPredicateOverload3()
        {
            using (var testInst = new ConditionVariableBad())
            {
                int result = 0;
                int state = 0;
                var task = Task.Run(() =>
                {
                    object tmp = new object();
                    if (testInst.Wait((ref object s) => Volatile.Read(ref state) > 0, ref tmp, 60000))
                        Interlocked.Exchange(ref result, 1);
                    else
                        Interlocked.Exchange(ref result, 2);
                });

                Thread.Sleep(100);
                Assert.AreEqual(0, Volatile.Read(ref result));

                testInst.Signal();
                Thread.Sleep(100);
                Assert.AreEqual(0, Volatile.Read(ref result));

                Interlocked.Increment(ref state);
                testInst.Signal();
                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref result));
            }
        }


        [TestMethod]
        public void TestTimeoutWorks()
        {
            using (var testInst = new ConditionVariableBad())
            {
                int result = 0;
                var task = Task.Run(() =>
                {
                    if (testInst.Wait(() => false, 500))
                        Interlocked.Exchange(ref result, 1);
                    else
                        Interlocked.Exchange(ref result, 2);
                });

                testInst.Signal();
                TimingAssert.AreEqual(10000, 2, () => Volatile.Read(ref result));
            }
        }


        [TestMethod]
        public void TestCancellationWorks()
        {
            using (var testInst = new ConditionVariableBad())
            {
                int result = 0;
                CancellationTokenSource tokenSrc = new CancellationTokenSource();
                var task = Task.Run(() =>
                {
                    try
                    {
                        if (testInst.Wait(() => false, 60000, tokenSrc.Token))
                            Interlocked.Exchange(ref result, 1);
                        else
                            Interlocked.Exchange(ref result, 2);
                    }
                    catch (OperationCanceledException)
                    {
                        Interlocked.Exchange(ref result, 3);
                    }
                });

                testInst.Signal();

                Thread.Sleep(100);
                Assert.AreEqual(0, Volatile.Read(ref result));

                tokenSrc.Cancel();
                TimingAssert.AreEqual(10000, 3, () => Volatile.Read(ref result));
            }
        }


        [TestMethod]
        public void TestLongPredicateEstimatesOnceWithSmallTimeout()
        {
            using (var testInst = new ConditionVariableBad())
            {
                int result = 0;
                int estimCount = 0;
                var task = Task.Run(() =>
                {
                    if (testInst.Wait(() => { Interlocked.Increment(ref estimCount); Thread.Sleep(500); return false; }, 200))
                        Interlocked.Exchange(ref result, 1);
                    else
                        Interlocked.Exchange(ref result, 2);
                });

                testInst.Signal();
                TimingAssert.AreEqual(10000, 2, () => Volatile.Read(ref result));
                Assert.AreEqual(1, Volatile.Read(ref estimCount));
            }
        }


        [TestMethod]
        public void TestSingleThreadWakeUpOnSignal()
        {
            using (var testInst = new ConditionVariableBad())
            {
                int exitCount = 0;
                int state = 0;
                List<Task> tasks = new List<Task>();
                for (int i = 0; i < 6; i++)
                {
                    var task = Task.Run(() =>
                    {
                        testInst.Wait(() => { return Volatile.Read(ref state) > 0; });
                        Interlocked.Increment(ref exitCount);
                    });
                    tasks.Add(task);
                }


                TimingAssert.AreEqual(10000, 6, () => testInst.WaiterCount);
                Interlocked.Increment(ref state);

                for (int i = 0; i < 6; i++)
                {
                    testInst.Signal();
                    TimingAssert.AreEqual(10000, 5 - i, () => testInst.WaiterCount);
                    Thread.Sleep(50);
                    TimingAssert.AreEqual(10000, i + 1, () => Volatile.Read(ref exitCount));
                }
            }
        }


        [TestMethod]
        public void TestAllThreadWakeUpOnSignalAll()
        {
            using (var testInst = new ConditionVariableBad())
            {
                int exitCount = 0;
                int state = 0;
                List<Task> tasks = new List<Task>();
                for (int i = 0; i < 6; i++)
                {
                    var task = Task.Run(() =>
                    {
                        testInst.Wait(() => { return Volatile.Read(ref state) > 0; });
                        Interlocked.Increment(ref exitCount);
                    });
                    tasks.Add(task);
                }


                TimingAssert.AreEqual(10000, 6, () => testInst.WaiterCount);
                Interlocked.Increment(ref state);

                testInst.SignalAll();
                TimingAssert.AreEqual(10000, 0, () => testInst.WaiterCount);
                TimingAssert.AreEqual(10000, 6, () => Volatile.Read(ref exitCount));
            }
        }

        [TestMethod]
        public void TestInterruptOnDispose()
        {
            using (var testInst = new ConditionVariableBad())
            {
                int exitCount = 0;
                var task = Task.Run(() =>
                {
                    try
                    {
                        testInst.Wait(() => false);
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



        // ==========================

        private class ThreadSafeQueue<T>
        {
            public object SharedSyncObj = new object();
            public ConditionVariable VarFull = null;
            public ConditionVariable VarEmpty = null;
            public Queue<T> Queue = new Queue<T>();
            public int MaxCount = 1000;

            public ThreadSafeQueue()
            {
                VarFull = new ConditionVariable(SharedSyncObj);
                VarEmpty = new ConditionVariable(SharedSyncObj);
            }

            public bool TryAdd(T value, int timeout, CancellationToken token)
            {
                lock (SharedSyncObj)
                {
                    if (VarFull.Wait(s => s.Queue.Count < s.MaxCount, this, timeout, token))
                    {
                        Queue.Enqueue(value);
                        VarEmpty.Pulse();
                        return true;
                    }

                    return false;
                }
            }

            public bool TryTake(out T value, int timeout, CancellationToken token)
            {
                lock (SharedSyncObj)
                {
                    if (VarEmpty.Wait(s => s.Queue.Count > 0, this, timeout, token))
                    {
                        value = Queue.Dequeue();
                        VarFull.Pulse();
                        return true;
                    }

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
