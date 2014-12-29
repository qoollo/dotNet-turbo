using Qoollo.Turbo.Threading.ThreadPools.Common;
using Qoollo.Turbo.Threading.ThreadPools.ServiceStuff;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.PerformanceTests
{
    public static class ConcurrentQueueTest
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

        // ==============


        private static bool TestStandardConcurrentQueue(int elemCount, int thCount, bool useRandom)
        {
            ConcurrentQueue<ThreadPoolWorkItem> q = new ConcurrentQueue<ThreadPoolWorkItem>();

            int atomicRandom = 0;

            int trackElemCount = elemCount;
            int addFinished = 0;

            Thread[] threadsAdd = new Thread[thCount];
            Thread[] threadsRemove = new Thread[thCount];

            List<int> global = new List<int>(elemCount);

            Action addAction = () =>
            {
                Random rnd = null;
                if (useRandom)
                    rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * thCount * 2);

                while (true)
                {
                    int item = Interlocked.Decrement(ref trackElemCount);
                    if (item < 0)
                        break;

                    q.Enqueue(new TestThreadPoolItem(item));

                    int sleepTime = 0;
                    if (rnd != null)
                        sleepTime = rnd.Next(2);
                    if (sleepTime > 0)
                        Thread.Sleep(sleepTime);
                }

                Interlocked.Increment(ref addFinished);
            };

            Action removeAction = () =>
            {
                Random rnd = null;
                if (useRandom)
                    rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * thCount * 2);

                List<int> data = new List<int>();

                while (Volatile.Read(ref addFinished) < thCount)
                {
                    ThreadPoolWorkItem tmp;
                    if (q.TryDequeue(out tmp))
                        data.Add((TestThreadPoolItem)tmp);

                    int sleepTime = 0;
                    if (rnd != null)
                        sleepTime = rnd.Next(2);
                    if (sleepTime > 0)
                        Thread.Sleep(sleepTime);
                }

                ThreadPoolWorkItem tmp2;
                while (q.TryDequeue(out tmp2))
                    data.Add((TestThreadPoolItem)tmp2);

                lock (global)
                    global.AddRange(data);
            };


            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i] = new Thread(new ThreadStart(addAction));

            for (int i = 0; i < threadsRemove.Length; i++)
                threadsRemove[i] = new Thread(new ThreadStart(removeAction));

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i].Start();
            for (int i = 0; i < threadsRemove.Length; i++)
                threadsRemove[i].Start();


            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i].Join();
            for (int i = 0; i < threadsRemove.Length; i++)
                threadsRemove[i].Join();


            sw.Stop();

            bool result = true;
            global.Sort();
            if (global.Count != elemCount)
            {
                result = false;
                Console.WriteLine("Incorrect element count");
            }

            HashSet<int> set = new HashSet<int>(global);
            if (set.Count != global.Count)
            {
                result = false;
                Console.WriteLine("Incorrect distinct element count");
            }

            for (int i = 0; i < Math.Min(elemCount, global.Count); i++)
            {
                if (global[i] != i)
                {
                    result = false;
                    Console.WriteLine("Incorrect data");
                    break;
                }
            }


            Console.WriteLine(q.GetType().Name + ". Element count = " + elemCount.ToString() + ", Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
            return result;
        }



        private static bool TestThreadPoolConcurrentQueue(int elemCount, int thCount, bool useRandom)
        {
            ThreadPoolConcurrentQueue q = new ThreadPoolConcurrentQueue();

            int atomicRandom = 0;

            int trackElemCount = elemCount;
            int addFinished = 0;

            Thread[] threadsAdd = new Thread[thCount];
            Thread[] threadsRemove = new Thread[thCount];

            List<int> global = new List<int>(elemCount);

            Action addAction = () =>
            {
                Random rnd = null;
                if (useRandom)
                    rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * thCount * 2);

                while (true)
                {
                    int item = Interlocked.Decrement(ref trackElemCount);
                    if (item < 0)
                        break;

                    q.Add(new TestThreadPoolItem(item));

                    int sleepTime = 0;
                    if (rnd != null)
                        sleepTime = rnd.Next(elemCount / 10000) - elemCount / 10000 + 2;
                    if (sleepTime > 0)
                        Thread.Sleep(sleepTime);
                }

                Interlocked.Increment(ref addFinished);
            };

            Action removeAction = () =>
            {
                Random rnd = null;
                if (useRandom)
                    rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * thCount * 2);

                List<int> data = new List<int>();

                while (Volatile.Read(ref addFinished) < thCount)
                {
                    ThreadPoolWorkItem tmp;
                    if (q.TryTake(out tmp))
                        data.Add((TestThreadPoolItem)tmp);

                    int sleepTime = 0;
                    if (rnd != null)
                        sleepTime = rnd.Next(elemCount / 10000) - elemCount / 10000 + 2;
                    if (sleepTime > 0)
                        Thread.Sleep(sleepTime);
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

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i].Start();
            for (int i = 0; i < threadsRemove.Length; i++)
                threadsRemove[i].Start();


            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i].Join();
            for (int i = 0; i < threadsRemove.Length; i++)
                threadsRemove[i].Join();


            sw.Stop();

            bool result = true;
            global.Sort();
            if (global.Count != elemCount)
            {
                result = false;
                Console.WriteLine("Incorrect element count");
            }

            HashSet<int> set = new HashSet<int>(global);
            if (set.Count != global.Count)
            {
                result = false;
                Console.WriteLine("Incorrect distinct element count");
            }

            for (int i = 0; i < Math.Min(elemCount, global.Count); i++)
            {
                if (global[i] != i)
                {
                    result = false;
                    Console.WriteLine("Incorrect data");
                    break;
                }
            }


            Console.WriteLine(q.GetType().Name + ". Element count = " + elemCount.ToString() + ", Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
            return result;
        }



        public static void RunTest()
        {
            TestStandardConcurrentQueue(10000000, 4, false);

            bool wasOk = true;
            for (int i = 0; i < 100; i++)
            {
                wasOk = TestThreadPoolConcurrentQueue(10000000, 4, true) && wasOk;
            }

            Console.WriteLine("wasOk = " + wasOk.ToString());
        }
    }
}
