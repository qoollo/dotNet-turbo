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
    public class ThreadPoolQueueControllerTest
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
            ThreadPoolQueueController q = new ThreadPoolQueueController(100, 1000);
            Assert.IsTrue(q.TryAdd(new TestThreadPoolItem(10), null, false, 0, CancellationToken.None));
            ThreadPoolWorkItem res = null;
            Assert.IsTrue(q.TryTake(null, out res, 0, CancellationToken.None, true));
            Assert2.AreEqual(10, res);
        }


        [TestMethod]
        public void TestManyAddTake()
        {
            ThreadPoolQueueController q = new ThreadPoolQueueController(100, 1000);

            for (int i = 0; i < 100; i++)
            {
                Assert.IsTrue(q.TryAdd(new TestThreadPoolItem(i), null, false, 0, CancellationToken.None));
            }
            Assert.IsFalse(q.TryAdd(new TestThreadPoolItem(int.MaxValue), null, false, 0, CancellationToken.None));

            for (int i = 0; i < 100; i++)
            {
                ThreadPoolWorkItem res = null;
                Assert.IsTrue(q.TryTake(null, out res, 0, CancellationToken.None, true));
                Assert2.AreEqual(i, res, "(TestThreadPoolItem)res == i");
            }

            ThreadPoolWorkItem tmp = null;
            Assert.IsFalse(q.TryTake(null, out tmp, 0, CancellationToken.None, true));
        }


        [TestMethod]
        public void TestAddLock()
        {
            ThreadPoolQueueController q = new ThreadPoolQueueController(100, 1000);

            for (int i = 0; i < 100; i++)
                q.Add(new TestThreadPoolItem(i), null);

            bool addCompleted = false;
            int startedFlag = 0;
            Task.Run(() =>
                {
                    Interlocked.Exchange(ref startedFlag, 1);
                    q.Add(new TestThreadPoolItem(int.MaxValue), null);
                    Volatile.Write(ref addCompleted, true);
                });

            TimingAssert.IsTrue(5000, () => Volatile.Read(ref startedFlag) == 1);
            Thread.Sleep(100);
            Assert.IsFalse(addCompleted);

            Assert2.AreEqual(0, q.Take(null));
            TimingAssert.IsTrue(5000, () => Volatile.Read(ref addCompleted));

            Assert.IsFalse(q.TryAdd(new TestThreadPoolItem(int.MinValue), null, false, 0, CancellationToken.None));

            for (int i = 1; i < 100; i++)
                Assert2.AreEqual(i, (TestThreadPoolItem)q.Take(null), "(TestThreadPoolItem)q.Take(null, null) == i");

            Assert2.AreEqual(int.MaxValue, q.Take(null), "(TestThreadPoolItem)q.Take(null, null) == int.MaxValue");

            Assert.AreEqual(0, q.GlobalQueue.OccupiedNodesCount);
        }


        [TestMethod]
        public void TestTakeLock()
        {
            ThreadPoolQueueController q = new ThreadPoolQueueController(100, 1000);

            bool takeCompleted = false;
            int takeItem = 0;
            int startedFlag = 0;
            Task.Run(() =>
            {
                Interlocked.Exchange(ref startedFlag, 1);
                takeItem = (TestThreadPoolItem)q.Take(null);
                Volatile.Write(ref takeCompleted, true);
            });

            TimingAssert.IsTrue(5000, () => Volatile.Read(ref startedFlag) == 1);
            Thread.Sleep(100);
            Assert.IsFalse(takeCompleted);

            q.Add(new TestThreadPoolItem(10), null);

            TimingAssert.IsTrue(5000, () => Volatile.Read(ref takeCompleted));
            Assert.AreEqual(10, takeItem);

            Assert.AreEqual(0, q.GlobalQueue.OccupiedNodesCount);
        }


        [TestMethod]
        public void TestAddCancellation()
        {
            ThreadPoolQueueController q = new ThreadPoolQueueController(100, 1000);
            CancellationTokenSource cSrc = new CancellationTokenSource();

            for (int i = 0; i < 100; i++)
                q.Add(new TestThreadPoolItem(i), null);

            bool addCompleted = false;
            int startedFlag = 0;
            Task.Run(() =>
            {
                try
                {
                    Interlocked.Exchange(ref startedFlag, 1);
                    q.TryAdd(new TestThreadPoolItem(int.MaxValue), null, false, -1, cSrc.Token);
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
                Assert2.AreEqual(i, q.Take(null));

            Assert.AreEqual(0, q.GlobalQueue.OccupiedNodesCount);
            Assert.AreEqual(100, q.GlobalQueue.FreeNodesCount);
        }


        [TestMethod]
        public void TestTakeCancellation()
        {
            ThreadPoolQueueController q = new ThreadPoolQueueController(100, 1000);
            CancellationTokenSource cSrc = new CancellationTokenSource();


            bool takeCompleted = false;
            ThreadPoolWorkItem takenItem = null;
            int startedFlag = 0;
            Task.Run(() =>
            {
                try
                {
                    Interlocked.Exchange(ref startedFlag, 1);
                    q.TryTake(null, out takenItem, -1, cSrc.Token, true);
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

            Assert.IsTrue(q.TryAdd(new TestThreadPoolItem(1), null, false, 0, CancellationToken.None));
            Assert2.AreEqual(1, q.Take(null));

            Assert.AreEqual(0, q.GlobalQueue.OccupiedNodesCount);
            Assert.AreEqual(100, q.GlobalQueue.FreeNodesCount);
        }

        [TestMethod]
        public void TestSilentTakeCancellation()
        {
            ThreadPoolQueueController q = new ThreadPoolQueueController(100, 1000);
            CancellationTokenSource cSrc = new CancellationTokenSource();


            bool takeCompleted = false;
            bool takeResult = false;
            ThreadPoolWorkItem takenItem = null;
            int startedFlag = 0;
            Task.Run(() =>
            {
                Interlocked.Exchange(ref startedFlag, 1);
                var res = q.TryTake(null, out takenItem, -1, cSrc.Token, false);
                Volatile.Write(ref takeResult, res);
                Volatile.Write(ref takeCompleted, true);
            });

            TimingAssert.IsTrue(5000, () => Volatile.Read(ref startedFlag) == 1);
            Thread.Sleep(100);
            Assert.IsFalse(takeCompleted);

            cSrc.Cancel();
            TimingAssert.IsTrue(5000, () => Volatile.Read(ref takeCompleted));
            Assert.IsFalse(takeResult);

            Assert.IsTrue(q.TryAdd(new TestThreadPoolItem(1), null, false, 0, CancellationToken.None));
            Assert2.AreEqual(1, q.Take(null));

            Assert.AreEqual(0, q.GlobalQueue.OccupiedNodesCount);
            Assert.AreEqual(100, q.GlobalQueue.FreeNodesCount);
        }


        [TestMethod]
        public void TestExtensionWork()
        {
            ThreadPoolQueueController q = new ThreadPoolQueueController(100, 1000);

            Assert.AreEqual(100, q.GlobalQueue.ExtendedCapacity);

            for (int i = 0; i < 100; i++)
                q.Add(new TestThreadPoolItem(i), null);

            bool addCompleted = false;
            int startedFlag = 0;
            Task.Run(() =>
            {
                Interlocked.Exchange(ref startedFlag, 1);
                q.Add(new TestThreadPoolItem(100), null);
                Volatile.Write(ref addCompleted, true);
            });

            TimingAssert.IsTrue(5000, () => Volatile.Read(ref startedFlag) == 1);
            Thread.Sleep(100);
            Assert.IsFalse(addCompleted);

            q.ExtendGlobalQueueCapacity(100);
            TimingAssert.IsTrue(5000, () => Volatile.Read(ref addCompleted));

            Assert.AreEqual(200, q.GlobalQueue.ExtendedCapacity);

            for (int i = 101; i < 200; i++)
                Assert.IsTrue(q.TryAdd(new TestThreadPoolItem(i), null, false, 0, CancellationToken.None));

            Assert.IsFalse(q.TryAdd(new TestThreadPoolItem(int.MaxValue), null, false, 0, CancellationToken.None));

            for (int i = 0; i < 200; i++)
                Assert2.AreEqual(i, q.Take(null));

            Assert.AreEqual(0, q.GlobalQueue.OccupiedNodesCount);
            Assert.AreEqual(100, q.GlobalQueue.ExtendedCapacity);
            Assert.AreEqual(100, q.GlobalQueue.FreeNodesCount);
        }


        [TestMethod]
        public void TestLocalThreadQueueUsage()
        {
            ThreadPoolLocalQueue local = new ThreadPoolLocalQueue();
            ThreadPoolQueueController q = new ThreadPoolQueueController(100, 1000);
            q.AddLocalQueue(local);

            q.Add(new TestThreadPoolItem(1), local);

            ThreadPoolWorkItem item = null;
            Assert.IsTrue(local.TryTakeLocal(out item));
            Assert2.AreEqual(1, item);

            local.TryAddLocal(new TestThreadPoolItem(1));

            item = null;
            Assert.IsTrue(q.TryTake(local, out item, 0, CancellationToken.None, true));
            Assert2.AreEqual(1, item);
        }

        [TestMethod]
        public void TestItemStealingWork()
        {
            ThreadPoolLocalQueue[] locQ = new ThreadPoolLocalQueue[]
            {
                new ThreadPoolLocalQueue(),
                new ThreadPoolLocalQueue(),
                new ThreadPoolLocalQueue(),
                new ThreadPoolLocalQueue(),
            };

            ThreadPoolQueueController q = new ThreadPoolQueueController(100, 1000);
            for (int i = 0; i < locQ.Length; i++)
                q.AddLocalQueue(locQ[i]);

            locQ[0].TryAddLocal(new TestThreadPoolItem(0));
            locQ[1].TryAddLocal(new TestThreadPoolItem(1));
            locQ[2].TryAddLocal(new TestThreadPoolItem(2));
            locQ[3].TryAddLocal(new TestThreadPoolItem(3));

            List<int> extractedItems = new List<int>();

            for (int i = 0; i < 4; i++)
            {
                ThreadPoolWorkItem item = null;
                Assert.IsTrue(q.TryTake(locQ[0], out item, 0, CancellationToken.None, true));
                extractedItems.Add((TestThreadPoolItem)item);
            }

            ThreadPoolWorkItem tmp = null;
            Assert.IsFalse(q.TryTake(locQ[0], out tmp, 0, CancellationToken.None, true));

            extractedItems.Sort();
            Assert.AreEqual(4, extractedItems.Count);
            for (int i = 0; i < extractedItems.Count; i++)
                Assert.AreEqual(i, extractedItems[i]);
        }

        [TestMethod]
        public void TestStealingWakeUp()
        {
            ThreadPoolLocalQueue[] locQ = new ThreadPoolLocalQueue[]
            {
                new ThreadPoolLocalQueue(),
                new ThreadPoolLocalQueue(),
                new ThreadPoolLocalQueue(),
                new ThreadPoolLocalQueue(),
            };

            ThreadPoolQueueController q = new ThreadPoolQueueController(100, 50);
            for (int i = 0; i < locQ.Length; i++)
                q.AddLocalQueue(locQ[i]);

            bool takeCompleted = false;
            List<int> takenItems = new List<int>();
            Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                    takenItems.Add((TestThreadPoolItem)q.Take(locQ[0]));
                takeCompleted = true;
            });

            Thread.Sleep(1000);
            Assert.IsFalse(takeCompleted);


            for (int i = 0; i < 100; i++)
            {
                locQ[1 + (i % (locQ.Length - 1))].TryAddLocal(new TestThreadPoolItem(i));
                Thread.Sleep(100);
            }

            Thread.Sleep(1000);
            Assert.IsTrue(takeCompleted);

            Assert.AreEqual(100, takenItems.Count);
            takenItems.Sort();

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(i, takenItems[i]);
        }


        [TestMethod]
        public void TestMoveFromLocalQueueToGlobal()
        {
            ThreadPoolQueueController q = new ThreadPoolQueueController(100, 50);
            ThreadPoolLocalQueue local = new ThreadPoolLocalQueue();

            for (int i = 0; i < 100; i++)
                q.Add(new TestThreadPoolItem(i), null);

            Assert.AreEqual(100, q.GlobalQueue.OccupiedNodesCount);
            Assert.AreEqual(0, q.GlobalQueue.FreeNodesCount);

            for (int i = 100; i < 110; i++)
                Assert.IsTrue(local.TryAddLocal(new TestThreadPoolItem(i)));

            q.MoveItemsFromLocalQueueToGlobal(local);
            Assert.AreEqual(0, q.GlobalQueue.FreeNodesCount);
            Assert.AreEqual(110, q.GlobalQueue.OccupiedNodesCount);
            Assert.AreEqual(110, q.GlobalQueue.ExtendedCapacity);

            for (int i = 0; i < 110; i++)
                Assert2.AreEqual(i, q.Take(null));

            Assert.AreEqual(100, q.GlobalQueue.FreeNodesCount);
            Assert.AreEqual(0, q.GlobalQueue.OccupiedNodesCount);
            Assert.AreEqual(100, q.GlobalQueue.ExtendedCapacity);
        }



        private void RunComplexTest(ThreadPoolQueueController q, int elemCount, int thCount)
        {
            int mainIndex = -1;
            int atomicRandom = 0;

            int trackElemCount = elemCount;
            int addFinished = 0;

            ThreadPoolLocalQueue[] locQ = new ThreadPoolLocalQueue[thCount];
            for (int i = 0; i < locQ.Length; i++)
            {
                locQ[i] = new ThreadPoolLocalQueue();
                q.AddLocalQueue(locQ[i]);
            }

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

                    q.Add(new TestThreadPoolItem(item), null);

                    int sleepTime = rnd.Next(1000);
                    if (sleepTime > 0)
                        Thread.SpinWait(sleepTime);

                    if (rnd.Next(100) == 0)
                        q.ExtendGlobalQueueCapacity(50);
                }

                Interlocked.Increment(ref addFinished);
            };

            
            Action mainAction = () =>
            {
                var localQ = locQ[Interlocked.Increment(ref mainIndex)];

                Random rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * thCount * 2);

                List<int> data = new List<int>();

                try
                {
                    while (Volatile.Read(ref addFinished) < thCount)
                    {
                        ThreadPoolWorkItem tmp = null;
                        if (q.TryTake(localQ, out tmp, -1, tokSrc.Token, true))
                            data.Add((TestThreadPoolItem)tmp);

                        int sleepTime = rnd.Next(500);
                        if (sleepTime > 0)
                            Thread.SpinWait(sleepTime);

                        if (rnd.Next(10) == 0)
                        {
                            int item = Interlocked.Decrement(ref trackElemCount);
                            if (item >= 0)
                            {
                                while (!q.TryAdd(new TestThreadPoolItem(item), localQ, false, 0, CancellationToken.None))
                                {
                                    if (q.TryTake(localQ, out tmp, 0, CancellationToken.None, true))
                                        data.Add((TestThreadPoolItem)tmp);
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { }

                ThreadPoolWorkItem tmp2;
                while (q.TryTake(localQ, out tmp2, 0, CancellationToken.None, true))
                    data.Add((TestThreadPoolItem)tmp2);

                lock (global)
                    global.AddRange(data);
            };

            Task.Delay(1000).ContinueWith(t => q.ExtendGlobalQueueCapacity(50));


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

            for (int i = 0; i < locQ.Length; i++)
            {
                q.RemoveLocalQueue(locQ[i]);
            }
        }


        [TestMethod]
        public void ComplexTest()
        {
            ThreadPoolQueueController q = new ThreadPoolQueueController(1000, 100);

            for (int i = 0; i < 5; i++)
                RunComplexTest(q, 5000000, Math.Max(1, Environment.ProcessorCount / 2));
        }
    }
}
