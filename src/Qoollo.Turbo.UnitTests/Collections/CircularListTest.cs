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
    }
}
