using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Common
{
    [TestClass]
    public class EnumerableExtensionsTest
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
            var enumWrapper = new EnumerableWrapper<int>(data);

            for (int i = -1; i < 100; i++)
            {
                int expected = data.FindIndex(v => v == i);
                Assert.AreEqual(expected, (data as IEnumerable<int>).FindIndex(v => v == i));
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
    }
}
