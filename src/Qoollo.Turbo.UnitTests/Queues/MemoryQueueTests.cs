using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Queues;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Queues
{
    [TestClass]
    public class MemoryQueueTests
    {
        [TestMethod]
        public void AddTakeMultithreadTest()
        {
            const int ItemsCount = 10000;

            using (var queue = new MemoryQueue<int>())
            using (var barrier = new Barrier(2))
            {
                ConcurrentBag<int> bag = new ConcurrentBag<int>();

                var task1 = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    Parallel.For(0, ItemsCount, val =>
                    {
                        queue.Add(val);
                        Thread.SpinWait(val % 100);
                    });
                });

                var task2 = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    Parallel.For(0, 10000, val =>
                    {
                        int res = 0;
                        if (!queue.TryTake(out res, 10000))
                            Assert.Fail("Value was expected in MemoryQueue");
                        bag.Add(res);
                        Thread.SpinWait((val + 37) % 100);
                    });
                });

                Task.WaitAll(task1, task2);

                Assert.AreEqual(0, queue.Count);
                Assert.AreEqual(ItemsCount, bag.Count);

                var array = bag.ToArray();
                Array.Sort(array);
                for (int i = 0; i < array.Length; i++)
                    Assert.AreEqual(i, array[i], "i != array[i]");
            }
        }

        [TestMethod]
        public void AddTakeSequentialTest()
        {
            const int ItemsCount = 10000;

            using (var queue = new MemoryQueue<int>())
            using (var barrier = new Barrier(2))
            {
                List<int> bag = new List<int>();

                var task1 = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    for (int val = 0; val < ItemsCount; val++)
                    {
                        queue.Add(val);
                        Thread.SpinWait(val % 100);
                    }
                });

                var task2 = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    for (int val = 0; val < ItemsCount; val++)
                    {
                        int res = 0;
                        if (!queue.TryTake(out res, 10000))
                            Assert.Fail("Value was expected in MemoryQueue");
                        bag.Add(res);
                        Thread.SpinWait((val + 37) % 100);
                    }
                });

                Task.WaitAll(task1, task2);

                Assert.AreEqual(0, queue.Count);
                Assert.AreEqual(ItemsCount, bag.Count);

                for (int i = 0; i < bag.Count; i++)
                    Assert.AreEqual(i, bag[i], "i != bag[i]");
            }
        }


        [TestMethod]
        public void WorksThroughIQueue()
        {
            using (var queue = (IQueue<int>)new MemoryQueue<int>())
            {
                Assert.AreEqual(0, queue.Count);
                Assert.IsTrue(queue.TryAdd(1, 10000, CancellationToken.None));
                Assert.AreEqual(1, queue.Count);
                queue.AddForced(2);
                Assert.AreEqual(2, queue.Count);

                int peekItem = 0;
                Assert.IsTrue(queue.TryPeek(out peekItem, 10000, CancellationToken.None));
                Assert.AreEqual(1, peekItem);

                int takeItem = 0;
                Assert.IsTrue(queue.TryTake(out takeItem, 10000, CancellationToken.None));
                Assert.AreEqual(1, takeItem);
                Assert.AreEqual(1, queue.Count);
                Assert.IsTrue(queue.TryTake(out takeItem, 10000, CancellationToken.None));
                Assert.AreEqual(2, takeItem);
                Assert.AreEqual(0, queue.Count);

                Assert.IsFalse(queue.TryTake(out takeItem, 0, CancellationToken.None));
                Assert.AreEqual(0, queue.Count);
            }
        }
    }
}
