using Qoollo.Turbo.ObjectPools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.ObjectPools
{
    [TestClass]
    public class BalancingDynamicPoolManagerTest
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


        private class TestPoolElem
        {
            public TestPoolElem(int val)
            {
                Value = val;
            }

            public int Value { get; private set; }
            public bool IsDestroyed { get; set; }
            public void MakeInvalid() { Value = -1; }
        }

        private class TestDynamicPool : BalancingDynamicPoolManager<TestPoolElem>
        {
            private int _seed = Environment.TickCount;

            public TestDynamicPool(int min, int max) :
                base(min, max, "a")
            {
                CanCreateElement = true;
            }
            public TestDynamicPool(int min, int max, int trimPeriod) :
                base(min, max, "a", trimPeriod)
            {
                CanCreateElement = true;
            }

            public bool CanCreateElement { get; set; }
            public bool AlwaysCreateNew { get; set; }

            protected override bool CreateElement(out TestPoolElem elem, int timeout, CancellationToken token)
            {
                if (!CanCreateElement)
                {
                    elem = null;
                    return false;
                }

                Random rnd = new Random(Interlocked.Increment(ref _seed));
                elem = new TestPoolElem(rnd.Next());
                return true;
            }

            protected override bool IsValidElement(TestPoolElem elem)
            {
                return elem != null && elem.Value >= 0;
            }

            protected override void DestroyElement(TestPoolElem elem)
            {
                elem.IsDestroyed = true;
            }

            protected override int CompareElements(TestPoolElem a, TestPoolElem b, out bool stopHere)
            {
                stopHere = false;
                return a.Value.CompareTo(b.Value);
            }

            protected override bool IsBetterAllocateNew(TestPoolElem elem)
            {
                return AlwaysCreateNew;
            }
        }

        // ==========================


        [TestMethod]
        public void TestSimpleRentRelease()
        {
            using (TestDynamicPool testInst = new TestDynamicPool(0, 10))
            {
                Assert.AreEqual(0, testInst.ElementCount);
                Assert.AreEqual(0, testInst.FreeElementCount);

                for (int i = 0; i < 1000; i++)
                {
                    using (var el = testInst.Rent())
                    {
                        Assert.IsTrue(el.IsValid);
                    }
                }

                Assert.IsTrue(testInst.ElementCount > 0);
                Assert.AreEqual(testInst.ElementCount, testInst.FreeElementCount);
            }
        }



        [TestMethod]
        public void TestRentReleaseMany()
        {
            using (TestDynamicPool testInst = new TestDynamicPool(0, 10))
            {
                List<RentedElementMonitor<TestPoolElem>> rented = new List<RentedElementMonitor<TestPoolElem>>();

                for (int i = 0; i < 10; i++)
                {
                    rented.Add(testInst.Rent());
                    Assert.AreEqual(i + 1, testInst.ElementCount);
                }

                for (int i = 0; i < rented.Count; i++)
                {
                    rented[i].Dispose();
                    Assert.AreEqual(i + 1, testInst.FreeElementCount);
                }

                Assert.IsTrue(testInst.ElementCount > 0);
                Assert.AreEqual(testInst.ElementCount, testInst.FreeElementCount);
            }
        }


        [TestMethod]
        public void TestRentInOrder()
        {
            using (TestDynamicPool testInst = new TestDynamicPool(0, 1000))
            {
                testInst.FillPoolUpTo(1000);

                List<RentedElementMonitor<TestPoolElem>> rented = new List<RentedElementMonitor<TestPoolElem>>();

                for (int i = 0; i < 1000; i++)
                    rented.Add(testInst.Rent());

                for (int i = 0; i < rented.Count - 1; i++)
                    Assert.IsTrue(rented[i].Element.Value > rented[i + 1].Element.Value);

                for (int i = 0; i < rented.Count; i++)
                {
                    rented[i].Dispose();
                    Assert.AreEqual(i + 1, testInst.FreeElementCount);
                }
            }
        }


        [TestMethod]
        public void TestFillUpToWork()
        {
            using (TestDynamicPool testInst = new TestDynamicPool(0, 10))
            {
                Assert.AreEqual(0, testInst.ElementCount);
                testInst.FillPoolUpTo(5);
                Assert.AreEqual(5, testInst.ElementCount);
                testInst.FillPoolUpTo(10);
                Assert.AreEqual(10, testInst.ElementCount);
                testInst.FillPoolUpTo(100);
                Assert.AreEqual(10, testInst.ElementCount);
            }
        }


        [TestMethod]
        public void TestElementDestroyedOnDispose()
        {
            List<TestPoolElem> elements = new List<TestPoolElem>();

            using (TestDynamicPool testInst = new TestDynamicPool(0, 100))
            {
                List<RentedElementMonitor<TestPoolElem>> rented = new List<RentedElementMonitor<TestPoolElem>>();

                for (int i = 0; i < 100; i++)
                {
                    var rentedItem = testInst.Rent();
                    rented.Add(rentedItem);
                    elements.Add(rentedItem.Element);
                    Assert.AreEqual(i + 1, testInst.ElementCount);
                }

                for (int i = 0; i < rented.Count; i++)
                {
                    rented[i].Dispose();
                    Assert.AreEqual(i + 1, testInst.FreeElementCount);
                }

                Assert.IsTrue(testInst.ElementCount > 0);
                Assert.AreEqual(testInst.ElementCount, testInst.FreeElementCount);
            }

            foreach (var item in elements)
                Assert.IsTrue(item.IsDestroyed);
        }


        [TestMethod]
        public void TestFaultedElementDestroyed()
        {
            using (TestDynamicPool testInst = new TestDynamicPool(0, 10))
            {
                Assert.AreEqual(0, testInst.ElementCount);
                Assert.AreEqual(0, testInst.FreeElementCount);

                TestPoolElem rentedEl = null;
                using (var el = testInst.Rent())
                {
                    Assert.IsTrue(el.IsValid);
                    rentedEl = el.Element;
                    rentedEl.MakeInvalid();
                }

                Assert.IsTrue(rentedEl.IsDestroyed);
            }
        }

        [TestMethod]
        public void TestElementDestroyedAfterDispose()
        {
            RentedElementMonitor<TestPoolElem> elementMonitor = default(RentedElementMonitor<TestPoolElem>);
            TestPoolElem elem = null;

            try
            {
                using (TestDynamicPool testInst = new TestDynamicPool(0, 10))
                {
                    elementMonitor = testInst.Rent();
                    elem = elementMonitor.Element;
                }

                Assert.IsTrue(elementMonitor.IsValid);
                Assert.IsFalse(elem.IsDestroyed);
            }
            finally
            {
                if (!object.ReferenceEquals(elementMonitor, null))
                    elementMonitor.Dispose();
            }

            Assert.IsTrue(elem.IsDestroyed);
        }



        [TestMethod]
        public void TestDoubleReleaseNotThrow()
        {
            using (TestDynamicPool testInst = new TestDynamicPool(0, 10))
            {
                using (var item = testInst.Rent())
                {
                    item.Dispose();
                    item.Dispose();
                }
            }
        }





        [TestMethod]
        [Timeout(10 * 1000)]
        public void TestRentWait()
        {
            using (TestDynamicPool testInst = new TestDynamicPool(0, 1))
            {
                var item = testInst.Rent();

                Task.Delay(500).ContinueWith(t => item.Dispose());

                using (var item2 = testInst.Rent())
                {
                    Assert.IsTrue(item2.IsValid);
                }
            }
        }

        [TestMethod]
        [Timeout(10 * 1000)]
        public void TestRentWaitWithRecreation()
        {
            using (TestDynamicPool testInst = new TestDynamicPool(0, 1))
            {
                var item = testInst.Rent();

                Task.Delay(500).ContinueWith(t =>
                    {
                        item.Element.MakeInvalid();
                        item.Dispose();
                    });

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
            using (TestDynamicPool testInst = new TestDynamicPool(0, 1))
            {
                using (var item1 = testInst.Rent(100))
                {
                    using (var item2 = testInst.Rent(100))
                    {
                    }
                }
            }
        }

        [TestMethod]
        public void TestRentTimeoutWithoutException()
        {
            using (TestDynamicPool testInst = new TestDynamicPool(0, 1))
            {
                using (var item1 = testInst.Rent(100, false))
                {
                    using (var item2 = testInst.Rent(100, false))
                    {
                        Assert.IsFalse(item2.IsValid);
                    }
                }
            }
        }


        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public void TestRentCancelled()
        {
            CancellationTokenSource tokSrc = new CancellationTokenSource();
            Task.Delay(200).ContinueWith(t => tokSrc.Cancel());

            using (TestDynamicPool testInst = new TestDynamicPool(0, 1))
            {
                using (var item1 = testInst.Rent(tokSrc.Token))
                {
                    using (var item2 = testInst.Rent(tokSrc.Token))
                    {
                    }
                }
            }
        }

        [TestMethod]
        public void TestRentCancelledWithoutException()
        {
            CancellationTokenSource tokSrc = new CancellationTokenSource();
            Task.Delay(200).ContinueWith(t => tokSrc.Cancel());

            using (TestDynamicPool testInst = new TestDynamicPool(0, 1))
            {
                using (var item1 = testInst.Rent(tokSrc.Token, false))
                {
                    using (var item2 = testInst.Rent(tokSrc.Token, false))
                    {
                        Assert.IsFalse(item2.IsValid);
                    }
                }
            }
        }


        [TestMethod]
        [ExpectedException(typeof(CantRetrieveElementException))]
        public void TestDisposeCancelWaiters()
        {
            using (TestDynamicPool testInst = new TestDynamicPool(0, 1))
            {
                Task.Delay(400).ContinueWith(t => testInst.Dispose());

                using (var item1 = testInst.Rent())
                {
                    using (var item2 = testInst.Rent())
                    {
                    }
                }
            }
        }


        [TestMethod]
        public void TestDisposeCancelWaitersWithoutException()
        {
            using (TestDynamicPool testInst = new TestDynamicPool(0, 1))
            {
                Task.Delay(400).ContinueWith(t => testInst.Dispose());

                using (var item1 = testInst.Rent(false))
                {
                    using (var item2 = testInst.Rent(false))
                    {
                        Assert.IsTrue(item1.IsValid);
                        Assert.IsFalse(item2.IsValid);
                    }
                }
            }
        }


        [TestMethod]
        public void TestNotCreateMoreThanMax()
        {
            using (TestDynamicPool testInst = new TestDynamicPool(0, 10))
            {
                List<RentedElementMonitor<TestPoolElem>> rented = new List<RentedElementMonitor<TestPoolElem>>();
                for (int i = 0; i < 10; i++)
                {
                    RentedElementMonitor<TestPoolElem> item = testInst.Rent(1);
                    rented.Add(item);

                    Assert.IsTrue(item.IsValid);
                }

                RentedElementMonitor<TestPoolElem> itemFail = testInst.Rent(1, false);
                Assert.IsFalse(itemFail.IsValid);
                itemFail.Dispose();

                for (int i = 0; i < rented.Count; i++)
                    rented[i].Dispose();
            }
        }



        [TestMethod]
        public void TestRecreateElementIfFaulted()
        {
            using (TestDynamicPool testInst = new TestDynamicPool(0, 10))
            {
                for (int i = 0; i < 100; i++)
                {
                    using (var item = testInst.Rent())
                    {
                        Assert.IsTrue(item.IsValid);
                        item.Element.MakeInvalid();
                        Assert.IsFalse(item.IsValid);
                    }
                }
            }
        }




        [TestMethod]
        public void TestTrimWork()
        {
            using (TestDynamicPool testInst = new TestDynamicPool(0, 10, 1000))
            {
                testInst.FillPoolUpTo(10);
                Assert.AreEqual(10, testInst.ElementCount);

                Stopwatch sw = Stopwatch.StartNew();

                while (sw.ElapsedMilliseconds < 1500)
                {
                    using (var item = testInst.Rent())
                    {
                        Assert.IsTrue(item.IsValid);
                    }
                }

                Assert.AreEqual(1, testInst.ElementCount);
            }
        }

        [TestMethod]
        [Timeout(10 * 1000)]
        public void TestWaitWhenCreatingNotPossible()
        {
            using (TestDynamicPool testInst = new TestDynamicPool(0, 10))
            {
                testInst.CanCreateElement = false;

                using (var item = testInst.Rent(1, false))
                {
                    Assert.IsFalse(item.IsValid);
                }

                Task.Delay(1000).ContinueWith(t => { testInst.CanCreateElement = true; });

                using (var item = testInst.Rent())
                {
                    Assert.IsTrue(item.IsValid);
                }
            }
        }


        [TestMethod]
        public void TestIsBetterCreateNew()
        {
            using (TestDynamicPool testInst = new TestDynamicPool(0, 10))
            {
                testInst.FillPoolUpTo(1);

                Assert.AreEqual(1, testInst.ElementCount);

                testInst.AlwaysCreateNew = true;

                for (int i = 1; i < 10; i++)
                {
                    using (var item = testInst.Rent())
                    {
                        Assert.IsTrue(item.IsValid);
                    }
                    Assert.AreEqual(i + 1, testInst.ElementCount);
                }

                for (int i = 0; i < 10; i++)
                {
                    using (var item = testInst.Rent())
                    {
                        Assert.IsTrue(item.IsValid);
                    }
                    Assert.AreEqual(10, testInst.ElementCount);
                }
            }
        }


        private void RunComplexTest(TestDynamicPool testInst, int threadCount, int opCount, int pauseSpin, bool faultElements)
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
                    using (var el = testInst.Rent())
                    {
                        if (faultElements && localRand.Next(10) == 0)
                            el.Element.MakeInvalid();

                        int spinCount = localRand.Next(pauseSpin - pasueDiff, pauseSpin + pasueDiff);
                        Thread.SpinWait(spinCount);
                    }
                }
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
            using (TestDynamicPool testInst = new TestDynamicPool(0, 100))
            {
                RunComplexTest(testInst, 1, 100000, 10, true);
                RunComplexTest(testInst, Environment.ProcessorCount, 2000000, 10, true);
                RunComplexTest(testInst, Environment.ProcessorCount, 1000000, 100, true);
                RunComplexTest(testInst, Environment.ProcessorCount, 100000, 4000, true);
            }
        }

        [TestMethod]
        [Timeout(4 * 60 * 1000)]
        public void ComplexTest2()
        {
            using (TestDynamicPool testInst = new TestDynamicPool(20, 100))
            {
                RunComplexTest(testInst, 1, 100000, 10, false);
                RunComplexTest(testInst, Environment.ProcessorCount, 2000000, 10, false);
                RunComplexTest(testInst, Environment.ProcessorCount, 1000000, 100, false);
            }
        }
    }
}
