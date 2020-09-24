using Qoollo.Turbo.Threading;
using Qoollo.Turbo.Threading.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.PerformanceTests
{
    public class SemaphorePerfTest
    {
        private static TimeSpan TestSemaphore(string name, int elemCount, int addThCount, int takeThCount, int addSpin, int takeSpin)
        {
            Semaphore sem = new Semaphore(0, int.MaxValue);

            CancellationTokenSource srcCancel = new CancellationTokenSource();

            Thread[] addThreads = new Thread[addThCount];
            Thread[] takeThreads = new Thread[takeThCount];

            int addedElemCount = 0;

            Barrier barierStart = new Barrier(1 + addThreads.Length + takeThreads.Length);
            Barrier barierAdders = new Barrier(1 + addThreads.Length);
            Barrier barierTakers = new Barrier(1 + takeThreads.Length);

            Action addAction = () =>
            {
                barierStart.SignalAndWait();

                int index = 0;
                while ((index = Interlocked.Increment(ref addedElemCount)) <= elemCount)
                {
                    sem.Release();
                    SpinWaitHelper.SpinWait(addSpin);
                }

                barierAdders.SignalAndWait();
            };


            Action takeAction = () =>
            {
                CancellationToken myToken = srcCancel.Token;

                barierStart.SignalAndWait();

                try
                {
                    while (!srcCancel.IsCancellationRequested)
                    {
                        sem.WaitOne(1000);
                        SpinWaitHelper.SpinWait(takeSpin);
                    }
                }
                catch (OperationCanceledException)
                {
                }

                while (sem.WaitOne(0)) { }

                barierTakers.SignalAndWait();
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

            Console.WriteLine(name + ". Semaphore. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            return sw.Elapsed;
        }


        private static TimeSpan TestSemaphoreSlim(string name, int elemCount, int addThCount, int takeThCount, int addSpin, int takeSpin)
        {
            SemaphoreSlim sem = new SemaphoreSlim(0, int.MaxValue);

            CancellationTokenSource srcCancel = new CancellationTokenSource();

            Thread[] addThreads = new Thread[addThCount];
            Thread[] takeThreads = new Thread[takeThCount];

            int addedElemCount = 0;

            Barrier barierStart = new Barrier(1 + addThreads.Length + takeThreads.Length);
            Barrier barierAdders = new Barrier(1 + addThreads.Length);
            Barrier barierTakers = new Barrier(1 + takeThreads.Length);

            Action addAction = () =>
            {
                barierStart.SignalAndWait();

                int index = 0;
                while ((index = Interlocked.Increment(ref addedElemCount)) <= elemCount)
                {
                    sem.Release();
                    SpinWaitHelper.SpinWait(addSpin);
                }

                barierAdders.SignalAndWait();
            };


            Action takeAction = () =>
            {
                CancellationToken myToken = srcCancel.Token;

                barierStart.SignalAndWait();

                try
                {
                    while (!srcCancel.IsCancellationRequested)
                    {
                        sem.Wait(myToken);
                        SpinWaitHelper.SpinWait(takeSpin);
                    }
                }
                catch (OperationCanceledException)
                {
                }

                while (sem.Wait(0)) { }

                barierTakers.SignalAndWait();
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

            Console.WriteLine(name + ". SemaphoreSlim. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            return sw.Elapsed;
        }



        private static TimeSpan TestSemaphoreLight(string name, int elemCount, int addThCount, int takeThCount, int addSpin, int takeSpin)
        {
            SemaphoreLight sem = new SemaphoreLight(0, int.MaxValue);

            CancellationTokenSource srcCancel = new CancellationTokenSource();

            Thread[] addThreads = new Thread[addThCount];
            Thread[] takeThreads = new Thread[takeThCount];

            int addedElemCount = 0;

            Barrier barierStart = new Barrier(1 + addThreads.Length + takeThreads.Length);
            Barrier barierAdders = new Barrier(1 + addThreads.Length);
            Barrier barierTakers = new Barrier(1 + takeThreads.Length);

            Action addAction = () =>
            {
                barierStart.SignalAndWait();

                int index = 0;
                while ((index = Interlocked.Increment(ref addedElemCount)) <= elemCount)
                {
                    sem.Release();
                    SpinWaitHelper.SpinWait(addSpin);
                }

                barierAdders.SignalAndWait();
            };


            Action takeAction = () =>
            {
                CancellationToken myToken = srcCancel.Token;

                barierStart.SignalAndWait();

                try
                {
                    while (!srcCancel.IsCancellationRequested)
                    {
                        sem.Wait(myToken);
                        SpinWaitHelper.SpinWait(takeSpin);
                    }
                }
                catch (OperationCanceledException)
                {
                }

                while (sem.Wait(0)) { }

                barierTakers.SignalAndWait();
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

            Console.WriteLine(name + ". SemaphoreLight. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            return sw.Elapsed;
        }





        public static void RunTest()
        {
            //for (int i = 0; i < 3; i++)
            //    TestSemaphore("16, 4", 10000000, 16, 4, 2, 2);

            //for (int i = 0; i < 3; i++)
            //    TestSemaphoreSlim("16, 4", 10000000, 16, 4, 2, 2);

            //for (int i = 0; i < 3; i++)
            //    TestSemaphoreLight("8, 1", 10000000, 8, 1, 2, 2);

            for (int i = 0; i < 3; i++)
            {
                TestSemaphoreLight("1, 1", 10000000, 1, 1, 2, 2);
                //TestSemaphoreLight("2, 1", 10000000, 2, 1, 2, 2);
                //TestSemaphoreLight("1, 2", 10000000, 1, 2, 2, 2);
                TestSemaphoreLight("8, 8", 10000000, 8, 8, 2, 2);
                TestSemaphoreLight("1, 8", 10000000, 1, 8, 2, 2);
                TestSemaphoreLight("4, 16", 10000000, 4, 16, 2, 2);
                TestSemaphoreLight("8, 1", 10000000, 8, 1, 2, 2);
                TestSemaphoreLight("16, 4", 10000000, 16, 4, 2, 2);

                Console.WriteLine();
            }
        }
    }
}
