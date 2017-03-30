using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Queues;

namespace Qoollo.Turbo.UnitTests.Queues
{
    [TestClass]
    public class LevelingQueueTest
    {
        private static void AtomicSet(ref int target, bool source)
        {
            if (source)
                Interlocked.Exchange(ref target, 1);
            else
                Interlocked.Exchange(ref target, 2);
        }

        private static LevelingQueue<int> CreateQueue(int highLevelSize, int lowLevelSize, LevelingQueueAddingMode addingMode = LevelingQueueAddingMode.PreserveOrder, bool isBcgTransferEnabled = false)
        {
            return new LevelingQueue<int>(new MemoryQueue<int>(highLevelSize), new MemoryQueue<int>(lowLevelSize), addingMode, isBcgTransferEnabled);
        }
        private static LevelingQueue<int> CreateQueue(int highLevelSize, int lowLevelSize, bool ord = true, bool bcg = false)
        {
            LevelingQueueAddingMode mode = ord ? LevelingQueueAddingMode.PreserveOrder : LevelingQueueAddingMode.PreferLiveData;
            return new LevelingQueue<int>(new MemoryQueue<int>(highLevelSize), new MemoryQueue<int>(lowLevelSize), mode, bcg);
        }

        private static void RunTest(int highLevelSize, int lowLevelSize, bool ord, bool bcg, Action<LevelingQueue<int>> testRunner)
        {
            using (var q = CreateQueue(highLevelSize, lowLevelSize, ord, bcg))
                testRunner(q);
        }

        // ===============

        private void AddTakeTest(LevelingQueue<int> queue, int bound)
        {
            Assert.AreEqual(bound, queue.BoundedCapacity);
            Assert.AreEqual(0, queue.Count);
            Assert.IsTrue(queue.IsEmpty);

            queue.Add(-1);
            Assert.AreEqual(1, queue.Count);
            Assert.IsFalse(queue.IsEmpty);

            int takeVal = queue.Take();
            Assert.AreEqual(-1, takeVal);
            Assert.AreEqual(0, queue.Count);
            Assert.IsTrue(queue.IsEmpty);

            int itemCount = 1;
            while (queue.TryAdd(itemCount))
                itemCount++;
            itemCount--;

            Assert.AreEqual(itemCount, queue.Count);
            Assert.IsFalse(queue.IsEmpty);

            queue.AddForced(++itemCount);
            Assert.AreEqual(itemCount, queue.Count);

            List<int> takenItems = new List<int>();

            while (queue.TryTake(out takeVal))
            {
                takenItems.Add(takeVal);
                Assert.AreEqual(itemCount - takenItems.Count, queue.Count);
            }

            Assert.AreEqual(itemCount, takenItems.Count, "count diff");
            Assert.IsTrue(queue.IsEmpty);

            if (queue.AddingMode != LevelingQueueAddingMode.PreserveOrder)
                takenItems.Sort();

            for (int i = 0; i < takenItems.Count; i++)
                Assert.AreEqual(i + 1, takenItems[i]);
        }

        [TestMethod]
        public void AddTakeTestOrdNoBcg() { RunTest(10, 10, true, false, q => AddTakeTest(q, 20)); }
        [TestMethod]
        public void AddTakeTestOrdBcg() { RunTest(10, 10, true, true, q => AddTakeTest(q, 20)); }
        [TestMethod]
        public void AddTakeTestNonOrdNoBcg() { RunTest(10, 10, false, false, q => AddTakeTest(q, 20)); }
        [TestMethod]
        public void AddTakeTestNonOrdBcg() { RunTest(10, 10, false, true, q => AddTakeTest(q, 20)); }


        // =========================

        private void AddWakesUpTest(LevelingQueue<int> queue)
        {
            while (queue.TryAdd(100)) ; // Fill queue
            if (queue.IsBackgroundTransferingEnabled)
            {
                SpinWait sw = new SpinWait();
                while (queue.IsBackgroundInWork && sw.Count < 100)
                    sw.SpinOnce();

                for (int i = 0; i < 100; i++)
                {
                    queue.TryAdd(100);
                    Thread.SpinWait(100);
                }
            }

            Barrier bar = new Barrier(2);
            int addResult = 0;
            Task task = Task.Run(() =>
            {
                bar.SignalAndWait();
                AtomicSet(ref addResult, queue.TryAdd(-100, 60000));
            });

            bar.SignalAndWait();
            Thread.Sleep(10);
            Assert.AreEqual(0, Volatile.Read(ref addResult));

            queue.Take();
            if (queue.AddingMode == LevelingQueueAddingMode.PreserveOrder && !queue.IsBackgroundTransferingEnabled)
            {
                int item;
                while (queue.TryTake(out item)) ;
            }

            TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref addResult));

            task.Wait();
        }

        [TestMethod]
        public void AddWakesUpTestOrdNoBcg() { RunTest(10, 10, true, false, q => AddWakesUpTest(q)); }
        [TestMethod]
        public void AddWakesUpTestOrdBcg() { RunTest(10, 10, true, true, q => AddWakesUpTest(q)); }
        [TestMethod]
        public void AddWakesUpTestNonOrdNoBcg() { RunTest(10, 10, false, false, q => AddWakesUpTest(q)); }
        [TestMethod]
        public void AddWakesUpTestNonOrdBcg() { RunTest(10, 10, false, true, q => AddWakesUpTest(q)); }



        // =========================

        private void TakeWakesUpTest(LevelingQueue<int> queue)
        {
            Barrier bar = new Barrier(2);
            int takeResult = 0;
            int takeResult2 = 0;
            Task task = Task.Run(() =>
            {
                bar.SignalAndWait();
                int item = 0;
                AtomicSet(ref takeResult, queue.TryTake(out item, 60000));
                Assert.AreEqual(100, item);

                item = 0;
                AtomicSet(ref takeResult2, queue.TryTake(out item, 60000));
                Assert.AreEqual(200, item);
            });

            bar.SignalAndWait();
            Thread.Sleep(10);
            Assert.AreEqual(0, Volatile.Read(ref takeResult));

            queue.Add(100);
            TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref takeResult));

            Thread.Sleep(10);
            Assert.AreEqual(0, Volatile.Read(ref takeResult2));

            queue.Add(200);
            TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref takeResult2));

            task.Wait();
        }

        [TestMethod]
        public void TakeWakesUpTestOrdNoBcg() { RunTest(10, 10, true, false, q => TakeWakesUpTest(q)); }
        [TestMethod]
        public void TakeWakesUpTestOrdBcg() { RunTest(10, 10, true, true, q => TakeWakesUpTest(q)); }
        [TestMethod]
        public void TakeWakesUpTestNonOrdNoBcg() { RunTest(10, 10, false, false, q => TakeWakesUpTest(q)); }
        [TestMethod]
        public void TakeWakesUpTestNonOrdBcg() { RunTest(10, 10, false, true, q => TakeWakesUpTest(q)); }




        // =========================

        private void TimeoutWorksTest(LevelingQueue<int> queue)
        {
            Barrier bar = new Barrier(2);
            int takeResult = 0;
            int addResult = 0;
            Task task = Task.Run(() =>
            {
                bar.SignalAndWait();
                int item = 0;
                AtomicSet(ref takeResult, queue.TryTake(out item, 100));

                while (queue.TryAdd(-1)) ;

                if (queue.IsBackgroundTransferingEnabled)
                {
                    queue.AddForcedToHighLevelQueue(-1);
                    queue.AddForced(-1); // To prevent background transferer from freeing the space
                }

                AtomicSet(ref addResult, queue.TryAdd(100, 100));
            });

            bar.SignalAndWait();

            TimingAssert.AreEqual(10000, 2, () => Volatile.Read(ref takeResult), "take");
            TimingAssert.AreEqual(10000, 2, () => Volatile.Read(ref addResult), "Add");

            task.Wait();
        }

        [TestMethod]
        public void TimeoutWorksTestOrdNoBcg() { RunTest(10, 10, true, false, q => TimeoutWorksTest(q)); }
        [TestMethod]
        public void TimeoutWorksTestOrdBcg() { RunTest(10, 10, true, true, q => TimeoutWorksTest(q)); }
        [TestMethod]
        public void TimeoutWorksTestNonOrdNoBcg() { RunTest(10, 10, false, false, q => TimeoutWorksTest(q)); }
        [TestMethod]
        public void TimeoutWorksTestNonOrdBcg() { RunTest(10, 10, false, true, q => TimeoutWorksTest(q)); }


        // =========================

        private void CancellationWorksTest(LevelingQueue<int> queue)
        {
            Barrier bar = new Barrier(2);
            CancellationTokenSource takeSource = new CancellationTokenSource();
            int takeResult = 0;
            CancellationTokenSource addSource = new CancellationTokenSource();
            int addResult = 0;
            Task task = Task.Run(() =>
            {
                bar.SignalAndWait();
                int item = 0;
                try
                {
                    AtomicSet(ref takeResult, queue.TryTake(out item, 60000, takeSource.Token));
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Exchange(ref takeResult, 3);
                }

                while (queue.TryAdd(-1)) ;
                if (queue.IsBackgroundTransferingEnabled)
                    queue.AddForced(-1);

                try
                {
                    AtomicSet(ref addResult, queue.TryAdd(100, 60000, addSource.Token));
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Exchange(ref addResult, 3);
                }
            });

            bar.SignalAndWait();

            Thread.Sleep(10);
            Assert.AreEqual(0, takeResult);
            takeSource.Cancel();
            TimingAssert.AreEqual(10000, 3, () => Volatile.Read(ref takeResult));

            Thread.Sleep(10);
            Assert.AreEqual(0, addResult);
            addSource.Cancel();
            TimingAssert.AreEqual(10000, 3, () => Volatile.Read(ref addResult));

            task.Wait();
        }

        [TestMethod]
        public void CancellationWorksTestOrdNoBcg() { RunTest(10, 10, true, false, q => CancellationWorksTest(q)); }
        [TestMethod]
        public void CancellationWorksTestOrdBcg() { RunTest(10, 10, true, true, q => CancellationWorksTest(q)); }
        [TestMethod]
        public void CancellationWorksTestNonOrdNoBcg() { RunTest(10, 10, false, false, q => CancellationWorksTest(q)); }
        [TestMethod]
        public void CancellationWorksTestNonOrdBcg() { RunTest(10, 10, false, true, q => CancellationWorksTest(q)); }


        // ======================


        [TestMethod]
        public void BacgroundTransferingWorksOrdBcg()
        {
            MemoryQueue<int> high = new MemoryQueue<int>(10);
            MemoryQueue<int> low = new MemoryQueue<int>(10);
            high.Add(1);
            while (low.TryAdd(low.Count + 2)) ;

            using (var inst = new LevelingQueue<int>(high, low, LevelingQueueAddingMode.PreserveOrder, true))
            {
                Assert.AreEqual(1, inst.Take()); // Touch the queue to enable transfering

                TimingAssert.AreEqual(10000, 10, () => high.Count);
                Assert.AreEqual(0, low.Count);

                int item = 0;
                int expected = 2;
                while (inst.TryTake(out item))
                    Assert.AreEqual(expected++, item);
            }
        }
        [TestMethod]
        public void BacgroundTransferingWorksNonOrdBcg()
        {
            MemoryQueue<int> high = new MemoryQueue<int>(10);
            MemoryQueue<int> low = new MemoryQueue<int>(10);
            high.Add(1);
            while (low.TryAdd(low.Count + 2)) ;

            using (var inst = new LevelingQueue<int>(high, low, LevelingQueueAddingMode.PreferLiveData, true))
            {
                Assert.AreEqual(1, inst.Take()); 

                TimingAssert.AreEqual(10000, 10, () => high.Count);
                Assert.AreEqual(0, low.Count);

                int item = 0;
                int expected = 2;
                while (inst.TryTake(out item))
                    Assert.AreEqual(expected++, item);
            }
        }

        // ==========================

        private void StableIsEmptyAndCountTest(bool ord, bool bcg)
        {
            const int ItemCount = 100000;

            MemoryQueue<int> high = new MemoryQueue<int>();
            MemoryQueue<int> low = new MemoryQueue<int>();

            for (int i = 0; i < ItemCount + 1; i++)
                low.Add(i);

            using (var q = new LevelingQueue<int>(high, low, ord ? LevelingQueueAddingMode.PreserveOrder : LevelingQueueAddingMode.PreferLiveData, bcg))
            {
                Assert.AreEqual(ItemCount + 1, q.Count);
                if (bcg && !ord)
                    q.Take();
                else
                    Assert.AreEqual(0, q.Take());

                int startTime = Environment.TickCount;
                while (!low.IsEmpty && (Environment.TickCount - startTime) < 500)
                {
                    Assert.AreEqual(ItemCount, q.Count);
                    Assert.IsFalse(q.IsEmpty);
                }
            }
        }

        [TestMethod]
        public void StableIsEmptyAndCountTestOrdNoBcg() { StableIsEmptyAndCountTest(true, false); }
        [TestMethod]
        public void StableIsEmptyAndCountTestOrdBcg() { StableIsEmptyAndCountTest(true, true); }
        [TestMethod]
        public void StableIsEmptyAndCountTestNonOrdNoBcg() { StableIsEmptyAndCountTest(false, false); }
        [TestMethod]
        public void StableIsEmptyAndCountTestNonOrdBcg() { StableIsEmptyAndCountTest(false, true); }


        // ==========================


        private void PreserveOrderTest(LevelingQueue<int> queue, int elemCount)
        {
            Barrier bar = new Barrier(2);
            CancellationTokenSource cancelled = new CancellationTokenSource();
            List<int> takenElems = new List<int>(elemCount + 1);

            Action addAction = () =>
            {
                int curElem = 0;
                Random rnd = new Random(Environment.TickCount + Thread.CurrentThread.ManagedThreadId);

                bar.SignalAndWait();
                while (curElem < elemCount)
                {
                    if (rnd.Next(100) == 0)
                        queue.AddForced(curElem);
                    else
                        queue.Add(curElem);

                    if (rnd.Next(100) == 0)
                        Thread.Yield();
                    Thread.SpinWait(rnd.Next(100));

                    curElem++;
                }

                cancelled.Cancel();
            };

            StringBuilder badInfo = new StringBuilder();

            Action takeAction = () =>
            {
                Random rnd = new Random(Environment.TickCount + Thread.CurrentThread.ManagedThreadId);

                bar.SignalAndWait();
                try
                {
                    while (!cancelled.IsCancellationRequested)
                    {
                        takenElems.Add(queue.Take(cancelled.Token));
                        //if (takenElems[takenElems.Count - 1] != takenElems.Count - 1)
                        //    badInfo.Append("Is taken from high = " + queue.LastTakeTop.ToString());

                        if (rnd.Next(100) == 0)
                            Thread.Yield();
                        Thread.SpinWait(rnd.Next(100));
                    }
                }
                catch (OperationCanceledException) { }

                int item = 0;
                while (queue.TryTake(out item))
                    takenElems.Add(item);
            };


            var sw = System.Diagnostics.Stopwatch.StartNew();

            Task addTask = Task.Factory.StartNew(addAction, TaskCreationOptions.LongRunning);
            Task takeTask = Task.Factory.StartNew(takeAction, TaskCreationOptions.LongRunning);

            Task.WaitAll(addTask, takeTask);

            Assert.AreEqual(elemCount, takenElems.Count);
            for (int i = 0; i < takenElems.Count; i++)
                if (i != takenElems[i])
                    Assert.AreEqual(i, takenElems[i], $"i != takenElems[i], nextItem = {takenElems[i + 1]}, badInfo = '{badInfo}'");
        }


        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void PreserveOrderTestOrdNoBcg()
        {
            //for (int i = 0; i < 30; i++)
            {
                RunTest(1, 1, true, false, q => PreserveOrderTest(q, 500));
                RunTest(1, 2, true, false, q => PreserveOrderTest(q, 500));
                RunTest(1000, 2000, true, false, q => PreserveOrderTest(q, 500000));
                RunTest(2013, 17003, true, false, q => PreserveOrderTest(q, 1000000));
            }
        }

        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void PreserveOrderTestOrdBcg()
        {
            //for (int i = 0; i < 30; i++)
            {
                RunTest(1, 1, true, true, q => PreserveOrderTest(q, 500));
                RunTest(1, 2, true, true, q => PreserveOrderTest(q, 500));
                RunTest(1000, 2000, true, true, q => PreserveOrderTest(q, 500000));
                RunTest(2013, 17003, true, true, q => PreserveOrderTest(q, 1000000));
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }



        // ==========================

        private void RunComplexTest(LevelingQueue<int> q, int elemCount, int thCount)
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
                        int tmp = 0;
                        if (q.TryTake(out tmp, -1, tokSrc.Token))
                            data.Add((int)tmp);

                        int sleepTime = rnd.Next(100);
                        if (sleepTime > 0)
                            Thread.SpinWait(sleepTime);
                    }
                }
                catch (OperationCanceledException) { }

                int tmp2;
                while (q.TryTake(out tmp2))
                    data.Add((int)tmp2);

                lock (global)
                    global.AddRange(data);
            };


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
        [Timeout(2 * 60 * 1000)]
        public void ComplexTestOrdNoBcg()
        {
            RunTest(1000, 2000, true, false, q => RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount / 2)));
            RunTest(1000, 2000, true, false, q => RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }
        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void ComplexTestOrdBcg()
        {
            RunTest(1000, 2000, true, true, q => RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount / 2)));
            RunTest(1000, 2000, true, true, q => RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }
        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void ComplexTestNonOrdNoBcg()
        {
            RunTest(1000, 2000, false, false, q => RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount / 2)));
            RunTest(1000, 2000, false, false, q => RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }
        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void ComplexTestNonOrdBcg()
        {
            RunTest(1000, 2000, false, true, q => RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount / 2)));
            RunTest(1000, 2000, false, true, q => RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }
    }
}
