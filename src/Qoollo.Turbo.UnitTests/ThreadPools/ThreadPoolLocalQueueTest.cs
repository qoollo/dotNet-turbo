using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Threading.ThreadPools.ServiceStuff;
using System.Collections.Generic;
using System.Threading;
using Qoollo.Turbo.Threading.ThreadPools.Common;

namespace Qoollo.Turbo.UnitTests.ThreadPools
{
    [TestClass]
    public class ThreadPoolLocalQueueTest : TestClassBase
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
            public static bool operator != (TestThreadPoolItem a, TestThreadPoolItem b)
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
        public void TestSingleAddTake()
        {
            ThreadPoolLocalQueue q = new ThreadPoolLocalQueue();
            Assert.IsTrue(q.TryAddLocal(new TestThreadPoolItem(1)));
            ThreadPoolWorkItem item = null;
            Assert.IsTrue(q.TryTakeLocal(out item));
            Assert.IsNotNull(item);
            Assert2.AreEqual(1, item);
        }

        [TestMethod]
        public void TestSingleAddSteal()
        {
            ThreadPoolLocalQueue q = new ThreadPoolLocalQueue();
            Assert.IsTrue(q.TryAddLocal(new TestThreadPoolItem(1)));
            ThreadPoolWorkItem item = null;
            Assert.IsTrue(q.TrySteal(out item));
            Assert.IsNotNull(item);
            Assert2.AreEqual(1, item);
        }


        [TestMethod]
        public void TestAddTakeUpToTheLimit()
        {
            ThreadPoolLocalQueue q = new ThreadPoolLocalQueue();
            int index = 0;
            while (q.TryAddLocal(new TestThreadPoolItem(index)))
            {
                index++;
                Assert.IsTrue(index < int.MaxValue);
            }

            ThreadPoolWorkItem item = null;
            while (q.TryTakeLocal(out item))
            {
                Assert.IsNotNull(item);
                Assert.IsTrue((TestThreadPoolItem)item == --index);
            }

            Assert.AreEqual(0, index);
        }


        [TestMethod]
        public void TestAddStealUpToTheLimit()
        {
            ThreadPoolLocalQueue q = new ThreadPoolLocalQueue();
            int index = 0;
            while (q.TryAddLocal(new TestThreadPoolItem(index)))
            {
                index++;
                Assert.IsTrue(index < int.MaxValue);
            }

            int index2 = 0;
            ThreadPoolWorkItem item = null;
            while (q.TrySteal(out item))
            {
                Assert.IsNotNull(item);
                Assert.IsTrue((TestThreadPoolItem)item == index2++);
            }

            Assert.AreEqual(index, index2);
        }



        private void RunLocalThreadQueueAddTakeTest(ThreadPoolLocalQueue q, int elemCount, int fillFactor)
        {
            Random rnd = new Random();
            int addElem = 0;
            int takeElem = 0;

            List<int> takenIndexes = new List<int>(elemCount + 1);

            while (takeElem < elemCount)
            {
                int addCount = rnd.Next(fillFactor);
                for (int i = 0; i < addCount; i++)
                {
                    if (addElem >= elemCount || !q.TryAddLocal(new TestThreadPoolItem(addElem)))
                        break;
                    addElem++;
                }

                int removeCount = rnd.Next(fillFactor);
                for (int i = 0; i < removeCount; i++)
                {
                    ThreadPoolWorkItem tmp = null;
                    if (!q.TryTakeLocal(out tmp))
                        break;

                    Assert.IsNotNull(tmp);
                    takenIndexes.Add((TestThreadPoolItem)tmp);
                    takeElem++;
                }
            }

            Assert.AreEqual(elemCount, takenIndexes.Count);

            takenIndexes.Sort();

            for (int i = 0; i < elemCount; i++)
                Assert.AreEqual(i, takenIndexes[i]);
        }


        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void TestAddTakeManyElements()
        {
            ThreadPoolLocalQueue q = new ThreadPoolLocalQueue();
            RunLocalThreadQueueAddTakeTest(q, int.MaxValue / 200 + 13, 32);
        }

        [TestMethod]
        [Timeout(20 * 60 * 1000)]
        [Ignore]
        public void TestAddTakeManyElementsManyRun()
        {
            ThreadPoolLocalQueue q = new ThreadPoolLocalQueue();

            for (int i = 0; i < 100; i++)
                RunLocalThreadQueueAddTakeTest(q, int.MaxValue / 20 + 13, 32);
        }








        private void RunLocalThreadQueueAddStealTest(ThreadPoolLocalQueue q, int elemCount, int fillFactor)
        {
            Random rnd = new Random();
            int addElem = 0;
            int takeElem = 0;

            List<int> takenIndexes = new List<int>(elemCount + 1);

            while (takeElem < elemCount)
            {
                int addCount = rnd.Next(fillFactor);
                for (int i = 0; i < addCount; i++)
                {
                    if (addElem >= elemCount || !q.TryAddLocal(new TestThreadPoolItem(addElem)))
                        break;
                    addElem++;
                }

                int removeCount = rnd.Next(fillFactor);
                for (int i = 0; i < removeCount; i++)
                {
                    ThreadPoolWorkItem tmp = null;
                    if (!q.TrySteal(out tmp))
                        break;

                    Assert.IsNotNull(tmp);
                    takenIndexes.Add((TestThreadPoolItem)tmp);
                    takeElem++;
                }
            }

            Assert.AreEqual(elemCount, takenIndexes.Count);

            takenIndexes.Sort();

            for (int i = 0; i < elemCount; i++)
                Assert.AreEqual(i, takenIndexes[i]);
        }


        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void TestAddStealManyElements()
        {
            ThreadPoolLocalQueue q = new ThreadPoolLocalQueue();
            RunLocalThreadQueueAddStealTest(q, int.MaxValue / 200 + 13, 32);
        }

        [TestMethod]
        [Timeout(20 * 60 * 1000)]
        [Ignore]
        public void TestAddStealManyElementsManyRun()
        {
            ThreadPoolLocalQueue q = new ThreadPoolLocalQueue();

            for (int i = 0; i < 100; i++)
                RunLocalThreadQueueAddStealTest(q, int.MaxValue / 20 + 13, 32);
        }









        private void RunLocalThreadQueuePrimaryScenario(ThreadPoolLocalQueue q, int elemCount, int slealThCount, int fillFactor)
        {
            int trackElemCount = elemCount;
            int addFinished = 0;

            int atomicRandom = 0;

            Thread mainThread = null;
            Thread[] stealThreads = new Thread[slealThCount];

            List<int> global = new List<int>(elemCount + 1);

            Action mainAction = () =>
            {
                List<int> data = new List<int>(elemCount);

                Random rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * slealThCount);

                while (Volatile.Read(ref trackElemCount) >= 0)
                {
                    int addCount = fillFactor;
                    if (rnd != null)
                        addCount = rnd.Next(fillFactor);

                    for (int i = 0; i < addCount; i++)
                    {
                        int item = --trackElemCount;
                        if (item < 0)
                            break;
                        if (!q.TryAddLocal(new TestThreadPoolItem(item)))
                        {
                            ++trackElemCount;
                            break;
                        }
                    }

                    int removeCount = rnd.Next(fillFactor);

                    for (int i = 0; i < removeCount; i++)
                    {
                        ThreadPoolWorkItem item = null;
                        if (!q.TryTakeLocal(out item))
                            break;
                        data.Add((TestThreadPoolItem)item);
                    }
                }

                Interlocked.Increment(ref addFinished);

                ThreadPoolWorkItem finalItem = null;
                while (q.TryTakeLocal(out finalItem))
                    data.Add((TestThreadPoolItem)finalItem);

                lock (global)
                    global.AddRange(data);
            };

            Action stealAction = () =>
            {
                Random rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * slealThCount);

                List<int> data = new List<int>();

                while (Volatile.Read(ref addFinished) < 1 && Volatile.Read(ref trackElemCount) > elemCount / 1000)
                {
                    ThreadPoolWorkItem tmp;
                    if (q.TrySteal(out tmp))
                        data.Add((TestThreadPoolItem)tmp);

                    int sleepTime = rnd.Next(5) - 3;
                    if (sleepTime > 0)
                        Thread.Sleep(sleepTime);
                }

                lock (global)
                    global.AddRange(data);
            };


            mainThread = new Thread(new ThreadStart(mainAction));
            for (int i = 0; i < stealThreads.Length; i++)
                stealThreads[i] = new Thread(new ThreadStart(stealAction));


            mainThread.Start();
            for (int i = 0; i < stealThreads.Length; i++)
                stealThreads[i].Start();


            mainThread.Join();
            for (int i = 0; i < stealThreads.Length; i++)
                stealThreads[i].Join();


            Assert.AreEqual(elemCount, global.Count, "Incorrect element count");

            global.Sort();


            for (int i = 0; i < elemCount; i++)
                Assert.AreEqual(i, global[i], "Incorrect data");
        }


        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void TestLocalThreadQueuePrimaryScenario()
        {
            ThreadPoolLocalQueue q = new ThreadPoolLocalQueue();

            RunLocalThreadQueuePrimaryScenario(q, int.MaxValue / 400 + 13, Environment.ProcessorCount, 32);
            RunLocalThreadQueuePrimaryScenario(q, int.MaxValue / 400 + 13, Environment.ProcessorCount, 32);
        }

        [TestMethod]
        [Timeout(30 * 60 * 1000)]
        [Ignore]
        public void TestLocalThreadQueuePrimaryScenarioManyRun()
        {
            ThreadPoolLocalQueue q = new ThreadPoolLocalQueue();

            for (int i = 0; i < 200; i++)
                RunLocalThreadQueuePrimaryScenario(q, int.MaxValue / 40 + 13, (i % Environment.ProcessorCount) + 1, 32);
        }
    }
}
