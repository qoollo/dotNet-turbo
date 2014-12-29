using Qoollo.Turbo.ObjectPools.ServiceStuff;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.ObjectPools
{
    [TestClass]
    public class UsedElementTrackerTest
    {
        [TestMethod]
        public void TestInitialPreset()
        {
            UsedElementTracker testInst = new UsedElementTracker(1000);
            Assert.AreEqual(int.MaxValue, testInst.MinFreeElementsCount);
            Assert.AreEqual(0, testInst.ElementToDestroy);
        }


        [TestMethod]
        public void TestMinFreeElementChanging()
        {
            UsedElementTracker testInst = new UsedElementTracker(1000);
            Assert.AreEqual(int.MaxValue, testInst.MinFreeElementsCount);

            testInst.UpdateMinFreeElementCount(100);
            Assert.AreEqual(100, testInst.MinFreeElementsCount);

            testInst.UpdateMinFreeElementCount(200);
            Assert.AreEqual(100, testInst.MinFreeElementsCount);

            testInst.UpdateMinFreeElementCount(1);
            Assert.AreEqual(1, testInst.MinFreeElementsCount);
        }


        [TestMethod]
        public void TestResetWorks()
        {
            UsedElementTracker testInst = new UsedElementTracker(1000);
            Assert.AreEqual(int.MaxValue, testInst.MinFreeElementsCount);

            testInst.UpdateMinFreeElementCount(100);
            Assert.AreEqual(100, testInst.MinFreeElementsCount);

            testInst.Reset();
            Assert.AreEqual(int.MaxValue, testInst.MinFreeElementsCount);
        }



        [TestMethod]
        public void TestItemFixationWorks()
        {
            UsedElementTracker testInst = new UsedElementTracker(10);
            Assert.AreEqual(int.MaxValue, testInst.MinFreeElementsCount);

            testInst.UpdateMinFreeElementCount(100);
            Assert.AreEqual(100, testInst.MinFreeElementsCount);

            Thread.Sleep(100);
            testInst.UpdateState();

            Assert.AreEqual(100, testInst.ElementToDestroy);
            Assert.AreEqual(int.MaxValue, testInst.MinFreeElementsCount);
        }


        [TestMethod]
        public void TestElementToDestroyDecrements()
        {
            UsedElementTracker testInst = new UsedElementTracker(10);
            testInst.UpdateMinFreeElementCount(100);
            Thread.Sleep(100);
            testInst.UpdateState();


            Assert.AreEqual(100, testInst.ElementToDestroy);
            Assert.AreEqual(int.MaxValue, testInst.MinFreeElementsCount);

            for (int i = 0; i < 100; i++)
            {
                Assert.IsTrue(testInst.RequestElementToDestroy());
                Assert.AreEqual(100 - i - 1, testInst.ElementToDestroy);
            }

            Assert.IsFalse(testInst.RequestElementToDestroy());
        }
    }
}
