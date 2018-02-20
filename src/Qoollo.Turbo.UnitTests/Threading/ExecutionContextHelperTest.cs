using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Threading.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Threading
{
    [TestClass]
    public class ExecutionContextHelperTest: TestClassBase
    {
        class CustomSyncContext : SynchronizationContext
        {
        }

        [TestMethod]
        public void CaptureContextNoSyncContextTest()
        {
            var originalContext = SynchronizationContext.Current;
            var syncContext = new CustomSyncContext();
            try
            {
                SynchronizationContext.SetSynchronizationContext(syncContext);
                var eContext = ExecutionContextHelper.CaptureContextNoSyncContextIfPossible();
                Assert.IsNotNull(eContext);

                AtomicBool isDefaulContext = new AtomicBool(false);
                var task = Task.Run(() =>
                {
                    SynchronizationContext.SetSynchronizationContext(null);
                    ExecutionContextHelper.RunInContext(eContext, (st) =>
                    {
                        isDefaulContext.Value = SynchronizationContext.Current == null;
                    }, null, true);
                });


                task.Wait();
                TimingAssert.IsTrue(10000, ref isDefaulContext, "Default context expected");
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalContext);
            }
        }
    }
}
