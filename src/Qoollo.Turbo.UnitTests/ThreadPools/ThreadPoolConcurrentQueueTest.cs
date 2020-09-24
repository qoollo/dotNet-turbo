using Qoollo.Turbo.Threading.ThreadPools.Common;
using Qoollo.Turbo.Threading.ThreadPools.ServiceStuff;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Turbo.Threading.ServiceStuff;

namespace Qoollo.Turbo.UnitTests.ThreadPools
{
    [TestClass]
    public class ThreadPoolConcurrentQueueTest : TestClassBase
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
        public void TestSingleAddTake()
        {
            ThreadPoolConcurrentQueue q = new ThreadPoolConcurrentQueue();

            q.Add(new TestThreadPoolItem(10));
            ThreadPoolWorkItem res = null;
            Assert.IsTrue(q.TryTake(out res));
            Assert.IsNotNull(res);
            Assert2.AreEqual(10, res);
        }

        [TestMethod]
        public void TestManyAddTake()
        {
            ThreadPoolConcurrentQueue q = new ThreadPoolConcurrentQueue();

            for (int i = 0; i < 100000; i++)
            {
                q.Add(new TestThreadPoolItem(i));
            }

            for (int i = 0; i < 100000; i++)
            {
                ThreadPoolWorkItem res = null;
                Assert.IsTrue(q.TryTake(out res));
                Assert.IsNotNull(res);
                Assert2.AreEqual(i, res);
            }
        }

        [TestMethod]
        public void TestIsEmpty()
        {
            ThreadPoolConcurrentQueue q = new ThreadPoolConcurrentQueue();
            Assert.IsTrue(q.IsEmpty);

            for (int i = 0; i < 10000; i++)
            {
                q.Add(new TestThreadPoolItem(i));
                Assert.IsFalse(q.IsEmpty);
            }

            for (int i = 0; i < 10000; i++)
            {
                Assert.IsFalse(q.IsEmpty);
                ThreadPoolWorkItem res = null;
                Assert.IsTrue(q.TryTake(out res));   
            }

            Assert.IsTrue(q.IsEmpty);


            ThreadPoolWorkItem tmp = null;
            Assert.IsFalse(q.TryTake(out tmp));
            Assert.IsNull(tmp);
            Assert.IsTrue(q.IsEmpty);
        }







        private void RunThreadPoolConcurrentQueueTest(ThreadPoolConcurrentQueue q, int elemCount, int thCount)
        {
            int atomicRandom = 0;

            int trackElemCount = elemCount;
            int addFinished = 0;

            Thread[] threadsAdd = new Thread[thCount];
            Thread[] threadsRemove = new Thread[thCount];

            List<int> global = new List<int>(elemCount);

            Action addAction = () =>
            {
                Random rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * thCount * 2);

                while (true)
                {
                    int item = Interlocked.Decrement(ref trackElemCount);
                    if (item < 0)
                        break;

                    q.Add(new TestThreadPoolItem(item));

                    int sleepTime = rnd.Next(120);
                    if (sleepTime > 0)
                        SpinWaitHelper.SpinWait(sleepTime);
                }

                Interlocked.Increment(ref addFinished);
            };

            Action removeAction = () =>
            {
                Random rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * thCount * 2);

                List<int> data = new List<int>();

                while (Volatile.Read(ref addFinished) < thCount)
                {
                    ThreadPoolWorkItem tmp;
                    if (q.TryTake(out tmp))
                        data.Add((TestThreadPoolItem)tmp);

                    int sleepTime = rnd.Next(120);
                    if (sleepTime > 0)
                        SpinWaitHelper.SpinWait(sleepTime);
                }

                ThreadPoolWorkItem tmp2;
                while (q.TryTake(out tmp2))
                    data.Add((TestThreadPoolItem)tmp2);

                lock (global)
                    global.AddRange(data);
            };


            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i] = new Thread(new ThreadStart(addAction));

            for (int i = 0; i < threadsRemove.Length; i++)
                threadsRemove[i] = new Thread(new ThreadStart(removeAction));


            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i].Start();
            for (int i = 0; i < threadsRemove.Length; i++)
                threadsRemove[i].Start();


            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i].Join();
            for (int i = 0; i < threadsRemove.Length; i++)
                threadsRemove[i].Join();


            Assert.AreEqual(elemCount, global.Count);
            global.Sort();

            for (int i = 0; i < elemCount; i++)
                Assert.AreEqual(i, global[i]);
        }


        [TestMethod]
        [Timeout(60 * 1000)]
        public void ConcurrentTest()
        {
            ThreadPoolConcurrentQueue q = new ThreadPoolConcurrentQueue();

            for (int i = 0; i < 5; i++)
                RunThreadPoolConcurrentQueueTest(q, 300000, Math.Max(Environment.ProcessorCount / 2, 1));
        }
    }
}
