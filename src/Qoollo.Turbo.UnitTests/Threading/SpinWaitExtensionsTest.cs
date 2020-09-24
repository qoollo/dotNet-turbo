using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Threading.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Qoollo.Turbo.UnitTests.Threading
{
    [TestClass]
    public class SpinWaitExtensionsTest
    {
        [TestMethod]
        public void TestSpinWaitSpins()
        {
            SpinWait sw = new SpinWait();
            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, sw.Count);
                sw.SpinOnceNoSleep();
            }
        }

#if NET45 || NET46

        [TestMethod]
        public void TestSpinWaitNotSleep()
        {
            SpinWait sw = new SpinWait();
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                sw.SpinOnceNoSleep();
            }

            stopwatch.Stop();
            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= 5);
        }
#endif
    }
}
