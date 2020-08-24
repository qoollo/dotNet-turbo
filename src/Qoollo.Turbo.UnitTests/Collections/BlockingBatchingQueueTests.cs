using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Collections
{
    [TestClass]
    public class BlockingBatchingQueueTests
    {
        [TestMethod]
        public void TestSimpleEnqueueDequeue()
        {
            const int batchSize = 20;
            BlockingBatchingQueue<int> col = new BlockingBatchingQueue<int>(batchSize: batchSize);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, col.Count);
                Assert.AreEqual(i / batchSize, col.CompletedBatchCount);
                col.Add(i);
            }

            Assert.AreEqual(100, col.Count);
            int expectedCount = 100;

            while (true)
            {
                bool dequeueRes = col.TryTake(out int[] batch);
                if (!dequeueRes)
                    break;

                Assert.IsNotNull(batch);
                Assert.AreEqual(batchSize, batch.Length);
                for (int i = 0; i < batch.Length; i++)
                    Assert.AreEqual(i + (100 - expectedCount), batch[i]);

                expectedCount -= batch.Length;
                Assert.AreEqual(expectedCount, col.Count);
                Assert.AreEqual(expectedCount / batchSize, col.CompletedBatchCount);
            }

            Assert.AreEqual(0, col.Count);
        }


        [TestMethod]
        public void TestEnqueueDequeueToTheLimit()
        {
            const int batchSize = 13;
            BlockingBatchingQueue<int> col = new BlockingBatchingQueue<int>(batchSize: batchSize, boundedCapacityInBatches: 2);

            for (int i = 0; i < batchSize * col.BoundedCapacityInBatches; i++)
                Assert.IsTrue(col.TryAdd(i));

            Assert.IsFalse(col.TryAdd(int.MaxValue));

            List<int> takenItems = new List<int>();
            for (int i = 0; i < col.BoundedCapacityInBatches; i++)
            {
                Assert.IsTrue(col.TryTake(out int[] batch));
                takenItems.AddRange(batch);
            }

            Assert.IsFalse(col.TryTake(out _));

            for (int i = 0; i < takenItems.Count; i++)
                Assert.AreEqual(i, takenItems[i]);
        }


        [TestMethod]
        public void TestCompleteCurrentBatch()
        {
            const int batchSize = 10;
            BlockingBatchingQueue<int> col = new BlockingBatchingQueue<int>(batchSize: batchSize);
            Assert.AreEqual(0, col.Count);
            Assert.AreEqual(0, col.CompletedBatchCount);

            Assert.IsFalse(col.CompleteCurrentBatch());
            Assert.AreEqual(0, col.Count);
            Assert.AreEqual(0, col.CompletedBatchCount);


            int[] dequeuedItems = null;

            Assert.IsFalse(col.TryTake(out dequeuedItems));
            col.Add(0);
            col.Add(1);
            Assert.AreEqual(2, col.Count);
            Assert.AreEqual(0, col.CompletedBatchCount);


            Assert.IsTrue(col.CompleteCurrentBatch());
            Assert.AreEqual(2, col.Count);
            Assert.AreEqual(1, col.CompletedBatchCount);


            Assert.IsTrue(col.TryTake(out dequeuedItems));
            Assert.AreEqual(2, dequeuedItems.Length);
            for (int i = 0; i < dequeuedItems.Length; i++)
                Assert.AreEqual(i, dequeuedItems[i]);

            Assert.AreEqual(0, col.Count);
            Assert.AreEqual(0, col.CompletedBatchCount);
        }


        [TestMethod]
        public void TestQueueEnumeration()
        {
            const int batchSize = 17;
            BlockingBatchingQueue<int> col = new BlockingBatchingQueue<int>(batchSize: batchSize);

            for (int i = 0; i < 113; i++)
            {
                col.Add(i);

                int j = 0;
                foreach (var item in col)
                {
                    Assert.AreEqual(j, item);
                    j++;
                }
                Assert.AreEqual(j - 1, i);
            }

            int offset = 0;
            int initialCount = col.Count;
            while (col.TryTake(out int[] items))
            {
                offset += items.Length;

                int j = offset;
                foreach (var item in col)
                {
                    Assert.AreEqual(j, item);
                    j++;
                }
                Assert.AreEqual(j - 1, initialCount - 1);
            }
        }


        [TestMethod]
        public void TestTimeoutWorks()
        {
            const int batchSize = 17;
            BlockingBatchingQueue<int> queue = new BlockingBatchingQueue<int>(batchSize: batchSize, boundedCapacityInBatches: 8);
            Barrier bar = new Barrier(2);
            int takeResult = 0;
            int addResult = 0;
            Task task = Task.Run(() =>
            {
                bar.SignalAndWait();
                int[] item = null;
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
        public void TestDisposeInterruptWaitersOnTake()
        {
            BlockingBatchingQueue<int> queue = new BlockingBatchingQueue<int>(batchSize: 10, boundedCapacityInBatches: 2);
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
                bool taken = queue.TryTake(out int[] val, 10000);
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

        [TestMethod]
        public void TestDisposeInterruptWaitersOnAdd()
        {
            BlockingBatchingQueue<int> queue = new BlockingBatchingQueue<int>(batchSize: 2, boundedCapacityInBatches: 1);
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

        private void TestCancellationNotCorruptDataCore(int batchSize, int boundedCapacityInBatches)
        {
            BlockingBatchingQueue<long> col = new BlockingBatchingQueue<long>(batchSize: batchSize, boundedCapacityInBatches: boundedCapacityInBatches);
            Barrier bar = new Barrier(4);
            CancellationTokenSource cancelToken = new CancellationTokenSource();
            CancellationTokenSource temporaryCancelTokenAdd = new CancellationTokenSource();
            CancellationTokenSource temporaryCancelTokenTake = new CancellationTokenSource();

            List<long> takenItems = new List<long>();

            Task addTask = Task.Run(() =>
            {
                Random rnd = new Random();
                bar.SignalAndWait();
                long data = 0;
                CancellationToken token = temporaryCancelTokenAdd.Token;
                while (!cancelToken.IsCancellationRequested)
                {
                    try
                    {
                        col.Add(data, token);
                        data++;
                    }
                    catch (OperationCanceledException) 
                    {
                        token = temporaryCancelTokenAdd.Token;
                    }
                }
            });

            Task takeTask = Task.Run(() =>
            {
                bar.SignalAndWait();

                CancellationToken token = temporaryCancelTokenTake.Token;
                while (!cancelToken.IsCancellationRequested)
                {
                    try
                    {
                        long[] itemsT = col.Take(token);
                        takenItems.AddRange(itemsT);
                    }
                    catch (OperationCanceledException)
                    {
                        token = temporaryCancelTokenTake.Token;
                    }

                    if (takenItems.Count > int.MaxValue / 2)
                        cancelToken.Cancel();
                }
            });


            Task cancelTask = Task.Run(() =>
            {
                Random rnd = new Random();
                bar.SignalAndWait();

                while (!cancelToken.IsCancellationRequested)
                {
                    if (rnd.Next(100) == 1)
                    {
                        temporaryCancelTokenAdd.Cancel();
                        temporaryCancelTokenAdd = new CancellationTokenSource();
                    }
                    if (rnd.Next(100) == 1)
                    {
                        temporaryCancelTokenTake.Cancel();
                        temporaryCancelTokenTake = new CancellationTokenSource();
                    }

                    Thread.SpinWait(rnd.Next(100));
                }
            });

            bar.SignalAndWait();
            Thread.Sleep(500);
            cancelToken.Cancel();
            temporaryCancelTokenAdd.Cancel();
            temporaryCancelTokenTake.Cancel();

            Task.WaitAll(addTask, takeTask, cancelTask);

            while (col.TryTake(out long[] itemsF))
                takenItems.AddRange(itemsF);

            col.CompleteCurrentBatch();

            if (col.TryTake(out long[] itemsFF))
                takenItems.AddRange(itemsFF);

            for (int i = 0; i < takenItems.Count; i++)
                Assert.AreEqual(i, takenItems[i]);
        }

        [TestMethod]
        public void TestCancellationNotCorruptData()
        {
            TestCancellationNotCorruptDataCore(batchSize: 16, boundedCapacityInBatches: 7);
            TestCancellationNotCorruptDataCore(batchSize: 20, boundedCapacityInBatches: 1);
            TestCancellationNotCorruptDataCore(batchSize: 1, boundedCapacityInBatches: 1);
            TestCancellationNotCorruptDataCore(batchSize: 100000, boundedCapacityInBatches: 1);
        }


        private void SimpleConcurrentTestCore(int batchSize, int boundedCapacityInBatches)
        {
            BlockingBatchingQueue<long> col = new BlockingBatchingQueue<long>(batchSize: batchSize, boundedCapacityInBatches: boundedCapacityInBatches);
            Barrier bar = new Barrier(4);
            CancellationTokenSource cancelToken = new CancellationTokenSource();

            List<long> takenItems = new List<long>();

            Task addTask = Task.Run(() =>
            {
                Random rnd = new Random();
                bar.SignalAndWait();
                long data = 0;
                while (!cancelToken.IsCancellationRequested)
                {
                    if (col.TryAdd(data))
                        data++;
                    if (rnd.Next(100) == 1)
                        col.CompleteCurrentBatch();
                }
            });

            Task takeTask = Task.Run(() =>
            {
                bar.SignalAndWait();

                while (!cancelToken.IsCancellationRequested)
                {
                    if (col.TryTake(out long[] itemsT))
                        takenItems.AddRange(itemsT);

                    if (takenItems.Count > int.MaxValue / 2)
                        cancelToken.Cancel();
                }
            });

            Task enumerateTask = Task.Run(() =>
            {
                bar.SignalAndWait();

                while (!cancelToken.IsCancellationRequested)
                {
                    int count = 0;
                    long prevItem = -1;
                    foreach (long item in col)
                    {
                        count++;
                        if (prevItem > 0)
                            Assert.AreEqual(prevItem + 1, item);

                        prevItem = item;
                    }
                    Thread.Sleep(count > 100 ? 0 : 1);
                }
            });

            bar.SignalAndWait();
            Thread.Sleep(300);
            cancelToken.Cancel();

            Task.WaitAll(addTask, takeTask, enumerateTask);

            while (col.TryTake(out long[] itemsF))
                takenItems.AddRange(itemsF);

            col.CompleteCurrentBatch();

            if (col.TryTake(out long[] itemsFF))
                takenItems.AddRange(itemsFF);

            for (int i = 0; i < takenItems.Count; i++)
                Assert.AreEqual(i, takenItems[i]);
        }

        [TestMethod]
        public void SimpleConcurrentTest()
        {
            SimpleConcurrentTestCore(batchSize: 16, boundedCapacityInBatches: 7);
            SimpleConcurrentTestCore(batchSize: 20, boundedCapacityInBatches: 1);
            SimpleConcurrentTestCore(batchSize: 1, boundedCapacityInBatches: 1);
            SimpleConcurrentTestCore(batchSize: 100000, boundedCapacityInBatches: 1);
        }



        private void RunComplexTest(BlockingBatchingQueue<int> q, int elemCount, int thCount)
        {
            int atomicRandom = 0;

            int trackElemCount = elemCount;
            int addFinished = 0;

            Thread[] threadsTake = new Thread[thCount];
            Thread[] threadsAdd = new Thread[thCount];
            Thread completeBatchThread = null;

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

                    q.Add(item);

                    int sleepTime = rnd.Next(100);

                    if (sleepTime > 0)
                        Thread.SpinWait(sleepTime);
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
                        int[] tmp;
                        if (q.TryTake(out tmp, -1, tokSrc.Token))
                            data.AddRange(tmp);

                        int sleepTime = rnd.Next(100);
                        if (sleepTime > 0)
                            Thread.SpinWait(sleepTime);
                    }
                }
                catch (OperationCanceledException) { }

                int[] tmp2;
                while (q.TryTake(out tmp2))
                    data.AddRange(tmp2);

                q.CompleteCurrentBatch();
                while (q.TryTake(out tmp2))
                    data.AddRange(tmp2);

                lock (global)
                    global.AddRange(data);
            };

            Action completeBatchAction = () =>
            {
                Random rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * thCount * 2);
                while (Volatile.Read(ref addFinished) < thCount && !tokSrc.IsCancellationRequested)
                {
                    q.CompleteCurrentBatch();
                    Thread.Sleep(rnd.Next(2));
                }
            };


            for (int i = 0; i < threadsTake.Length; i++)
                threadsTake[i] = new Thread(new ThreadStart(takeAction));
            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i] = new Thread(new ThreadStart(addAction));
            completeBatchThread = new Thread(new ThreadStart(completeBatchAction));

            for (int i = 0; i < threadsTake.Length; i++)
                threadsTake[i].Start();
            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i].Start();
            completeBatchThread.Start();


            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i].Join();
            tokSrc.Cancel();
            for (int i = 0; i < threadsTake.Length; i++)
                threadsTake[i].Join();
            completeBatchThread.Join();


            Assert.AreEqual(elemCount, global.Count);
            global.Sort();

            for (int i = 0; i < elemCount; i++)
                Assert.AreEqual(i, global[i]);
        }


        [TestMethod]
        public void ComplexTest()
        {
            BlockingBatchingQueue<int> q = new BlockingBatchingQueue<int>(batchSize: 73, boundedCapacityInBatches: 3);

            for (int i = 0; i < 8; i++)
                RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount / 2));

            RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount));

            q = new BlockingBatchingQueue<int>(batchSize: 1, boundedCapacityInBatches: 1);
            RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount / 2));
        }
    }
}
