using Qoollo.Turbo.Threading.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Qoollo.Turbo.PerformanceTests
{
    public class CancellationTokenRegistrationTest
    {
        private static void Callback(object obj)
        {

        }

        private static void TestPublicRegister(CancellationToken token, int iter)
        {
            Action<object> act = new Action<object>(Callback);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iter; i++)
            {
                using (var reg = token.Register(act, null, false))
                {

                }
            }
            sw.Stop();
            Console.WriteLine($"Public register: {sw.ElapsedMilliseconds}ms");
        }

        private static void TestGeneratedRegister(CancellationToken token, int iter)
        {
            Action<object> act = new Action<object>(Callback);

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < iter; i++)
            {
                using (var reg = CancellationTokenHelper.RegisterWithoutECIfPossible(token, act, null))
                {

                }
            }
            sw.Stop();
            Console.WriteLine($"Generated register: {sw.ElapsedMilliseconds}ms");
        }


        public static void RunTest()
        {
            CancellationTokenSource src = new CancellationTokenSource();
            var token = src.Token;

            for (int i = 0; i < 10; i++)
            {
                TestPublicRegister(token, 2000000);
                TestGeneratedRegister(token, 2000000);
            }
        }
    }
}
