using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.PerformanceTests
{
    public static class ThreadPoolTaskSpawnPerformanceTest
    {
        private static int Variable = 0;
        private static void InnerAction()
        {
            Interlocked.Increment(ref Variable);
        }

        private static void RunTestOnSystemThreadPool(int elemCount)
        {
            WaitCallback clb = new WaitCallback(s => InnerAction());
            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < elemCount; i++)
                ThreadPool.QueueUserWorkItem(clb);

            sw.Stop();

            Console.WriteLine("RunTestOnSystemThreadPool. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }

        private static void RunTestOnNewSystemThreadPoolWrapper(int elemCount)
        {
            Action clb = new Action(InnerAction);
            Qoollo.Turbo.Threading.ThreadPools.SystemThreadPool pool = new Threading.ThreadPools.SystemThreadPool();

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < elemCount; i++)
                pool.Run(clb);

            sw.Stop();

            Console.WriteLine("RunTestOnNewSystemThreadPoolWrapper. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }



        public static void RunTest()
        {
            for (int i = 0; i < 10; i++)
            {
                RunTestOnSystemThreadPool(2000000);
                Thread.Sleep(5000);

                RunTestOnNewSystemThreadPoolWrapper(2000000);
                Thread.Sleep(5000);
            }
        }
    }
}
