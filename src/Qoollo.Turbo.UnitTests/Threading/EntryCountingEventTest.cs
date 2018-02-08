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
    public class EntryCountingEventTest : TestClassBase
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
        public void TestEnterExit()
        {
            using (EntryCountingEvent inst = new EntryCountingEvent())
            {
                using (var guard = inst.Enter())
                {
                    Assert.AreEqual(1, inst.CurrentCount);
                }
                Assert.AreEqual(0, inst.CurrentCount);
            }
        }


        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException), AllowDerivedTypes = true)]
        public void TestExitMoreTimesError()
        {
            using (EntryCountingEvent inst = new EntryCountingEvent())
            {
                inst.ExitClientCore();
            }
        }

        [TestMethod]
        public void TestTerminateWaiting()
        {
            using (EntryCountingEvent inst = new EntryCountingEvent())
            {
                bool finished = false;
                Task task = null;
                using (inst.Enter())
                {
                    task = Task.Run(() =>
                        {
                            inst.TerminateAndWait();
                            finished = true;
                        });

                    TimingAssert.IsFalse(5000, () => finished);
                }
                TimingAssert.IsTrue(5000, () => finished);

                task.Wait();
            }
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

                using (var guard = inst.TryEnter())
                {
                    Assert.IsTrue(guard.IsAcquired);
                    Assert.AreEqual(1, inst.CurrentCount);
                }
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
                using (var guard = inst.TryEnter())
                {
                }
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



        private void RunComplexTest()
        {
            using (EntryCountingEvent inst = new EntryCountingEvent())
            {
                Barrier bar = new Barrier(7);
                int threadFinished = 0;
                int entryCount = 0;
                bool isTestDispose = false;
                List<Task> taskList = new List<Task>();

                for (int i = 0; i < 6; i++)
                {
                    int a = i;
                    var task = Task.Run(() =>
                    {
                        Random rnd = new Random(a);
                        bar.SignalAndWait();
                        for (int j = 0; j < 1000; j++)
                        {
                            using (var eee = inst.TryEnter())
                            {
                                if (!eee.IsAcquired)
                                    break;

                                Interlocked.Increment(ref entryCount);

                                if (isTestDispose)
                                    throw new Exception();

                                Thread.Sleep(rnd.Next(10, 100));
                                if (isTestDispose)
                                    throw new Exception();
                            }
                        }

                        Interlocked.Increment(ref threadFinished);
                    });
                    taskList.Add(task);
                }

                bar.SignalAndWait();
                TimingAssert.IsTrue(5000, () => Volatile.Read(ref entryCount) > 12);
                inst.TerminateAndWait();
                isTestDispose = true;
                inst.Dispose();
                TimingAssert.AreEqual(5000, 6, () => Volatile.Read(ref threadFinished));

                Task.WhenAll(taskList);
            }
        }

        [TestMethod]
        public void ComplexTest()
        {
            for (int i = 0; i < 20; i++)
                RunComplexTest();
        }
    }
}
