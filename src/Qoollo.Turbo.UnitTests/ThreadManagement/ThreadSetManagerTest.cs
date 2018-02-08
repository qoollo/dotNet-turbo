using Qoollo.Turbo.Threading.ThreadManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.ThreadManagement
{
    [TestClass]
    public class ThreadSetManagerTest : TestClassBase
    {
        [ClassInitialize]
        public static void Init(TestContext context)
        {
            SubscribeToUnhandledExceptions(context, false);
        }
        [ClassCleanup]
        public static void Cleanup()
        {
            UnsubscribeFromUnhandledExceptions();
        }


        //=============================

        [TestMethod]
        public void TestSimpleRunAndStop()
        {
            int threadStart = 0;

            using (DelegateThreadSetManager testInst = new DelegateThreadSetManager(Environment.ProcessorCount, "name", (id, state, token) =>
            {
                Interlocked.Increment(ref threadStart);
            }))
            {

                Assert.IsTrue(testInst.State == ThreadSetManagerState.Created, "State != Created");

                Assert.AreEqual(Environment.ProcessorCount, testInst.ThreadCount);
                Assert.IsFalse(testInst.IsWork);

                testInst.Start();

                SpinWait.SpinUntil(() => Volatile.Read(ref threadStart) >= testInst.ThreadCount, 10000);
                TestContext.WriteLine("All thread started");
                bool byCondition = SpinWait.SpinUntil(() => testInst.ActiveThreadCount == 0, 5000);
                TestContext.WriteLine(byCondition ? "ActiveThreadCount == 0" : "ActiveThreadCount != 0 (timeout)");

                TimingAssert.AreEqual(15000, Environment.ProcessorCount, () => Volatile.Read(ref threadStart));
                TimingAssert.IsTrue(5000, () => testInst.State == ThreadSetManagerState.AllThreadsExited, "State != AllThreadsExited");

                testInst.Stop();
                Assert.IsTrue(testInst.State == ThreadSetManagerState.Stopped, "State != Stopped");
            }
        }

        [TestMethod]
        public void TestRunAndStopWithLongWork()
        {
            int threadExit = 0;

            using (DelegateThreadSetManager testInst = new DelegateThreadSetManager(Environment.ProcessorCount, "name", (id, state, token) =>
            {
                Thread.Sleep(2500);
                Interlocked.Increment(ref threadExit);
            }))
            {

                Assert.IsTrue(testInst.State == ThreadSetManagerState.Created);

                Assert.AreEqual(Environment.ProcessorCount, testInst.ThreadCount);
                Assert.IsFalse(testInst.IsWork);

                testInst.Start();

                TimingAssert.IsTrue(15000, () => testInst.ActiveThreadCount == Environment.ProcessorCount);

                testInst.Stop();
                Assert.IsTrue(testInst.State == ThreadSetManagerState.Stopped);
                Assert.AreEqual(0, testInst.ActiveThreadCount);
                Assert.AreEqual(Environment.ProcessorCount, threadExit);
            }
        }


        [TestMethod]
        [Timeout(10 * 1000)]
        public void CanStopNotStartedManager()
        {
            int threadStart = 0;

            using (DelegateThreadSetManager testInst = new DelegateThreadSetManager(Environment.ProcessorCount, "name", (id, state, token) =>
            {
                Interlocked.Increment(ref threadStart);
            }))
            {
                testInst.Stop();
            }
        }


        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void TestCancellationOnStop()
        {
            int threadEnter = 0;
            int threadExit = 0;
            ManualResetEventSlim waiter = new ManualResetEventSlim(false);

            using (DelegateThreadSetManager testInst = new DelegateThreadSetManager(Environment.ProcessorCount, "name", (id, state, token) =>
            {
                try
                {
                    Interlocked.Increment(ref threadEnter);
                    waiter.Wait(token);
                }
                finally
                {
                    Interlocked.Increment(ref threadExit);
                }
            }))
            {

                testInst.Start();

                TimingAssert.IsTrue(15000, () => testInst.State == ThreadSetManagerState.Running);
                TimingAssert.IsTrue(15000, () => testInst.ActiveThreadCount == Environment.ProcessorCount);
                TimingAssert.IsTrue(15000, () => Volatile.Read(ref threadEnter) == Environment.ProcessorCount);

                testInst.Stop();
                Assert.IsTrue(testInst.State == ThreadSetManagerState.Stopped);
                Assert.AreEqual(0, testInst.ActiveThreadCount);
                Assert.AreEqual(Environment.ProcessorCount, threadExit, "threadExit != configurated thread count");
            }
        }


        [TestMethod]
        public void TestProperties()
        {
            int threadExit = 0;

            using (DelegateThreadSetManager testInst = new DelegateThreadSetManager(Environment.ProcessorCount, "name", (id, state, token) =>
            {
                Interlocked.Increment(ref threadExit);
            }))
            {

                Assert.IsNotNull(testInst.CurrentCulture);
                Assert.IsNotNull(testInst.CurrentUICulture);
                Assert.IsTrue(testInst.IsBackground == false);
                Assert.IsTrue(testInst.Priority == ThreadPriority.Normal);


                testInst.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
                testInst.CurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
                testInst.IsBackground = true;
                testInst.Priority = ThreadPriority.AboveNormal;

                Assert.AreEqual(System.Globalization.CultureInfo.InvariantCulture, testInst.CurrentCulture);
                Assert.AreEqual(System.Globalization.CultureInfo.InvariantCulture, testInst.CurrentUICulture);
                Assert.IsTrue(testInst.IsBackground == true);
                Assert.IsTrue(testInst.Priority == ThreadPriority.AboveNormal);


                testInst.Stop();
            }
        }

    }
}
