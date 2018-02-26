using Qoollo.Turbo.ObjectPools.Common;
using Qoollo.Turbo.ObjectPools.ServiceStuff.ElementContainers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.ObjectPools
{
    [TestClass]
    public class SimpleElementsContainerTest : TestClassBase
    {
        [ClassInitialize]
        public static void Init(TestContext context)
        {
            SubscribeToUnhandledExceptions(context, true);
        }
        [ClassCleanup]
        public static void Cleanup()
        {
            UnsubscribeFromUnhandledExceptions();
        }

        //=============================

        private class PoolOperations : Qoollo.Turbo.ObjectPools.Common.DefaultPoolElementOperationSource<int>
        {
        }


        [TestMethod]
        public void TestAddElementAndMakeAvailable()
        {
            SimpleElementsContainer<int> testInst = new SimpleElementsContainer<int>();
            try
            {
                Assert.AreEqual(0, testInst.Count);
                Assert.AreEqual(0, testInst.AvailableCount);

                var wrapper = testInst.Add(100, new PoolOperations(), true);
                Assert.IsNotNull(wrapper);
                Assert.IsFalse(wrapper.IsBusy);
                Assert.AreEqual(100, wrapper.Element);

                Assert.AreEqual(1, testInst.Count);
                Assert.AreEqual(1, testInst.AvailableCount);
            }
            finally
            {
                testInst.ProcessAllElements(o => o.MarkElementDestroyed());
            }
        }


        [TestMethod]
        public void TestAddElementAndNotMakeAvailable()
        {
            SimpleElementsContainer<int> testInst = new SimpleElementsContainer<int>();
            try
            {
                Assert.AreEqual(0, testInst.Count);
                Assert.AreEqual(0, testInst.AvailableCount);

                var wrapper = testInst.Add(100, new PoolOperations(), false);
                Assert.IsNotNull(wrapper);
                Assert.IsTrue(wrapper.IsBusy);
                Assert.AreEqual(100, wrapper.Element);

                Assert.AreEqual(1, testInst.Count);
                Assert.AreEqual(0, testInst.AvailableCount);
            }
            finally
            {
                testInst.ProcessAllElements(o => o.MarkElementDestroyed());
            }
        }


        [TestMethod]
        public void TestSimpleTakeRelease()
        {
            SimpleElementsContainer<int> testInst = new SimpleElementsContainer<int>();
            try
            {
                for (int i = 0; i < 10; i++)
                    testInst.Add(i, new PoolOperations(), true);

                Assert.AreEqual(10, testInst.Count);
                Assert.AreEqual(10, testInst.AvailableCount);

                var item = testInst.Take();
                Assert.IsNotNull(item);
                Assert.IsTrue(item.Element >= 0 && item.Element < 10);
                Assert.IsTrue(item.IsBusy);
                Assert.IsFalse(item.IsElementDestroyed);


                Assert.AreEqual(10, testInst.Count);
                Assert.AreEqual(9, testInst.AvailableCount);


                testInst.Release(item);
                Assert.IsFalse(item.IsBusy);

                Assert.AreEqual(10, testInst.Count);
                Assert.AreEqual(10, testInst.AvailableCount);
            }
            finally
            {
                testInst.ProcessAllElements(o => o.MarkElementDestroyed());
            }
        }



        [TestMethod]
        public void TestTakeUntilEmpty()
        {
            SimpleElementsContainer<int> testInst = new SimpleElementsContainer<int>();
            try
            {
                for (int i = 0; i < 10; i++)
                    testInst.Add(i, new PoolOperations(), true);

                List<PoolElementWrapper<int>> takenElems = new List<PoolElementWrapper<int>>();
                PoolElementWrapper<int> item;

                for (int i = 0; i < 10; i++)
                {
                    bool takeRes = testInst.TryTake(out item, 0, new CancellationToken());
                    takenElems.Add(item);

                    Assert.IsTrue(takeRes);
                    Assert.IsNotNull(item);
                    Assert.IsTrue(item.IsBusy);
                }

                Assert.AreEqual(0, testInst.AvailableCount);

                bool takeResO = testInst.TryTake(out item, 0, new CancellationToken());
                Assert.IsFalse(takeResO);

                for (int i = 0; i < takenElems.Count; i++)
                    testInst.Release(takenElems[i]);
            }
            finally
            {
                testInst.ProcessAllElements(o => o.MarkElementDestroyed());
            }
        }


        [TestMethod]
        public void TestTakeBlocks()
        {
            SimpleElementsContainer<int> testInst = new SimpleElementsContainer<int>();
            CancellationTokenSource tokSrc = new CancellationTokenSource();

            bool wasCancelled = false;
            bool wasEntered = false;
            bool wasExited = false;

            Task.Run(() =>
                {
                    try
                    {
                        Volatile.Write(ref wasEntered, true);
                        PoolElementWrapper<int> item;
                        testInst.TryTake(out item, -1, tokSrc.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        Volatile.Write(ref wasCancelled, true);
                    }
                    Volatile.Write(ref wasExited, true);
                });


            TimingAssert.IsTrue(10000, () => Volatile.Read(ref wasEntered));
            Thread.Sleep(100);

            Assert.IsFalse(Volatile.Read(ref wasExited));

            tokSrc.Cancel();
            TimingAssert.IsTrue(10000, () => Volatile.Read(ref wasExited));

            Assert.IsTrue(Volatile.Read(ref wasCancelled));
        }


        [TestMethod]
        public void TakeDestroyReleaseWorkAsRemove()
        {
            SimpleElementsContainer<int> testInst = new SimpleElementsContainer<int>();
            try
            {
                for (int i = 0; i < 10; i++)
                    testInst.Add(i, new PoolOperations(), true);

                for (int i = 0; i < 10; i++)
                {
                    var item = testInst.Take();
                    item.MarkElementDestroyed();
                    testInst.Release(item);


                    Assert.IsTrue(item.IsRemoved);

                    Assert.IsTrue(item.IsRemoved);
                    Assert.AreEqual(10 - i - 1, testInst.AvailableCount);
                    Assert.AreEqual(10 - i - 1, testInst.Count);
                }
            }
            finally
            {
                testInst.ProcessAllElements(o => o.MarkElementDestroyed());
            }
        }


        [TestMethod]
        public void TestRemoveOnTake()
        {
            SimpleElementsContainer<int> testInst = new SimpleElementsContainer<int>();
            try
            {
                for (int i = 0; i < 10; i++)
                    testInst.Add(i, new PoolOperations(), true);

                testInst.ProcessAllElements(o => o.MarkElementDestroyed());

                PoolElementWrapper<int> item;
                bool takeResult = testInst.TryTake(out item, 0, new CancellationToken());
                Assert.IsFalse(takeResult);

                Assert.AreEqual(0, testInst.Count);
            }
            finally
            {
                testInst.ProcessAllElements(o => o.MarkElementDestroyed());
            }
        }



        [TestMethod]
        public void TestRescanWorks()
        {
            SimpleElementsContainer<int> testInst = new SimpleElementsContainer<int>();
            try
            {
                for (int i = 0; i < 10; i++)
                    testInst.Add(i, new PoolOperations(), true);

                testInst.ProcessAllElements(o => o.MarkElementDestroyed());
                testInst.RescanContainer();

                Assert.AreEqual(0, testInst.Count);
                Assert.AreEqual(0, testInst.AvailableCount);
            }
            finally
            {
                testInst.ProcessAllElements(o => o.MarkElementDestroyed());
            }
        }




        private void RunComplexTest(SimpleElementsContainer<int> testInst, int threadCount, int opCount, int pauseSpin)
        {
            Assert.AreEqual(testInst.AvailableCount, testInst.Count);

            Thread[] threads = new Thread[threadCount];
            Barrier startBar = new Barrier(threadCount + 1);

            int opCountPerThread = opCount / threadCount;

            Action thAct = () =>
            {
                startBar.SignalAndWait();
                TestContext.WriteLine("Inside thread. Signal and wait passed");

                try
                {
                    int execOp = 0;
                    while (execOp++ < opCountPerThread)
                    {
                        PoolElementWrapper<int> item = null;
                        try
                        {
                            item = testInst.Take();
                            //Thread.Sleep(pauseSpin);
                            Thread.SpinWait(pauseSpin);
                        }
                        finally
                        {
                            testInst.Release(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine("Unhandled exception: " + ex.ToString());
                    throw;
                }
            };


            for (int i = 0; i < threads.Length; i++)
                threads[i] = new Thread(new ThreadStart(thAct));

            TestContext.WriteLine("Threads created");

            for (int i = 0; i < threads.Length; i++)
                threads[i].Start();

            TestContext.WriteLine("Threads started");

            startBar.SignalAndWait();

            TestContext.WriteLine("Threads before join");

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();

            TestContext.WriteLine("All threads stopped");

            Assert.AreEqual(testInst.AvailableCount, testInst.Count);
        }


        [TestMethod]
        public void ComplexTest()
        {
            SimpleElementsContainer<int> testInst = new SimpleElementsContainer<int>();
            try
            {
                for (int i = 0; i < 1; i++)
                    testInst.Add(i, new PoolOperations(), true);

                TestContext.WriteLine("ComplexTest started");

                RunComplexTest(testInst, Environment.ProcessorCount, 100000, 10);

                TestContext.WriteLine("ComplexTest phase 1 finished");

                for (int i = testInst.Count; i < Environment.ProcessorCount; i++)
                    testInst.Add(i, new PoolOperations(), true);

                RunComplexTest(testInst, Environment.ProcessorCount, 1000000, 10);

                TestContext.WriteLine("ComplexTest phase 2 finished");

                for (int i = testInst.Count; i < 2 * Environment.ProcessorCount; i++)
                    testInst.Add(i, new PoolOperations(), true);

                RunComplexTest(testInst, Environment.ProcessorCount, 1000000, 10);

                TestContext.WriteLine("ComplexTest phase 3 finished");
            }
            finally
            {
                testInst.ProcessAllElements(o => o.MarkElementDestroyed());
            }
        }

    }
}
