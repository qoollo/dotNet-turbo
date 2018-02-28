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
    public class CancellationTokenHelperTest : TestClassBase
    {
        class CustomSyncContext: SynchronizationContext
        {
        }

        [TestMethod]
        public void RegisterWithoutECTest()
        {
            var originalContext = SynchronizationContext.Current;
            var syncContext = new CustomSyncContext();
            try
            {
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                CancellationToken token = tokenSource.Token;

                AtomicBool isNoExecutionContext = new AtomicBool(false);

                using (CancellationTokenHelper.RegisterWithoutECIfPossible(token, (st) =>
                {
                    isNoExecutionContext.Value = SynchronizationContext.Current == null;
                }, null))
                {
                    Barrier barrier = new Barrier(2);
                    Barrier barrier2 = new Barrier(2);

                    Task.Run(() =>
                    {
                        barrier.SignalAndWait();
                        barrier2.SignalAndWait();
                        tokenSource.Cancel();
                    });

                    barrier.SignalAndWait();
                    SynchronizationContext.SetSynchronizationContext(syncContext);
                    barrier2.SignalAndWait();
                    TimingAssert.IsTrue(10000, isNoExecutionContext, "isNoExecutionContext");
                }
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalContext);
            }
        }
    }
}
