using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Queues.DiskQueueComponents;
using Qoollo.Turbo.Threading.ServiceStuff;
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
    public class PersistentDiskQueueSegmentTest : TestClassBase
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
            string fileName = Guid.NewGuid().ToString().Replace('-', '_') + PersistentDiskQueueSegmentFactory<int>.SegmentFileExtension;

            try
            {
                return new PersistentDiskQueueSegment<int>(1, fileName, new ItemSerializer(), capacity, flushPeriod, cachedMemoryWriteStreamSize, readBufferSize, cachedMemoryReadStreamSize);
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

        private void SegmentFactoryTest(bool fix)
        {
            string prefix = Guid.NewGuid().ToString().Replace('-', '_');
            var factory = new PersistentDiskQueueSegmentFactory<int>(100, prefix, new ItemSerializer(), fix, 1000, 10000, 10, 10000);

            string fileName = null;
            string fileName2 = null;
            try
            {
                using (var segment = (PersistentDiskQueueSegment<int>)factory.CreateSegment(".", 100))
                {
                    fileName = segment.FileName;
                    Assert.IsTrue(File.Exists(fileName));

                    for (int i = 0; i < 100; i++)
                        segment.TryAdd(i);
                }
                using (var segment = (PersistentDiskQueueSegment<int>)factory.CreateSegment(".", 101))
                {
                    fileName2 = segment.FileName;
                    Assert.IsTrue(File.Exists(fileName2));

                    for (int i = 0; i < 100; i++)
                        segment.TryAdd(i);
                }

                Assert.IsTrue(File.Exists(fileName));
                Assert.IsTrue(File.Exists(fileName2));

                var discoveryResult = factory.DiscoverSegments(".");
                Assert.AreEqual(2, discoveryResult.Length);

                using (var segment = discoveryResult[0])
                {
                    for (int i = 0; i < 100; i++)
                    {
                        int item = 0;
                        Assert.IsTrue(segment.TryTake(out item));
                        Assert.AreEqual(i, item);
                    }
                }
                using (var segment = discoveryResult[1])
                {
                    for (int i = 0; i < 100; i++)
                    {
                        int item = 0;
                        Assert.IsTrue(segment.TryTake(out item));
                        Assert.AreEqual(i, item);
                    }
                }
            }
            finally
            {
                if (fileName != null && File.Exists(fileName))
                    File.Delete(fileName);
                if (fileName2 != null && File.Exists(fileName2))
                    File.Delete(fileName2);
            }
        }

        [TestMethod]
        public void SegmentFactoryTestWithFix() { SegmentFactoryTest(true); }
        [TestMethod]
        public void SegmentFactoryTestWithoutFix() { SegmentFactoryTest(false); }

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

        
        private void RunWriteAbort(PersistentDiskQueueSegment<int> segment, int count, Random rnd)
        {
            Barrier bar = new Barrier(2);
            Exception observedEx = null;
            int added = 0;
            ThreadStart act = () =>
            {
                bar.SignalAndWait();
                int item = 1;
                try
                {
                    while (segment.TryAdd(item++)) { Interlocked.Increment(ref added); }
                }
                catch (Exception ex)
                {
                    if (!(ex is ThreadAbortException))
                        Volatile.Write(ref observedEx, ex);
                }
                Interlocked.Add(ref added, count);
            };

            Thread th = new Thread(act);
            th.Start();
            bar.SignalAndWait();

            while (Volatile.Read(ref added) < count)
                SpinWaitHelper.SpinWait(20);

            SpinWaitHelper.SpinWait(rnd.Next(4000));
            th.Abort();
            th.Join();

            if (observedEx != null)
                throw observedEx;
        }
        private void RunReadAbort(PersistentDiskQueueSegment<int> segment, int count, Random rnd)
        {
            Barrier bar = new Barrier(2);
            Exception observedEx = null;
            int taken = 0;
            ThreadStart act = () =>
            {
                bar.SignalAndWait();
                int item = 1;
                try
                {
                    while (segment.TryTake(out item)) { Interlocked.Increment(ref taken); }
                }
                catch (Exception ex)
                {
                    if (!(ex is ThreadAbortException))
                        Volatile.Write(ref observedEx, ex);
                }
                Interlocked.Add(ref taken, count);
            };

            Thread th = new Thread(act);
            th.Start();
            bar.SignalAndWait();

            while (Volatile.Read(ref taken) < count)
                SpinWaitHelper.SpinWait(20);
            SpinWaitHelper.SpinWait(rnd.Next(4000));
            th.Abort();
            th.Join();

            if (observedEx != null)
                throw observedEx;
        }


        private void WriteAbortTestCore(int itemCount)
        {
            string segmentFileName = null;
            try
            {
                Random rnd = new Random();
                using (var segment = Create(10000, 1000, 16))
                {
                    segmentFileName = segment.FileName;
                    RunWriteAbort(segment, itemCount, rnd);
                }

                int readCnt = 0;

                using (var segment = PersistentDiskQueueSegment<int>.Open(100, segmentFileName, new ItemSerializer(), true))
                {
                    Assert.IsTrue(segment.Count >= itemCount, "Item missed");

                    int item = 0;
                    while (segment.TryTake(out item))
                        readCnt++;
                }

                Assert.IsTrue(readCnt >= itemCount, "Item missed");
            }
            finally
            {
                if (segmentFileName != null && File.Exists(segmentFileName))
                {
                    try
                    {
                        File.Delete(segmentFileName);
                    }
                    catch { }
                }
            }
        }


#if NET45 || NET46 || NET462
        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void WriteAbortTest()
        {
            for (int i = 0; i < 150; i++)
                WriteAbortTestCore(100);
        }
#endif


        private void ReadAbortTestCore(int itemCount)
        {
            string segmentFileName = null;
            try
            {
                Random rnd = new Random();
                using (var segment = Create(10000, 1000, 16))
                {
                    segmentFileName = segment.FileName;
                    for (int i = 0; i < itemCount; i++)
                        Assert.IsTrue(segment.TryAdd(i));
                }


                while (true)
                {
                    using (var segment = PersistentDiskQueueSegment<int>.Open(100, segmentFileName, new ItemSerializer(), true))
                    {
                        if (segment.IsCompleted)
                            break;
                        RunReadAbort(segment, Math.Max(1, itemCount / 10), rnd);
                    }
                }
            }
            finally
            {
                if (segmentFileName != null && File.Exists(segmentFileName))
                {
                    try
                    {
                        File.Delete(segmentFileName);
                    }
                    catch { }
                }
            }
        }



#if NET45 || NET46 || NET462
        [TestMethod]
        [Timeout(2 * 60 * 1000)]
        public void ReadAbortTest()
        {
            for (int i = 0; i < 150; i++)
                ReadAbortTestCore(100);
        }
#endif


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
                    SpinWaitHelper.SpinWait(rnd.Next(12));

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
                        SpinWaitHelper.SpinWait(rnd.Next(12));
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
        public void PreserveOrderTestReadBuffer() { RunTest(100000, 600, 16, s => PreserveOrderTest(s, s.Capacity)); }
        [TestMethod]
        public void PreserveOrderTestSmallBuffer() { RunTest(100000, 600, 2, s => PreserveOrderTest(s, s.Capacity)); }

        // =========================

        private void RunComplexTest(PersistentDiskQueueSegment<int> segment, int elemCount, int thCount)
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


                    int sleepTime = rnd.Next(12);

                    int tmpItem = 0;
                    if (segment.TryPeek(out tmpItem) && tmpItem == item)
                        sleepTime += 12;

                    if (sleepTime > 0)
                        SpinWaitHelper.SpinWait(sleepTime);
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

                        int sleepTime = rnd.Next(12);
                        if (sleepTime > 0)
                            SpinWaitHelper.SpinWait(sleepTime);
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
            RunTest(100000, 600, 16, s => RunComplexTest(s, s.Capacity, Math.Max(1, Environment.ProcessorCount / 2)));
            RunTest(100000, 600, 16, s => RunComplexTest(s, s.Capacity, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }
        [TestMethod]
        public void ComplexTestSmallBuffer()
        {
            RunTest(100000, 1200, 2, s => RunComplexTest(s, s.Capacity, Math.Max(1, Environment.ProcessorCount / 2)));
            RunTest(100000, 1200, 2, s => RunComplexTest(s, s.Capacity, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }

    }
}
