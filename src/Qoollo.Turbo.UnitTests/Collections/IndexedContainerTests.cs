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
    public class IndexedContainerTests : TestClassBase
    {
        [TestMethod]
        public void TestGetSetCreate()
        {
            var testInst = new IndexedContainer<int>(10);
            Assert.AreEqual(10, testInst.Capacity);

            testInst.SetItem(5, 100);
            Assert.AreEqual(100, testInst.GetItem(5));

            for (int i = 0; i < 100; i++)
            {
                if (i == 5)
                    Assert.AreEqual(100, testInst.GetItem(i));
                else
                    Assert.AreEqual(default(int), testInst.GetItem(i));
            }
        }


        [TestMethod]
        public void TestResize()
        {
            var testInst = new IndexedContainer<int>(10);
            Assert.AreEqual(10, testInst.Capacity);

            testInst.SetItem(100, 100);
            Assert.IsTrue(testInst.Capacity > 100);
            Assert.AreEqual(100, testInst.GetItem(100));

            for (int i = 0; i < 200; i++)
            {
                if (i == 100)
                    Assert.AreEqual(100, testInst.GetItem(i));
                else
                    Assert.AreEqual(default(int), testInst.GetItem(i));
            }
        }


        [TestMethod]
        public void TestGetSetCreateRef()
        {
            var testObj = new object();

            var testInst = new IndexedContainerRef<object>(10);
            Assert.AreEqual(10, testInst.Capacity);

            testInst.SetItem(5, testObj);
            Assert.AreEqual(testObj, testInst.GetItem(5));

            for (int i = 0; i < 100; i++)
            {
                if (i == 5)
                {                 
                    Assert.AreEqual(testObj, testInst.GetItemOrDefault(i));
                    object tmp = null;
                    Assert.IsTrue(testInst.TryGetItem(i, out tmp));
                    Assert.AreEqual(testObj, tmp);
                }
                else
                {
                    Assert.AreEqual(null, testInst.GetItemOrDefault(i));
                    object tmp = null;
                    Assert.IsFalse(testInst.TryGetItem(i, out tmp));
                }
            }
        }


        [TestMethod]
        public void TestResizeRef()
        {
            var testObj = new object();

            var testInst = new IndexedContainerRef<object>(10);
            Assert.AreEqual(10, testInst.Capacity);

            testInst.SetItem(100, testObj);
            Assert.IsTrue(testInst.Capacity > 100);
            Assert.AreEqual(testObj, testInst.GetItem(100));

            for (int i = 0; i < 200; i++)
            {
                if (i == 100)
                    Assert.AreEqual(testObj, testInst.GetItemOrDefault(i));
                else
                    Assert.AreEqual(null, testInst.GetItemOrDefault(i));
            }
        }

        [TestMethod]
        public void TestEnumerationRef()
        {
            var testInst = new IndexedContainerRef<object>();
            testInst.SetItem(10, 10);
            testInst.SetItem(20, 20);

            Assert.IsTrue(testInst.SequenceEqual(new object[] { 10, 20 }));

            foreach (var item in testInst.EnumerateWithKeys())
            {
                Assert.AreEqual(item.Key, item.Value);
            }
        }
    }
}
