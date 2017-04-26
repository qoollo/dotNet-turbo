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
using System.Collections.Concurrent;
using System.IO;

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
            public volatile int TakenItemCount;

            public MemorySegment(long number, int capacity) : base(number)
            {
                Capacity = capacity;
                Queue = new BlockingQueue<T>(capacity);
            }

            public override int Count { get { return Queue.Count; } }

            public override bool IsCompleted { get { return FilledItemCount >= Capacity && TakenItemCount == FilledItemCount; } }

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

                SpinWait sw = new SpinWait();
                int filledItemCount = FilledItemCount;
                while (filledItemCount < Capacity && Interlocked.CompareExchange(ref FilledItemCount, filledItemCount + 1, filledItemCount) != filledItemCount)
                {
                    sw.SpinOnce();
                    filledItemCount = FilledItemCount;
                }

                if (filledItemCount >= Capacity)
                    return false;

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
                if (Queue.TryTake(out item))
                {
                    Interlocked.Increment(ref TakenItemCount);
                    return true;
                }

                return false;
            }

            protected override void Dispose(DiskQueueSegmentDisposeBehaviour disposeBehaviour, bool isUserCall)
            {
                if (disposeBehaviour == DiskQueueSegmentDisposeBehaviour.Delete)
                    IsDeleted = true;
            }
        }

        public abstract class CommonSegmentFactory<T>: DiskQueueSegmentFactory<T>
        {
            public abstract long SumCountFromAllocated();
        }

        public class MemorySegmentFactory<T> : CommonSegmentFactory<T>
        {
            public readonly int Capacity;
            public readonly List<MemorySegment<T>> AllocatedSegments;

            public MemorySegmentFactory(int capacity)
            {
                Capacity = capacity;
                AllocatedSegments = new List<MemorySegment<T>>();
            }

            public override long SumCountFromAllocated()
            {
                lock (AllocatedSegments)
                {
                    return AllocatedSegments.Sum(o => (long)o.Count);
                }
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


        public class CountingMemorySegment<T> : CountingDiskQueueSegment<T>
        {
            public readonly ConcurrentQueue<T> Queue;
            public volatile bool IsDeleted;
            public volatile bool IsDisposed;

            public CountingMemorySegment(long number, int capacity) 
                : base(number, capacity, 0, 0)
            {
                Queue = new ConcurrentQueue<T>();
            }

            protected override void AddCore(T item)
            {
                Assert.IsFalse(IsDeleted);
                Assert.IsFalse(IsDisposed);
                Queue.Enqueue(item);
            }

            protected override bool TryTakeCore(out T item)
            {
                Assert.IsFalse(IsDeleted);
                Assert.IsFalse(IsDisposed);
                return Queue.TryDequeue(out item);
            }

            protected override bool TryPeekCore(out T item)
            {
                Assert.IsFalse(IsDeleted);
                Assert.IsFalse(IsDisposed);
                return Queue.TryPeek(out item);
            }

            protected override void Dispose(DiskQueueSegmentDisposeBehaviour disposeBehaviour, bool isUserCall)
            {
                if (disposeBehaviour == DiskQueueSegmentDisposeBehaviour.Delete)
                    IsDeleted = true;

                IsDisposed = true;
            }
        }

        public class CountingMemorySegmentFactory<T> : CommonSegmentFactory<T>
        {
            public readonly int Capacity;
            public readonly List<CountingMemorySegment<T>> AllocatedSegments;

            public CountingMemorySegmentFactory(int capacity)
            {
                Capacity = capacity;
                AllocatedSegments = new List<CountingMemorySegment<T>>();
            }

            public override long SumCountFromAllocated()
            {
                lock (AllocatedSegments)
                {
                    return AllocatedSegments.Sum(o => (long)o.Count);
                }
            }

            public override int SegmentCapacity { get { return Capacity; } }

            public override DiskQueueSegment<T> CreateSegment(string path, long number)
            {
                Assert.AreEqual("dummy", path);
                var newSeg = new CountingMemorySegment<T>(number, Capacity);
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


        private class ItemSerializer : IDiskQueueItemSerializer<int>
        {
            public int ExpectedSizeInBytes { get { return 4; } }

            public int Deserialize(BinaryReader reader)
            {
                return reader.ReadInt32();
            }

            public void Serialize(BinaryWriter writer, int item)
            {
                writer.Write(item);
            }
        }


        // ==============================


        private static DiskQueue<int> CreateOnMem(int segmentCapacity, int segmentCount = -1, bool backComp = false)
        {
            return new DiskQueue<int>("dummy", new MemorySegmentFactory<int>(segmentCapacity), segmentCount, backComp, 500);
        }
        private static DiskQueue<int> CreateOnMemCounting(int segmentCapacity, int segmentCount = -1, bool backComp = false)
        {
            return new DiskQueue<int>("dummy", new CountingMemorySegmentFactory<int>(segmentCapacity), segmentCount, backComp, 500);
        }
        private static DiskQueue<int> CreateOnMemDefault(int segmentCapacity, int segmentCount = -1, bool backComp = false)
        {
            return new DiskQueue<int>("dummy", new MemoryDiskQueueSegmentFactory<int>(segmentCapacity), segmentCount, backComp, 500);
        }
        private static DiskQueue<int> CreateNonPersistent(string dir, int segmentCapacity, int segmentCount = -1, bool backComp = false)
        {
            return new DiskQueue<int>(dir, new NonPersistentDiskQueueSegmentFactory<int>(segmentCapacity, "prefix", new ItemSerializer()), segmentCount, backComp, 500);
        }
        private static DiskQueue<int> CreatePersistent(string dir, int segmentCapacity, int segmentCount = -1, bool backComp = false)
        {
            return new DiskQueue<int>(dir, new PersistentDiskQueueSegmentFactory<int>(segmentCapacity, "prefix", new ItemSerializer()), segmentCount, backComp, 500);
        }

        private static void DeleteDir(string dir)
        {
            try
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
            catch { }
        }


        private static void RunMemTest(int segmentCapacity, int segmentCount, bool backComp, Action<DiskQueue<int>> testRunner)
        {
            using (var q = CreateOnMem(segmentCapacity, segmentCount, backComp))
                testRunner(q);
        }
        private static void RunMemTest(int segmentCapacity, int segmentCount, bool backComp, Action<DiskQueue<int>, MemorySegmentFactory<int>> testRunner)
        {
            var factory = new MemorySegmentFactory<int>(segmentCapacity);
            using (var q = new DiskQueue<int>("dummy", factory, segmentCount, backComp, 500))
                testRunner(q, factory);
        }

        private static void RunMemCountingTest(int segmentCapacity, int segmentCount, bool backComp, Action<DiskQueue<int>> testRunner)
        {
            using (var q = CreateOnMemCounting(segmentCapacity, segmentCount, backComp))
                testRunner(q);
        }
        private static void RunMemCountingTest(int segmentCapacity, int segmentCount, bool backComp, Action<DiskQueue<int>, CountingMemorySegmentFactory<int>> testRunner)
        {
            var factory = new CountingMemorySegmentFactory<int>(segmentCapacity);
            using (var q = new DiskQueue<int>("dummy", factory, segmentCount, backComp, 500))
                testRunner(q, factory);
        }

        private static void RunMemDefaultTest(int segmentCapacity, int segmentCount, bool backComp, Action<DiskQueue<int>> testRunner)
        {
            using (var q = CreateOnMemDefault(segmentCapacity, segmentCount, backComp))
                testRunner(q);
        }

        private static void RunNonPersistent(int segmentCapacity, int segmentCount, bool backComp, Action<DiskQueue<int>> testRunner)
        {
            string dir = Guid.NewGuid().ToString().Replace('-', '_');
            try
            {
                Directory.CreateDirectory(dir);
                using (var q = CreateNonPersistent(dir, segmentCapacity, segmentCount, backComp))
                    testRunner(q);
            }
            finally
            {
                DeleteDir(dir);
            }
        }

        private static void RunPersistent(int segmentCapacity, int segmentCount, bool backComp, Action<DiskQueue<int>> testRunner)
        {
            string dir = Guid.NewGuid().ToString().Replace('-', '_');
            try
            {
                Directory.CreateDirectory(dir);
                using (var q = CreatePersistent(dir, segmentCapacity, segmentCount, backComp))
                    testRunner(q);
            }
            finally
            {
                DeleteDir(dir);
            }
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
        [TestMethod]
        public void TestSimpleAddTakePeekMemCounting() { RunMemCountingTest(100, -1, false, q => TestSimpleAddTakePeek(q)); }
        [TestMethod]
        public void TestSimpleAddTakePeekMemCountingStress() { RunMemCountingTest(1, -1, true, q => TestSimpleAddTakePeek(q)); }
        [TestMethod]
        public void TestSimpleAddTakePeekMemDefault() { RunMemDefaultTest(100, -1, false, q => TestSimpleAddTakePeek(q)); }
        [TestMethod]
        public void TestSimpleAddTakePeekMemDefaultStress() { RunMemDefaultTest(1, -1, true, q => TestSimpleAddTakePeek(q)); }
        [TestMethod]
        public void TestSimpleAddTakePeekNonPersistent() { RunNonPersistent(1000, -1, true, q => TestSimpleAddTakePeek(q)); }
        [TestMethod]
        public void TestSimpleAddTakePeekPersistent() { RunPersistent(1000, -1, true, q => TestSimpleAddTakePeek(q)); }

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
        [TestMethod]
        public void AddWakesUpTestMemCountingStress() { RunMemCountingTest(1, 2, true, q => AddWakesUpTest(q, 1)); }

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
        [TestMethod]
        public void TakeWakesUpTestMemCountingStress() { RunMemCountingTest(1, 2, true, q => TakeWakesUpTest(q)); }



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
        [TestMethod]
        public void PeekWakesUpTestMemCountingStress() { RunMemCountingTest(1, 2, true, q => PeekWakesUpTest(q)); }



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
        [TestMethod]
        public void TimeoutWorksTestMemCountingStress() { RunMemCountingTest(1, 2, true, q => TimeoutWorksTest(q)); }

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
        [TestMethod]
        public void CancellationWorksTestMemCountingStress() { RunMemCountingTest(1, 2, true, q => CancellationWorksTest(q)); }


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
                Assert.AreEqual(3, queue.SegmentCount);
                Assert.AreEqual(3, segmentFactory.AllocatedSegments.Count);

                for (int i = 0; i < segmentFactory.Capacity * 2; i++)
                    Assert.AreEqual(i, queue.Take());

                Assert.AreEqual(2, queue.SegmentCount);
            }
        }

        [TestMethod]
        public void SegmentDiscoveryOnDiskTest()
        {
            string dir = Guid.NewGuid().ToString().Replace('-', '_');
            const int capacity = 1000;
            try
            {
                Directory.CreateDirectory(dir);
                using (var queue = CreatePersistent(dir, capacity, 10, false))
                {
                    Assert.AreEqual(1, queue.SegmentCount);

                    for (int i = 0; i < capacity * 2; i++)
                        queue.Add(i);

                    Assert.AreEqual(2, queue.SegmentCount);
                }

                using (var queue = CreatePersistent(dir, capacity, 10, false))
                {
                    Assert.AreEqual(3, queue.SegmentCount);

                    int item = 0;
                    int expected = 0;
                    while (queue.TryTake(out item))
                    {
                        Assert.AreEqual(expected, item);
                        expected++;
                    }
                    Assert.AreEqual(capacity * 2, expected);

                    Assert.AreEqual(1, queue.SegmentCount);
                }

                using (var queue = CreatePersistent(dir, capacity, 10, false))
                {
                    Assert.AreEqual(2, queue.SegmentCount);

                    queue.AddForced(100);
                    Assert.IsTrue(queue.TryAdd(200));

                    int item = 0;
                    Assert.IsTrue(queue.TryTake(out item));
                    Assert.AreEqual(100, item);
                    Assert.IsTrue(queue.TryTake(out item));
                    Assert.AreEqual(200, item);
                    Assert.IsFalse(queue.TryTake(out item));

                    Assert.AreEqual(1, queue.SegmentCount);
                }
            }
            finally
            {
                DeleteDir(dir);
            }
        }

        // ======================


        private void PreserveOrderTest(DiskQueue<int> queue, int elemCount)
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

            Action takeAction = () =>
            {
                Random rnd = new Random(Environment.TickCount + Thread.CurrentThread.ManagedThreadId);

                bar.SignalAndWait();
                try
                {
                    while (!cancelled.IsCancellationRequested)
                    {
                        takenElems.Add(queue.Take(cancelled.Token));

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
                    Assert.AreEqual(i, takenElems[i], $"i != takenElems[i], nextItem = {takenElems[i + 1]}");
        }

        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void PreserveOrderTestOnMem()
        {
            //for (int i = 0; i < 30; i++)
            {
                RunMemTest(100, 2, false, q => PreserveOrderTest(q, 5090));
                RunMemTest(100, 5, true, q => PreserveOrderTest(q, 5000));
                RunMemTest(1000, -1, false, q => PreserveOrderTest(q, 500000));
                RunMemTest(2000, -1, true, q => PreserveOrderTest(q, 1000000));
            }
        }

        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void PreserveOrderTestOnMemStress()
        {
            //for (int i = 0; i < 30; i++)
            {
                RunMemTest(1, 2, false, q => PreserveOrderTest(q, 500));
                RunMemTest(1, 2, true, q => PreserveOrderTest(q, 500));
                RunMemTest(1, -1, false, q => PreserveOrderTest(q, 20000));
                RunMemTest(1, -1, true, q => PreserveOrderTest(q, 20000));
            }
        }

        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void PreserveOrderTestOnMemCounting()
        {
            //for (int i = 0; i < 30; i++)
            {
                RunMemCountingTest(100, 2, false, q => PreserveOrderTest(q, 5090));
                RunMemCountingTest(100, 5, true, q => PreserveOrderTest(q, 5000));
                RunMemCountingTest(1000, -1, false, q => PreserveOrderTest(q, 500000));
                RunMemCountingTest(2000, -1, true, q => PreserveOrderTest(q, 1000000));
            }
        }

        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void PreserveOrderTestOnMemCountingStress()
        {
            //for (int i = 0; i < 30; i++)
            {
                RunMemCountingTest(1, 2, false, q => PreserveOrderTest(q, 500));
                RunMemCountingTest(1, 2, true, q => PreserveOrderTest(q, 500));
                RunMemCountingTest(1, -1, false, q => PreserveOrderTest(q, 10000));
                RunMemCountingTest(1, -1, true, q => PreserveOrderTest(q, 10000));
            }
        }

        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void PreserveOrderTestOnMemDefault()
        {
            //for (int i = 0; i < 30; i++)
            {
                RunMemDefaultTest(100, 2, false, q => PreserveOrderTest(q, 5090));
                RunMemDefaultTest(100, 5, true, q => PreserveOrderTest(q, 5000));
                RunMemDefaultTest(1000, -1, false, q => PreserveOrderTest(q, 500000));
                RunMemDefaultTest(2000, -1, true, q => PreserveOrderTest(q, 1000000));
            }
        }

        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void PreserveOrderTestOnMemDefaultStress()
        {
            //for (int i = 0; i < 30; i++)
            {
                RunMemDefaultTest(1, 2, false, q => PreserveOrderTest(q, 500));
                RunMemDefaultTest(1, 2, true, q => PreserveOrderTest(q, 500));
                RunMemDefaultTest(1, -1, false, q => PreserveOrderTest(q, 10000));
                RunMemDefaultTest(1, -1, true, q => PreserveOrderTest(q, 10000));
            }
        }

        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void PreserveOrderTestOnNonPersistent()
        {
            //for (int i = 0; i < 30; i++)
            {
                RunNonPersistent(1000, 2, false, q => PreserveOrderTest(q, 5000));
                RunNonPersistent(1000, 2, true, q => PreserveOrderTest(q, 5000));
                RunNonPersistent(1000, -1, false, q => PreserveOrderTest(q, 70000));
                RunNonPersistent(1000, -1, true, q => PreserveOrderTest(q, 70000));
            }
        }

        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void PreserveOrderTestOnPersistent()
        {
            //for (int i = 0; i < 30; i++)
            {
                RunPersistent(1000, 2, false, q => PreserveOrderTest(q, 5000));
                RunPersistent(1000, 2, true, q => PreserveOrderTest(q, 5000));
                RunPersistent(1000, -1, false, q => PreserveOrderTest(q, 70000));
                RunPersistent(1000, -1, true, q => PreserveOrderTest(q, 70000));
            }
        }

        // ======================

        private void ValidateCountTest(DiskQueue<int> queue, CommonSegmentFactory<int> factory, int elemCount)
        {
            Barrier bar = new Barrier(2);
            CancellationTokenSource cancelled = new CancellationTokenSource();
            List<int> takenElems = new List<int>(elemCount + 1);
            AtomicBool needSync = new AtomicBool();

            Action addAction = () =>
            {
                int curElem = 0;
                Random rnd = new Random(Environment.TickCount + Thread.CurrentThread.ManagedThreadId);

                bar.SignalAndWait();
                while (curElem < elemCount)
                {
                    bool itemAdded = true;
                    if (rnd.Next(100) == 0)
                        queue.AddForced(curElem);
                    else if (needSync.Value)
                        itemAdded = queue.TryAdd(curElem);
                    else
                        queue.Add(curElem);

                    if (rnd.Next(100) == 0)
                        Thread.Yield();
                    Thread.SpinWait(rnd.Next(100));

                    if (itemAdded)
                        curElem++;

                    Assert.IsTrue(itemAdded || needSync.Value);

                    if (curElem % 1000 == 0)
                    {
                        needSync.Value = true;
                    }
                    if (needSync.Value && bar.ParticipantsRemaining == 1)
                    {
                        Assert.AreEqual(factory.SumCountFromAllocated(), queue.Count);
                        needSync.Value = false;
                        bar.SignalAndWait();
                    }
                }

                cancelled.Cancel();
            };

            Action takeAction = () =>
            {
                Random rnd = new Random(Environment.TickCount + Thread.CurrentThread.ManagedThreadId);

                bar.SignalAndWait();
                try
                {
                    while (!cancelled.IsCancellationRequested)
                    {
                        takenElems.Add(queue.Take(cancelled.Token));

                        if (rnd.Next(100) == 0)
                            Thread.Yield();
                        Thread.SpinWait(rnd.Next(100));

                        if (needSync.Value)
                        {
                            bar.SignalAndWait(cancelled.Token);
                        }
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
                    Assert.AreEqual(i, takenElems[i], $"i != takenElems[i], nextItem = {takenElems[i + 1]}");
        }


        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void ValidateCountTestOnMem()
        {
            //for (int i = 0; i < 30; i++)
            {
                RunMemTest(1, 2, false, (q, f) => ValidateCountTest(q, f, 2000));
                RunMemTest(100, 5, true, (q, f) => ValidateCountTest(q, f, 10000));
                RunMemTest(1000, -1, false, (q, f) => ValidateCountTest(q, f, 5000000));
                RunMemTest(2000, -1, true, (q, f) => ValidateCountTest(q, f, 1000000));
            }
        }
        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void ValidateCountTestOnMemCounting()
        {
            //for (int i = 0; i < 30; i++)
            {
                RunMemCountingTest(1, 2, false, (q, f) => ValidateCountTest(q, f, 2000));
                RunMemCountingTest(100, 5, true, (q, f) => ValidateCountTest(q, f, 10000));
                RunMemCountingTest(1000, -1, false, (q, f) => ValidateCountTest(q, f, 5000000));
                RunMemCountingTest(2000, -1, true, (q, f) => ValidateCountTest(q, f, 1000000));
            }
        }

        // ==================================

        private void RunComplexTest(DiskQueue<int> q, int elemCount, int thCount)
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

                    int tmpItem = 0;
                    if (q.TryPeek(out tmpItem) && tmpItem == item)
                        sleepTime += 100;

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
        public void ComplexTestOnMem()
        {
            RunMemTest(1000, -1, false, q => RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount / 2)));
            RunMemTest(20000, 2000, true, q => RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }
        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void ComplexTestOnMemStress()
        {
            RunMemTest(1, 1000, false, q => RunComplexTest(q, 15000, Math.Max(1, Environment.ProcessorCount / 2)));
            RunMemTest(1, -1, true, q => RunComplexTest(q, 15000, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }

        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void ComplexTestOnMemCounting()
        {
            RunMemCountingTest(1000, -1, false, q => RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount / 2)));
            RunMemCountingTest(20000, 2000, true, q => RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }
        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void ComplexTestOnMemCountingStress()
        {
            RunMemCountingTest(1, 1000, false, q => RunComplexTest(q, 15000, Math.Max(1, Environment.ProcessorCount / 2)));
            RunMemCountingTest(1, -1, true, q => RunComplexTest(q, 15000, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }

        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void ComplexTestOnMemDefault()
        {
            RunMemDefaultTest(1000, -1, false, q => RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount / 2)));
            RunMemDefaultTest(20000, 2000, true, q => RunComplexTest(q, 2000000, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }
        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void ComplexTestOnMemDefaultStress()
        {
            RunMemDefaultTest(1, 1000, false, q => RunComplexTest(q, 15000, Math.Max(1, Environment.ProcessorCount / 2)));
            RunMemDefaultTest(1, -1, true, q => RunComplexTest(q, 15000, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }
        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void ComplexTestOnNonPersistent()
        {
            RunNonPersistent(10000, 1000, false, q => RunComplexTest(q, 500000, Math.Max(1, Environment.ProcessorCount / 2)));
            RunNonPersistent(10000, -1, true, q => RunComplexTest(q, 1000000, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }
        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void ComplexTestOnPersistent()
        {
            RunPersistent(10000, 1000, false, q => RunComplexTest(q, 100000, Math.Max(1, Environment.ProcessorCount / 2)));
            RunPersistent(10000, -1, true, q => RunComplexTest(q, 200000, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }
    }
}
