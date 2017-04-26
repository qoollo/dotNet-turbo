using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Queues.DiskQueueComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace Qoollo.Turbo.UnitTests.Queues
{
    [TestClass]
    public class NonPersistentDiskQueueSegmentTest
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


        private static NonPersistentDiskQueueSegment<int> Create(int capacity, int writeBufferSize, int readBufferSize, int cachedMemoryWriteStreamSize = -1, int cachedMemoryReadStreamSize = -1)
        {
            string fileName = Guid.NewGuid().ToString().Replace('-', '_') + NonPersistentDiskQueueSegmentFactory<int>.SegmentFileExtension;

            try
            {
                return new NonPersistentDiskQueueSegment<int>(1, fileName, new ItemSerializer(), capacity, writeBufferSize, cachedMemoryWriteStreamSize, readBufferSize, cachedMemoryReadStreamSize);
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

        private static void RunTest(int capacity, int writeBufferSize, int readBufferSize, Action<NonPersistentDiskQueueSegment<int>> runner)
        {
            string fileName = null;
            try
            {
                using (var segment = Create(capacity, writeBufferSize, readBufferSize))
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
                Assert.IsTrue(segment.WriteBufferSize > 0);
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
                Assert.IsTrue(segment.WriteBufferSize == 0);
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
                Assert.IsTrue(segment.WriteBufferSize == 10);
                Assert.IsTrue(segment.CachedMemoryReadStreamSize == 1000);
                Assert.IsTrue(segment.CachedMemoryWriteStreamSize == 1000);

                segment.Dispose(DiskQueueSegmentDisposeBehaviour.Delete);
            }

            Assert.IsFalse(File.Exists(fileName));
        }
        

        // =======================

        [TestMethod]
        public void SegmentFactoryTest()
        {
            string prefix = Guid.NewGuid().ToString().Replace('-', '_');
            var factory = new NonPersistentDiskQueueSegmentFactory<int>(100, prefix, new ItemSerializer(), 10, 10000, 10, 10000);

            string fileName = null;
            string fileName2 = null;
            try
            {
                using (var segment = (NonPersistentDiskQueueSegment<int>)factory.CreateSegment(".", 100))
                {
                    fileName = segment.FileName;
                    Assert.IsTrue(File.Exists(fileName));
                }
                using (var segment = (NonPersistentDiskQueueSegment<int>)factory.CreateSegment(".", 101))
                {
                    fileName2 = segment.FileName;
                    Assert.IsTrue(File.Exists(fileName2));
                }

                Assert.IsTrue(File.Exists(fileName));
                Assert.IsTrue(File.Exists(fileName2));

                var discoveryResult = factory.DiscoverSegments(".");
                Assert.AreEqual(0, discoveryResult.Length);

                Assert.IsFalse(File.Exists(fileName));
                Assert.IsFalse(File.Exists(fileName2));
            }
            finally
            {
                if (fileName != null && File.Exists(fileName))
                    File.Delete(fileName);
                if (fileName2 != null && File.Exists(fileName2))
                    File.Delete(fileName2);
            }
        }

        // =======================

        private void AddTakePeekTest(NonPersistentDiskQueueSegment<int> segment)
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
        public void AddTakePeekTestReadBuffer() { RunTest(100, 0, 16, s => AddTakePeekTest(s)); }
        [TestMethod]
        public void AddTakePeekTestWriteBuffer() { RunTest(100, 16, 0, s => AddTakePeekTest(s)); }
        [TestMethod]
        public void AddTakePeekTestReadWriteBuffer() { RunTest(100, 16, 16, s => AddTakePeekTest(s)); }
        [TestMethod]
        public void AddTakePeekTestSmallBuffer() { RunTest(100, 2, 2, s => AddTakePeekTest(s)); }


        // ====================

        private void AddTakeUpToTheLimitTest(NonPersistentDiskQueueSegment<int> segment)
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
        public void AddTakeUpToTheLimitTestReadBuffer() { RunTest(100, 0, 16, s => AddTakeUpToTheLimitTest(s)); }
        [TestMethod]
        public void AddTakeUpToTheLimitTestWriteBuffer() { RunTest(100, 16, 0, s => AddTakeUpToTheLimitTest(s)); }
        [TestMethod]
        public void AddTakeUpToTheLimitTestReadWriteBuffer() { RunTest(100, 16, 16, s => AddTakeUpToTheLimitTest(s)); }
        [TestMethod]
        public void AddTakeUpToTheLimitTestSmallBuffer() { RunTest(100, 2, 2, s => AddTakeUpToTheLimitTest(s)); }


        // ====================


        private void PreserveOrderTest(NonPersistentDiskQueueSegment<int> segment, int elemCount)
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
        public void PreserveOrderTestReadBuffer() { RunTest(100000, 0, 16, s => PreserveOrderTest(s, s.Capacity)); }
        [TestMethod]
        public void PreserveOrderTestWriteBuffer() { RunTest(100000, 16, 0, s => PreserveOrderTest(s, s.Capacity)); }
        [TestMethod]
        public void PreserveOrderTestReadWriteBuffer() { RunTest(100000, 16, 16, s => PreserveOrderTest(s, s.Capacity)); }
        [TestMethod]
        public void PreserveOrderTestSmallBuffer() { RunTest(100000, 2, 2, s => PreserveOrderTest(s, s.Capacity)); }

        // =========================

        private void RunComplexTest(NonPersistentDiskQueueSegment<int> segment, int elemCount, int thCount)
        {
            Assert.IsTrue(elemCount <= segment.Capacity);

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
                        segment.AddForced(item);
                    else
                        Assert.IsTrue(segment.TryAdd(item));


                    int sleepTime = rnd.Next(100);

                    int tmpItem = 0;
                    if (segment.TryPeek(out tmpItem) && tmpItem == item)
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
                        if (segment.TryTake(out tmp))
                            data.Add((int)tmp);

                        int sleepTime = rnd.Next(100);
                        if (sleepTime > 0)
                            Thread.SpinWait(sleepTime);
                    }
                }
                catch (OperationCanceledException) { }

                int tmp2;
                while (segment.TryTake(out tmp2))
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
        public void ComplexTestNoBuffer()
        {
            RunTest(100000, 0, 0, s => RunComplexTest(s, s.Capacity, Math.Max(1, Environment.ProcessorCount / 2)));
            RunTest(100000, 0, 0, s => RunComplexTest(s, s.Capacity, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }
        [TestMethod]
        public void ComplexTestReadBuffer()
        {
            RunTest(100000, 0, 16, s => RunComplexTest(s, s.Capacity, Math.Max(1, Environment.ProcessorCount / 2)));
            RunTest(100000, 0, 16, s => RunComplexTest(s, s.Capacity, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }
        [TestMethod]
        public void ComplexTestWriteBuffer()
        {
            RunTest(100000, 16, 0, s => RunComplexTest(s, s.Capacity, Math.Max(1, Environment.ProcessorCount / 2)));
            RunTest(100000, 16, 0, s => RunComplexTest(s, s.Capacity, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }
        [TestMethod]
        public void ComplexTestReadWriteBuffer()
        {
            RunTest(100000, 16, 16, s => RunComplexTest(s, s.Capacity, Math.Max(1, Environment.ProcessorCount / 2)));
            RunTest(100000, 16, 16, s => RunComplexTest(s, s.Capacity, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }
        [TestMethod]
        public void ComplexTestSmallBuffer()
        {
            RunTest(100000, 2, 2, s => RunComplexTest(s, s.Capacity, Math.Max(1, Environment.ProcessorCount / 2)));
            RunTest(100000, 2, 2, s => RunComplexTest(s, s.Capacity, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }

    }
}
