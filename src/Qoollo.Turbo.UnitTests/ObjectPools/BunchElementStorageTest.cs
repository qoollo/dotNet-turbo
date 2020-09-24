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
using Qoollo.Turbo.Threading.ServiceStuff;

namespace Qoollo.Turbo.UnitTests.ObjectPools
{
    [TestClass]
    public class BunchElementStorageTest : TestClassBase
    {
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
        public void TestAddTakeSingleThreadCase()
        {
            SparceArrayStorage<PoolElementWrapper<int>> storage = CreateSparceArray(100);
            BunchElementStorage<int> testInst = new BunchElementStorage<int>(storage);

            try
            {
                for (int i = 0; i < storage.Count; i++)
                    testInst.Add(storage.RawData[i]);


                for (int i = storage.Count - 1; i >= 0; i--)
                {
                    PoolElementWrapper<int> elem = null;
                    testInst.Take(out elem);
                    Assert.AreEqual(i, elem.Element);
                }
            }
            finally
            {
                DestroySparceArray(storage);
            }
        }

        [TestMethod]
        public void TestAddTakeCommonCase()
        {
            SparceArrayStorage<PoolElementWrapper<int>> storage = CreateSparceArray(100);
            BunchElementStorage<int> testInst = new BunchElementStorage<int>(storage);

            try
            {
                for (int i = 0; i < storage.Count; i++)
                    testInst.Add(storage.RawData[i]);

                List<int> takenElems = new List<int>();

                for (int i = 0; i < storage.Count; i++)
                {
                    PoolElementWrapper<int> elem = null;
                    testInst.Take(out elem);
                    takenElems.Add(elem.Element);
                }

                Assert.AreEqual(100, takenElems.Count);
                takenElems.Sort();
                for (int i = 0; i < takenElems.Count; i++)
                {
                    Assert.AreEqual(i, takenElems[i]);
                }
            }
            finally
            {
                DestroySparceArray(storage);
            }
        }



        private void RunComplexTest(SparceArrayStorage<PoolElementWrapper<int>> storage, BunchElementStorage<int> bunchStorage, int elemCount, int thCount)
        {
            int atomicRandom = 0;

            int trackElemCount = elemCount;
            int addFinished = 0;
            

            Thread[] threadsTake = new Thread[thCount];
            Thread[] threadsAdd = new Thread[thCount];

            CancellationTokenSource tokSrc = new CancellationTokenSource();

            int elementCountInBunchStorage = 0;
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
                    bunchStorage.Add(elem);
                    Interlocked.Increment(ref elementCountInBunchStorage);
                    Interlocked.Increment(ref addedCountArray[storage.IndexOf(elem)]);

                    int sleepTime = rnd.Next(12);
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
                        int elementCountInBunchStorageLoc = Volatile.Read(ref elementCountInBunchStorage);
                        if (elementCountInBunchStorageLoc > 0 &&
                            Interlocked.CompareExchange(ref elementCountInBunchStorage, elementCountInBunchStorageLoc - 1, elementCountInBunchStorageLoc) == elementCountInBunchStorageLoc)
                        {
                            PoolElementWrapper<int> tmp = null;
                            bunchStorage.Take(out tmp);
                            freeElements.Add(tmp);
                            Interlocked.Increment(ref takenCountArray[storage.IndexOf(tmp)]);
                        }

                        int sleepTime = rnd.Next(12);
                        if (sleepTime > 0)
                            SpinWaitHelper.SpinWait(sleepTime);
                    }
                }
                catch (OperationCanceledException) { }

                int elementCountInBunchStorageLoc2 = Volatile.Read(ref elementCountInBunchStorage);
                while (elementCountInBunchStorageLoc2 > 0)
                {
                    if (Interlocked.CompareExchange(ref elementCountInBunchStorage, elementCountInBunchStorageLoc2 - 1, elementCountInBunchStorageLoc2) == elementCountInBunchStorageLoc2)
                    {
                        PoolElementWrapper<int> tmp = null;
                        bunchStorage.Take(out tmp);
                        freeElements.Add(tmp);
                        Interlocked.Increment(ref takenCountArray[storage.IndexOf(tmp)]);
                    }

                    elementCountInBunchStorageLoc2 = Volatile.Read(ref elementCountInBunchStorage);
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
            Assert.AreEqual(0, elementCountInBunchStorage);

            for (int i = 0; i < storage.Count; i++)
            {
                Assert.IsTrue(addedCountArray[i] == takenCountArray[i], "Added count != taken count");
            }
        }


        [TestMethod]
        public void TestConcurrentAddTake()
        {
            SparceArrayStorage<PoolElementWrapper<int>> storage = CreateSparceArray(1000);
            BunchElementStorage<int> testInst = new BunchElementStorage<int>(storage);

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

