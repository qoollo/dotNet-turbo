using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Collections
{
    [TestClass]
    public class CircularListTest
    {
        private static void AreEqual<T>(List<T> expected, CircularList<T> actual, string info)
        {
            Assert.AreEqual(expected.Count, actual.Count, "Count is not equal for: " + (info ?? ""));

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], actual[i], "Elements not equal at index " + i.ToString() + ", info: " + (info ?? ""));
            }

            using (var enumExpected = expected.GetEnumerator())
            using (var enumActual = actual.GetEnumerator())
            {
                while (enumExpected.MoveNext())
                {
                    Assert.IsTrue(enumActual.MoveNext(), "MoveNext finished on enumerator. Info: " + (info ?? ""));
                    Assert.AreEqual(enumExpected.Current, enumActual.Current, "Elements from enumerator not equal, info: " + (info ?? ""));
                }

                Assert.IsFalse(enumActual.MoveNext(), "MoveNext not finished on enumerator when expected. Info: " + (info ?? ""));
            }
        }

        //==================


        [TestMethod]
        public void CircularListCreates()
        {
            var testInst = new CircularList<int>();
            Assert.AreEqual(0, testInst.Count);
            Assert.AreEqual(0, testInst.Capacity);

            AreEqual(new List<int>(), new CircularList<int>(), "CircularListCreates");
        }

        [TestMethod]
        public void CircularListCreatesWithCapacity()
        {
            var testInst = new CircularList<int>(100);
            Assert.AreEqual(0, testInst.Count);
            Assert.AreEqual(100, testInst.Capacity);

            AreEqual(new List<int>(100), new CircularList<int>(100), "CircularListCreatesWithCapacity");
        }


        [TestMethod]
        public void CircularListCreatesFromSequence()
        {
            var seq = Enumerable.Range(0, 100);

            var testInst = new CircularList<int>(seq);
            Assert.AreEqual(100, testInst.Count);

            AreEqual(new List<int>(seq), new CircularList<int>(seq), "CircularListCreates");
        }


        [TestMethod]
        public void IndexationWorks()
        {
            var testInst = new CircularList<int>(Enumerable.Range(0, 100));

            for (int i = 0; i < testInst.Count; i++)
                Assert.AreEqual(i, testInst[i]);

            for (int i = 0; i < testInst.Count; i++)
                testInst[i] = -testInst[i];

            for (int i = 0; i < testInst.Count; i++)
                Assert.AreEqual(-i, testInst[i]);
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Indexation_IndexOutOfRange()
        {
            var testInst = new CircularList<int>(Enumerable.Range(0, 100));
            try
            {
                Console.WriteLine(testInst[-1]);
            }
            catch (Exception ex)
            {
                if (ex.IsCodeContractException())
                    throw new ArgumentOutOfRangeException();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Indexation_IndexOutOfRange2()
        {
            var testInst = new CircularList<int>(Enumerable.Range(0, 100));
            try
            {
                Console.WriteLine(testInst[100]);
            }
            catch (Exception ex)
            {
                if (ex.IsCodeContractException())
                    throw new ArgumentOutOfRangeException();
            }
        }


        [TestMethod]
        public void ContainsWorks()
        {
            var testInst = new CircularList<int>(Enumerable.Range(0, 100));

            for (int i = -100; i < 0; i++)
                Assert.IsFalse(testInst.Contains(i));

            for (int i = 0; i < 100; i++)
                Assert.IsTrue(testInst.Contains(i));

            for (int i = 100; i < 200; i++)
                Assert.IsFalse(testInst.Contains(i));
        }

        [TestMethod]
        public void ContainsWorksWithOffset()
        {
            var testInst = new CircularList<int>(500);
            for (int i = 50; i < 100; i++)
                testInst.AddLast(i);
            for (int i = 49; i >= 0; i--)
                testInst.AddFirst(i);


            for (int i = -100; i < 0; i++)
                Assert.IsFalse(testInst.Contains(i));

            for (int i = 0; i < 100; i++)
                Assert.IsTrue(testInst.Contains(i));

            for (int i = 100; i < 200; i++)
                Assert.IsFalse(testInst.Contains(i));
        }


        [TestMethod]
        public void ContainsForObjectsWorks()
        {
            var testInst = new CircularList<object>(Enumerable.Range(0, 100).Select(o => (object)o));

            for (int i = -100; i < 0; i++)
                Assert.IsFalse(testInst.Contains(i));

            for (int i = 0; i < 100; i++)
                Assert.IsTrue(testInst.Contains(i));

            for (int i = 100; i < 200; i++)
                Assert.IsFalse(testInst.Contains(i));
        }


        [TestMethod]
        public void ContainsForNullWorks()
        {
            var testInst = new CircularList<object>();
            testInst.Add(null);

            Assert.IsFalse(testInst.Contains(new object()));
            Assert.IsTrue(testInst.Contains(null));
        }



        [TestMethod]
        public void CopyToWorks()
        {
            var testInst = new CircularList<int>(500);
            for (int i = 50; i < 100; i++)
                testInst.AddLast(i);
            for (int i = 49; i >= 0; i--)
                testInst.AddFirst(i);


            int[] array = new int[200];
            testInst.CopyTo(array, 100);

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(0, array[i]);

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(i, array[i + 100]);
        }


        [TestMethod]
        public void ToArrayWorks()
        {
            var testInst = new CircularList<int>(500);
            for (int i = 1; i < 100; i++)
                testInst.AddLast(i);
            for (int i = 0; i >= 0; i--)
                testInst.AddFirst(i);

            var array = testInst.ToArray();
            Assert.AreEqual(100, array.Length);
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(i, array[i]);
        }


        [TestMethod]
        public void IndexOfWorks()
        {
            var testInst = new CircularList<int>(500);
            Assert.AreEqual(-1, testInst.IndexOf(1));

            for (int i = 50; i < 100; i++)
                testInst.AddLast(i);
            for (int i = 49; i >= 0; i--)
                testInst.AddFirst(i);
            for (int i = 0; i < 100; i++)
                testInst.Add(i);

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(i, testInst.IndexOf(i));

            Assert.AreEqual(-1, testInst.IndexOf(-1));
        }

        [TestMethod]
        public void IndexOfWorksOnObjects()
        {
            var testInst = new CircularList<object>(500);
            for (int i = 50; i < 100; i++)
                testInst.AddLast(i);
            for (int i = 49; i >= 0; i--)
                testInst.AddFirst(i);
            for (int i = 0; i < 100; i++)
                testInst.Add(i);

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(i, testInst.IndexOf(i));

            Assert.AreEqual(-1, testInst.IndexOf(null));

            testInst.AddFirst(null);
            Assert.AreEqual(0, testInst.IndexOf(null));
        }

        [TestMethod]
        public void IndexOfOnSubrangeWorks()
        {
            var testInst = new CircularList<int>(500);
            for (int i = 50; i < 100; i++)
                testInst.AddLast(i);
            for (int i = 49; i >= 0; i--)
                testInst.AddFirst(i);

            var cmpList = new List<int>(Enumerable.Range(0, 100));


            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(cmpList.IndexOf(i, Math.Max(0, i - 10), Math.Min(20, testInst.Count - i)), testInst.IndexOf(i, Math.Max(0, i - 10), Math.Min(20, testInst.Count - i)));
                Assert.AreEqual(i, testInst.IndexOf(i, Math.Max(0, i - 10), Math.Min(20, testInst.Count - i + 10)));
            }

            Assert.AreEqual(-1, testInst.IndexOf(50, 40, 10));
            Assert.AreEqual(-1, testInst.IndexOf(-1, 0, 99));
            Assert.AreEqual(-1, testInst.IndexOf(99, 0, 1));
        }



        [TestMethod]
        public void LastIndexOfWorks()
        {
            var testInst = new CircularList<int>(500);
            Assert.AreEqual(-1, testInst.LastIndexOf(100));

            for (int i = 50; i < 100; i++)
                testInst.AddLast(i);
            for (int i = 49; i >= 0; i--)
                testInst.AddFirst(i);

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(i, testInst.LastIndexOf(i));

            Assert.AreEqual(-1, testInst.LastIndexOf(-1));

            for (int i = 0; i < 100; i++)
                testInst.AddFirst(i);

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(i + 100, testInst.LastIndexOf(i));
        }


        [TestMethod]
        public void LastIndexOfWorksOnObjects()
        {
            var testInst = new CircularList<object>(500);
            for (int i = 50; i < 100; i++)
                testInst.AddLast(i);
            for (int i = 49; i >= 0; i--)
                testInst.AddFirst(i);

            for (int i = 0; i < 100; i++)
                Assert.AreEqual(i, testInst.LastIndexOf(i));

            Assert.AreEqual(-1, testInst.LastIndexOf(null));

            testInst.AddFirst(null);
            Assert.AreEqual(0, testInst.LastIndexOf(null));
        }



        [TestMethod]
        public void LastIndexOfOnSubrangeWorks()
        {
            var testInst = new CircularList<int>(500);
            for (int i = 50; i < 100; i++)
                testInst.AddLast(i);
            for (int i = 49; i >= 0; i--)
                testInst.AddFirst(i);

            var cmpList = new List<int>(Enumerable.Range(0, 100));


            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(cmpList.LastIndexOf(i, Math.Min(testInst.Count - 1, i + 10), Math.Min(20, i + 11)), testInst.LastIndexOf(i, Math.Min(testInst.Count - 1, i + 10), Math.Min(20, i + 11)));
                Assert.AreEqual(i, testInst.LastIndexOf(i, Math.Min(testInst.Count - 1, i + 10), Math.Min(20, i + 11)));
            }

            Assert.AreEqual(-1, testInst.LastIndexOf(50, 60, 10));
            Assert.AreEqual(-1, testInst.LastIndexOf(-1, 99, 100));
            Assert.AreEqual(-1, testInst.LastIndexOf(0, 99, 1));
        }


        [TestMethod]
        public void FindIndexWorks()
        {
            var testInst = new CircularList<int>(500);
            for (int i = 0; i < 500; i++)
                testInst.AddLast(i);
            for (int i = 0; i < 200; i++)
                testInst.RemoveFirst();
            for (int i = 0; i < 100; i++)
                testInst.AddLast(i);

            for (int i = 0; i < 100; i++)
            {
                int index = testInst.FindIndex(val => val == i);
                Assert.AreEqual(i + 300, index);
            }

            Assert.AreEqual(-1, testInst.FindIndex(val => false));
        }


        [TestMethod]
        public void FindLastIndexWorks()
        {
            var testInst = new CircularList<int>(500);
            for (int i = 0; i < 100; i++)
                testInst.AddLast(i);
            for (int i = 0; i < 100; i++)
                testInst.AddFirst(i);

            for (int i = 0; i < 100; i++)
            {
                int index = testInst.FindLastIndex(val => val == i);
                Assert.AreEqual(i + 100, index);
            }

            Assert.AreEqual(-1, testInst.FindLastIndex(val => false));
        }


        [TestMethod]
        public void FindWorks()
        {
            var testInst = new CircularList<int>(500);
            for (int i = 0; i < 100; i++)
                testInst.AddLast(i);
            for (int i = 0; i < 100; i++)
                testInst.AddFirst(i);

            for (int i = 0; i < 100; i++)
            {
                int value = testInst.Find(val => val == i);
                Assert.AreEqual(i, value);
            }


            Assert.AreEqual(default(int), testInst.Find(val => false));
        }


        [TestMethod]
        public void ExistsWorks()
        {
            var testInst = new CircularList<int>(500);
            for (int i = 0; i < 100; i++)
                testInst.AddLast(i);
            for (int i = 0; i < 100; i++)
                testInst.AddFirst(i);

            for (int i = 0; i < 100; i++)
                Assert.IsTrue(testInst.Exists(val => val == i));

            Assert.IsFalse(testInst.Exists(val => false));
        }


        [TestMethod]
        public void ForEachWorks()
        {
            var testInst = new CircularList<int>(500);
            for (int i = 0; i < 100; i++)
                testInst.AddLast(i);
            for (int i = 0; i < 100; i++)
                testInst.AddFirst(i);


            List<int> cmpLst = new List<int>();
            testInst.ForEach(val => cmpLst.Add(val));

            AreEqual(cmpLst, testInst, "ForEach");
        }


        [TestMethod]
        public void ClearWorks()
        {
            var testInst = new CircularList<int>(500);
            for (int i = 0; i < 100; i++)
                testInst.AddLast(i);
            for (int i = 0; i < 100; i++)
                testInst.AddFirst(i);

            Assert.AreEqual(200, testInst.Count);
            Assert.AreEqual(500, testInst.Capacity);


            testInst.Clear();
            Assert.AreEqual(0, testInst.Count);
            Assert.AreEqual(500, testInst.Capacity);

            testInst.TrimExcess();
            Assert.AreEqual(0, testInst.Count);
            Assert.AreEqual(0, testInst.Capacity);
        }



        [TestMethod]
        public void AddFirstWorks()
        {
            var testInst = new CircularList<int>();
            var cmpList = new List<int>();

            for (int i = 0; i < 100; i++)
            {
                testInst.AddFirst(i);
                cmpList.Insert(0, i);
                AreEqual(cmpList, testInst, "AddFirst");
            }

            for (int i = 0; i < 10; i++)
            {
                testInst.RemoveLast();
                cmpList.RemoveAt(cmpList.Count - 1);
                AreEqual(cmpList, testInst, "AddFirst.Remove");
            }


            for (int i = 0; i < 200; i++)
            {
                testInst.AddFirst(i);
                cmpList.Insert(0, i);
                AreEqual(cmpList, testInst, "AddFirst.Second");
            }
        }


        [TestMethod]
        public void AddLastWorks()
        {
            var testInst = new CircularList<int>();
            var cmpList = new List<int>();

            for (int i = 0; i < 100; i++)
            {
                testInst.AddLast(i);
                cmpList.Add(i);
                AreEqual(cmpList, testInst, "AddLast");
            }

            for (int i = 0; i < 50; i++)
            {
                testInst.RemoveFirst();
                cmpList.RemoveAt(0);
                AreEqual(cmpList, testInst, "AddLast.Remove");
            }


            for (int i = 0; i < 200; i++)
            {
                testInst.AddLast(i);
                cmpList.Add(i);
                AreEqual(cmpList, testInst, "AddLast.Second");
            }
        }


        [TestMethod]
        public void RemoveFirstLastWorks()
        {
            var testInst = new CircularList<int>();
            var cmpList = new List<int>();

            for (int i = 0; i < 100; i++)
            {
                testInst.AddFirst(i);
                testInst.AddLast(-i);
                cmpList.Insert(0, i);
                cmpList.Add(-i);
            }

            AreEqual(cmpList, testInst, "RemoveFirstLastWorks.Initial");


            for (int i = 99; i >= 0; i--)
            {
                Assert.AreEqual(i, testInst.RemoveFirst());
                Assert.AreEqual(-i, testInst.RemoveLast());

                cmpList.RemoveAt(0);
                cmpList.RemoveAt(cmpList.Count - 1);

                AreEqual(cmpList, testInst, "RemoveFirstLastWorks");
            }
        }



        [TestMethod]
        public void InsertWorks()
        {
            var testInst = new CircularList<int>();
            var cmpList = new List<int>();

            testInst.Insert(0, 1);
            cmpList.Insert(0, 1);
            AreEqual(cmpList, testInst, "InsertWorks.1");

            testInst.Insert(0, 2);
            cmpList.Insert(0, 2);
            AreEqual(cmpList, testInst, "InsertWorks.2");

            testInst.Insert(2, 3);
            cmpList.Insert(2, 3);
            AreEqual(cmpList, testInst, "InsertWorks.3");

            testInst.Insert(1, 4);
            cmpList.Insert(1, 4);
            AreEqual(cmpList, testInst, "InsertWorks.4");

            testInst.Insert(4, 5);
            cmpList.Insert(4, 5);
            AreEqual(cmpList, testInst, "InsertWorks.5");

            testInst.Insert(1, 6);
            cmpList.Insert(1, 6);
            AreEqual(cmpList, testInst, "InsertWorks.6");

            testInst.TrimExcess();
            cmpList.TrimExcess();

            testInst.Insert(6, 7);
            cmpList.Insert(6, 7);
            AreEqual(cmpList, testInst, "InsertWorks.7");
        }

        [TestMethod]
        public void InsertWorksRandomTest()
        {
            var testInst = new CircularList<int>();
            var cmpList = new List<int>();

            int seed = Environment.TickCount;
            Random rnd = new Random(seed);

            for (int i = 1; i < 200; i++)
            {
                int index = rnd.Next(0, testInst.Count + 1);
                testInst.Insert(index, i);
                cmpList.Insert(index, i);

                AreEqual(cmpList, testInst, "InsertWorksRandomTest. Seed = " + seed.ToString());
            }
        }



        [TestMethod]
        public void RemoveAtWorks()
        {
            var testInst = new CircularList<int>(Enumerable.Range(0, 10));
            var cmpList = new List<int>(Enumerable.Range(0, 10));

            testInst.RemoveAt(0);
            cmpList.RemoveAt(0);
            AreEqual(cmpList, testInst, "InsertWorks.1");

            testInst.RemoveAt(8);
            cmpList.RemoveAt(8);
            AreEqual(cmpList, testInst, "InsertWorks.2");

            testInst.RemoveAt(3);
            cmpList.RemoveAt(3);
            AreEqual(cmpList, testInst, "InsertWorks.3");


            testInst.Clear();
            cmpList.Clear();
            testInst.TrimExcess();


            testInst.AddFirst(10);
            cmpList.Insert(0, 10);

            testInst.RemoveAt(0);
            cmpList.RemoveAt(0);
            AreEqual(cmpList, testInst, "InsertWorks.4");


            testInst.AddLast(20);
            cmpList.Add(20);
            testInst.AddFirst(21);
            cmpList.Insert(0, 21);

            testInst.RemoveAt(1);
            cmpList.RemoveAt(1);
            AreEqual(cmpList, testInst, "InsertWorks.5");

            testInst.RemoveAt(0);
            cmpList.RemoveAt(0);
            AreEqual(cmpList, testInst, "InsertWorks.6");
        }


        [TestMethod]
        public void RemoveAtWorksRandomTest()
        {
            var testInst = new CircularList<int>();
            var cmpList = new List<int>();

            int seed = Environment.TickCount;
            Random rnd = new Random(seed);

            for (int i = 1; i < 200; i++)
            {
                if (rnd.Next(2) == 0)
                {
                    testInst.AddFirst(i);
                    cmpList.Insert(0, i);
                }
                else
                {
                    testInst.Add(i);
                    cmpList.Add(i);
                }               
            }

            AreEqual(cmpList, testInst, "RemoveAtWorksRandomTest.Prepare. Seed = " + seed.ToString());


            for (int i = 1; i < 200; i++)
            {
                int index = rnd.Next(0, testInst.Count);
                testInst.RemoveAt(index);
                cmpList.RemoveAt(index);

                AreEqual(cmpList, testInst, "RemoveAtWorksRandomTest. Seed = " + seed.ToString());
            }
        }


        [TestMethod]
        public void RemoveWorks()
        {
            var testInst = new CircularList<int>(Enumerable.Range(0, 200));
            var cmpList = new List<int>(Enumerable.Range(0, 200));


            for (int i = 0; i < 200; i++)
            {
                Assert.IsTrue(testInst.Remove(i));
                cmpList.Remove(i);

                AreEqual(cmpList, testInst, "RemoveWorks");
            }

            cmpList.Remove(10000);
            Assert.IsFalse(testInst.Remove(10000));
        }


        private void RunCompexTest(CircularList<int> testInst, List<int> cmpLst, int iterationCount)
        {
            Random rnd = new Random();

            for (int i = 0; i < iterationCount; i++)
            {
                bool add = testInst.Count < 200 || (testInst.Count < 2000 && rnd.Next(2) == 0);
                if (add)
                {
                    switch (rnd.Next(3))
                    {
                        case 0:
                            testInst.AddFirst(i);
                            cmpLst.Insert(0, i);
                            break;
                        case 1:
                            testInst.AddLast(i);
                            cmpLst.Add(i);
                            break;
                        case 2:
                            int index = rnd.Next(0, testInst.Count + 1);
                            testInst.Insert(index, i);
                            cmpLst.Insert(index, i);
                            break;
                    }
                }
                else
                {
                    switch (rnd.Next(3))
                    {
                        case 0:
                            testInst.RemoveFirst();
                            cmpLst.RemoveAt(0);
                            break;
                        case 1:
                            testInst.RemoveLast();
                            cmpLst.RemoveAt(cmpLst.Count - 1);
                            break;
                        case 2:
                            int index = rnd.Next(0, testInst.Count);
                            testInst.RemoveAt(index);
                            cmpLst.RemoveAt(index);
                            break;
                    }
                }
            }


            AreEqual(cmpLst, testInst, "ComplexTest");

            for (int i = 0; i < cmpLst.Count; i++)
            {
                Assert.AreEqual(cmpLst.IndexOf(cmpLst[i]), testInst.IndexOf(cmpLst[i]), "IndexOf");
                Assert.AreEqual(cmpLst.LastIndexOf(cmpLst[i]), testInst.LastIndexOf(cmpLst[i]), "LastIndexOf");
            }
        }


        [TestMethod]
        public void ComplexTest()
        {
            var testInst = new CircularList<int>();
            var cmpList = new List<int>();

            for (int i = 0; i < 100; i++)
                RunCompexTest(testInst, cmpList, 100);
        }
    }
}
