using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Threading.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Qoollo.Turbo.UnitTests.Threading
{
    [TestClass]
    public class ExecutionContextHelperTest
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
                var eContext = ExecutionContextHelper.CaptureContextNoSyncContext();
                Assert.IsNotNull(eContext);

                bool isDefaulContext = false;
                ExecutionContext.Run(eContext, (st) =>
                {
                    isDefaulContext = SynchronizationContext.Current == null;
                }, null);

                Assert.IsTrue(isDefaulContext, "Default context expected");
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalContext);
            }
        }
    }
}
