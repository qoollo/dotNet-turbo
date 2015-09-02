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
    public static class ThreadPoolQueueTest
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


        private static bool RunComplexTestOnThreadPoolQueue(int elemCount, int thCount)
        {
            ThreadPoolQueueController q = new ThreadPoolQueueController(1000, 100);
            int mainIndex = -1;

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
                while (true)
                {
                    int item = Interlocked.Decrement(ref trackElemCount);
                    if (item < 0)
                        break;

                    q.Add(new TestThreadPoolItem(item), null);
                }

                Interlocked.Increment(ref addFinished);
            };


            Action mainAction = () =>
            {
                var localQ = locQ[Interlocked.Increment(ref mainIndex)];

                List<int> data = new List<int>();

                try
                {
                    int index = 0;
                    while (Volatile.Read(ref addFinished) < thCount)
                    {
                        ThreadPoolWorkItem tmp = null;
                        if (q.TryTake(localQ, out tmp, -1, tokSrc.Token, true))
                            data.Add((TestThreadPoolItem)tmp);

                        if (((index++) % 10) == 0)
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


            for (int i = 0; i < threadsMain.Length; i++)
                threadsMain[i] = new Thread(new ThreadStart(mainAction));
            for (int i = 0; i < threadsAdditional.Length; i++)
                threadsAdditional[i] = new Thread(new ThreadStart(additionalAction));

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < threadsMain.Length; i++)
                threadsMain[i].Start();
            for (int i = 0; i < threadsAdditional.Length; i++)
                threadsAdditional[i].Start();


            for (int i = 0; i < threadsAdditional.Length; i++)
                threadsAdditional[i].Join();
            tokSrc.Cancel();
            for (int i = 0; i < threadsMain.Length; i++)
                threadsMain[i].Join();

            sw.Stop();

            bool result = true;
            if (elemCount != global.Count)
            {
                Console.WriteLine("Incorrect items count");
                result = false;
            }

            global.Sort();

            for (int i = 0; i < Math.Min(elemCount, global.Count); i++)
            {
                if (global[i] != i)
                {
                    Console.WriteLine("Incorrect items value");
                    result = false;
                    break;
                }
            }

            Console.WriteLine(q.GetType().Name + ". Element count = " + elemCount.ToString() + ", Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
            return result;
        }



        private static bool RunComplexTestOnBlockingCollection(int elemCount, int thCount)
        {
            BlockingCollection<ThreadPoolWorkItem> q = new BlockingCollection<ThreadPoolWorkItem>(1000);
            int mainIndex = -1;

            int trackElemCount = elemCount;
            int addFinished = 0;

            ThreadPoolLocalQueue[] locQ = new ThreadPoolLocalQueue[thCount];
            for (int i = 0; i < locQ.Length; i++)
                locQ[i] = new ThreadPoolLocalQueue();

            Thread[] threadsMain = new Thread[thCount];
            Thread[] threadsAdditional = new Thread[thCount];

            CancellationTokenSource tokSrc = new CancellationTokenSource();

            List<int> global = new List<int>(elemCount);

            Action additionalAction = () =>
            {
                while (true)
                {
                    int item = Interlocked.Decrement(ref trackElemCount);
                    if (item < 0)
                        break;

                    q.Add(new TestThreadPoolItem(item));
                }

                Interlocked.Increment(ref addFinished);
            };


            Action mainAction = () =>
            {
                var localQ = locQ[Interlocked.Increment(ref mainIndex)];

                List<int> data = new List<int>();

                try
                {
                    int index = 0;
                    while (Volatile.Read(ref addFinished) < thCount)
                    {
                        ThreadPoolWorkItem tmp = null;
                        if (q.TryTake(out tmp, -1, tokSrc.Token))
                            data.Add((TestThreadPoolItem)tmp);

                        if (((index++) % 10) == 0)
                        {
                            int item = Interlocked.Decrement(ref trackElemCount);
                            if (item >= 0)
                            {
                                while (!q.TryAdd(new TestThreadPoolItem(item)))
                                {
                                    if (q.TryTake(out tmp, 0, CancellationToken.None))
                                        data.Add((TestThreadPoolItem)tmp);
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { }

                ThreadPoolWorkItem tmp2;
                while (q.TryTake(out tmp2))
                    data.Add((TestThreadPoolItem)tmp2);

                lock (global)
                    global.AddRange(data);
            };


            for (int i = 0; i < threadsMain.Length; i++)
                threadsMain[i] = new Thread(new ThreadStart(mainAction));
            for (int i = 0; i < threadsAdditional.Length; i++)
                threadsAdditional[i] = new Thread(new ThreadStart(additionalAction));

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < threadsMain.Length; i++)
                threadsMain[i].Start();
            for (int i = 0; i < threadsAdditional.Length; i++)
                threadsAdditional[i].Start();


            for (int i = 0; i < threadsAdditional.Length; i++)
                threadsAdditional[i].Join();
            tokSrc.Cancel();
            for (int i = 0; i < threadsMain.Length; i++)
                threadsMain[i].Join();

            sw.Stop();

            bool result = true;
            if (elemCount != global.Count)
            {
                Console.WriteLine("Incorrect items count");
                result = false;
            }

            global.Sort();

            for (int i = 0; i < Math.Min(elemCount, global.Count); i++)
            {
                if (global[i] != i)
                {
                    Console.WriteLine("Incorrect items value");
                    result = false;
                    break;
                }
            }

            Console.WriteLine(q.GetType().Name + ". Element count = " + elemCount.ToString() + ", Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
            return result;
        }


        public static void RunTest()
        {
            RunComplexTestOnBlockingCollection(10000000, Math.Max(1, Environment.ProcessorCount / 2));
            RunComplexTestOnThreadPoolQueue(10000000, Math.Max(1, Environment.ProcessorCount / 2));
        }
    }
}
