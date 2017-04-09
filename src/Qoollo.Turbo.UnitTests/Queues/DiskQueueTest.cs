using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Collections.Concurrent;
using Qoollo.Turbo.Queues;
using Qoollo.Turbo.Queues.DiskQueueComponents;

namespace Qoollo.Turbo.UnitTests.Queues
{
    [TestClass]
    public class DiskQueueTest
    {
        public class MemorySegment<T> : DiskQueueSegment<T>
        {
            public readonly BlockingQueue<T> Queue;
            public readonly int Capacity;
            public volatile bool IsDeleted;
            public volatile int FilledItemCount;

            public MemorySegment(long number, int capacity) : base(number)
            {
                Capacity = capacity;
                Queue = new BlockingQueue<T>(capacity);
            }

            public override int Count { get { return Queue.Count; } }

            public override bool IsCompleted { get { return FilledItemCount >= Capacity && Queue.Count == 0; } }

            public override bool IsFull { get { return FilledItemCount >= Capacity; } }

            public override void AddForced(T item)
            {
                Assert.IsFalse(IsDeleted);
                Queue.AddForced(item);
                Interlocked.Increment(ref FilledItemCount);
            }

            public override bool TryAdd(T item)
            {
                Assert.IsFalse(IsDeleted);

                if (FilledItemCount >= Capacity)
                    return false;

                int filledItems = Interlocked.Increment(ref FilledItemCount);
                if (filledItems > Capacity)
                {
                    Interlocked.Decrement(ref FilledItemCount);
                    return false;
                }

                if (!Queue.TryAdd(item))
                {
                    Interlocked.Decrement(ref FilledItemCount);
                    return false;
                }
                return true;
            }

            public override bool TryPeek(out T item)
            {
                Assert.IsFalse(IsDeleted);
                return Queue.TryPeek(out item);
            }

            public override bool TryTake(out T item)
            {
                Assert.IsFalse(IsDeleted);
                return Queue.TryTake(out item);
            }

            protected override void Delete()
            {
                IsDeleted = true;
            }
        }

        public class MemorySegmentFactory<T> : DiskQueueSegmentFactory<T>
        {
            public readonly int Capacity;
            public readonly List<MemorySegment<T>> AllocatedSegments;

            public MemorySegmentFactory(int capacity)
            {
                Capacity = capacity;
                AllocatedSegments = new List<MemorySegment<T>>();
            }

            public override int SegmentCapacity { get { return Capacity; } }

            public override DiskQueueSegment<T> CreateSegment(string path, long number)
            {
                Assert.AreEqual("dummy", path);
                var newSeg = new MemorySegment<T>(number, Capacity);
                lock (AllocatedSegments)
                {
                    AllocatedSegments.Add(newSeg);
                }
                return newSeg;
            }

            public override DiskQueueSegment<T>[] DiscoverSegments(string path)
            {
                Assert.AreEqual("dummy", path);
                lock (AllocatedSegments)
                {
                    return AllocatedSegments.Where(o => !o.IsCompleted && !o.IsDeleted).ToArray();
                }
            }
        }


        // ==============================


        private static DiskQueue<int> CreateOnMem(int segmentCapacity, int segmentCount = -1, bool backComp = false)
        {
            return new DiskQueue<int>("dummy", new MemorySegmentFactory<int>(segmentCapacity), segmentCount, backComp, 500);
        }


        private static void RunMemTest(int segmentCapacity, int segmentCount, bool backComp, Action<DiskQueue<int>> testRunner)
        {
            using (var q = CreateOnMem(segmentCapacity, segmentCount, backComp))
                testRunner(q);
        }



        // ==============================

        [TestMethod]
        public void TestConstruction()
        {
            var factory = new MemorySegmentFactory<int>(100);
            using (var queue = new DiskQueue<int>("dummy", factory, 10, true, 500))
            {
                Assert.AreEqual("dummy", queue.Path);
                Assert.AreEqual(0, queue.Count);
                Assert.IsTrue(queue.IsEmpty);
                Assert.IsTrue(queue.IsBackgroundCompactionEnabled);
                Assert.AreEqual(1, queue.SegmentCount);
                Assert.AreEqual(1, factory.AllocatedSegments.Count);
                Assert.AreEqual(factory.Capacity * 10, queue.BoundedCapacity);

                for (int i = 0; i < factory.Capacity + 1; i++)
                    queue.Add(1);

                Assert.AreEqual(factory.Capacity + 1, queue.Count);
                Assert.IsFalse(queue.IsEmpty);
                Assert.AreEqual(2, queue.SegmentCount);
                Assert.AreEqual(2, factory.AllocatedSegments.Count);
            }
        }

        // ==============================


        private void TestSimpleAddTakePeek(DiskQueue<int> queue)
        {
            Assert.AreEqual(0, queue.Count);
            Assert.IsTrue(queue.IsEmpty);

            Assert.IsTrue(queue.TryAdd(1));

            Assert.AreEqual(1, queue.Count);
            Assert.IsFalse(queue.IsEmpty);

            int item = 0;
            Assert.IsTrue(queue.TryPeek(out item));
            Assert.AreEqual(1, item);

            Assert.IsFalse(queue.IsEmpty);

            item = 0;
            Assert.IsTrue(queue.TryTake(out item));
            Assert.AreEqual(1, item);

            Assert.AreEqual(0, queue.Count);
            Assert.IsTrue(queue.IsEmpty);


            for (int i = 1; i < 1000; i++)
            {
                if (i % 2 == 0)
                    queue.Add(i);
                else
                    queue.AddForced(i);

                Assert.AreEqual(i, queue.Count);
            }

            int itemNum = 0;
            while (queue.TryTake(out item))
            {
                itemNum++;
                Assert.AreEqual(itemNum, item);
            }

            Assert.AreEqual(999, itemNum);

            Assert.AreEqual(0, queue.Count);
            Assert.IsTrue(queue.IsEmpty);
        }


        [TestMethod]
        public void TestSimpleAddTakePeekMem() { RunMemTest(100, -1, false, q => TestSimpleAddTakePeek(q)); }
        [TestMethod]
        public void TestSimpleAddTakePeekMemStress() { RunMemTest(1, -1, true, q => TestSimpleAddTakePeek(q)); }


        // =========================

        private void AddWakesUpTest(DiskQueue<int> queue, int segmentCapacity)
        {
            while (queue.TryAdd(100)) ; // Fill queue
            queue.AddForced(200);

            Barrier bar = new Barrier(2);
            AtomicNullableBool addResult = new AtomicNullableBool();
            Task task = Task.Run(() =>
            {
                bar.SignalAndWait();
                addResult.Value = queue.TryAdd(-100, 60000);
            });

            bar.SignalAndWait();
            Thread.Sleep(10);
            Assert.IsFalse(addResult.HasValue);

            queue.Take();
            Thread.Sleep(10);
            Assert.IsFalse(addResult.HasValue);

            for (int i = 0; i < segmentCapacity; i++)
            {
                int tmp = 0;
                Assert.IsTrue(queue.TryTake(out tmp));
            }
            TimingAssert.AreEqual(10000, true, () => addResult.Value);

            task.Wait();
        }

        [TestMethod]
        public void AddWakesUpTestMem() { RunMemTest(100, 10, false, q => AddWakesUpTest(q, 100)); }
        [TestMethod]
        public void AddWakesUpTestMemStress() { RunMemTest(1, 2, true, q => AddWakesUpTest(q, 1)); }


        // =========================
        
        private void TakeWakesUpTest(DiskQueue<int> queue)
        {
            Barrier bar = new Barrier(2);
            AtomicNullableBool takeResult = new AtomicNullableBool();
            AtomicNullableBool takeResult2 = new AtomicNullableBool();
            Task task = Task.Run(() =>
            {
                bar.SignalAndWait();
                int item = 0;
                takeResult.Value = queue.TryTake(out item, 60000);
                Assert.AreEqual(100, item);

                item = 0;
                takeResult2.Value = queue.TryTake(out item, 60000);
                Assert.AreEqual(200, item);
            });

            bar.SignalAndWait();
            Thread.Sleep(10);
            Assert.IsFalse(takeResult.HasValue);

            queue.Add(100);
            TimingAssert.AreEqual(10000, true, () => takeResult.Value);

            Thread.Sleep(10);
            Assert.IsFalse(takeResult2.HasValue);

            queue.Add(200);
            TimingAssert.AreEqual(10000, true, () => takeResult2.Value);

            task.Wait();
        }

        [TestMethod]
        public void TakeWakesUpTestMem() { RunMemTest(100, 10, false, q => TakeWakesUpTest(q)); }
        [TestMethod]
        public void TakeWakesUpTestMemStress() { RunMemTest(1, 2, true, q => TakeWakesUpTest(q)); }




        // =========================
        
        private void PeekWakesUpTest(DiskQueue<int> queue)
        {
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
            Thread.Sleep(10);
            Assert.IsFalse(peekResult.HasValue);
            Assert.IsFalse(peekResult2.HasValue);

            queue.Add(100);
            TimingAssert.AreEqual(10000, true, () => peekResult.Value);
            TimingAssert.AreEqual(10000, true, () => peekResult2.Value);

            Task.WaitAll(task, task2);
        }

        [TestMethod]
        public void PeekWakesUpTestMem() { RunMemTest(100, 10, false, q => PeekWakesUpTest(q)); }
        [TestMethod]
        public void PeekWakesUpTestMemStress() { RunMemTest(1, 2, true, q => PeekWakesUpTest(q)); }




        // =========================
        
        private void TimeoutWorksTest(DiskQueue<int> queue)
        {
            Barrier bar = new Barrier(2);
            AtomicNullableBool takeResult = new AtomicNullableBool();
            AtomicNullableBool peekResult = new AtomicNullableBool();
            AtomicNullableBool addResult = new AtomicNullableBool();
            Task task = Task.Run(() =>
            {
                bar.SignalAndWait();
                int item = 0;
                takeResult.Value = queue.TryTake(out item, 100);
                peekResult.Value = queue.TryPeek(out item, 100);

                while (queue.TryAdd(-1)) ;

                addResult.Value = queue.TryAdd(100, 100);
            });

            bar.SignalAndWait();

            TimingAssert.AreEqual(10000, false, () => takeResult.Value, "take");
            TimingAssert.AreEqual(10000, false, () => peekResult.Value, "peek");
            TimingAssert.AreEqual(10000, false, () => addResult.Value, "Add");

            task.Wait();
        }

        [TestMethod]
        public void TimeoutWorksTestMem() { RunMemTest(100, 10, false, q => TimeoutWorksTest(q)); }
        [TestMethod]
        public void TimeoutWorksTestMemStress() { RunMemTest(1, 2, true, q => TimeoutWorksTest(q)); }
        

        // =========================
        
        private void CancellationWorksTest(DiskQueue<int> queue)
        {
            Barrier bar = new Barrier(2);
            CancellationTokenSource takeSource = new CancellationTokenSource();
            AtomicInt takeResult = new AtomicInt();
            CancellationTokenSource peekSource = new CancellationTokenSource();
            AtomicInt peekResult = new AtomicInt();
            CancellationTokenSource addSource = new CancellationTokenSource();
            AtomicInt addResult = new AtomicInt();
            Task task = Task.Run(() =>
            {
                bar.SignalAndWait();
                int item = 0;
                try
                {
                    takeResult.Value = queue.TryTake(out item, 60000, takeSource.Token) ? 1 : -1;
                }
                catch (OperationCanceledException)
                {
                    takeResult.Value = 3;
                }

                try
                {
                    peekResult.Value = queue.TryPeek(out item, 60000, peekSource.Token) ? 1 : -1;
                }
                catch (OperationCanceledException)
                {
                    peekResult.Value = 3;
                }

                while (queue.TryAdd(-1)) ;

                try
                {
                    addResult.Value = queue.TryAdd(100, 60000, addSource.Token) ? 1 : -1;
                }
                catch (OperationCanceledException)
                {
                    addResult.Value = 3;
                }
            });

            bar.SignalAndWait();

            Thread.Sleep(10);
            Assert.AreEqual(0, takeResult);
            takeSource.Cancel();
            TimingAssert.AreEqual(10000, 3, () => takeResult);

            Thread.Sleep(10);
            Assert.AreEqual(0, peekResult);
            peekSource.Cancel();
            TimingAssert.AreEqual(10000, 3, () => peekResult);

            Thread.Sleep(10);
            Assert.AreEqual(0, addResult);
            addSource.Cancel();
            TimingAssert.AreEqual(10000, 3, () => addResult);

            task.Wait();
        }

        [TestMethod]
        public void CancellationWorksTestMem() { RunMemTest(100, 10, false, q => CancellationWorksTest(q)); }
        [TestMethod]
        public void CancellationWorksTestMemStress() { RunMemTest(1, 2, true, q => CancellationWorksTest(q)); }
        
        // ======================


        [TestMethod]
        public void SegmentAllocationCompactionTest()
        {
            var segmentFactory = new MemorySegmentFactory<int>(10);
            using (var queue = new DiskQueue<int>("dummy", segmentFactory, -1, false))
            {
                Assert.AreEqual(1, queue.SegmentCount);

                for (int i = 0; i < segmentFactory.Capacity; i++)
                    queue.Add(i);

                Assert.AreEqual(1, queue.SegmentCount);

                queue.Add(-1);
                Assert.AreEqual(2, queue.SegmentCount);
                Assert.AreEqual(2, segmentFactory.AllocatedSegments.Count);

                for (int i = 0; i < segmentFactory.Capacity; i++)
                    queue.Take();

                Assert.IsTrue(segmentFactory.AllocatedSegments[0].IsCompleted);

                // should trigger compaction
                queue.Take();

                Assert.AreEqual(1, queue.SegmentCount);
                Assert.AreEqual(2, segmentFactory.AllocatedSegments.Count);
            }
        }


        [TestMethod]
        public void SegmentBackgroundCompactionTest()
        {
            var segmentFactory = new MemorySegmentFactory<int>(10);
            using (var queue = new DiskQueue<int>("dummy", segmentFactory, -1, true, 100))
            {
                Assert.AreEqual(1, queue.SegmentCount);

                for (int i = 0; i < segmentFactory.Capacity; i++)
                    queue.Add(i);

                Assert.AreEqual(1, queue.SegmentCount);

                queue.Add(-1);
                Assert.AreEqual(2, queue.SegmentCount);
                Assert.AreEqual(2, segmentFactory.AllocatedSegments.Count);

                for (int i = 0; i < segmentFactory.Capacity; i++)
                    queue.Take();

                Assert.IsTrue(segmentFactory.AllocatedSegments[0].IsCompleted);

                TimingAssert.AreEqual(10000, 1, () => { Interlocked.MemoryBarrier(); return queue.SegmentCount; });
                Assert.AreEqual(2, segmentFactory.AllocatedSegments.Count);
            }
        }


        [TestMethod]
        public void SegmentDiscoveryTest()
        {
            var segmentFactory = new MemorySegmentFactory<int>(10);
            using (var queue = new DiskQueue<int>("dummy", segmentFactory, -1, false))
            {
                Assert.AreEqual(1, queue.SegmentCount);

                for (int i = 0; i < segmentFactory.Capacity * 2; i++)
                    queue.Add(i);

                Assert.AreEqual(2, queue.SegmentCount);
            }

            using (var queue = new DiskQueue<int>("dummy", segmentFactory, -1, false))
            {
                Assert.AreEqual(2, queue.SegmentCount);

                for (int i = 0; i < segmentFactory.Capacity * 2; i++)
                    Assert.AreEqual(i, queue.Take());

                Assert.AreEqual(1, queue.SegmentCount);
            }
        }

        // ======================
    }
}
