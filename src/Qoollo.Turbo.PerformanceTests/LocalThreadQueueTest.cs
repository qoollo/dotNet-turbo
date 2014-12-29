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
    public static class LocalThreadQueueTest
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


        private static void TestStandardQueue(Queue<ThreadPoolWorkItem> q, int elemCount, int fillFactor)
        {
            int trackElemCount = elemCount;

            Stopwatch sw = Stopwatch.StartNew();


            while (trackElemCount > 0)
            {
                int initial = trackElemCount;

                for (int i = 0; i < fillFactor; i++)
                    q.Enqueue(new TestThreadPoolItem(initial--));

                initial = trackElemCount;
                for (int i = 0; i < fillFactor; i++)
                {
                    //if (q.Count > 0)
                    //    q.Dequeue();
                    var tmp = q.Dequeue();
                    if ((TestThreadPoolItem)tmp != initial--)
                        Console.WriteLine("11");
                }
                trackElemCount -= fillFactor;
            }

            sw.Stop();

            Console.WriteLine("StandardQueue. Element count = " + elemCount.ToString() + ", FillFactor = " + fillFactor.ToString() + ", Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }

        private static void TestConcurrentQueue(ConcurrentQueue<ThreadPoolWorkItem> q, int elemCount, int fillFactor)
        {
            int trackElemCount = elemCount;

            Stopwatch sw = Stopwatch.StartNew();


            while (trackElemCount > 0)
            {
                int initial = trackElemCount;

                for (int i = 0; i < fillFactor; i++)
                {
                    q.Enqueue(new TestThreadPoolItem(initial--));
                }

                initial = trackElemCount;
                for (int i = 0; i < fillFactor; i++)
                {
                    ThreadPoolWorkItem tmp = null;
                    if (!q.TryDequeue(out tmp))
                        Console.WriteLine("22");
                    if ((TestThreadPoolItem)tmp != initial--)
                        Console.WriteLine("33");
                }
                trackElemCount -= fillFactor;
            }

            sw.Stop();

            Console.WriteLine(q.GetType().Name + ". Element count = " + elemCount.ToString() + ", FillFactor = " + fillFactor.ToString() + ", Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }



        //private static void TestLocalThreadQueueAsQueue(ILocalThreadQueue q, int elemCount, int fillFactor)
        //{
        //    int trackElemCount = elemCount;

        //    Stopwatch sw = Stopwatch.StartNew();


        //    while (trackElemCount > 0)
        //    {
        //        int initial = trackElemCount;

        //        for (int i = 0; i < fillFactor; i++)
        //        {
        //            if (!q.TryAddGlobal(initial--))
        //                Console.WriteLine("11");
        //        }

        //        initial = trackElemCount;
        //        for (int i = 0; i < fillFactor; i++)
        //        {
        //            object tmp = null;
        //            //q.TryRemove(out tmp);
        //            if (!q.TryRemoveGlobal(out tmp))
        //                Console.WriteLine("22");
        //            if ((int)tmp != initial--)
        //                Console.WriteLine("33");
        //        }
        //        trackElemCount -= fillFactor;
        //    }

        //    sw.Stop();

        //    Console.WriteLine(q.GetType().Name + ". Element count = " + elemCount.ToString() + ", FillFactor = " + fillFactor.ToString() + ", Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
        //    Console.WriteLine();
        //}


        //private static void TestLocalThreadQueue2AsQueue(ILocalThreadQueue2 q, int elemCount, int fillFactor)
        //{
        //    int trackElemCount = elemCount;

        //    Stopwatch sw = Stopwatch.StartNew();


        //    while (trackElemCount > 0)
        //    {
        //        int initial = trackElemCount;

        //        for (int i = 0; i < fillFactor; i++)
        //        {
        //            if (!q.TryAddLocal(initial--))
        //                Console.WriteLine("11");
        //        }

        //        initial = trackElemCount;
        //        for (int i = 0; i < fillFactor; i++)
        //        {
        //            object tmp = null;
        //            //q.TryRemove(out tmp);
        //            if (!q.TryTakeLocal(out tmp))
        //                Console.WriteLine("22");
        //            if ((int)tmp != initial--)
        //                Console.WriteLine("33");
        //        }
        //        trackElemCount -= fillFactor;
        //    }

        //    sw.Stop();

        //    Console.WriteLine(q.GetType().Name + ". Element count = " + elemCount.ToString() + ", FillFactor = " + fillFactor.ToString() + ", Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
        //    Console.WriteLine();
        //}


        //private static void TestLocalThreadQueueAsStack(LocalThreadQueue q, int elemCount, int fillFactor)
        //{
        //    int trackElemCount = elemCount;

        //    Stopwatch sw = Stopwatch.StartNew();


        //    while (trackElemCount > 0)
        //    {
        //        int initial = trackElemCount;
        //        for (int i = 0; i < fillFactor; i++)
        //        {
        //            if (!q.TryAddGlobal(initial--))
        //                Console.WriteLine("11");
        //        }

        //        for (int i = 0; i < fillFactor; i++)
        //        {
        //            object tmp = null;
        //            //q.TryRemove(out tmp);
        //            if (!q.TryRemoveGlobal(out tmp))
        //                Console.WriteLine("22");
        //            if ((int)tmp != ++initial)
        //                Console.WriteLine("33");
        //        }
        //        trackElemCount -= fillFactor;
        //    }

        //    sw.Stop();

        //    Console.WriteLine(q.GetType().Name + ". Element count = " + elemCount.ToString() + ", FillFactor = " + fillFactor.ToString() + ", Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
        //    Console.WriteLine();
        //}


        private static void TestLocalThreadQueue2AsStack(ThreadPoolLocalQueue q, int elemCount, int fillFactor)
        {
            int trackElemCount = elemCount;

            Stopwatch sw = Stopwatch.StartNew();


            while (trackElemCount > 0)
            {
                int initial = trackElemCount;
                for (int i = 0; i < fillFactor; i++)
                {
                    if (!q.TryAddLocal(new TestThreadPoolItem(initial--)))
                        Console.WriteLine("11");
                }

                for (int i = 0; i < fillFactor; i++)
                {
                    ThreadPoolWorkItem tmp = null;
                    //q.TryRemove(out tmp);
                    if (!q.TryTakeLocal(out tmp))
                        Console.WriteLine("22");
                    if ((TestThreadPoolItem)tmp != ++initial)
                        Console.WriteLine("33");
                }
                trackElemCount -= fillFactor;
            }

            sw.Stop();

            Console.WriteLine(q.GetType().Name + ". Element count = " + elemCount.ToString() + ", FillFactor = " + fillFactor.ToString() + ", Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }




        //private static bool TestLocalThreadQueueConcurrently(ILocalThreadQueue q, int elemCount, int thCount)
        //{
        //    int trackElemCount = elemCount;
        //    int addFinished = 0;

        //    Thread[] threadsAdd = new Thread[thCount];
        //    Thread[] threadsRemove = new Thread[thCount];

        //    List<int> global = new List<int>(elemCount);

        //    Action addAction = () =>
        //    {
        //        while (true)
        //        {
        //            int item = Interlocked.Decrement(ref trackElemCount);
        //            if (item < 0)
        //                break;

        //            //q.TryAddGlobal(item);
        //            SpinWait swait = new SpinWait();
        //            while (!q.TryAddGlobal(item))
        //                swait.SpinOnce();
        //        }

        //        Interlocked.Increment(ref addFinished);
        //    };

        //    Action removeAction = () =>
        //    {
        //        List<int> data = new List<int>();

        //        while (Volatile.Read(ref addFinished) < thCount)
        //        {
        //            object tmp;
        //            if (q.TryRemoveGlobal(out tmp))
        //                data.Add((int)tmp);
        //        }

        //        object tmp2;
        //        while (q.TryRemoveGlobal(out tmp2))
        //            data.Add((int)tmp2);

        //        lock (global)
        //            global.AddRange(data);
        //    };


        //    for (int i = 0; i < threadsAdd.Length; i++)
        //        threadsAdd[i] = new Thread(new ThreadStart(addAction));

        //    for (int i = 0; i < threadsRemove.Length; i++)
        //        threadsRemove[i] = new Thread(new ThreadStart(removeAction));

        //    Stopwatch sw = Stopwatch.StartNew();

        //    for (int i = 0; i < threadsAdd.Length; i++)
        //        threadsAdd[i].Start();
        //    for (int i = 0; i < threadsRemove.Length; i++)
        //        threadsRemove[i].Start();


        //    for (int i = 0; i < threadsAdd.Length; i++)
        //        threadsAdd[i].Join();
        //    for (int i = 0; i < threadsRemove.Length; i++)
        //        threadsRemove[i].Join();


        //    sw.Stop();

        //    bool result = true;
        //    global.Sort();
        //    if (global.Count != elemCount)
        //    {
        //        result = false;
        //        Console.WriteLine("Incorrect element count");
        //    }

        //    HashSet<int> set = new HashSet<int>(global);
        //    if (set.Count != global.Count)
        //    {
        //        result = false;
        //        Console.WriteLine("Incorrect distinct element count");
        //    }

        //    for (int i = 0; i < Math.Min(elemCount, global.Count); i++)
        //    {
        //        if (global[i] != i)
        //        {
        //            result = false;
        //            Console.WriteLine("Incorrect data");
        //            break;
        //        }
        //    }


        //    Console.WriteLine("Concurrent " + q.GetType().Name + ". Element count = " + elemCount.ToString() + ", Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
        //    Console.WriteLine();
        //    return result;
        //}



        private static bool TestConcurrentQueuePrimaryScenario(ConcurrentQueue<ThreadPoolWorkItem> q, int elemCount, int slealThCount, int fillFactor, bool useRandom)
        {
            int trackElemCount = elemCount;
            int addFinished = 0;

            int atomicRandom = 0;

            Thread mainThread = null;
            Thread[] stealThreads = new Thread[slealThCount];

            List<int> global = new List<int>(elemCount);

            Action mainAction = () =>
            {
                List<int> data = new List<int>(elemCount);

                Random rnd = null;
                if (useRandom)
                    rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * slealThCount);

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
                        q.Enqueue(new TestThreadPoolItem(item));
                    }

                    int removeCount = fillFactor;
                    if (rnd != null)
                        removeCount = rnd.Next(fillFactor);

                    for (int i = 0; i < removeCount; i++)
                    {
                        ThreadPoolWorkItem item = null;
                        if (!q.TryDequeue(out item))
                            break;
                        data.Add((TestThreadPoolItem)item);
                    }
                }

                Interlocked.Increment(ref addFinished);

                ThreadPoolWorkItem finalItem = null;
                while (q.TryDequeue(out finalItem))
                    data.Add((TestThreadPoolItem)finalItem);

                lock (global)
                    global.AddRange(data);
            };

            Action stealAction = () =>
            {
                Random rnd = null;
                if (useRandom)
                    rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * slealThCount);

                List<int> data = new List<int>();

                while (Volatile.Read(ref addFinished) < 1 && Volatile.Read(ref trackElemCount) > elemCount / 1000)
                {
                    ThreadPoolWorkItem tmp;
                    if (q.TryDequeue(out tmp))
                        data.Add((TestThreadPoolItem)tmp);

                    int sleepTime = Volatile.Read(ref trackElemCount) % 2;
                    if (rnd != null)
                        sleepTime = rnd.Next(2);
                    if (sleepTime > 0)
                        Thread.Sleep(sleepTime);
                }

                lock (global)
                    global.AddRange(data);
            };


            mainThread = new Thread(new ThreadStart(mainAction));
            for (int i = 0; i < stealThreads.Length; i++)
                stealThreads[i] = new Thread(new ThreadStart(stealAction));

            Stopwatch sw = Stopwatch.StartNew();

            mainThread.Start();
            for (int i = 0; i < stealThreads.Length; i++)
                stealThreads[i].Start();


            mainThread.Join();
            for (int i = 0; i < stealThreads.Length; i++)
                stealThreads[i].Join();


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


            Console.WriteLine("PrimaryScenario " + q.GetType().Name + ". Element count = " + elemCount.ToString() + ", Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
            return result;
        }


        private static bool TestLocalThreadQueuePrimaryScenario(ThreadPoolLocalQueue q, int elemCount, int slealThCount, int fillFactor, bool useRandom)
        {
            int trackElemCount = elemCount;
            int addFinished = 0;

            int atomicRandom = 0;

            Thread mainThread = null;
            Thread[] stealThreads = new Thread[slealThCount];

            List<int> global = new List<int>(elemCount);

            Action mainAction = () =>
            {
                List<int> data = new List<int>(elemCount);

                Random rnd = null;
                if (useRandom)
                    rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * slealThCount);

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

                    int removeCount = fillFactor;
                    if (rnd != null)
                        removeCount = rnd.Next(fillFactor);

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
                Random rnd = null;
                if (useRandom)
                    rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * slealThCount);

                List<int> data = new List<int>();

                while (Volatile.Read(ref addFinished) < 1 && Volatile.Read(ref trackElemCount) > elemCount / 1000)
                {
                    ThreadPoolWorkItem tmp;
                    if (q.TrySteal(out tmp))
                        data.Add((TestThreadPoolItem)tmp);

                    int sleepTime = Volatile.Read(ref trackElemCount) % 2;
                    if (rnd != null)
                        sleepTime = rnd.Next(2);
                    if (sleepTime > 0)
                        Thread.Sleep(sleepTime);
                }

                lock (global)
                    global.AddRange(data);
            };


            mainThread = new Thread(new ThreadStart(mainAction));
            for (int i = 0; i < stealThreads.Length; i++)
                stealThreads[i] = new Thread(new ThreadStart(stealAction));

            Stopwatch sw = Stopwatch.StartNew();

            mainThread.Start();
            for (int i = 0; i < stealThreads.Length; i++)
                stealThreads[i].Start();


            mainThread.Join();
            for (int i = 0; i < stealThreads.Length; i++)
                stealThreads[i].Join();


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


            Console.WriteLine("PrimaryScenario " + q.GetType().Name + ". Element count = " + elemCount.ToString() + ", Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
            return result;
        }



        public static void RunTest()
        {
            TestStandardQueue(new Queue<ThreadPoolWorkItem>(32), 10000000, 16);
            TestConcurrentQueue(new ConcurrentQueue<ThreadPoolWorkItem>(), 10000000, 16);
            TestLocalThreadQueue2AsStack(new ThreadPoolLocalQueue(), 10000000, 16);


            TestConcurrentQueuePrimaryScenario(new ConcurrentQueue<ThreadPoolWorkItem>(), 10000000, 4, 30, false);
            TestLocalThreadQueuePrimaryScenario(new ThreadPoolLocalQueue(), 10000000, 4, 30, false);
        }
    }
}
