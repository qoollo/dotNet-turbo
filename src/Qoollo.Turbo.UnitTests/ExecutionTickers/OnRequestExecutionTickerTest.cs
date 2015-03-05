using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Threading.ExecutionTickers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.ExecutionTickers
{
    [TestClass]
    public class OnRequestExecutionTickerTest
    {
        [TestMethod]
        [Timeout(5000)]
        public void TestBasicFunctions()
        {
            OnRequestExecutionTicker testInst = new OnRequestExecutionTicker();

            Assert.AreEqual(0, testInst.TickWaiters);
            Assert.AreEqual(false, testInst.IsProcessAllowed);

            testInst.AllowProcess();
            Assert.AreEqual(true, testInst.IsProcessAllowed);

            testInst.Tick();

            Assert.AreEqual(0, testInst.TickWaiters);
            Assert.AreEqual(false, testInst.IsProcessAllowed);
        }

        [TestMethod]
        [Timeout(60000)]
        public void TestAsyncThreeStageWork()
        {
            OnRequestExecutionTicker testInst = new OnRequestExecutionTicker();
            int stageNum = 0;

            var task = Task.Run(() =>
                {
                    Interlocked.Increment(ref stageNum);
                    testInst.Tick();
                    Interlocked.Increment(ref stageNum);
                    testInst.Tick();
                    Interlocked.Increment(ref stageNum);
                });


            TimingAssert.AreEqual(5000, 1, () => Volatile.Read(ref stageNum));

            testInst.AllowProcess();
            testInst.WaitForTickers();
            Assert.AreEqual(2, stageNum);

            testInst.AllowProcess();

            task.Wait();
            Assert.AreEqual(3, stageNum);
        }
    }
}
