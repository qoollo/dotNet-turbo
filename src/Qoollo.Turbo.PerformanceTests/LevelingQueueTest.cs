using Qoollo.Turbo.Queues;
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
    public static class LevelingQueueTest
    {
        /// <summary>
        /// Queue that stores items in memory
        /// </summary>
        /// <typeparam name="T">The type of elements in queue</typeparam>
        [DebuggerDisplay("Count = {Count}")]
        private class MemoryQueue2<T> : BlockingCollection<T>, IQueue<T>
        {
            /// <summary>
            /// MemoryQueue constructor
            /// </summary>
            /// <param name="boundedCapacity">The bounded size of the queue (if less or equeal to 0 then no limitation)</param>
            public MemoryQueue2(int boundedCapacity) : base(boundedCapacity) { }
            /// <summary>
            /// MemoryQueue constructor
            /// </summary>
            public MemoryQueue2() { }

            /// <summary>
            /// The bounded size of the queue (-1 means not bounded)
            /// </summary>
            long IQueue<T>.BoundedCapacity { get { return base.BoundedCapacity; } }
            /// <summary>
            /// Number of items inside the queue
            /// </summary>
            long IQueue<T>.Count { get { return base.Count; } }
            /// <summary>
            /// Indicates whether the queue is empty
            /// </summary>
            public bool IsEmpty { get { return base.Count == 0; } }

            public void AddForced(T item)
            {
                throw new NotImplementedException();
            }

            public bool TryPeek(out T item, int timeout, CancellationToken token)
            {
                throw new NotImplementedException();
            }
        }


        private static TimeSpan RunConcurrentMemQ(string name, int elemCount, int addThCount, int takeThCount, int addSpin, int takeSpin)
        {
            MemoryQueue<int> col = new MemoryQueue<int>(10000);

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
                    Thread.SpinWait(addSpin);
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
                        Thread.SpinWait(takeSpin);
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
                Console.WriteLine(name + ". LvlQ. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            return sw.Elapsed;
        }


        private static TimeSpan RunConcurrentLvlQ(string name, int elemCount, int addThCount, int takeThCount, int addSpin, int takeSpin)
        {
            LevelingQueue<int> col = new LevelingQueue<int>(new MemoryQueue<int>(3000), new MemoryQueue<int>(7000), LevelingQueueAddingMode.PreserveOrder, false);

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
                    Thread.SpinWait(addSpin);
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
                        Thread.SpinWait(takeSpin);
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
                Console.WriteLine(name + ". LvlQ. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            return sw.Elapsed;
        }


        private static void Free()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Thread.Sleep(1000);
        }


        public static void RunTest()
        {
            for (int i = 0; i < 10; i++)
            {
                //RunConcurrentMemQ("1, 1", 5000000, 1, 1, 10, 10);
                //Free();

                //RunConcurrentMemQ("4, 4", 5000000, 4, 4, 10, 10);
                //Free();

                //RunConcurrentMemQ("16, 1", 5000000, 16, 1, 10, 10);
                //Free();

                //RunConcurrentMemQ("1, 16", 5000000, 1, 16, 10, 10);
                //Free();

                //RunConcurrentMemQ("16, 16", 5000000, 16, 16, 10, 10);
                //Free();

                //Console.WriteLine();


                //RunConcurrentLvlQ("1, 1", 5000000, 1, 1, 10, 10);
                //Free();

                RunConcurrentLvlQ("4, 4", 5000000, 4, 4, 10, 10);
                Free();

                //RunConcurrentLvlQ("16, 1", 5000000, 16, 1, 10, 10);
                //Free();

                //RunConcurrentLvlQ("1, 16", 5000000, 1, 16, 10, 10);
                //Free();

                //RunConcurrentLvlQ("16, 16", 5000000, 16, 16, 10, 10);
                //Free();

                Console.WriteLine();
            }
        }
    }
}
