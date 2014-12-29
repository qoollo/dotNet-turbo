using Qoollo.Turbo.ObjectPools;
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
    public class StaticPoolManagerTest
    {
        [ClassInitialize]
        public static void Init(TestContext context)
        {
            TestLoggingHelper.Subscribe(context, true);
        }
        [ClassCleanup]
        public static void Cleanup()
        {
            TestLoggingHelper.Unsubscribe();
        }


        public TestContext TestContext { get; set; }

        //=============================


        [TestMethod]
        public void TestAddElements()
        {
            using (StaticPoolManager<int> testInst = new StaticPoolManager<int>())
            {
                Assert.AreEqual(0, testInst.ElementCount);
                Assert.AreEqual(0, testInst.FreeElementCount);

                for (int i = 0; i < 10; i++)
                {
                    testInst.AddElement(i);

                    Assert.AreEqual(i + 1, testInst.ElementCount);
                    Assert.AreEqual(i + 1, testInst.FreeElementCount);
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void TestNotAddToDisposed()
        {
            using (StaticPoolManager<int> testInst = new StaticPoolManager<int>())
            {
                testInst.Dispose();

                for (int i = 0; i < 10; i++)
                    testInst.AddElement(i);
            }
        }

        [TestMethod]
        public void TestSimpleRentRelease()
        {
            using (StaticPoolManager<int> testInst = new StaticPoolManager<int>())
            {
                Assert.AreEqual(0, testInst.ElementCount);
                Assert.AreEqual(0, testInst.FreeElementCount);

                for (int i = 0; i < 10; i++)
                    testInst.AddElement(i);

                Assert.AreEqual(10, testInst.ElementCount);
                Assert.AreEqual(10, testInst.FreeElementCount);

                for (int i = 0; i < 1000; i++)
                {
                    using (var el = testInst.Rent())
                    {
                        Assert.AreEqual(9, testInst.FreeElementCount);
                        Assert.IsTrue(el.IsValid);
                    }
                }

                Assert.AreEqual(10, testInst.FreeElementCount);
            }
        }



        [TestMethod]
        public void TestRentReleaseMany()
        {
            using (StaticPoolManager<int> testInst = new StaticPoolManager<int>())
            {
                for (int i = 0; i < 10; i++)
                    testInst.AddElement(i);

                List<RentedElementMonitor<int>> rented = new List<RentedElementMonitor<int>>();

                for (int i = 0; i < 10; i++)
                {
                    rented.Add(testInst.Rent());
                    Assert.AreEqual(10 - i - 1, testInst.FreeElementCount);
                }

                for (int i = 0; i < rented.Count; i++)
                {
                    rented[i].Dispose();
                    Assert.AreEqual(i + 1, testInst.FreeElementCount);
                }
            }
        }

        [TestMethod]
        public void TestDoubleReleaseNotThrow()
        {
            using (StaticPoolManager<int> testInst = new StaticPoolManager<int>())
            {
                testInst.AddElement(1);

                using (var item = testInst.Rent())
                {
                    item.Dispose();
                    item.Dispose();
                }
            }
        }

        [TestMethod]
        public void TestRemoveElement()
        {
            using (StaticPoolManager<int> testInst = new StaticPoolManager<int>())
            {
                Assert.AreEqual(0, testInst.ElementCount);
                Assert.AreEqual(0, testInst.FreeElementCount);

                for (int i = 0; i < 10; i++)
                    testInst.AddElement(i);

                Assert.AreEqual(10, testInst.ElementCount);
                Assert.AreEqual(10, testInst.FreeElementCount);

                for (int i = 0; i < 10; i++)
                {
                    using (var el = testInst.Rent())
                    {
                        Assert.IsTrue(el.IsValid);
                        testInst.RemoveElement(el);
                        Assert.IsFalse(el.IsValid);
                    }

                    Assert.AreEqual(10 - i - 1, testInst.ElementCount);
                    Assert.AreEqual(10 - i - 1, testInst.FreeElementCount);
                }
            }
        }

        [TestMethod]
        public void TestDestroyCalledOnRemove()
        {
            int destroyed = 0;
            Action<int> destroyAct = a =>
                {
                    Interlocked.Increment(ref destroyed);
                };

            using (StaticPoolManager<int> testInst = new StaticPoolManager<int>("a", destroyAct))
            {
                Assert.AreEqual(0, testInst.ElementCount);
                Assert.AreEqual(0, testInst.FreeElementCount);

                for (int i = 0; i < 10; i++)
                    testInst.AddElement(i);

                Assert.AreEqual(10, testInst.ElementCount);
                Assert.AreEqual(10, testInst.FreeElementCount);

                for (int i = 0; i < 10; i++)
                {
                    using (var el = testInst.Rent())
                    {
                        Assert.IsTrue(el.IsValid);
                        testInst.RemoveElement(el);
                        Assert.IsFalse(el.IsValid);
                    }

                    Assert.AreEqual(i + 1, destroyed);
                }

                Assert.AreEqual(10, destroyed);
                Assert.AreEqual(0, testInst.ElementCount);
            }
        }


        [TestMethod]
        public void TestDestroyCalledOnDispose()
        {
            int destroyed = 0;
            Action<int> destroyAct = a =>
            {
                Interlocked.Increment(ref destroyed);
            };

            using (StaticPoolManager<int> testInst = new StaticPoolManager<int>("a", destroyAct))
            {
                for (int i = 0; i < 10; i++)
                    testInst.AddElement(i);
            }

            Assert.AreEqual(10, destroyed);
        }


        [TestMethod]
        public void TestElementDestroyedAfterDispose()
        {
            RentedElementMonitor<int> elementMonitor = default(RentedElementMonitor<int>);
            Turbo.ObjectPools.Common.PoolElementWrapper<int> elemWrapper = null;

            try
            {
                using (StaticPoolManager<int> testInst = new StaticPoolManager<int>())
                {
                    testInst.AddElement(1);

                    elementMonitor = testInst.Rent();
                    elemWrapper = elementMonitor.ElementWrapper;
                }

                Assert.IsTrue(elementMonitor.IsValid);
                Assert.IsFalse(elemWrapper.IsElementDestroyed);
            }
            finally
            {
                if (!object.ReferenceEquals(elementMonitor, null))
                    elementMonitor.Dispose();
            }

            Assert.IsTrue(elemWrapper.IsElementDestroyed);
        }



        [TestMethod]
        [Timeout(10 * 1000)]
        public void TestRentWait()
        {
            using (StaticPoolManager<int> testInst = new StaticPoolManager<int>())
            {
                for (int i = 0; i < 1; i++)
                    testInst.AddElement(i);

                var item = testInst.Rent();

                Task.Delay(500).ContinueWith(t => item.Dispose());

                using (var item2 = testInst.Rent())
                {
                    Assert.IsTrue(item2.IsValid);
                }
            }
        }



        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public void TestRentTimeout()
        {
            using (StaticPoolManager<int> testInst = new StaticPoolManager<int>())
            {
                using (var item = testInst.Rent(100))
                {
                }
            }
        }

        [TestMethod]
        public void TestRentTimeoutWithoutException()
        {
            using (StaticPoolManager<int> testInst = new StaticPoolManager<int>())
            {
                using (var item = testInst.Rent(100, false))
                {
                    Assert.IsFalse(item.IsValid);
                }
            }
        }


        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public void TestRentCancelled()
        {
            CancellationTokenSource tokSrc = new CancellationTokenSource();
            Task.Delay(200).ContinueWith(t => tokSrc.Cancel());

            using (StaticPoolManager<int> testInst = new StaticPoolManager<int>())
            {
                using (var item = testInst.Rent(tokSrc.Token))
                {
                }
            }
        }

        [TestMethod]
        public void TestRentCancelledWithoutException()
        {
            CancellationTokenSource tokSrc = new CancellationTokenSource();
            Task.Delay(200).ContinueWith(t => tokSrc.Cancel());

            using (StaticPoolManager<int> testInst = new StaticPoolManager<int>())
            {
                using (var item = testInst.Rent(tokSrc.Token, false))
                {
                    Assert.IsFalse(item.IsValid);
                }
            }
        }


        [TestMethod]
        [ExpectedException(typeof(CantRetrieveElementException))]
        public void TestDisposeCancellWaiters()
        {
            using (StaticPoolManager<int> testInst = new StaticPoolManager<int>())
            {
                Task.Delay(400).ContinueWith(t => testInst.Dispose());

                using (var item = testInst.Rent())
                {
                }
            }
        }


        [TestMethod]
        public void TestDisposeCancellWaitersWithoutException()
        {
            using (StaticPoolManager<int> testInst = new StaticPoolManager<int>())
            {
                Task.Delay(400).ContinueWith(t => testInst.Dispose());

                using (var item = testInst.Rent(false))
                {
                    Assert.IsFalse(item.IsValid);
                }
            }
        }



        private void RunComplexTest(StaticPoolManager<int> testInst, int threadCount, int opCount, int pauseSpin, bool autoAddRemove)
        {
            Thread[] threads = new Thread[threadCount];
            Barrier startBar = new Barrier(threadCount + 1);

            int opCountPerThread = opCount / threadCount;

            Action thAct = () =>
            {
                Random localRand = new Random(Thread.CurrentThread.ManagedThreadId + Environment.TickCount);
                int pasueDiff = pauseSpin / 4;

                startBar.SignalAndWait();

                int execOp = 0;
                while (execOp++ < opCountPerThread)
                {
                    if (autoAddRemove && (testInst.ElementCount == 0 || localRand.Next(5) == 0))
                        testInst.AddElement(execOp);

                    using (var el = testInst.Rent())
                    {
                        if (autoAddRemove && testInst.ElementCount > 1 && localRand.Next(5) == 0)
                            testInst.RemoveElement(el);

                        int spinCount = localRand.Next(pauseSpin - pasueDiff, pauseSpin + pasueDiff);
                        Thread.SpinWait(spinCount);
                    }
                }

                if (autoAddRemove)
                    testInst.AddElement(execOp);
            };


            for (int i = 0; i < threads.Length; i++)
                threads[i] = new Thread(new ThreadStart(thAct));

            for (int i = 0; i < threads.Length; i++)
                threads[i].Start();

            startBar.SignalAndWait();

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();

            Assert.AreEqual(testInst.ElementCount, testInst.FreeElementCount);
        }



        [TestMethod]
        [Timeout(4 * 60 * 1000)]
        public void ComplexTest()
        {
            using (StaticPoolManager<int> testInst = new StaticPoolManager<int>())
            {
                for (int i = testInst.ElementCount; i < 1; i++)
                    testInst.AddElement(i);

                RunComplexTest(testInst, 1, 100000, 10, false);

                for (int i = testInst.ElementCount; i < Environment.ProcessorCount; i++)
                    testInst.AddElement(i);

                RunComplexTest(testInst, Environment.ProcessorCount, 1000000, 10, false);

                for (int i = testInst.ElementCount; i < 2 * Environment.ProcessorCount; i++)
                    testInst.AddElement(i);

                RunComplexTest(testInst, Environment.ProcessorCount, 1000000, 10, false);




                RunComplexTest(testInst, 1, 100000, 10, true);
                RunComplexTest(testInst, Environment.ProcessorCount, 2000000, 10, true);
                RunComplexTest(testInst, Environment.ProcessorCount, 1000000, 100, true);
            }
        }
    }
}
