using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
