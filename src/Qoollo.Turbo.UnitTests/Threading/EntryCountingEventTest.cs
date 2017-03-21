using Qoollo.Turbo.Threading;
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
    public class EntryCountingEventTest
    {
        [ClassInitialize]
        public static void Init(TestContext context)
        {
            TestLoggingHelper.Subscribe(context, false);
        }
        [ClassCleanup]
        public static void Cleanup()
        {
            TestLoggingHelper.Unsubscribe();
        }


        public TestContext TestContext { get; set; }

        //=============================




        [TestMethod]
        public void TestEnterExit()
        {
            EntryCountingEvent inst = new EntryCountingEvent();

            inst.EnterClient();
            Assert.AreEqual(1, inst.CurrentCount);

            inst.ExitClient();
            Assert.AreEqual(0, inst.CurrentCount);
        }


        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException), AllowDerivedTypes = true)]
        public void TestExitMoreTimesError()
        {
            EntryCountingEvent inst = new EntryCountingEvent();

            inst.ExitClient();
        }

        [TestMethod]
        public void TestTerminateWaiting()
        {
            EntryCountingEvent inst = new EntryCountingEvent();
            inst.EnterClient();

            bool finished = false;
            Task.Run(() =>
                {
                    inst.TerminateAndWait();
                    finished = true;
                });

            TimingAssert.IsFalse(5000, () => finished);

            inst.ExitClient();
            TimingAssert.IsTrue(5000, () => finished);
        }

        [TestMethod]
        public void TestResest()
        {
            using (EntryCountingEvent inst = new EntryCountingEvent())
            {
                inst.TerminateAndWait(100);
                Assert.IsTrue(inst.IsTerminated);

                inst.Reset();
                Assert.IsFalse(inst.IsTerminateRequested);
                Assert.IsFalse(inst.IsTerminated);

                Assert.IsTrue(inst.TryEnterClient());
                Assert.AreEqual(1, inst.CurrentCount);
                inst.ExitClient();
                Assert.AreEqual(0, inst.CurrentCount);
            }
        }


        private void EnterTerminateConcurrently()
        {
            EntryCountingEvent inst = new EntryCountingEvent();
            Barrier enterBar = new Barrier(2);

            List<Task> tasks = new List<Task>();

            tasks.Add(Task.Run(() =>
            {
                enterBar.SignalAndWait();
                if (inst.TryEnterClient())
                    inst.ExitClient();
            }));
            tasks.Add(Task.Run(() =>
            {
                enterBar.SignalAndWait();
                inst.Terminate();
            }));


            Task.WaitAll(tasks.ToArray());
            Assert.IsTrue(inst.Wait(0));
        }
        [TestMethod]
        public void EnterTerminateConcurrentlyTest()
        {
            for (int i = 0; i < 5000; i++)
                EnterTerminateConcurrently();
        }



        [TestMethod]
        public void ComplexTest()
        {
            EntryCountingEvent inst = new EntryCountingEvent();
            int threadFinished = 0;
            int entryCount = 0;
            bool isTestDispose = false;

            for (int i = 0; i < 6; i++)
            {
                int a = i;
                Task.Run(() =>
                {
                    Random rnd = new Random(a);
                    for (int j = 0; j < 1000; j++)
                    {
                        using (var eee = inst.TryEnterClientGuarded())
                        {
                            if (!eee.IsAcquired)
                                break;

                            Interlocked.Increment(ref entryCount);

                            if (isTestDispose)
                                throw new Exception();

                            Thread.Sleep(rnd.Next(100, 300));
                            if (isTestDispose)
                                throw new Exception();
                        }           
                    }

                    Interlocked.Increment(ref threadFinished);
                });
            }


            TimingAssert.IsTrue(5000, () => Volatile.Read(ref entryCount) > 12);
            inst.TerminateAndWait();
            isTestDispose = true;
            inst.Dispose();
            TimingAssert.AreEqual(5000, 6, () => Volatile.Read(ref threadFinished));
        }
    }
}
