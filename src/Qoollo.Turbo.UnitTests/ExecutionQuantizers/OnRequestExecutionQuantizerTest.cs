using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Threading.ExecutionQuantizers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.ExecutionTickers
{
    [TestClass]
    public class OnRequestExecutionQuantizerTest
    {
        [TestMethod]
        [Timeout(5000)]
        public void TestBasicFunctions()
        {
            OnRequestExecutionQuantizer testInst = new OnRequestExecutionQuantizer();

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
            OnRequestExecutionQuantizer testInst = new OnRequestExecutionQuantizer();
            int stageNum = 0;

            var task = Task.Run(() =>
                {
                    Interlocked.Increment(ref stageNum);
                    testInst.Tick();
                    Interlocked.Increment(ref stageNum);
                    testInst.Tick();
                    Interlocked.Increment(ref stageNum);
                });


            TimingAssert.AreEqual(5000, 1, () => Volatile.Read(ref stageNum), "StageNum = 1");

            testInst.AllowProcess();
            testInst.WaitForTickers();
            TimingAssert.AreEqual(5000, 2, () => Volatile.Read(ref stageNum), "StageNum = 2");

            testInst.AllowProcess();

            task.Wait();
            Assert.AreEqual(3, stageNum, "stageNum = 3");
        }
    }
}
