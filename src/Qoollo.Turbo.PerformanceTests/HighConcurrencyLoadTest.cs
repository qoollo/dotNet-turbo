using Qoollo.Turbo.Collections.Concurrent;
using Qoollo.Turbo.Threading.ServiceStuff;
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
    public static class HighConcurrencyLoadTest
    {
        private class CondVarThreadSafeQueue<T>
        {
            private readonly object SharedSyncObj = new object();
            private readonly Threading.ConditionVariable VarFull = null;
            private readonly Threading.ConditionVariable VarEmpty = null;
            private readonly Queue<T> Queue = new Queue<T>();
            private readonly int MaxCount = 1000;
            private volatile int ItemCount = 0;

            public CondVarThreadSafeQueue(int boundingCapacity)
            {
                MaxCount = boundingCapacity;
                VarFull = new Threading.ConditionVariable(SharedSyncObj);
                VarEmpty = new Threading.ConditionVariable(SharedSyncObj);
            }

            public bool TryAdd(T value, int timeout, CancellationToken token)
            {
                using (var waiter = VarFull.Enter(timeout, token))
                {
                    do
                    {
                        if (ItemCount < MaxCount)
                        {
                            Queue.Enqueue(value);
                            Interlocked.Increment(ref ItemCount);
                            VarEmpty.Pulse();
                            return true;
                        }
                    }
                    while (waiter.Wait());

                    //if (waiter.Wait(s => s.ItemCount < s.MaxCount, this))
                    //{
                    //    Queue.Enqueue(value);
                    //    Interlocked.Increment(ref ItemCount);
                    //    VarEmpty.Pulse();
                    //    return true;
                    //}

                    return false;
                }
            }
            public void Add(T value)
            {
                TryAdd(value, Timeout.Infinite, default(CancellationToken));
            }

            public bool TryTake(out T value, int timeout, CancellationToken token)
            {
                using (var waiter = VarEmpty.Enter(timeout, token))
                {
                    do
                    {
                        if (ItemCount > 0)
                        {
                            value = Queue.Dequeue();
                            Interlocked.Decrement(ref ItemCount);
                            VarFull.Pulse();
                            return true;
                        }
                    }
                    while (waiter.Wait());

                    //if (waiter.Wait(s => s.ItemCount > 0, this))
                    //{
                    //    value = Queue.Dequeue();
                    //    Interlocked.Decrement(ref ItemCount);
                    //    VarFull.Pulse();
                    //    return true;
                    //}

                    value = default(T);
                    return false;
                }
            }
            public T Take(CancellationToken token)
            {
                T val;
                TryTake(out val, Timeout.Infinite, token);
                return val;
            }
            public bool TryTake(out T val)
            {
                return TryTake(out val, 0, default(CancellationToken));
            }
        }

        // ==============

        private class MonitorThreadSafeQueue<T>
        {
            private readonly Threading.MonitorObject VarFull = new Threading.MonitorObject();
            private readonly Threading.MonitorObject VarEmpty = new Threading.MonitorObject();
            private readonly ConcurrentQueue<T> Queue = new ConcurrentQueue<T>();
            private readonly int MaxCount = 1000;
            private volatile int ItemCount = 0;

            public MonitorThreadSafeQueue(int boundingCapacity)
            {
                MaxCount = boundingCapacity;
            }

            public bool TryAdd(T value, int timeout, CancellationToken token)
            {
                bool result = false;
                using (var waiter = VarFull.Enter(timeout, token))
                {
                    do
                    {
                        if (ItemCount < MaxCount)
                        {
                            Queue.Enqueue(value);
                            Interlocked.Increment(ref ItemCount);
                            result = true;
                            break;
                        }
                    }
                    while (waiter.Wait());
                }

                if (result)
                    VarEmpty.Pulse();
                return result;
            }
            public void Add(T value)
            {
                TryAdd(value, Timeout.Infinite, default(CancellationToken));
            }

            public bool TryTake(out T value, int timeout, CancellationToken token)
            {
                bool result = false;
                value = default(T);

                using (var waiter = VarEmpty.Enter(timeout, token))
                {
                    do
                    {
                        if (ItemCount > 0)
                        {
                            Queue.TryDequeue(out value);
                            Interlocked.Decrement(ref ItemCount);
                            result = true;
                            break;
                        }
                    }
                    while (waiter.Wait());
                }

                if (result)
                {
                    VarFull.Pulse();
                    return true;
                }

                return false;
            }
            public T Take(CancellationToken token)
            {
                T val;
                TryTake(out val, Timeout.Infinite, token);
                return val;
            }
            public bool TryTake(out T val)
            {
                return TryTake(out val, 0, default(CancellationToken));
            }
        }

        // ==============


        private static TimeSpan RunConcurrentBC(string name, int elemCount, int addThCount, int takeThCount, int addSpin, int takeSpin)
        {
            BlockingCollection<int> col = new BlockingCollection<int>(10000);

            CancellationTokenSource srcCancel = new CancellationTokenSource();

            Thread[] addThreads = new Thread[addThCount];
            Thread[] takeThreads = new Thread[takeThCount];

            int addedElemCount = 0;
            List<int> globalList = new List<int>();

            Barrier barierStart = new Barrier(1 + addThreads.Length + takeThreads.Length);
            Barrier barierAdders = new Barrier(1 + addThreads.Length);
            Barrier barierTakers = new Barrier(1 + takeThreads.Length);

            Action addAction = () =>
                {
                    barierStart.SignalAndWait();

                    int index = 0;
                    while ((index = Interlocked.Increment(ref addedElemCount)) <= elemCount)
                    {
                        col.Add(index - 1);
                        SpinWaitHelper.SpinWait(addSpin);
                    }

                    barierAdders.SignalAndWait();
                };


            Action takeAction = () =>
                {
                    CancellationToken myToken = srcCancel.Token;
                    List<int> valList = new List<int>(elemCount / takeThCount + 100);

                    barierStart.SignalAndWait();

                    try
                    {
                        while (!srcCancel.IsCancellationRequested)
                        {
                            int val = 0;
                            val = col.Take(myToken);

                            valList.Add(val);
                            SpinWaitHelper.SpinWait(takeSpin);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }

                    int val2 = 0;
                    while (col.TryTake(out val2))
                        valList.Add(val2);

                    barierTakers.SignalAndWait();

                    lock (globalList)
                    {
                        globalList.AddRange(valList);
                    }
                };

            for (int i = 0; i < addThreads.Length; i++)
                addThreads[i] = new Thread(new ThreadStart(addAction));
            for (int i = 0; i < takeThreads.Length; i++)
                takeThreads[i] = new Thread(new ThreadStart(takeAction));


            for (int i = 0; i < takeThreads.Length; i++)
                takeThreads[i].Start();
            for (int i = 0; i < addThreads.Length; i++)
                addThreads[i].Start();

            barierStart.SignalAndWait();

            Stopwatch sw = Stopwatch.StartNew();

            barierAdders.SignalAndWait();
            srcCancel.Cancel();
            barierTakers.SignalAndWait();
            sw.Stop();

            for (int i = 0; i < addThreads.Length; i++)
                addThreads[i].Join();
            for (int i = 0; i < takeThreads.Length; i++)
                takeThreads[i].Join();

            globalList.Sort();
            if (globalList.Count != elemCount)
                Console.WriteLine("Bad count");

            for (int i = 0; i < globalList.Count; i++)
            {
                if (globalList[i] != i)
                {
                    Console.WriteLine("invalid elements");
                    break;
                }
            }

            Console.WriteLine(name + ". BlocCol. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            return sw.Elapsed;
        }

        private static TimeSpan RunConcurrentBQ(string name, int elemCount, int addThCount, int takeThCount, int addSpin, int takeSpin)
        {
            BlockingQueue<int> col = new BlockingQueue<int>(10000);

            CancellationTokenSource srcCancel = new CancellationTokenSource();

            Thread[] addThreads = new Thread[addThCount];
            Thread[] takeThreads = new Thread[takeThCount];

            int addedElemCount = 0;
            List<int> globalList = new List<int>();

            Barrier barierStart = new Barrier(1 + addThreads.Length + takeThreads.Length);
            Barrier barierAdders = new Barrier(1 + addThreads.Length);
            Barrier barierTakers = new Barrier(1 + takeThreads.Length);

            Action addAction = () =>
            {
                barierStart.SignalAndWait();

                int index = 0;
                while ((index = Interlocked.Increment(ref addedElemCount)) <= elemCount)
                {
                    col.Add(index - 1);
                    SpinWaitHelper.SpinWait(addSpin);
                }

                barierAdders.SignalAndWait();
            };


            Action takeAction = () =>
            {
                CancellationToken myToken = srcCancel.Token;
                List<int> valList = new List<int>(elemCount / takeThCount + 100);

                barierStart.SignalAndWait();

                try
                {
                    while (!srcCancel.IsCancellationRequested)
                    {
                        int val = 0;
                        val = col.Take(myToken);

                        valList.Add(val);
                        SpinWaitHelper.SpinWait(takeSpin);
                    }
                }
                catch (OperationCanceledException)
                {
                }

                int val2 = 0;
                while (col.TryTake(out val2))
                    valList.Add(val2);

                barierTakers.SignalAndWait();

                lock (globalList)
                {
                    globalList.AddRange(valList);
                }
            };

            for (int i = 0; i < addThreads.Length; i++)
                addThreads[i] = new Thread(new ThreadStart(addAction));
            for (int i = 0; i < takeThreads.Length; i++)
                takeThreads[i] = new Thread(new ThreadStart(takeAction));


            for (int i = 0; i < takeThreads.Length; i++)
                takeThreads[i].Start();
            for (int i = 0; i < addThreads.Length; i++)
                addThreads[i].Start();

            barierStart.SignalAndWait();

            Stopwatch sw = Stopwatch.StartNew();

            barierAdders.SignalAndWait();
            srcCancel.Cancel();
            barierTakers.SignalAndWait();
            sw.Stop();

            for (int i = 0; i < addThreads.Length; i++)
                addThreads[i].Join();
            for (int i = 0; i < takeThreads.Length; i++)
                takeThreads[i].Join();

            globalList.Sort();
            if (globalList.Count != elemCount)
                Console.WriteLine("Bad count");

            for (int i = 0; i < globalList.Count; i++)
            {
                if (globalList[i] != i)
                {
                    Console.WriteLine("invalid elements");
                    break;
                }
            }

            if (name != null)
                Console.WriteLine(name + ". BlocQ. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            return sw.Elapsed;
        }


        private static TimeSpan RunConcurrentCondVar(string name, int elemCount, int addThCount, int takeThCount, int addSpin, int takeSpin)
        {
            CondVarThreadSafeQueue<int> col = new CondVarThreadSafeQueue<int>(10000);

            CancellationTokenSource srcCancel = new CancellationTokenSource();

            Thread[] addThreads = new Thread[addThCount];
            Thread[] takeThreads = new Thread[takeThCount];

            int addedElemCount = 0;
            List<int> globalList = new List<int>();

            Barrier barierStart = new Barrier(1 + addThreads.Length + takeThreads.Length);
            Barrier barierAdders = new Barrier(1 + addThreads.Length);
            Barrier barierTakers = new Barrier(1 + takeThreads.Length);

            Action addAction = () =>
            {
                barierStart.SignalAndWait();

                int index = 0;
                while ((index = Interlocked.Increment(ref addedElemCount)) <= elemCount)
                {
                    col.Add(index - 1);
                    SpinWaitHelper.SpinWait(addSpin);
                }

                barierAdders.SignalAndWait();
            };


            Action takeAction = () =>
            {
                CancellationToken myToken = srcCancel.Token;
                List<int> valList = new List<int>(elemCount / takeThCount + 100);

                barierStart.SignalAndWait();

                try
                {
                    while (!srcCancel.IsCancellationRequested)
                    {
                        int val = 0;
                        val = col.Take(myToken);

                        valList.Add(val);
                        SpinWaitHelper.SpinWait(takeSpin);
                    }
                }
                catch (OperationCanceledException)
                {
                }

                int val2 = 0;
                while (col.TryTake(out val2))
                    valList.Add(val2);

                barierTakers.SignalAndWait();

                lock (globalList)
                {
                    globalList.AddRange(valList);
                }
            };

            for (int i = 0; i < addThreads.Length; i++)
                addThreads[i] = new Thread(new ThreadStart(addAction));
            for (int i = 0; i < takeThreads.Length; i++)
                takeThreads[i] = new Thread(new ThreadStart(takeAction));


            for (int i = 0; i < takeThreads.Length; i++)
                takeThreads[i].Start();
            for (int i = 0; i < addThreads.Length; i++)
                addThreads[i].Start();

            barierStart.SignalAndWait();

            Stopwatch sw = Stopwatch.StartNew();

            barierAdders.SignalAndWait();
            srcCancel.Cancel();
            barierTakers.SignalAndWait();
            sw.Stop();

            for (int i = 0; i < addThreads.Length; i++)
                addThreads[i].Join();
            for (int i = 0; i < takeThreads.Length; i++)
                takeThreads[i].Join();

            globalList.Sort();
            if (globalList.Count != elemCount)
                Console.WriteLine("Bad count");

            for (int i = 0; i < globalList.Count; i++)
            {
                if (globalList[i] != i)
                {
                    Console.WriteLine("invalid elements");
                    break;
                }
            }

            if (name != null)
                Console.WriteLine(name + ". CondVarQ. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            return sw.Elapsed;
        }



        private static TimeSpan RunConcurrentMon(string name, int elemCount, int addThCount, int takeThCount, int addSpin, int takeSpin)
        {
            MonitorThreadSafeQueue<int> col = new MonitorThreadSafeQueue<int>(10000);

            CancellationTokenSource srcCancel = new CancellationTokenSource();

            Thread[] addThreads = new Thread[addThCount];
            Thread[] takeThreads = new Thread[takeThCount];

            int addedElemCount = 0;
            List<int> globalList = new List<int>();

            Barrier barierStart = new Barrier(1 + addThreads.Length + takeThreads.Length);
            Barrier barierAdders = new Barrier(1 + addThreads.Length);
            Barrier barierTakers = new Barrier(1 + takeThreads.Length);

            Action addAction = () =>
            {
                barierStart.SignalAndWait();

                int index = 0;
                while ((index = Interlocked.Increment(ref addedElemCount)) <= elemCount)
                {
                    col.Add(index - 1);
                    SpinWaitHelper.SpinWait(addSpin);
                }

                barierAdders.SignalAndWait();
            };


            Action takeAction = () =>
            {
                CancellationToken myToken = srcCancel.Token;
                List<int> valList = new List<int>(elemCount / takeThCount + 100);

                barierStart.SignalAndWait();

                try
                {
                    while (!srcCancel.IsCancellationRequested)
                    {
                        int val = 0;
                        val = col.Take(myToken);

                        valList.Add(val);
                        SpinWaitHelper.SpinWait(takeSpin);
                    }
                }
                catch (OperationCanceledException)
                {
                }

                int val2 = 0;
                while (col.TryTake(out val2))
                    valList.Add(val2);

                barierTakers.SignalAndWait();

                lock (globalList)
                {
                    globalList.AddRange(valList);
                }
            };

            for (int i = 0; i < addThreads.Length; i++)
                addThreads[i] = new Thread(new ThreadStart(addAction));
            for (int i = 0; i < takeThreads.Length; i++)
                takeThreads[i] = new Thread(new ThreadStart(takeAction));


            for (int i = 0; i < takeThreads.Length; i++)
                takeThreads[i].Start();
            for (int i = 0; i < addThreads.Length; i++)
                addThreads[i].Start();

            barierStart.SignalAndWait();

            Stopwatch sw = Stopwatch.StartNew();

            barierAdders.SignalAndWait();
            srcCancel.Cancel();
            barierTakers.SignalAndWait();
            sw.Stop();

            for (int i = 0; i < addThreads.Length; i++)
                addThreads[i].Join();
            for (int i = 0; i < takeThreads.Length; i++)
                takeThreads[i].Join();

            globalList.Sort();
            if (globalList.Count != elemCount)
                Console.WriteLine("Bad count");

            for (int i = 0; i < globalList.Count; i++)
            {
                if (globalList[i] != i)
                {
                    Console.WriteLine("invalid elements");
                    break;
                }
            }

            if (name != null)
                Console.WriteLine(name + ". MonQ. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            return sw.Elapsed;
        }



        private static void Free()
        {
            GC.Collect();
            Thread.Sleep(1000);
        }

        private static void RunAvgTest(string name, Func<TimeSpan> test, int count)
        {
            TimeSpan[] data = new TimeSpan[count];
            for (int i = 0; i < count; i++)
                data[i] = test();

            Array.Sort(data);
            var finData = data.Skip(2).Take(count - 2).ToArray();

            TimeSpan sum = TimeSpan.Zero;
            for (int i = 0; i < finData.Length; i++)
                sum += finData[i];

            int avgMs = (int)(sum.TotalMilliseconds / finData.Length);
            Console.WriteLine(name + ". AvgTime = " + avgMs.ToString() + "ms");
        }

        //private static void TstBQ()
        //{
        //    RunAvgTest("Simple", () =>
        //    {
        //        Free();
        //        return RunConcurrentBQ("Simple", 5000000, 8, int.MaxValue, 10, 100);
        //    }, 14);

        //    RunAvgTest("Fast", () =>
        //    {
        //        Free();
        //        return RunConcurrentBQ("Fast", 5000000, 8, int.MaxValue, 0, 0);
        //    }, 14);

        //    RunAvgTest("OverConcurrency", () =>
        //    {
        //        Free();
        //        return RunConcurrentBQ("OverConcurrency", 5000000, 16, int.MaxValue, 0, 0);
        //    }, 14);

        //    RunAvgTest("OverConcurrency fix", () =>
        //    {
        //        Free();
        //        return RunConcurrentBQ("OverConcurrency fix", 5000000, 16, 2, 0, 0);
        //    }, 14);
        //}


        private static double RunOptimProc(int n, int a, int b)
        {
            //Qoollo.Turbo.Threading.SemaphoreLight.N = n;
            //Qoollo.Turbo.Threading.SemaphoreLight.A = a;
            //Qoollo.Turbo.Threading.SemaphoreLight.B = b;

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < 3; i++)
            {
                RunConcurrentBQ(null, 5000000, 1, 1, 10, 10);
                RunConcurrentBQ(null, 5000000, 4, 4, 10, 10);
                RunConcurrentBQ(null, 5000000, 16, 1, 10, 10);
                RunConcurrentBQ(null, 5000000, 1, 16, 10, 10);
                RunConcurrentBQ(null, 5000000, 16, 16, 10, 10);
            }

            sw.Stop();

            return (int)sw.ElapsedMilliseconds / 3;
        }

        public static void RunOptimization()
        {
            double bestResult = double.MaxValue;
            int bestN = 9;
            int bestA = 150;
            int bestB = 16;

            for (int n = 9; n < 10; n++)
            {
                for (int a = 60; a <= 140; a += 20)
                {
                    for (int b = 40; b <= 120; b += 4)
                    {
                        double curResult = RunOptimProc(n, a, b);
                        Console.WriteLine(string.Format("{0}ms. N = {1}, A = {2}, B = {3}", curResult, n, a, b));

                        if (curResult < bestResult)
                        {
                            bestResult = curResult;
                            bestN = n;
                            bestA = a;
                            bestB = b;
                        }
                    }
                }
            }


            Console.WriteLine();
            Console.WriteLine("============= Best ===========");
            Console.WriteLine(string.Format("{0}ms. N = {1}, A = {2}, B = {3}", bestResult, bestN, bestA, bestB));
            Console.WriteLine("========================");

            Console.ReadLine();
        }


        public static void RunTest()
        {
            //TstBQ();

            //Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)1;
            SpinWaitHelper.WaitUntilNormalizationCoefCalculated();
            Console.WriteLine($"SpinWait norm coef = {SpinWaitHelper.NormalizationCoef}");


            for (int i = 0; i < 10; i++)
            {

                RunConcurrentBC("1, 1", 5000000, 1, 1, 20, 20);
                Free();

                RunConcurrentBC("4, 4", 5000000, 4, 4, 20, 20);
                Free();

                RunConcurrentBC("16, 1", 5000000, 16, 1, 20, 20);
                Free();

                RunConcurrentBC("1, 16", 5000000, 1, 16, 20, 20);
                Free();

                RunConcurrentBC("16, 16", 5000000, 16, 16, 20, 20);
                Free();

                Console.WriteLine();

                RunConcurrentBQ("1, 1", 5000000, 1, 1, 20, 20);
                Free();

                RunConcurrentBQ("4, 4", 5000000, 4, 4, 20, 20);
                Free();

                RunConcurrentBQ("16, 1", 5000000, 16, 1, 20, 20);
                Free();

                RunConcurrentBQ("1, 16", 5000000, 1, 16, 20, 20);
                Free();

                RunConcurrentBQ("16, 16", 5000000, 16, 16, 20, 20);
                Free();

                Console.WriteLine();

                RunConcurrentCondVar("1, 1", 5000000, 1, 1, 20, 20);
                Free();

                RunConcurrentCondVar("4, 4", 5000000, 4, 4, 20, 20);
                Free();

                RunConcurrentCondVar("16, 1", 5000000, 16, 1, 20, 20);
                Free();

                RunConcurrentCondVar("1, 16", 5000000, 1, 16, 20, 20);
                Free();

                RunConcurrentCondVar("16, 16", 5000000, 16, 16, 20, 20);
                Free();

                Console.WriteLine();

                RunConcurrentMon("1, 1", 5000000, 1, 1, 20, 20);
                Free();

                RunConcurrentMon("4, 4", 5000000, 4, 4, 20, 20);
                Free();

                RunConcurrentMon("16, 1", 5000000, 16, 1, 20, 20);
                Free();

                RunConcurrentMon("1, 16", 5000000, 1, 16, 20, 20);
                Free();

                RunConcurrentMon("16, 16", 5000000, 16, 16, 20, 20);
                Free();

                //RunConcurrentBC("Simple", 5000000, /*Environment.ProcessorCount */ 2, 2, 10, 100);//100 / Environment.ProcessorCount, 101);
                //Free();

                //RunConcurrentBQ("Simple", 5000000, /*Environment.ProcessorCount */ 2, 2, 10, 100);//100 / Environment.ProcessorCount, 101);
                //Free();


                //RunConcurrentBC("OverConcurrency", 5000000, /*4 * Environment.ProcessorCount*/ 8, 8, 0, 0);//100 / Environment.ProcessorCount, 101);
                //Free();

                //RunConcurrentBQ("OverConcurrency", 5000000, /*4 * Environment.ProcessorCount*/ 8, 8, 0, 0);//100 / Environment.ProcessorCount, 101);
                //Free();


                //RunConcurrentBC("OverConcurrency fix", 5000000, /*4 * Environment.ProcessorCount*/ 16, 16, 0, 0);
                //Free();

                //RunConcurrentBQ("OverConcurrency fix", 5000000, /*4 * Environment.ProcessorCount*/ 16, 16, 0, 0);
                //Free();


                Console.WriteLine();
            }
        }
    }
}
