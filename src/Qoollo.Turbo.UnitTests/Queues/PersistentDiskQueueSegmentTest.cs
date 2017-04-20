using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Queues.DiskQueueComponents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Queues
{
    [TestClass]
    public class PersistentDiskQueueSegmentTest
    {
        private class ItemSerializer : IDiskQueueItemSerializer<int>
        {
            public int ExpectedSizeInBytes { get { return -1; } }

            public int Deserialize(BinaryReader reader)
            {
                if (reader.BaseStream.Length == 0)
                    return 0;
                return reader.ReadInt32();
            }

            public void Serialize(BinaryWriter writer, int item)
            {
                if (item != 0)
                    writer.Write(item);
            }
        }


        private static PersistentDiskQueueSegment<int> Create(int capacity, int flushPeriod, int readBufferSize, int cachedMemoryWriteStreamSize = -1, int cachedMemoryReadStreamSize = -1)
        {
            string fileName = Guid.NewGuid().ToString().Replace('-', '_') + NonPersistentDiskQueueSegmentFactory<int>.SegmentFileExtension;

            try
            {
                return new PersistentDiskQueueSegment<int>(1, fileName, new ItemSerializer(), capacity, false, flushPeriod, cachedMemoryWriteStreamSize, readBufferSize, cachedMemoryReadStreamSize);
            }
            catch
            {
                if (File.Exists(fileName))
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch { }
                }

                throw;
            }
        }

        private static void RunTest(int capacity, int flushPeriod, int readBufferSize, Action<PersistentDiskQueueSegment<int>> runner)
        {
            string fileName = null;
            try
            {
                using (var segment = Create(capacity, flushPeriod, readBufferSize))
                {
                    fileName = segment.FileName;
                    runner(segment);
                }
            }
            finally
            {
                if (fileName != null && File.Exists(fileName))
                {
                    try
                    {
                        File.Delete(fileName);
                    }
                    catch { }
                }
            }
        }

        // =======================


        [TestMethod]
        public void SegmentCreationTest()
        {
            string fileName = null;
            using (var segment = Create(1000, -1, -1, -1, -1))
            {
                fileName = segment.FileName;

                Assert.AreEqual(1000, segment.Capacity);
                Assert.AreEqual(0, segment.Count);
                Assert.IsFalse(segment.IsFull);
                Assert.IsFalse(segment.IsCompleted);
                Assert.IsTrue(File.Exists(segment.FileName));

                Assert.IsTrue(segment.ReadBufferSize > 0);
                Assert.IsTrue(segment.FlushToDiskOnItem > 0);
                Assert.IsTrue(segment.CachedMemoryReadStreamSize > 0);
                Assert.IsTrue(segment.CachedMemoryWriteStreamSize > 0);

                segment.Dispose(DiskQueueSegmentDisposeBehaviour.Delete);
            }

            Assert.IsFalse(File.Exists(fileName));

            using (var segment = Create(1000, 0, 0, 0, 0))
            {
                fileName = segment.FileName;

                Assert.AreEqual(1000, segment.Capacity);
                Assert.AreEqual(0, segment.Count);
                Assert.IsFalse(segment.IsFull);
                Assert.IsFalse(segment.IsCompleted);
                Assert.IsTrue(File.Exists(segment.FileName));

                Assert.IsTrue(segment.ReadBufferSize == 0);
                Assert.IsTrue(segment.FlushToDiskOnItem == 0);
                Assert.IsTrue(segment.CachedMemoryReadStreamSize == 0);
                Assert.IsTrue(segment.CachedMemoryWriteStreamSize == 0);

                segment.Dispose(DiskQueueSegmentDisposeBehaviour.Delete);
            }

            Assert.IsFalse(File.Exists(fileName));

            using (var segment = Create(1000, 10, 10, 1000, 1000))
            {
                fileName = segment.FileName;

                Assert.AreEqual(1000, segment.Capacity);
                Assert.AreEqual(0, segment.Count);
                Assert.IsFalse(segment.IsFull);
                Assert.IsFalse(segment.IsCompleted);
                Assert.IsTrue(File.Exists(segment.FileName));

                Assert.IsTrue(segment.ReadBufferSize == 10);
                Assert.IsTrue(segment.FlushToDiskOnItem == 10);
                Assert.IsTrue(segment.CachedMemoryReadStreamSize == 1000);
                Assert.IsTrue(segment.CachedMemoryWriteStreamSize == 1000);

                segment.Dispose(DiskQueueSegmentDisposeBehaviour.Delete);
            }

            Assert.IsFalse(File.Exists(fileName));
        }


        // =======================

        private void AddTakePeekTest(PersistentDiskQueueSegment<int> segment)
        {
            Assert.AreEqual(0, segment.Count);
            Assert.IsFalse(segment.IsFull);
            Assert.IsFalse(segment.IsCompleted);

            Assert.IsTrue(segment.TryAdd(10));
            Assert.AreEqual(1, segment.Count);

            int item = 0;
            Assert.IsTrue(segment.TryPeek(out item));
            Assert.AreEqual(10, item);
            Assert.AreEqual(1, segment.Count);

            item = 0;
            Assert.IsTrue(segment.TryTake(out item));
            Assert.AreEqual(10, item);
            Assert.AreEqual(0, segment.Count);


            for (int i = 0; i <= 10; i++)
            {
                if (i % 2 == 0)
                    segment.AddForced(i);
                else
                    Assert.IsTrue(segment.TryAdd(i));

                Assert.AreEqual(i + 1, segment.Count);
            }


            for (int i = 0; i <= 10; i++)
            {
                item = 0;
                Assert.IsTrue(segment.TryPeek(out item));
                Assert.AreEqual(i, item);

                item = 0;
                Assert.IsTrue(segment.TryTake(out item));
                Assert.AreEqual(i, item);
            }

            Assert.AreEqual(0, segment.Count);
        }


        [TestMethod]
        public void AddTakePeekTestNoBuffer() { RunTest(100, 0, 0, s => AddTakePeekTest(s)); }
        [TestMethod]
        public void AddTakePeekTestReadBuffer() { RunTest(100, 1, 16, s => AddTakePeekTest(s)); }
        [TestMethod]
        public void AddTakePeekTestSmallBuffer() { RunTest(100, 4, 2, s => AddTakePeekTest(s)); }
            
        // ====================

        private void AddTakeUpToTheLimitTest(PersistentDiskQueueSegment<int> segment)
        {
            int item = 0;
            while (segment.TryAdd(item++))
            {
                Assert.AreEqual(item, segment.Count);
            }
            item--;

            Assert.IsTrue(segment.IsFull);
            Assert.IsFalse(segment.IsCompleted);
            Assert.AreEqual(item, segment.Count);
            Assert.AreEqual(segment.Capacity, segment.Count);

            segment.AddForced(item++);
            Assert.IsTrue(segment.IsFull);
            Assert.IsFalse(segment.IsCompleted);
            Assert.AreEqual(item, segment.Count);
            Assert.AreEqual(segment.Capacity + 1, segment.Count);

            for (int i = 0; i < 100; i++)
            {
                int peekItem = 0;
                Assert.IsTrue(segment.TryPeek(out peekItem));
                Assert.AreEqual(0, peekItem);
            }

            for (int i = 0; i < item; i++)
            {
                int takeItem = 0;
                Assert.IsTrue(segment.TryTake(out takeItem));
                Assert.AreEqual(i, takeItem);
                if (i != item - 1)
                    Assert.IsFalse(segment.IsCompleted);
                else
                    Assert.IsTrue(segment.IsCompleted);
            }

            Assert.IsTrue(segment.IsFull);
            Assert.IsTrue(segment.IsCompleted);
            Assert.AreEqual(0, segment.Count);


            int tmp = 0;
            Assert.IsFalse(segment.TryAdd(0));
            Assert.IsFalse(segment.TryPeek(out tmp));
            Assert.IsFalse(segment.TryTake(out tmp));
        }


        [TestMethod]
        public void AddTakeUpToTheLimitTestNoBuffer() { RunTest(100, 0, 0, s => AddTakeUpToTheLimitTest(s)); }
        [TestMethod]
        public void AddTakeUpToTheLimitTestReadBuffer() { RunTest(100, 1, 16, s => AddTakeUpToTheLimitTest(s)); }
        [TestMethod]
        public void AddTakeUpToTheLimitTestSmallBuffer() { RunTest(100, 4, 2, s => AddTakeUpToTheLimitTest(s)); }


        // ===================

        private void PreserveOrderTest(PersistentDiskQueueSegment<int> segment, int elemCount)
        {
            Assert.IsTrue(elemCount <= segment.Capacity);

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
                        segment.AddForced(curElem);
                    else
                        Assert.IsTrue(segment.TryAdd(curElem));

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
                        int curItem = 0;
                        if (segment.TryTake(out curItem))
                            takenElems.Add(curItem);
                        if (rnd.Next(100) == 0)
                            Thread.Yield();
                        Thread.SpinWait(rnd.Next(100));
                    }
                }
                catch (OperationCanceledException) { }

                int item = 0;
                while (segment.TryTake(out item))
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
        public void PreserveOrderTestNoBuffer() { RunTest(100000, 0, 0, s => PreserveOrderTest(s, s.Capacity)); }
        [TestMethod]
        public void PreserveOrderTestReadBuffer() { RunTest(100000, 64, 16, s => PreserveOrderTest(s, s.Capacity)); }
        [TestMethod]
        public void PreserveOrderTestSmallBuffer() { RunTest(100000, 64, 2, s => PreserveOrderTest(s, s.Capacity)); }

        // =========================

    }
}
