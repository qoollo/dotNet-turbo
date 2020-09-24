using Qoollo.Turbo.Collections.Concurrent;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Qoollo.Turbo.Threading.ServiceStuff;

namespace Qoollo.Turbo.UnitTests.Collections
{
    [TestClass]
    public class BlockingQueueTest: TestClassBase
    {
        [TestMethod]
        public void TestSimpleEnqueueDequeue()
        {
            BlockingQueue<int> col = new BlockingQueue<int>();

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, col.Count);
                col.Add(i);
            }

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(100 - i, col.Count);
                Assert.AreEqual(i, col.Take());
            }
        }


        [TestMethod]
        public void TestEnqueueDequeueToTheLimit()
        {
            BlockingQueue<int> col = new BlockingQueue<int>(100);

            for (int i = 0; i < 100; i++)
                col.Add(i);

            Assert.IsFalse(col.TryAdd(int.MaxValue));

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(i, col.Take());

            int resItem = 0;
            Assert.IsFalse(col.TryTake(out resItem));
        }

        [TestMethod]
        public void TestPeek()
        {
            BlockingQueue<int> col = new BlockingQueue<int>(100);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, col.Count);
                col.Add(i);
                Assert.AreEqual(0, col.Peek());
            }

            int item = 0;
            Assert.IsTrue(col.TryPeek(out item));

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, col.Peek());
                Assert.AreEqual(i, col.Take());
            }

            Assert.IsFalse(col.TryPeek(out item));
        }

        [TestMethod]
        public void TestPeekWait()
        {
            BlockingQueue<int> col = new BlockingQueue<int>(100);
            int startedFlag = 0;

            int peekVal = 0;
            Task.Run(() =>
                {
                    Interlocked.Exchange(ref startedFlag, 1);
                    peekVal = col.Peek();
                });

            TimingAssert.IsTrue(5000, () => Volatile.Read(ref startedFlag) == 1);
            Thread.Sleep(100);
            Assert.AreEqual(0, peekVal);

            col.Add(100);

            TimingAssert.AreEqual(5000, 100, () => Volatile.Read(ref peekVal));
            TimingAssert.AreEqual(5000, 1, () => col.Count);
        }

        [TestMethod]
        public void TestBoundaryExtension()
        {
            BlockingQueue<int> col = new BlockingQueue<int>(100);

            for (int i = 0; i < 100; i++)
                col.Add(i);

            Assert.IsFalse(col.TryAdd(int.MaxValue));
            Assert.AreEqual(100, col.BoundedCapacity);

            col.IncreaseBoundedCapacity(10);
            Assert.AreEqual(110, col.BoundedCapacity);


            for (int i = 100; i < 110; i++)
                col.Add(i);

            Assert.AreEqual(110, col.Count);

            for (int i = 0; i < 110; i++)
                Assert.AreEqual(i, col.Take());

            Assert.AreEqual(0, col.Count);
            Assert.AreEqual(110, col.BoundedCapacity);
        }

        [TestMethod]
        public void TestBoundaryContraction()
        {
            BlockingQueue<int> col = new BlockingQueue<int>(110);
            Assert.AreEqual(110, col.BoundedCapacity);

            for (int i = 0; i < 100; i++)
                col.Add(i);

            col.DecreaseBoundedCapacity(10);        
            Assert.AreEqual(100, col.BoundedCapacity);
            Assert.IsFalse(col.TryAdd(int.MaxValue));

            col.DecreaseBoundedCapacity(10);
            Assert.AreEqual(90, col.BoundedCapacity);
            Assert.AreEqual(100, col.Count);
            Assert.IsFalse(col.TryAdd(int.MaxValue));


            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, col.Take());
                Assert.IsFalse(col.TryAdd(int.MaxValue));
            }

            Assert.AreEqual(90, col.BoundedCapacity);
            Assert.AreEqual(90, col.Count);

            for (int i = 10; i < 100; i++)
            {
                Assert.AreEqual(i, col.Take());
            }
        }

        [TestMethod]
        public void TestBoundaryChange()
        {
            BlockingQueue<int> col = new BlockingQueue<int>(100);
            Assert.AreEqual(100, col.BoundedCapacity);

            col.SetBoundedCapacity(50);
            Assert.AreEqual(50, col.BoundedCapacity);

            col.SetBoundedCapacity(150);
            Assert.AreEqual(150, col.BoundedCapacity);
        }

        [TestMethod]
        public void TestForceEnqueue()
        {
            BlockingQueue<int> col = new BlockingQueue<int>(100);
            for (int i = 0; i < 100; i++)
                col.Add(i);

            Assert.AreEqual(100, col.Count);
            Assert.IsFalse(col.TryAdd(int.MaxValue));

            col.AddForced(100);
            Assert.AreEqual(101, col.Count);
            Assert.AreEqual(100, col.BoundedCapacity);
            Assert.IsFalse(col.TryAdd(int.MaxValue));

            Assert.AreEqual(0, col.Take());
            Assert.AreEqual(100, col.Count);
            Assert.AreEqual(100, col.BoundedCapacity);
            Assert.IsFalse(col.TryAdd(int.MaxValue));

            for (int i = 1; i < 101; i++)
                Assert.AreEqual(i, col.Take());
        }

        [TestMethod]
        public void PeekWakesUpTest()
        {
            var queue = new BlockingQueue<int>();

            Barrier bar = new Barrier(3);
            AtomicNullableBool peekResult = new AtomicNullableBool();
            AtomicNullableBool peekResult2 = new AtomicNullableBool();
            Task task = Task.Run(() =>
            {
                bar.SignalAndWait();
                int item = 0;
                peekResult.Value = queue.TryPeek(out item, 60000);
                Assert.AreEqual(100, item);
            });

            Task task2 = Task.Run(() =>
            {
                bar.SignalAndWait();
                int item = 0;
                peekResult2.Value = queue.TryPeek(out item, 60000);
                Assert.AreEqual(100, item);
            });

            bar.SignalAndWait();
            Thread.Sleep(20);
            Assert.IsFalse(peekResult.HasValue);
            Assert.IsFalse(peekResult2.HasValue);

            queue.Add(100);
            TimingAssert.AreEqual(10000, true, () => peekResult.Value);
            TimingAssert.AreEqual(10000, true, () => peekResult2.Value);

            Task.WaitAll(task, task2);
        }



        [TestMethod]
        public void AddTakeMultithreadTest()
        {
            const int ItemsCount = 10000;

            using (var queue = new BlockingQueue<int>())
            using (var barrier = new Barrier(2))
            {
                ConcurrentBag<int> bag = new ConcurrentBag<int>();

                var task1 = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    Parallel.For(0, ItemsCount, val =>
                    {
                        queue.Add(val);
                        SpinWaitHelper.SpinWait(val % 16);
                    });
                });

                var task2 = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    Parallel.For(0, 10000, val =>
                    {
                        int res = 0;
                        if (!queue.TryTake(out res, 10000))
                            Assert.Fail("Value was expected in MemoryQueue");
                        bag.Add(res);
                        SpinWaitHelper.SpinWait((val + 5) % 16);
                    });
                });

                Task.WaitAll(task1, task2);

                Assert.AreEqual(0, queue.Count);
                Assert.AreEqual(ItemsCount, bag.Count);

                var array = bag.ToArray();
                Array.Sort(array);
                for (int i = 0; i < array.Length; i++)
                    Assert.AreEqual(i, array[i], "i != array[i]");
            }
        }

        [TestMethod]
        public void AddTakeSequentialTest()
        {
            const int ItemsCount = 10000;

            using (var queue = new BlockingQueue<int>())
            using (var barrier = new Barrier(2))
            {
                List<int> bag = new List<int>();

                var task1 = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    for (int val = 0; val < ItemsCount; val++)
                    {
                        queue.Add(val);
                        SpinWaitHelper.SpinWait(val % 16);
                    }
                });

                var task2 = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    for (int val = 0; val < ItemsCount; val++)
                    {
                        int res = 0;
                        if (!queue.TryTake(out res, 10000))
                            Assert.Fail("Value was expected in MemoryQueue");
                        bag.Add(res);
                        SpinWaitHelper.SpinWait((val + 5) % 16);
                    }
                });

                Task.WaitAll(task1, task2);

                Assert.AreEqual(0, queue.Count);
                Assert.AreEqual(ItemsCount, bag.Count);

                for (int i = 0; i < bag.Count; i++)
                    Assert.AreEqual(i, bag[i], "i != bag[i]");
            }
        }

        [TestMethod]
        public void TestTimeoutWorks()
        {
            BlockingQueue<int> queue = new BlockingQueue<int>(100);
            Barrier bar = new Barrier(2);
            int takeResult = 0;
            int addResult = 0;
            Task task = Task.Run(() =>
            {
                bar.SignalAndWait();
                int item = 0;
                if (queue.TryTake(out item, 100))
                    Interlocked.Exchange(ref takeResult, 1);
                else
                    Interlocked.Exchange(ref takeResult, 2);

                while (queue.TryAdd(-1)) ;

                if (queue.TryAdd(100, 100))
                    Interlocked.Exchange(ref addResult, 1);
                else
                    Interlocked.Exchange(ref addResult, 2);
            });

            bar.SignalAndWait();

            TimingAssert.AreEqual(10000, 2, () => Volatile.Read(ref takeResult), "take");
            TimingAssert.AreEqual(10000, 2, () => Volatile.Read(ref addResult), "Add");

            task.Wait();
        }

        [TestMethod]
        public void TestNotAddAfterEnd()
        {
            BlockingQueue<int> queue = new BlockingQueue<int>(2);
            queue.AddForced(1);
            Assert.IsTrue(queue.TryAdd(2));
            Assert.IsFalse(queue.TryAdd(3));
        }

        //[TestMethod]
        //[Ignore("Old behaviour is not good. Changing it can break production code. This test should be enabled when BlockingQueue dispose logic will be updated")]
        public void TestDisposeInterruptWaitersOnTake()
        {
            BlockingQueue<int> queue = new BlockingQueue<int>(10);
            Barrier bar = new Barrier(2);

            Task disposeTask = Task.Run(() =>
            {
                bar.SignalAndWait();
                Thread.Sleep(10);
                queue.Dispose();
            });

            try
            {
                bar.SignalAndWait();
                bool taken = queue.TryTake(out int val, 10000);
                Assert.Fail();
            }
            catch (OperationInterruptedException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception)
            {
                Assert.Fail("Unexpected exception");
            }


            disposeTask.Wait();
        }

        //[TestMethod]
        //[Ignore("Old behaviour is not good. Changing it can break production code. This test should be enabled when BlockingQueue dispose logic will be updated")]
        public void TestDisposeInterruptWaitersOnAdd()
        {
            BlockingQueue<int> queue = new BlockingQueue<int>(2);
            queue.Add(1);
            queue.Add(2);
            Barrier bar = new Barrier(2);

            Task disposeTask = Task.Run(() =>
            {
                bar.SignalAndWait();
                Thread.Sleep(10);
                queue.Dispose();
            });

            try
            {
                bar.SignalAndWait();
                bool added = queue.TryAdd(3, 10000);
                Assert.Fail();
            }
            catch (OperationInterruptedException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception)
            {
                Assert.Fail("Unexpected exception");
            }


            disposeTask.Wait();
        }

        private void RunComplexTest(BlockingQueue<int> q, int elemCount, int thCount)
        {
            int atomicRandom = 0;

            int trackElemCount = elemCount;
            int addFinished = 0;

            Thread[] threadsTake = new Thread[thCount];
            Thread[] threadsAdd = new Thread[thCount];

            CancellationTokenSource tokSrc = new CancellationTokenSource();

            List<int> global = new List<int>(elemCount);

            Action addAction = () =>
            {
                Random rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * thCount * 2);

                while (true)
                {
                    int item = Interlocked.Decrement(ref trackElemCount);
                    if (item < 0)
                        break;

                    if (rnd.Next(100) == 0)
                        q.AddForced(item);
                    else
                        q.Add(item);


                    int sleepTime = rnd.Next(12);

                    int tmpItem = 0;
                    if (q.TryPeek(out tmpItem) && tmpItem == item)
                        sleepTime += 10;

                    if (sleepTime > 0)
                        SpinWaitHelper.SpinWait(sleepTime);

                    if (rnd.Next(100) == 0)
                        q.IncreaseBoundedCapacity(1);
                    if (rnd.Next(100) == 0 && q.BoundedCapacity > 20)
                        q.DecreaseBoundedCapacity(1);
                }

                Interlocked.Increment(ref addFinished);
            };


            Action takeAction = () =>
            {
                Random rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * thCount * 2);

                List<int> data = new List<int>();

                try
                {
                    while (Volatile.Read(ref addFinished) < thCount)
                    {
                        int tmp = 0;
                        if (q.TryTake(out tmp, -1, tokSrc.Token))
                            data.Add((int)tmp);

                        int sleepTime = rnd.Next(12);
                        if (sleepTime > 0)
                            SpinWaitHelper.SpinWait(sleepTime);
                    }
                }
                catch (OperationCanceledException) { }

                int tmp2;
                while (q.TryTake(out tmp2))
                    data.Add((int)tmp2);

                lock (global)
                    global.AddRange(data);
            };

            Task.Delay(1000).ContinueWith(t => q.IncreaseBoundedCapacity(50));


            for (int i = 0; i < threadsTake.Length; i++)
                threadsTake[i] = new Thread(new ThreadStart(takeAction));
            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i] = new Thread(new ThreadStart(addAction));


            for (int i = 0; i < threadsTake.Length; i++)
                threadsTake[i].Start();
            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i].Start();


            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i].Join();
            tokSrc.Cancel();
            for (int i = 0; i < threadsTake.Length; i++)
                threadsTake[i].Join();


            Assert.AreEqual(elemCount, global.Count);
            global.Sort();

            for (int i = 0; i < elemCount; i++)
                Assert.AreEqual(i, global[i]);
        }


        [TestMethod]
        public void ComplexTest()
        {
            BlockingQueue<int> q = new BlockingQueue<int>(1000);

            for (int i = 0; i < 10; i++)
                RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount / 2));
        }
    }
}
