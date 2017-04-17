using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Queues.DiskQueueComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

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
                return reader.ReadInt32();
            }

            public void Serialize(BinaryWriter writer, int item)
            {
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


            for (int i = 1; i <= 10; i++)
            {
                if (i % 2 == 0)
                    segment.AddForced(i);
                else
                    Assert.IsTrue(segment.TryAdd(i));

                Assert.AreEqual(i, segment.Count);
            }


            for (int i = 1; i <= 10; i++)
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
    }
}
