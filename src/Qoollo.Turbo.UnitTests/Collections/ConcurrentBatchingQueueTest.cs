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
    public class ConcurrentBatchingQueueTest: TestClassBase
    {
        [TestMethod]
        public void TestSimpleEnqueueDequeue()
        {
            const int batchSize = 10;
            ConcurrentBatchingQueue<int> col = new ConcurrentBatchingQueue<int>(batchSize: batchSize);
            Assert.AreEqual(batchSize, col.BatchSize);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, col.Count);
                Assert.AreEqual((i / batchSize) + 1, col.BatchCount);
                Assert.AreEqual(i / batchSize, col.CompletedBatchCount);
                col.Enqueue(i);
            }

            Assert.AreEqual(100, col.Count);
            int expectedCount = 100;

            while (true)
            {
                bool dequeueRes = col.TryDequeue(out int[] batch);
                if (!dequeueRes)
                    break;

                Assert.IsNotNull(batch);
                Assert.AreEqual(batchSize, batch.Length);
                for (int i = 0; i < batch.Length; i++)
                    Assert.AreEqual(i + (100 - expectedCount), batch[i]);

                expectedCount -= batch.Length;
                Assert.AreEqual(expectedCount, col.Count);
                Assert.AreEqual((expectedCount / batchSize) + 1, col.BatchCount);
                Assert.AreEqual(expectedCount / batchSize, col.CompletedBatchCount);
            }

            Assert.AreEqual(0, col.Count);
        }

        [TestMethod]
        public void TestDequeueWhenBatchCompleted()
        {
            const int batchSize = 10;
            ConcurrentBatchingQueue<int> col = new ConcurrentBatchingQueue<int>(batchSize: batchSize);

            int[] dequeuedItems = null;

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, col.Count);
                Assert.AreEqual(1, col.BatchCount);
                Assert.AreEqual(0, col.CompletedBatchCount);
                Assert.IsFalse(col.TryDequeue(out dequeuedItems));
                col.Enqueue(i, out int batchCountIncr);
                Assert.AreEqual(i == 9 ? 1 : 0, batchCountIncr);
            }

            Assert.AreEqual(2, col.BatchCount);
            Assert.AreEqual(1, col.CompletedBatchCount);

            Assert.IsTrue(col.TryDequeue(out dequeuedItems));
            for (int i = 0; i < batchSize; i++)
                Assert.AreEqual(i, dequeuedItems[i]);
        }


        [TestMethod]
        public void TestCompleteCurrentBatch()
        {
            const int batchSize = 10;
            ConcurrentBatchingQueue<int> col = new ConcurrentBatchingQueue<int>(batchSize: batchSize);
            Assert.AreEqual(0, col.Count);
            Assert.AreEqual(1, col.BatchCount);
            Assert.AreEqual(0, col.CompletedBatchCount);

            Assert.IsFalse(col.CompleteCurrentBatch());
            Assert.AreEqual(0, col.Count);
            Assert.AreEqual(1, col.BatchCount);
            Assert.AreEqual(0, col.CompletedBatchCount);


            int[] dequeuedItems = null;

            Assert.IsFalse(col.TryDequeue(out dequeuedItems));
            col.Enqueue(0);
            col.Enqueue(1);
            Assert.AreEqual(2, col.Count);
            Assert.AreEqual(1, col.BatchCount);
            Assert.AreEqual(0, col.CompletedBatchCount);


            Assert.IsTrue(col.CompleteCurrentBatch());
            Assert.AreEqual(2, col.Count);
            Assert.AreEqual(2, col.BatchCount);
            Assert.AreEqual(1, col.CompletedBatchCount);


            Assert.IsTrue(col.TryDequeue(out dequeuedItems));
            Assert.AreEqual(2, dequeuedItems.Length);
            for (int i = 0; i < dequeuedItems.Length; i++)
                Assert.AreEqual(i, dequeuedItems[i]);

            Assert.AreEqual(0, col.Count);
            Assert.AreEqual(1, col.BatchCount);
            Assert.AreEqual(0, col.CompletedBatchCount);
        }


        [TestMethod]
        public void TestCompleteCurrentBatchWhenMoreThanOneBatch()
        {
            const int batchSize = 10;
            ConcurrentBatchingQueue<int> col = new ConcurrentBatchingQueue<int>(batchSize: batchSize);
            int[] dequeuedItems = null;

            Assert.IsFalse(col.TryDequeue(out dequeuedItems));
            for (int i = 0; i < 15; i++)
            {
                col.Enqueue(i);
                Assert.AreEqual(i + 1, col.Count);
            }

            Assert.AreEqual(15, col.Count);
            Assert.AreEqual(2, col.BatchCount);
            Assert.AreEqual(1, col.CompletedBatchCount);


            Assert.IsTrue(col.CompleteCurrentBatch());
            Assert.AreEqual(15, col.Count);
            Assert.AreEqual(3, col.BatchCount);
            Assert.AreEqual(2, col.CompletedBatchCount);


            Assert.IsFalse(col.CompleteCurrentBatch());
            Assert.AreEqual(15, col.Count);
            Assert.AreEqual(3, col.BatchCount);
            Assert.AreEqual(2, col.CompletedBatchCount);


            Assert.IsTrue(col.TryDequeue(out dequeuedItems));
            Assert.AreEqual(batchSize, dequeuedItems.Length);
            for (int i = 0; i < dequeuedItems.Length; i++)
                Assert.AreEqual(i, dequeuedItems[i]);

            Assert.AreEqual(5, col.Count);
            Assert.AreEqual(2, col.BatchCount);
            Assert.AreEqual(1, col.CompletedBatchCount);

            Assert.IsTrue(col.TryDequeue(out dequeuedItems));
            Assert.AreEqual(5, dequeuedItems.Length);
            for (int i = 0; i < dequeuedItems.Length; i++)
                Assert.AreEqual(i + batchSize, dequeuedItems[i]);

            Assert.AreEqual(0, col.Count);
            Assert.AreEqual(1, col.BatchCount);
            Assert.AreEqual(0, col.CompletedBatchCount);
        }


        [TestMethod]
        public void TestQueueEnumeration()
        {
            const int batchSize = 10;
            ConcurrentBatchingQueue<int> col = new ConcurrentBatchingQueue<int>(batchSize: batchSize);
            
            for (int i = 0; i < 133; i++)
            {
                col.Enqueue(i);

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
            while (col.TryDequeue(out int[] items))
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
        public void TestQueueToArray()
        {
            const int batchSize = 10;
            ConcurrentBatchingQueue<int> col = new ConcurrentBatchingQueue<int>(batchSize: batchSize);

            for (int i = 0; i < 133; i++)
            {
                col.Enqueue(i);

                var array = col.ToArray();
                int j = 0;
                foreach (var item in array)
                {
                    Assert.AreEqual(j, item);
                    j++;
                }
                Assert.AreEqual(j - 1, i);
            }

            int offset = 0;
            int initialCount = col.Count;
            while (col.TryDequeue(out int[] items))
            {
                offset += items.Length;

                int j = offset;

                var array = col.ToArray();
                foreach (var item in array)
                {
                    Assert.AreEqual(j, item);
                    j++;
                }
                Assert.AreEqual(j - 1, initialCount - 1);
            }
        }


        private void TossArrayForward<T>(T[] data)
        {
            if (data.Length <= 1)
                return;

            T val = data[data.Length - 1];
            for (int i = 1; i < data.Length; i++)
                data[i] = data[i - 1];
            data[0] = val;
        }

        [TestMethod]
        public void TestQueueEnumerationNotAffectedByDequeue()
        {
            const int batchSize = 13;
            ConcurrentBatchingQueue<int> col = new ConcurrentBatchingQueue<int>(batchSize: batchSize);

            for (int i = 0; i < 577; i++)
                col.Enqueue(i);

            int offset = 0;
            int initialCount = col.Count;
            while (col.TryDequeue(out int[] items))
            {
                offset += items.Length;

                int j = offset;
                foreach (var item in col)
                {
                    Assert.AreEqual(j, item);
                    j++;
                    TossArrayForward(items);
                }
                Assert.AreEqual(j - 1, initialCount - 1);
            }
        }


        [TestMethod]
        public void BatchIdOverflowTest()
        {
            ConcurrentBatchingQueue<long> col = new ConcurrentBatchingQueue<long>(batchSize: 1);
            var head = col.GetType().GetField("_head", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(col);
            var batchIdField = head.GetType().GetField("_batchId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            batchIdField.SetValue(head, int.MaxValue - 1);

            for (long i = 0; i < 100; i++)
            {
                Assert.AreEqual(0, col.Count);
                Assert.AreEqual(1, col.BatchCount);
                Assert.AreEqual(0, col.CompletedBatchCount);

                col.Enqueue(i);

                Assert.AreEqual(1, col.Count);
                Assert.AreEqual(2, col.BatchCount);
                Assert.AreEqual(1, col.CompletedBatchCount);

                Assert.IsTrue(col.TryDequeue(out long[] items));
                Assert.AreEqual(1, items.Length);
                Assert.AreEqual(i, items[0]);

                Assert.AreEqual(0, col.Count);
                Assert.AreEqual(1, col.BatchCount);
                Assert.AreEqual(0, col.CompletedBatchCount);
            }
        }



        [TestMethod]
        public void SimpleConcurrentTest()
        {
            const int batchSize = 10;
            ConcurrentBatchingQueue<long> col = new ConcurrentBatchingQueue<long>(batchSize: batchSize);
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
                    col.Enqueue(data++);
                    if (rnd.Next(100) == 1)
                        col.CompleteCurrentBatch();
                }
            });

            Task takeTask = Task.Run(() =>
            {
                Random rnd2 = new Random();
                bar.SignalAndWait();

                while (!cancelToken.IsCancellationRequested)
                {
                    if (col.TryDequeue(out long[] itemsT))
                    {
                        takenItems.AddRange(itemsT);

                        if (rnd2.Next(100) == 1)
                            TossArrayForward(itemsT);
                    }

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

            while (col.TryDequeue(out long[] itemsF))
                takenItems.AddRange(itemsF);

            col.CompleteCurrentBatch();

            if (col.TryDequeue(out long[] itemsFF))
                takenItems.AddRange(itemsFF);

            for (int i = 0; i < takenItems.Count; i++)
                Assert.AreEqual(i, takenItems[i]);
        }




        private void RunComplexTest(ConcurrentBatchingQueue<int> q, int elemCount, int thCount)
        {
            int atomicRandom = 0;

            int trackElemCount = elemCount;
            int addFinished = 0;

            Thread[] threadsTake = new Thread[thCount];
            Thread[] threadsAdd = new Thread[thCount];
            Thread enumerateThread = null;

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

                    q.Enqueue(item);

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
                        if (q.TryDequeue(out tmp))
                            data.AddRange(tmp);

                        int sleepTime = rnd.Next(100);
                        if (sleepTime > 0)
                            Thread.SpinWait(sleepTime);
                    }
                }
                catch (OperationCanceledException) { }

                int[] tmp2;
                while (q.TryDequeue(out tmp2))
                    data.AddRange(tmp2);

                q.CompleteCurrentBatch();
                while (q.TryDequeue(out tmp2))
                    data.AddRange(tmp2);

                lock (global)
                    global.AddRange(data);
            };

            Action enumerateAction = () =>
            {
                Random rnd = new Random();
                while (Volatile.Read(ref addFinished) < thCount && !tokSrc.IsCancellationRequested)
                {
                    int count = 0;
                    foreach (long item in q)
                        count++;
                    Thread.Sleep(count > 100 ? 0 : 1);

                    if (rnd.Next(100) == 1)
                        q.CompleteCurrentBatch();
                }
            };


            for (int i = 0; i < threadsTake.Length; i++)
                threadsTake[i] = new Thread(new ThreadStart(takeAction));
            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i] = new Thread(new ThreadStart(addAction));
            enumerateThread = new Thread(new ThreadStart(enumerateAction));

            for (int i = 0; i < threadsTake.Length; i++)
                threadsTake[i].Start();
            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i].Start();
            enumerateThread.Start();


            for (int i = 0; i < threadsAdd.Length; i++)
                threadsAdd[i].Join();
            tokSrc.Cancel();
            for (int i = 0; i < threadsTake.Length; i++)
                threadsTake[i].Join();
            enumerateThread.Join();


            Assert.AreEqual(elemCount, global.Count);
            global.Sort();

            for (int i = 0; i < elemCount; i++)
                Assert.AreEqual(i, global[i]);
        }


        [TestMethod]
        public void ComplexTest()
        {
            ConcurrentBatchingQueue<int> q = new ConcurrentBatchingQueue<int>(batchSize: 373);

            for (int i = 0; i < 10; i++)
                RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount / 2));
        }
    }
}
