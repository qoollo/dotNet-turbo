using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Threading.ExecutionTickers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.ExecutionTickers
{
    [TestClass]
    public class ExecutionTickerTest
    {
        [TestMethod]
        public void TestDefaultTick()
        {
            ExecutionTicker testInst = new ExecutionTicker();
            testInst.Tick();
        }
        [TestMethod]
        public void TestDefaultTickTimeout()
        {
            ExecutionTicker testInst = new ExecutionTicker();

            Stopwatch sw = Stopwatch.StartNew();

            testInst.Tick(100);

            sw.Stop();
            Assert.IsTrue(sw.ElapsedMilliseconds > 50);
        }
        [TestMethod]
        [Timeout(5000)]
        public void TestDefaultTickCancellation()
        {
            ExecutionTicker testInst = new ExecutionTicker();

            CancellationTokenSource cancSrc = new CancellationTokenSource();
            cancSrc.CancelAfter(100);

            testInst.Tick(-1, cancSrc.Token);

            Assert.IsTrue(cancSrc.IsCancellationRequested);
        }
    }
}
