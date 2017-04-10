using Qoollo.Turbo.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.PerformanceTests
{
    public static class EntryCountingEventPerfTest
    {
        private static TimeSpan MeasureInterlockedInc(int count, int thCount)
        {
            int value = 0;
            Barrier barStart = new Barrier(thCount + 1);
            Barrier barEnd = new Barrier(thCount + 1);

            Action act = () =>
            {
                barStart.SignalAndWait();
                while (Interlocked.Increment(ref value) < count)
                {

                }
                barEnd.SignalAndWait();
            };

            Thread[] threads = new Thread[thCount];

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(new ThreadStart(act));
                threads[i].Start();
            }

            barStart.SignalAndWait();
            Stopwatch sw = Stopwatch.StartNew();

            barEnd.SignalAndWait();
            sw.Stop();

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();

            Console.WriteLine($"Inc. Elapsed = {sw.ElapsedMilliseconds}ms");
            return sw.Elapsed;
        }

        private static TimeSpan MeasureCompExchg(int count, int thCount)
        {
            int value = 0;
            Barrier barStart = new Barrier(thCount + 1);
            Barrier barEnd = new Barrier(thCount + 1);

            Action act = () =>
            {
                barStart.SignalAndWait();
                          
                while (Volatile.Read(ref value) < count)
                {
                    int val = Volatile.Read(ref value);
                    SpinWait swait = new SpinWait();
                    while (val < count && Interlocked.CompareExchange(ref value, val + 1, val) != val)
                    {
                        swait.SpinOnce();
                        val = Volatile.Read(ref value);
                    }
                }
                barEnd.SignalAndWait();
            };

            Thread[] threads = new Thread[thCount];

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(new ThreadStart(act));
                threads[i].Start();
            }

            barStart.SignalAndWait();
            Stopwatch sw = Stopwatch.StartNew();

            barEnd.SignalAndWait();
            sw.Stop();

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();

            Console.WriteLine($"CmpExch. Elapsed = {sw.ElapsedMilliseconds}ms");
            return sw.Elapsed;
        }


        private static TimeSpan MeasureNormalProc(int count, int thCount, int spin)
        {
            int value = 0;
            Barrier barStart = new Barrier(thCount + 1);
            Barrier barEnd = new Barrier(thCount + 1);

            Action act = () =>
            {
                barStart.SignalAndWait();

                while (Interlocked.Increment(ref value) < count)
                {
                    Thread.SpinWait(spin);
                }

                barEnd.SignalAndWait();
            };

            Thread[] threads = new Thread[thCount];

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(new ThreadStart(act));
                threads[i].Start();
            }

            barStart.SignalAndWait();
            Stopwatch sw = Stopwatch.StartNew();

            barEnd.SignalAndWait();
            sw.Stop();

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();

            Console.WriteLine($"Normal. Elapsed = {sw.ElapsedMilliseconds}ms");
            return sw.Elapsed;
        }

        private static TimeSpan MeasureCountingProc(int count, int thCount, int spin)
        {
            int value = 0;
            Barrier barStart = new Barrier(thCount + 1);
            Barrier barEnd = new Barrier(thCount + 1);
            EntryCountingEvent inst = new EntryCountingEvent();

            Action act = () =>
            {
                barStart.SignalAndWait();

                while (Interlocked.Increment(ref value) < count)
                {
                    using (var guard = inst.TryEnterClientGuarded())
                    {
                        Thread.SpinWait(spin);
                    }
                }

                barEnd.SignalAndWait();
            };

            Thread[] threads = new Thread[thCount];

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(new ThreadStart(act));
                threads[i].Start();
            }

            barStart.SignalAndWait();
            Stopwatch sw = Stopwatch.StartNew();

            barEnd.SignalAndWait();
            sw.Stop();

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();
            inst.Dispose();

            Console.WriteLine($"Counting. Elapsed = {sw.ElapsedMilliseconds}ms");
            return sw.Elapsed;
        }



        public static void RunTest()
        {
            for (int i = 0; i < 10; i++)
            {
                MeasureNormalProc(50000000, 4, 25);
                MeasureCountingProc(50000000, 4, 25);

                Console.WriteLine();
            }
        }
    }
}
