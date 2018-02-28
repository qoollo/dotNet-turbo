using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Queues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Queues
{
    [TestClass]
    public class TransformationQueueTests : TestClassBase
    {
        private class TypeConverter : ITransformationQueueConverter<int, long>
        {
            public int ConvertNum = 0;
            public int ConvertBackNum = 0;

            public long Convert(int item)
            {
                Interlocked.Increment(ref ConvertNum);
                return item;
            }

            public int ConvertBack(long item)
            {
                Interlocked.Increment(ref ConvertBackNum);
                return (int)item;
            }
        }



        // =============


        [TestMethod]
        public void AddTakeSequentialTest()
        {
            const int ItemsCount = 50000;

            var memQueue = new MemoryQueue<long>();
            var converter = new TypeConverter();

            using (var queue = new TransformationQueue<int, long>(memQueue, converter))
            using (var barrier = new Barrier(2))
            {
                List<int> bag = new List<int>();

                var task1 = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    for (int val = 0; val < ItemsCount; val++)
                    {
                        if (val % 10 == 0)
                            queue.Add(val);
                        else if (val % 10 == 1)
                            Assert.IsTrue(queue.TryAdd(val));
                        else if (val % 10 == 2)
                            Assert.IsTrue(queue.TryAdd(val, 1000));
                        else if (val % 10 == 3)
                            Assert.IsTrue(queue.TryAdd(val, TimeSpan.FromSeconds(1)));
                        else if (val % 10 == 4)
                            Assert.IsTrue(queue.TryAdd(val, 1000, new CancellationToken()));
                        else if(val % 10 == 5)
                            queue.AddForced(val);
                        else
                            queue.Add(val, new CancellationToken());

                        Thread.SpinWait(val % 100);
                    }
                });

                var task2 = Task.Run(() =>
                {
                    barrier.SignalAndWait();
                    for (int val = 0; val < ItemsCount; val++)
                    {
                        int res = 0;
                        if (val % 10 == 0)
                            Assert.IsTrue(queue.TryTake(out res, 10000), "Value was expected 1");
                        else if (val % 10 == 1)
                            Assert.IsTrue(queue.TryTake(out res, 10000, new CancellationToken()), "Value was expected 2");
                        else
                            Assert.IsTrue(queue.TryTake(out res, TimeSpan.FromSeconds(10)), "Value was expected 3");

                        bag.Add(res);
                        Thread.SpinWait((val + 37) % 100);
                    }
                });

                Task.WaitAll(task1, task2);

                Assert.AreEqual(0, queue.Count);
                Assert.AreEqual(ItemsCount, bag.Count);

                for (int i = 0; i < bag.Count; i++)
                    Assert.AreEqual(i, bag[i], "i != bag[i]");

                Assert.AreEqual(0, memQueue.Count);
                Assert.AreEqual(ItemsCount, converter.ConvertNum);
                Assert.AreEqual(ItemsCount, converter.ConvertBackNum);
            }
        }



        [TestMethod]
        public void WorksThroughIQueue()
        {
            var memQueue = new MemoryQueue<long>();
            var converter = new TypeConverter();
            using (var queue = (IQueue<int>)new TransformationQueue<int, long>(memQueue, converter))
            {
                Assert.AreEqual(0, queue.Count);
                Assert.IsTrue(queue.TryAdd(1, 10000, CancellationToken.None));
                Assert.AreEqual(1, queue.Count);
                Assert.AreEqual(queue.Count, memQueue.Count);
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

                Assert.AreEqual(queue.Count, memQueue.Count);

                Assert.AreEqual(2, Volatile.Read(ref converter.ConvertNum));
                Assert.AreEqual(3, Volatile.Read(ref converter.ConvertBackNum));
            }
        }
    }
}
