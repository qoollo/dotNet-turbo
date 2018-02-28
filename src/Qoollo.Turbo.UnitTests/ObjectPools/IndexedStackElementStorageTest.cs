using Qoollo.Turbo.ObjectPools.Common;
using Qoollo.Turbo.ObjectPools.ServiceStuff;
using Qoollo.Turbo.ObjectPools.ServiceStuff.ElementCollections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.ObjectPools
{
    [TestClass]
    public class IndexedStackElementStorageTest : TestClassBase
    {
        [ClassInitialize]
        public static void Init(TestContext context)
        {
            SubscribeToUnhandledExceptions(context, true);
        }
        [ClassCleanup]
        public static void Cleanup()
        {
            UnsubscribeFromUnhandledExceptions();
        }


        //=============================

        private class PoolElementOperationSource<T> : IPoolElementOperationSource<T>
        {
            public bool IsValid(PoolElementWrapper<T> container)
            {
                return container != null;
            }
            public int GetPriority(PoolElementWrapper<T> container)
            {
                return 0;
            }
        }

        // =================


        private SparceArrayStorage<PoolElementWrapper<int>> CreateSparceArray(int elemCount)
        {
            SparceArrayStorage<PoolElementWrapper<int>> storage = new SparceArrayStorage<PoolElementWrapper<int>>();
            PoolElementOperationSource<int> operations = new PoolElementOperationSource<int>();
            for (int i = 0; i < 100; i++)
            {
                storage.Add(new PoolElementWrapper<int>(i, operations, storage)
                    {
                        ThisIndex = i
                    });
            }

            return storage;
        }

        private void DestroySparceArray(SparceArrayStorage<PoolElementWrapper<int>> storage)
        {
            var rawArray = storage.RawData;
            for (int i = 0; i < rawArray.Length; i++)
            {
                if (rawArray[i] != null)
                    rawArray[i].MarkElementDestroyed();
            }
        }

        [TestMethod]
        public void TestAddTake()
        {
            SparceArrayStorage<PoolElementWrapper<int>> storage = CreateSparceArray(100);
            IndexedStackElementStorage<int> testInst = new IndexedStackElementStorage<int>(storage);

            try
            {
                Assert.IsTrue(testInst.HeadIndex < 0);

                for (int i = 0; i < storage.Count; i++)
                    testInst.Add(storage.RawData[i]);

                Assert.IsTrue(testInst.HeadIndex == 99);

                for (int i = storage.Count - 1; i >= 0; i--)
                {
                    PoolElementWrapper<int> elem = null;
                    bool takeResult = testInst.TryTake(out elem);
                    Assert.IsTrue(takeResult);
                    Assert.AreEqual(i, elem.Element);
                }

                PoolElementWrapper<int> elem2 = null;
                Assert.IsFalse(testInst.TryTake(out elem2));
            }
            finally
            {
                DestroySparceArray(storage);
            }
        }






        private void RunComplexTest(SparceArrayStorage<PoolElementWrapper<int>> storage, IndexedStackElementStorage<int> stack, int elemCount, int thCount)
        {
            int atomicRandom = 0;

            int trackElemCount = elemCount;
            int addFinished = 0;

            Thread[] threadsTake = new Thread[thCount];
            Thread[] threadsAdd = new Thread[thCount];

            CancellationTokenSource tokSrc = new CancellationTokenSource();

            BlockingCollection<PoolElementWrapper<int>> freeElements = new BlockingCollection<PoolElementWrapper<int>>();
            foreach (var elem in storage.RawData)
            {
                if (elem != null)
                    freeElements.Add(elem);
            }

            int[] addedCountArray = new int[storage.Count];
            int[] takenCountArray = new int[storage.Count];


            Action addAction = () =>
            {
                Random rnd = new Random(Environment.TickCount + Interlocked.Increment(ref atomicRandom) * thCount * 2);

                while (true)
                {
                    int item = Interlocked.Decrement(ref trackElemCount);
                    if (item < 0)
                        break;

                    var elem = freeElements.Take();
                    stack.Add(elem);
                    Interlocked.Increment(ref addedCountArray[storage.IndexOf(elem)]);

                    int sleepTime = rnd.Next(100);
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
                        PoolElementWrapper<int> tmp = null;
                        if (stack.TryTake(out tmp))
                        {
                            freeElements.Add(tmp);
                            Interlocked.Increment(ref takenCountArray[storage.IndexOf(tmp)]);
                        }

                        int sleepTime = rnd.Next(100);
                        if (sleepTime > 0)
                            Thread.SpinWait(sleepTime);
                    }
                }
                catch (OperationCanceledException) { }

                PoolElementWrapper<int> tmp2 = null;
                while (stack.TryTake(out tmp2))
                {
                    freeElements.Add(tmp2);
                    Interlocked.Increment(ref takenCountArray[storage.IndexOf(tmp2)]);
                }
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


            Assert.AreEqual(storage.Count, freeElements.Count);
            Assert.IsTrue(stack.HeadIndex < 0);
            
            for (int i = 0; i < storage.Count; i++)
            {
                Assert.IsTrue(addedCountArray[i] == takenCountArray[i], "Added count != taken count");
            }
        }


        [TestMethod]
        public void TestConcurrentAddTake()
        {
            SparceArrayStorage<PoolElementWrapper<int>> storage = CreateSparceArray(1000);
            IndexedStackElementStorage<int> testInst = new IndexedStackElementStorage<int>(storage);

            try
            {
                RunComplexTest(storage, testInst, 5000000, 1 + Environment.ProcessorCount / 2);
            }
            finally
            {
                DestroySparceArray(storage);
            }
        }

    }
}
