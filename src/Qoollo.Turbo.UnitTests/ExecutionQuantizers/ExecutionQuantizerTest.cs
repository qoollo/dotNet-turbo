using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Threading.ExecutionQuantizers;
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
    public class ExecutionQuantizerTest
    {
        [TestMethod]
        public void TestDefaultTick()
        {
            ExecutionQuantizer testInst = new ExecutionQuantizer();
            testInst.Tick();
        }
        [TestMethod]
        public void TestDefaultTickTimeout()
        {
            ExecutionQuantizer testInst = new ExecutionQuantizer();

            Stopwatch sw = Stopwatch.StartNew();

            testInst.Tick(100);

            sw.Stop();
            Assert.IsTrue(sw.ElapsedMilliseconds > 50);
        }
        [TestMethod]
        [Timeout(5000)]
        public void TestDefaultTickCancellation()
        {
            ExecutionQuantizer testInst = new ExecutionQuantizer();

            CancellationTokenSource cancSrc = new CancellationTokenSource();
            cancSrc.CancelAfter(100);

            testInst.Tick(-1, cancSrc.Token);

            Assert.IsTrue(cancSrc.IsCancellationRequested);
        }
    }
}
