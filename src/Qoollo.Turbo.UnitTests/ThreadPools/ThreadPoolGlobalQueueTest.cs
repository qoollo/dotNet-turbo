using Qoollo.Turbo.Threading.ThreadPools.Common;
using Qoollo.Turbo.Threading.ThreadPools.ServiceStuff;
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
    public class ThreadPoolGlobalQueueTest
    {
        private class TestThreadPoolItem : ThreadPoolWorkItem, IEquatable<TestThreadPoolItem>
        {
            public TestThreadPoolItem(int value) : base(true, false) { Value = value; }

            public int Value;

            protected override void RunInner()
            {
            }

            public bool Equals(TestThreadPoolItem other)
            {
                if (object.ReferenceEquals(other, null))
                    return false;

                return Value == other.Value;
            }
            public override bool Equals(object obj)
            {
                return Equals(obj as TestThreadPoolItem);
            }
            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }
            public static bool operator ==(TestThreadPoolItem a, TestThreadPoolItem b)
            {
                if (object.ReferenceEquals(a, null) && object.ReferenceEquals(b, null))
                    return true;

                if (object.ReferenceEquals(a, null) || object.ReferenceEquals(b, null))
                    return false;

                return a.Value == b.Value;
            }
            public static bool operator !=(TestThreadPoolItem a, TestThreadPoolItem b)
            {
                return !(a == b);
            }
            public static bool operator ==(TestThreadPoolItem a, int b)
            {
                if (object.ReferenceEquals(a, null))
                    return false;

                return a.Value == b;
            }
            public static bool operator !=(TestThreadPoolItem a, int b)
            {
                return !(a == b);
            }
            public static implicit operator TestThreadPoolItem(int val)
            {
                return new TestThreadPoolItem(val);
            }
            public static implicit operator int(TestThreadPoolItem val)
            {
                return val.Value;
            }
        }

        // =========================

        private static class Assert2
        {
            public static void AreEqual(int expected, ThreadPoolWorkItem actual)
            {
                Assert.IsNotNull(actual);
                Assert.IsInstanceOfType(actual, typeof(TestThreadPoolItem));
                Assert.AreEqual(expected, ((TestThreadPoolItem)actual).Value);
            }

            public static void AreEqual(int expected, ThreadPoolWorkItem actual, string message)
            {
                Assert.IsNotNull(actual, "Actual is NULL for: " + message);
                Assert.IsInstanceOfType(actual, typeof(TestThreadPoolItem), "Actual is not of type TestThreadPoolItem for: " + message);
                Assert.AreEqual(expected, ((TestThreadPoolItem)actual).Value, message);
            }
        }


        // =========================


        [TestMethod]
        public void TestSimpleAddTake()
        {
            ThreadPoolGlobalQueue q = new ThreadPoolGlobalQueue(100);
            Assert.IsTrue(q.TryAdd(new TestThreadPoolItem(10), 0, CancellationToken.None));
            ThreadPoolWorkItem res = null;
            Assert.IsTrue(q.TryTake(out res, 0, CancellationToken.None, true));
            Assert2.AreEqual(10, res);
        }


        [TestMethod]
        public void TestPropertiesCorrect()
        {
            ThreadPoolGlobalQueue q = new ThreadPoolGlobalQueue(100);

            Assert.IsTrue(q.IsBounded);
            Assert.AreEqual(100, q.BoundedCapacity);
            Assert.AreEqual(0, q.OccupiedNodesCount);
            Assert.AreEqual(100, q.FreeNodesCount);
            Assert.AreEqual(100, q.ExtendedCapacity);


            q.RequestCapacityExtension(100);
            Assert.AreEqual(200, q.ExtendedCapacity);

            q.Add(new TestThreadPoolItem(10));
            Assert.AreEqual(1, q.OccupiedNodesCount);
            Assert.AreEqual(200, q.ExtendedCapacity);
            Assert.AreEqual(199, q.FreeNodesCount);

            ThreadPoolWorkItem res = q.Take();
            Assert.AreEqual(0, q.OccupiedNodesCount);
            Assert.AreEqual(199, q.ExtendedCapacity);
            Assert.AreEqual(199, q.FreeNodesCount);
        }


        [TestMethod]
        public void TestManyAddTake()
        {
            ThreadPoolGlobalQueue q = new ThreadPoolGlobalQueue(100);

            for (int i = 0; i < 100; i++)
            {
                Assert.IsTrue(q.TryAdd(new TestThreadPoolItem(i), 0, CancellationToken.None));
            }
            Assert.IsFalse(q.TryAdd(new TestThreadPoolItem(int.MaxValue), 0, CancellationToken.None));

            for (int i = 0; i < 100; i++)
            {
                ThreadPoolWorkItem res = null;
                Assert.IsTrue(q.TryTake(out res, 0, CancellationToken.None, true));
                Assert2.AreEqual(i, res, "(TestThreadPoolItem)res == i");
            }

            ThreadPoolWorkItem tmp = null;
            Assert.IsFalse(q.TryTake(out tmp, 0, CancellationToken.None, true));
        }



        [TestMethod]
        public void TestAddLock()
        {
            ThreadPoolGlobalQueue q = new ThreadPoolGlobalQueue(100);

            for (int i = 0; i < 100; i++)
                q.Add(new TestThreadPoolItem(i));

            bool addCompleted = false;
            int startedFlag = 0;
            Task.Run(() =>
            {
                Interlocked.Exchange(ref startedFlag, 1);
                q.Add(new TestThreadPoolItem(int.MaxValue));
                Volatile.Write(ref addCompleted, true);
            });

            TimingAssert.IsTrue(10000, () => Volatile.Read(ref startedFlag) == 1);
            Thread.Sleep(100);
            Assert.IsFalse(addCompleted);

            Assert2.AreEqual(0, q.Take());
            TimingAssert.IsTrue(5000, () => Volatile.Read(ref addCompleted));

            Assert.IsFalse(q.TryAdd(new TestThreadPoolItem(int.MinValue), 0, CancellationToken.None));

            for (int i = 1; i < 100; i++)
                Assert2.AreEqual(i, (TestThreadPoolItem)q.Take(), "(TestThreadPoolItem)q.Take(null, null) == i");

            Assert2.AreEqual(int.MaxValue, q.Take(), "(TestThreadPoolItem)q.Take(null, null) == int.MaxValue");

            Assert.AreEqual(0, q.OccupiedNodesCount);
        }


        [TestMethod]
        public void TestTakeLock()
        {
            ThreadPoolGlobalQueue q = new ThreadPoolGlobalQueue(100);

            bool takeCompleted = false;
            int takeItem = 0;
            int startedFlag = 0;
            Task.Run(() =>
            {
                Interlocked.Exchange(ref startedFlag, 1);
                takeItem = (TestThreadPoolItem)q.Take();
                Volatile.Write(ref takeCompleted, true);
            });

            TimingAssert.IsTrue(10000, () => Volatile.Read(ref startedFlag) == 1);
            Thread.Sleep(100);
            Assert.IsFalse(takeCompleted);

            q.Add(new TestThreadPoolItem(10));

            TimingAssert.IsTrue(5000, () => Volatile.Read(ref takeCompleted));
            Assert.AreEqual(10, takeItem);

            Assert.AreEqual(0, q.OccupiedNodesCount);
        }


        [TestMethod]
        public void TestAddCancellation()
        {
            ThreadPoolGlobalQueue q = new ThreadPoolGlobalQueue(100);
            CancellationTokenSource cSrc = new CancellationTokenSource();

            for (int i = 0; i < 100; i++)
                q.Add(new TestThreadPoolItem(i));

            bool addCompleted = false;
            int startedFlag = 0;
            Task.Run(() =>
            {
                try
                {
                    Interlocked.Exchange(ref startedFlag, 1);
                    q.TryAdd(new TestThreadPoolItem(int.MaxValue), -1, cSrc.Token);
                }
                catch (OperationCanceledException)
                {

                }
                Volatile.Write(ref addCompleted, true);
            });

            TimingAssert.IsTrue(5000, () => Volatile.Read(ref startedFlag) == 1);
            Thread.Sleep(100);
            Assert.IsFalse(addCompleted);

            cSrc.Cancel();
            TimingAssert.IsTrue(5000, () => Volatile.Read(ref addCompleted));


            for (int i = 0; i < 100; i++)
                Assert2.AreEqual(i, q.Take());

            Assert.AreEqual(0, q.OccupiedNodesCount);
            Assert.AreEqual(100, q.FreeNodesCount);
        }



        [TestMethod]
        public void TestTakeCancellation()
        {
            ThreadPoolGlobalQueue q = new ThreadPoolGlobalQueue(100);
            CancellationTokenSource cSrc = new CancellationTokenSource();


            bool takeCompleted = false;
            ThreadPoolWorkItem takenItem = null;
            int startedFlag = 0;
            Task.Run(() =>
            {
                try
                {
                    Interlocked.Exchange(ref startedFlag, 1);
                    q.TryTake(out takenItem, -1, cSrc.Token, true);
                }
                catch (OperationCanceledException)
                {

                }
                Volatile.Write(ref takeCompleted, true);
            });

            TimingAssert.IsTrue(5000, () => Volatile.Read(ref startedFlag) == 1);
            Thread.Sleep(100);
            Assert.IsFalse(takeCompleted);

            cSrc.Cancel();
            TimingAssert.IsTrue(5000, () => Volatile.Read(ref takeCompleted));

            Assert.IsTrue(q.TryAdd(new TestThreadPoolItem(1), 0, CancellationToken.None));
            Assert2.AreEqual(1, q.Take());

            Assert.AreEqual(0, q.OccupiedNodesCount);
            Assert.AreEqual(100, q.FreeNodesCount);
        }


        [TestMethod]
        public void TestSilentTakeCancellation()
        {
            ThreadPoolGlobalQueue q = new ThreadPoolGlobalQueue(100);
            CancellationTokenSource cSrc = new CancellationTokenSource();


            bool takeCompleted = false;
            bool takeResult = false;
            ThreadPoolWorkItem takenItem = null;
            int startedFlag = 0;
            Task.Run(() =>
            {
                Interlocked.Exchange(ref startedFlag, 1);
                var res = q.TryTake(out takenItem, -1, cSrc.Token, false);
                Volatile.Write(ref takeResult, res);
                Volatile.Write(ref takeCompleted, true);
            });

            TimingAssert.IsTrue(5000, () => Volatile.Read(ref startedFlag) == 1);
            Thread.Sleep(100);
            Assert.IsFalse(takeCompleted);

            cSrc.Cancel();
            TimingAssert.IsTrue(5000, () => Volatile.Read(ref takeCompleted));
            Assert.IsFalse(takeResult);

            Assert.IsTrue(q.TryAdd(new TestThreadPoolItem(1), 0, CancellationToken.None));
            Assert2.AreEqual(1, q.Take());

            Assert.AreEqual(0, q.OccupiedNodesCount);
            Assert.AreEqual(100, q.FreeNodesCount);
        }



        [TestMethod]
        public void TestExtensionWork()
        {
            ThreadPoolGlobalQueue q = new ThreadPoolGlobalQueue(100);

            Assert.AreEqual(100, q.ExtendedCapacity);

            for (int i = 0; i < 100; i++)
                q.Add(new TestThreadPoolItem(i));

            bool addCompleted = false;
            int startedFlag = 0;
            Task.Run(() =>
            {
                Interlocked.Exchange(ref startedFlag, 1);
                q.Add(new TestThreadPoolItem(100));
                Volatile.Write(ref addCompleted, true);
            });

            TimingAssert.IsTrue(5000, () => Volatile.Read(ref startedFlag) == 1);
            Thread.Sleep(100);
            Assert.IsFalse(addCompleted);

            q.RequestCapacityExtension(100);
            TimingAssert.IsTrue(5000, () => Volatile.Read(ref addCompleted));

            Assert.AreEqual(200, q.ExtendedCapacity);

            for (int i = 101; i < 200; i++)
                Assert.IsTrue(q.TryAdd(new TestThreadPoolItem(i), 0, CancellationToken.None));

            Assert.IsFalse(q.TryAdd(new TestThreadPoolItem(int.MaxValue), 0, CancellationToken.None));

            for (int i = 0; i < 200; i++)
                Assert2.AreEqual(i, q.Take());

            Assert.AreEqual(0, q.OccupiedNodesCount);
            Assert.AreEqual(100, q.ExtendedCapacity);
            Assert.AreEqual(100, q.FreeNodesCount);
        }


        [TestMethod]
        public void TestForceAdd()
        {
            ThreadPoolGlobalQueue col = new ThreadPoolGlobalQueue(100);
            for (int i = 0; i < 100; i++)
                col.Add(new TestThreadPoolItem(i));

            Assert.AreEqual(100, col.OccupiedNodesCount);
            Assert.IsFalse(col.TryAdd(new TestThreadPoolItem(int.MaxValue), 0));

            col.ForceAdd(new TestThreadPoolItem(100));
            Assert.AreEqual(101, col.OccupiedNodesCount);
            Assert.AreEqual(100, col.BoundedCapacity);
            Assert.IsFalse(col.TryAdd(new TestThreadPoolItem(int.MaxValue), 0));

            Assert2.AreEqual(0, col.Take());
            Assert.AreEqual(100, col.OccupiedNodesCount);
            Assert.AreEqual(100, col.BoundedCapacity);
            Assert.IsFalse(col.TryAdd(new TestThreadPoolItem(int.MaxValue), 0));

            for (int i = 1; i < 101; i++)
                Assert2.AreEqual(i, col.Take());
        }



        private void RunComplexTest(ThreadPoolGlobalQueue q, int elemCount, int thCount)
        {
            int atomicRandom = 0;

            int trackElemCount = elemCount;
            int addFinished = 0;

            Thread[] threadsMain = new Thread[thCount];
            Thread[] threadsAdditional = new Thread[thCount];

            CancellationTokenSource tokSrc = new CancellationTokenSource();

            List<int> global = new List<int>(elemCount);

            Action additionalAction = () =>
            {
                Random rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * thCount * 2);

                while (true)
                {
                    int item = Interlocked.Decrement(ref trackElemCount);
                    if (item < 0)
                        break;

                    q.Add(new TestThreadPoolItem(item));

                    int sleepTime = rnd.Next(1000);
                    if (sleepTime > 0)
                        Thread.SpinWait(sleepTime);

                    if (rnd.Next(100) == 0)
                        q.RequestCapacityExtension(50);
                }

                Interlocked.Increment(ref addFinished);
            };


            Action mainAction = () =>
            {
                Random rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * thCount * 2);

                List<int> data = new List<int>();

                try
                {
                    while (Volatile.Read(ref addFinished) < thCount)
                    {
                        ThreadPoolWorkItem tmp = null;
                        if (q.TryTake(out tmp, -1, tokSrc.Token, true))
                            data.Add((TestThreadPoolItem)tmp);

                        int sleepTime = rnd.Next(500);
                        if (sleepTime > 0)
                            Thread.SpinWait(sleepTime);
                    }
                }
                catch (OperationCanceledException) { }

                ThreadPoolWorkItem tmp2;
                while (q.TryTake(out tmp2, 0, CancellationToken.None, true))
                    data.Add((TestThreadPoolItem)tmp2);

                lock (global)
                    global.AddRange(data);
            };

            Task.Delay(1000).ContinueWith(t => q.RequestCapacityExtension(50));


            for (int i = 0; i < threadsMain.Length; i++)
                threadsMain[i] = new Thread(new ThreadStart(mainAction));
            for (int i = 0; i < threadsAdditional.Length; i++)
                threadsAdditional[i] = new Thread(new ThreadStart(additionalAction));


            for (int i = 0; i < threadsMain.Length; i++)
                threadsMain[i].Start();
            for (int i = 0; i < threadsAdditional.Length; i++)
                threadsAdditional[i].Start();


            for (int i = 0; i < threadsAdditional.Length; i++)
                threadsAdditional[i].Join();
            tokSrc.Cancel();
            for (int i = 0; i < threadsMain.Length; i++)
                threadsMain[i].Join();


            Assert.AreEqual(elemCount, global.Count);
            global.Sort();

            for (int i = 0; i < elemCount; i++)
                Assert.AreEqual(i, global[i]);
        }


        [TestMethod]
        public void ComplexTest()
        {
            ThreadPoolGlobalQueue q = new ThreadPoolGlobalQueue(1000);

            for (int i = 0; i < 5; i++)
                RunComplexTest(q, 5000000, Math.Max(1, Environment.ProcessorCount / 2));
        }
    }
}
