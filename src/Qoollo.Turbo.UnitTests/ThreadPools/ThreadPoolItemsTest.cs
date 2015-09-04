using Qoollo.Turbo.Threading.ThreadPools.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.ThreadPools
{
    [TestClass]
    public class ThreadPoolItemsTest
    {
        [TestMethod]
        public void TestSimpleCall()
        {
            int value = 0;
            Action act = () =>
                {
                    value++;
                };

            var testInst = new ActionThreadPoolWorkItem(act);

            testInst.Run(false, false);

            Assert.AreEqual(1, value);
        }


        [TestMethod]
        public void TestCallWithExecutionContext()
        {
            int value = 0;
            Action act = () =>
            {
                value++;
            };

            var testInst = new ActionThreadPoolWorkItem(act);
            testInst.CaptureExecutionContext(false);

            testInst.Run(true, true);

            Assert.AreEqual(1, value);
        }


        [TestMethod]
        public void TestSendOrPostCallbackSyncThreadPoolWorkItem()
        {
            int value = 0;
            System.Threading.SendOrPostCallback act = (s) =>
                {
                    Interlocked.Increment(ref value);
                };

            var item = new SendOrPostCallbackSyncThreadPoolWorkItem(act, null);

            bool waitFinished = false;
            int startedFlag = 0;
            Task.Run(() =>
                {
                    Interlocked.Exchange(ref startedFlag, 1);
                    item.Wait();
                    Volatile.Write(ref waitFinished, true);
                });

            TimingAssert.IsTrue(5000, () => Volatile.Read(ref startedFlag) == 1);
            Thread.Sleep(100);
            Assert.AreEqual(0, value);
            Assert.AreEqual(false, waitFinished);

            item.Run(false, false);

            TimingAssert.AreEqual(5000, 1, () => Volatile.Read(ref value));
            TimingAssert.AreEqual(5000, true, () => Volatile.Read(ref waitFinished));
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes=false)]
        public void TestSendOrPostCallbackSyncThreadPoolWorkItemExceptions()
        {
            System.Threading.SendOrPostCallback act = (s) =>
            {
                throw new Exception();
            };

            var item = new SendOrPostCallbackSyncThreadPoolWorkItem(act, null);

            Task.Delay(1000).ContinueWith(t => item.Run(false, false));

            item.Wait();
        }

        [TestMethod]
        public void TestTaskThreadPoolWorkItemExecute()
        {
            int wasExecuted = 0;

            var item = new TaskThreadPoolWorkItem(() =>
            {
                Interlocked.Exchange(ref wasExecuted, 1);
            });

            item.Run(false, true);

            Assert.AreEqual(1, wasExecuted);
            Assert.IsTrue(item.Task.IsCompleted);
        }
        [TestMethod]
        public void TestTaskThreadPoolWorkItemWithParamExecute()
        {
            int wasExecuted = 0;
            int stateVal = 0;

            var item = new TaskThreadPoolWorkItem<int>((state) =>
            {
                stateVal = state;
                Interlocked.Exchange(ref wasExecuted, 1);
            }, 100);

            item.Run(false, true);

            Assert.AreEqual(1, wasExecuted);
            Assert.AreEqual(100, stateVal);
            Assert.IsTrue(item.Task.IsCompleted);
        }

        [TestMethod]
        public void TestTaskFuncThreadPoolWorkItemExecute()
        {
            int wasExecuted = 0;

            var item = new TaskFuncThreadPoolWorkItem<int>(() =>
            {
                Interlocked.Exchange(ref wasExecuted, 1);
                return 2;
            });

            item.Run(false, true);

            Assert.AreEqual(1, wasExecuted);
            Assert.IsTrue(item.Task.IsCompleted);
            Assert.AreEqual(2, item.Task.Result);
        }
        [TestMethod]
        public void TestTaskFuncThreadPoolWorkItemWithParamExecute()
        {
            int wasExecuted = 0;
            int stateVal = 0;

            var item = new TaskFuncThreadPoolWorkItem<int, int>((state) =>
            {
                stateVal = state;
                Interlocked.Exchange(ref wasExecuted, 1);
                return 2;
            }, 100);

            item.Run(false, true);

            Assert.AreEqual(1, wasExecuted);
            Assert.AreEqual(100, stateVal);
            Assert.IsTrue(item.Task.IsCompleted);
            Assert.AreEqual(2, item.Task.Result);
        }
    }
}
