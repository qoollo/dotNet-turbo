using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Queues;
using Qoollo.Turbo.Queues.DiskQueueComponents;

namespace Qoollo.Turbo.UnitTests.Queues
{
    [TestClass]
    public class QueueCombinationTest : TestClassBase
    {
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

        private class TypeConverter : ITransformationQueueConverter<long, int>
        {
            public int Convert(long item)
            {
                return (int)item;
            }

            public long ConvertBack(int item)
            {
                Thread.SpinWait(50);
                return item;
            }
        }

        private static IQueue<long> CreateQueue1(string dir)
        {
            DiskQueue<int> disk = new DiskQueue<int>(dir, new NonPersistentDiskQueueSegmentFactory<int>(10000, "prefix", new ItemSerializer()), 100, true);
            MemoryQueue<int> preDisk = new MemoryQueue<int>(5000);
            LevelingQueue<int> diskWrap = new LevelingQueue<int>(preDisk, disk, LevelingQueueAddingMode.PreserveOrder, true);
            TransformationQueue<long, int> transform = new TransformationQueue<long, int>(diskWrap, new TypeConverter());
            MemoryQueue<long> topMem = new MemoryQueue<long>(1000);
            LevelingQueue<long> topQ = new LevelingQueue<long>(topMem, transform, LevelingQueueAddingMode.PreserveOrder, true);

            return topQ;
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

        private static void RunTest<T>(Func<string, IQueue<T>> constructor, Action<IQueue<T>> testRunner)
        {
            string dir = Guid.NewGuid().ToString().Replace('-', '_');
            try
            {
                Directory.CreateDirectory(dir);
                using (var q = constructor(dir))
                    testRunner(q);
            }
            finally
            {
                DeleteDir(dir);
            }
        }


        // ============================

        private void PreserveOrderTest(IQueue<long> queue, int elemCount)
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
                        Assert.IsTrue(queue.TryAdd(curElem, -1, default(CancellationToken)));

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
                        long itemX = 0;
                        Assert.IsTrue(queue.TryTake(out itemX, -1, cancelled.Token));
                        takenElems.Add((int)itemX);

                        if (rnd.Next(100) == 0)
                            Thread.Yield();
                        Thread.SpinWait(rnd.Next(400));
                    }
                }
                catch (OperationCanceledException) { }

                long item = 0;
                while (queue.TryTake(out item, 0, default(CancellationToken)))
                    takenElems.Add((int)item);
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
        public void PreserveOrederTest1()
        {
            for (int i = 0; i < 10; i++)
                RunTest(CreateQueue1, q => PreserveOrderTest(q, 1000000));
        }

        // ============================


        private void RunComplexTest(IQueue<long> q, int elemCount, int addSpin, int takeSpin, int thCount)
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
                        Assert.IsTrue(q.TryAdd(item, -1, default(CancellationToken)));


                    int sleepTime = rnd.Next(addSpin);

                    long tmpItem = 0;
                    if (q.TryPeek(out tmpItem, 0, default(CancellationToken)) && tmpItem % 1000 == 0)
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
                        long tmp = 0;
                        if (q.TryTake(out tmp, -1, tokSrc.Token))
                            data.Add((int)tmp);

                        int sleepTime = rnd.Next(takeSpin);
                        if (sleepTime > 0)
                            Thread.SpinWait(sleepTime);
                    }
                }
                catch (OperationCanceledException) { }

                long tmp2;
                while (q.TryTake(out tmp2, 0, default(CancellationToken)))
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
        public void ComplexTest1()
        {
            RunTest(CreateQueue1, q => RunComplexTest(q, 1000000, 100, 100, Math.Max(1, Environment.ProcessorCount / 2)));
            RunTest(CreateQueue1, q => RunComplexTest(q, 1000000, 100, 1000, Math.Max(1, Environment.ProcessorCount / 2) + 2));
            RunTest(CreateQueue1, q => RunComplexTest(q, 1000000, 1000, 100, Math.Max(1, Environment.ProcessorCount / 2)));
            RunTest(CreateQueue1, q => RunComplexTest(q, 1000000, 100, 200, Math.Max(1, Environment.ProcessorCount / 2) + 2));
        }
    }
}
