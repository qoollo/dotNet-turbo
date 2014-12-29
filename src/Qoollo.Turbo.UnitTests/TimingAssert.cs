using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests
{
    internal static class TimingAssert
    {
        private static uint GetTimestamp()
        {
            return (uint)Environment.TickCount;
        }


        private static bool WaitUntil(Func<bool> func, int timeout)
        {
            if (timeout == 0)
                return func();

            if (timeout < 0)
            {
                while (!func())
                    Thread.Sleep(1);
            }

            uint startTime = GetTimestamp();

            while (!func() && (GetTimestamp() - startTime) < timeout)
                Thread.Sleep(1);

            return func();
        }



        public static void IsTrue(int timeout, Func<bool> cmp)
        {
            Assert.IsTrue(WaitUntil(cmp, timeout));
        }
        public static void IsTrue(int timeout, Func<bool> cmp, string message)
        {
            Assert.IsTrue(WaitUntil(cmp, timeout), message);
        }
        public static void IsTrue(int timeout, Func<bool> cmp, Func<string> message)
        {
            Assert.IsTrue(WaitUntil(cmp, timeout), message());
        }

        public static void IsFalse(int timeout, Func<bool> cmp)
        {
            Assert.IsTrue(WaitUntil(() => !cmp(), timeout));
        }
        public static void IsFalse(int timeout, Func<bool> cmp, string message)
        {
            Assert.IsTrue(WaitUntil(() => !cmp(), timeout), message);
        }


        public static void AreEqual<T>(int timeout, T expected, Func<T> getter)
        {
            var eq = EqualityComparer<T>.Default;
            if (WaitUntil(() => eq.Equals(expected, getter()), timeout))
                return;

            Assert.AreEqual(expected, getter());
        }
        public static void AreEqual<T>(int timeout, T expected, Func<T> getter, string message)
        {
            var eq = EqualityComparer<T>.Default;
            if (WaitUntil(() => eq.Equals(expected, getter()), timeout))
                return;

            Assert.AreEqual(expected, getter(), message);
        }
    }
}
