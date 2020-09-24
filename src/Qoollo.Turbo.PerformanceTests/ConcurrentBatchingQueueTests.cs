using Qoollo.Turbo.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Qoollo.Turbo.PerformanceTests
{
    public static class ConcurrentBatchingQueueTests
    {
        private static void TestEnqueueOnly(int elemCount, int threadCount, int batchSize, bool useRandom)
        {
            ConcurrentBatchingQueue<long> q = new ConcurrentBatchingQueue<long>(batchSize);

            int atomicRandom = 0;

            int trackElemCount = elemCount;
            int addFinished = 0;

            Thread[] threadsAdd = new Thread[threadCount];

            Action addAction = () =>
            {
                Random rnd = null;
                if (useRandom)
                    rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * threadCount * 2);

                while (true)
                {
                    int item = Interlocked.Decrement(ref trackElemCount);
                    if (item < 0)
                        break;

                    q.Enqueue(item);

                    int sleepTime = 0;
                    if (rnd != null)
                        sleepTime = rnd.Next(elemCount / 10000) - elemCount / 10000 + 2;
                    if (sleepTime > 0)
                        Thread.Sleep(sleepTime);
                }

                Interlocked.Increment(ref addFinished);
            };


            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i] = new Thread(new ThreadStart(addAction));

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i].Start();


            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i].Join();


            sw.Stop();

            Console.WriteLine($"ThreadCount: {threadCount}, ElementCount: {elemCount}, BatchSize: {batchSize}, Time: {sw.ElapsedMilliseconds}ms");
        }

        public static void RunTest()
        {
            for (int i = 0; i < 10; i++)
            {
                TestEnqueueOnly(20000000, 1, 32, false);
                TestEnqueueOnly(20000000, 2, 32, false);
                TestEnqueueOnly(20000000, 8, 32, false);
                TestEnqueueOnly(20000000, 8, 1024, false);
                TestEnqueueOnly(10000000, 4, 1, false);

                Console.WriteLine();
            }
        }
    }
}
