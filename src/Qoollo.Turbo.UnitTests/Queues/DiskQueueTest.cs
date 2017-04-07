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
            public readonly List<MemorySegment<T>> PreallocatedSegments;

            public MemorySegmentFactory(int capacity)
            {
                Capacity = capacity;
                PreallocatedSegments = new List<MemorySegment<T>>();
            }

            public override DiskQueueSegment<T> CreateSegment(string path, long number)
            {
                Assert.AreEqual("dummy", path);
                return new MemorySegment<T>(number, Capacity);
            }

            public override DiskQueueSegment<T>[] DiscoverSegments(string path)
            {
                Assert.AreEqual("dummy", path);
                return PreallocatedSegments.ToArray();
            }
        }


        // ==============================


        private static DiskQueue<int> CreateOnMem(int segmentCapacity, int segmentCount = -1, bool backComp = false)
        {
            return new DiskQueue<int>("dummy", new MemorySegmentFactory<int>(segmentCapacity), segmentCount, backComp);
        }


        private static void RunMemTest(int segmentCapacity, int segmentCount, bool backComp, Action<DiskQueue<int>> testRunner)
        {
            using (var q = CreateOnMem(segmentCapacity, segmentCount, backComp))
                testRunner(q);
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

        // ==============
    }
}
