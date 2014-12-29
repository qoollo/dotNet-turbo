using Qoollo.Turbo.Threading.ServiceStuff;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Threading
{
    [TestClass]
    public class ManagementThreadControllerTest
    {
        [TestMethod]
        public void TestRegisterDeregister()
        {
            int calledCount = 0;

            ManagementThreadControllerCallback act = (elapsed) =>
                {
                    Interlocked.Increment(ref calledCount);
                    return true;
                };


            ManagementThreadController.Instance.RegisterCallback(act);

            TimingAssert.IsTrue(10 * ManagementThreadController.SleepPeriod, () => Volatile.Read(ref calledCount) > 0);

            ManagementThreadController.Instance.UnregisterCallback(act);
            int lastCallCount = calledCount;

            TimingAssert.AreEqual(2 * ManagementThreadController.SleepPeriod, lastCallCount, () => Volatile.Read(ref calledCount));
        }
    }
}
