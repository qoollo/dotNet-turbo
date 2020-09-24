using Qoollo.Turbo.ObjectPools.ServiceStuff.ElementCollections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Qoollo.Turbo.Threading.ServiceStuff;

namespace Qoollo.Turbo.UnitTests.ObjectPools
{
    [TestClass]
    public class SparceArrayStorageTest : TestClassBase
    {
        [TestMethod]
        public void TestAdd()
        {
            SparceArrayStorage<object> testInst = new SparceArrayStorage<object>();

            for (int i = 0; i < 100; i++)
            {
                int index = testInst.Add(i);
                Assert.AreEqual(i, index);
                Assert.AreEqual(i, (int)testInst.GetItem(index));
            }

            Assert.AreEqual(100, testInst.Count);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, (int)testInst.RawData[i]);
            }
        }

        [TestMethod]
        public void TestIndexOf()
        {
            SparceArrayStorage<object> testInst = new SparceArrayStorage<object>();
            List<object> data = new List<object>();

            for (int i = 0; i < 100; i++)
            {
                data.Add(i);
                testInst.Add(data[i]);
            }

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(i, testInst.IndexOf(data[i]));
            }

            Assert.AreEqual(-1, testInst.IndexOf(-1));
        }


        [TestMethod]
        public void TestRemoveAt()
        {
            SparceArrayStorage<object> testInst = new SparceArrayStorage<object>();

            for (int i = 0; i < 100; i++)
                testInst.Add(i);

            Assert.AreEqual(100, testInst.Count);

            bool removeResult = testInst.RemoveAt(50);
            Assert.IsTrue(removeResult);
            Assert.IsNull(testInst.GetItem(50));

            Assert.AreEqual(99, testInst.Count);


            for (int i = 0; i < 100; i++)
            {
                removeResult = testInst.RemoveAt(i);
                if (i != 50)
                {
                    Assert.IsTrue(removeResult);
                    Assert.IsNull(testInst.GetItem(i));
                }
            }

            for (int i = 0; i < 100; i++)
            {
                removeResult = testInst.RemoveAt(i);
                Assert.IsFalse(removeResult);
            }
        }

        [TestMethod]
        public void TestRemove()
        {
            SparceArrayStorage<object> testInst = new SparceArrayStorage<object>();
            List<object> data = new List<object>();

            for (int i = 0; i < 100; i++)
            {
                data.Add(i);
                testInst.Add(data[i]);
            }

            Assert.AreEqual(100, testInst.Count);

            for (int i = 0; i < 100; i++)
            {
                bool removeResult = testInst.Remove(data[i]);
                Assert.IsTrue(removeResult);
                Assert.IsNull(testInst.GetItem(i));
            }

            for (int i = 0; i < 100; i++)
            {
                bool removeResult = testInst.Remove(data[i]);
                Assert.IsFalse(removeResult);
            }
        }


        [TestMethod]
        public void TestAddToFreeSpace()
        {
            SparceArrayStorage<object> testInst = new SparceArrayStorage<object>();

            for (int i = 0; i < 100; i++)
                testInst.Add(i);

            Assert.AreEqual(100, testInst.Count);

            testInst.RemoveAt(50);

            int newIndex = testInst.Add(-50);
            Assert.AreEqual(50, newIndex);
        }


        [TestMethod]
        public void TestNoIndexChange()
        {
            SparceArrayStorage<object> testInst = new SparceArrayStorage<object>();

            for (int i = 0; i < 100; i++)
                testInst.Add(i);

            Random rnd = new Random();

            for (int i = 0; i < 1000; i++)
            {
                if (rnd.Next(2) == 0)
                    testInst.Add(-rnd.Next());
                else
                    testInst.RemoveAt(rnd.Next(50));
            }

            for (int i = 0; i < 50; i++)
                testInst.RemoveAt(rnd.Next(50));

            for (int i = 0; i < 50; i++)
                testInst.Add(-rnd.Next());

            for (int i = 50; i < 100; i++)
                Assert.AreEqual(i, (int)testInst.RawData[i]);
        }


        [TestMethod]
        public void TestNoIndexChangeWithPartialCompaction()
        {
            SparceArrayStorage<object> testInst = new SparceArrayStorage<object>();

            for (int i = 0; i < 100; i++)
                testInst.Add(i);

            Random rnd = new Random();

            for (int i = 0; i < 1000; i++)
            {
                if (rnd.Next(2) == 0)
                    testInst.Add(-rnd.Next());
                else
                    testInst.RemoveAt(rnd.Next(50));
            }

            for (int i = 0; i < 50; i++)
                testInst.Add(-rnd.Next());

            for (int i = 100; i < testInst.Capacity; i++)
            {
                int elemIndex = i;
                testInst.CompactElementAt(ref elemIndex);
            }

            for (int i = 50; i < 100; i++)
                Assert.AreEqual(i, (int)testInst.RawData[i]);
        }


        [TestMethod]
        public void TestGlobalCompactionWorks()
        {
            SparceArrayStorage<object> testInst = new SparceArrayStorage<object>(true);

            for (int i = 0; i < 100; i++)
                testInst.Add(i);

            Assert.IsTrue(testInst.Capacity >= 100);


            List<int> removedElements = new List<int>();

            for (int i = 0; i < 50; i++)
            {
                removedElements.Add((int)testInst.GetItem(i));
                testInst.RemoveAt(i);
            }

            Assert.IsTrue(testInst.Capacity < 100);

            List<int> elements = new List<int>(testInst.RawData.Where(o => o != null).Cast<int>());
            Assert.AreEqual(50, elements.Count);

            elements.AddRange(removedElements);
            elements.Sort();
     
            for (int i = 0; i < elements.Count; i++)
                Assert.AreEqual(i, elements[i]);
        }


        [TestMethod]
        public void TestPerElementCompactionWorks()
        {
            SparceArrayStorage<object> testInst = new SparceArrayStorage<object>();

            for (int i = 0; i < 100; i++)
                testInst.Add(i);

            for (int i = 0; i < 50; i++)
                testInst.RemoveAt(i);

            Assert.IsTrue(testInst.Capacity >= 100);

            int targetIndex = 0;
            for (int i = 60; i < 100; i++)
            {
                int index = i;
                testInst.CompactElementAt(ref index);
                if (index != i)
                {
                    Assert.AreEqual(targetIndex, index);
                    targetIndex++;
                }
            }

            Assert.IsTrue(targetIndex > 0);
            Assert.IsTrue(testInst.Capacity < 100);

            for (int i = 50; i < 60; i++)
            {
                Assert.AreEqual(i, (int)testInst.GetItem(i));
            }
        }



        private void TestHeavyRandomAddRemoveCore(int operationCount, int threadCount, int spinCount, bool doAutoCompaction)
        {
            SparceArrayStorage<object> testInst = new SparceArrayStorage<object>(doAutoCompaction);
            List<object> activeElements = new List<object>();
            int randomSeed = 0;
            int operationPerThread = operationCount / threadCount;
            Thread[] threads = new Thread[threadCount];

            Action act = () =>
                {
                    Random rnd = new Random(Interlocked.Increment(ref randomSeed) + Environment.TickCount);
                    List<object> localElements = new List<object>();

                    for (int i = 0; i < operationPerThread; i++)
                    {
                        int operationId = doAutoCompaction ? rnd.Next(2) : rnd.Next(3);

                        if (operationId == 0)
                        {
                            object obj = new object();
                            localElements.Add(obj);
                            testInst.Add(obj);
                        }
                        else if (operationId == 1)
                        {
                            if (localElements.Count > 0)
                            {
                                object obj = localElements[rnd.Next(localElements.Count)];
                                localElements.Remove(obj);
                                testInst.Remove(obj);
                            }
                        }
                        else
                        {
                            if (localElements.Count > 0)
                            {
                                object obj = localElements[rnd.Next(localElements.Count)];
                                int compactionIndex = testInst.IndexOf(obj);
                                testInst.CompactElementAt(ref compactionIndex);
                            }
                        }

                        int mySpinCount = spinCount / 2 + rnd.Next(spinCount / 2);
                        SpinWaitHelper.SpinWait(mySpinCount);
                    }

                    lock (activeElements)
                    {
                        activeElements.AddRange(localElements);
                    }
                };


            for (int i = 0; i < threads.Length; i++)
                threads[i] = new Thread(new ThreadStart(act));

            for (int i = 0; i < threads.Length; i++)
                threads[i].Start();

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();

            Assert.AreEqual(activeElements.Count, testInst.Count, "activeElements.Count != testInst.Count");

            List<object> elementsFromTestInst = new List<object>();
            for (int i = 0; i < testInst.RawData.Length; i++)
                if (testInst.RawData[i] != null)
                    elementsFromTestInst.Add(testInst.RawData[i]);


            Assert.AreEqual(elementsFromTestInst.Count, testInst.Count, "elementsFromTestInst.Count != testInst.Count");

            foreach (var elem in activeElements)
                Assert.IsTrue(elementsFromTestInst.Contains(elem), "!elementsFromTestInst.Contains(elem)");
        }

        [TestMethod]
        public void TestHeavyRandomAddRemove()
        {
            TestHeavyRandomAddRemoveCore(1000000, Environment.ProcessorCount, 4, true);
            TestHeavyRandomAddRemoveCore(500000, Environment.ProcessorCount * 2, 4, true);
            TestHeavyRandomAddRemoveCore(1000000, Environment.ProcessorCount, 3, false);
        }
    }
}
