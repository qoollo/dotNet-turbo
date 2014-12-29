using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.PerformanceTests
{
    public static class InliningTest
    {
        private static int A = 0;
        private static int B = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DoWithA()
        {
            A++;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DoWithB()
        {
            B++;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DoCombo()
        {
            DoWithA();
            DoWithB();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DoDoubleCombo()
        {
            DoCombo();
        }

        
        private static void TestCallCombo(int count)
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                DoCombo();
            }
            sw.Stop();

            Console.WriteLine("Combo. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
        }

        private static void TestCallDoubleCombo(int count)
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                DoDoubleCombo();
            }
            sw.Stop();

            Console.WriteLine("DoubleCombo. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
        }

        private static void TestCallSplit(int count)
        {
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                DoWithA();
                DoWithB();
            }
            sw.Stop();

            Console.WriteLine("CallSplit. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
        }





        public static void RunTest()
        {
            for (int i = 0; i < 10; i++)
            {
                TestCallSplit(100000000);
                TestCallCombo(100000000);
                TestCallDoubleCombo(100000000);

                Console.WriteLine();
            }
        }
    }
}
