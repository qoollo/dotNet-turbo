using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Common
{
    [TestClass]
    public class EnumerableExtensionsTest : TestClassBase
    {
        public class EnumerableWrapper<T>: IEnumerable<T>
        {
            private List<T> _list;
            public EnumerableWrapper(List<T> list) { _list = list; }

            public IEnumerator<T> GetEnumerator()
            {
                return _list.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _list.GetEnumerator();
            }
        }

        public class IntWrapper: IComparable<IntWrapper>
        {
            public readonly int Data;
            public IntWrapper(int data)
            {
                Data = data;
            }

            public int CompareTo(IntWrapper other)
            {
                if (object.ReferenceEquals(other, null)) return -1;
                return Data.CompareTo(other.Data);
            }
        }

        public class ReverseIntComparer: IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return y.CompareTo(x);
            }
        }

        // ==============

        [TestMethod]
        public void TestFindIndex()
        {
            List<int> data = new List<int>(Enumerable.Range(0, 100));
            var arrayData = data.ToArray();
            var enumWrapper = new EnumerableWrapper<int>(data);

            for (int i = -1; i < 100; i++)
            {
                int expected = data.FindIndex(v => v == i);
                Assert.AreEqual(expected, (data as IEnumerable<int>).FindIndex(v => v == i));
                Assert.AreEqual(expected, (arrayData as IEnumerable<int>).FindIndex(v => v == i));
                Assert.AreEqual(expected, enumWrapper.FindIndex(v => v == i));
            }
        }


        [TestMethod]
        public void TestMax()
        {
            List<int> data = new List<int>(Enumerable.Range(0, 100));
            Assert.AreEqual(99, data.Max());
            Assert.AreEqual(99, data.Max((IComparer<int>)null));
            Assert.AreEqual(0, data.Max(new ReverseIntComparer()));
        }

        [TestMethod]
        public void TestMin()
        {
            List<int> data = new List<int>(Enumerable.Range(0, 100));
            Assert.AreEqual(0, data.Min());
            Assert.AreEqual(0, data.Min((IComparer<int>)null));
            Assert.AreEqual(99, data.Min(new ReverseIntComparer()));
        }

        [TestMethod]
        public void TestMaxBy()
        {
            List<IntWrapper> data = new List<IntWrapper>(Enumerable.Range(0, 100).Select(o => new IntWrapper(o)));
            var expected = data[99];
            var expectedMin = data[0];
            Assert.AreEqual(expected, data.MaxBy(v => v.Data));
            Assert.AreEqual(expected, data.MaxBy(v => v.Data, null));
            Assert.AreEqual(expectedMin, data.MaxBy(v => v.Data, new ReverseIntComparer()));
        }

        [TestMethod]
        public void TestMinBy()
        {
            List<IntWrapper> data = new List<IntWrapper>(Enumerable.Range(0, 100).Select(o => new IntWrapper(o)));
            var expected = data[0];
            var expectedMax = data[99];
            Assert.AreEqual(expected, data.MinBy(v => v.Data));
            Assert.AreEqual(expected, data.MinBy(v => v.Data, null));
            Assert.AreEqual(expectedMax, data.MinBy(v => v.Data, new ReverseIntComparer()));
        }
    }
}
