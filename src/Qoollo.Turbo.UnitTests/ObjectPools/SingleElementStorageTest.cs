using Qoollo.Turbo.ObjectPools.ServiceStuff.ElementCollections;
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
    public class SingleElementStorageTest
    {
        [TestMethod]
        public void TestCreation()
        {
            SingleElementStorage<object> testInst = new SingleElementStorage<object>();
            Assert.IsTrue(testInst.IsUnowned);
            Assert.IsFalse(testInst.HasElement);
        }

        [TestMethod]
        public void TestAddTake()
        {
            SingleElementStorage<object> testInst = new SingleElementStorage<object>(Thread.CurrentThread);
            Assert.AreEqual(Thread.CurrentThread, testInst.OwnerThread);
            Assert.IsFalse(testInst.IsUnowned);
            Assert.IsFalse(testInst.HasElement);

            object tmp = null;
            bool noElemTakeResult = testInst.TryTake(out tmp);
            Assert.IsFalse(noElemTakeResult);

         
            bool addResult = testInst.TryAdd(1);
            Assert.IsTrue(addResult);
            Assert.IsTrue(testInst.HasElement);

            bool hasElemAddResult = testInst.TryAdd(2);
            Assert.IsFalse(hasElemAddResult);


            bool takeResult = testInst.TryTake(out tmp);
            Assert.IsTrue(takeResult);
            Assert.AreEqual(1, (int)tmp);
            Assert.IsFalse(testInst.HasElement);
        }


        [TestMethod]
        public void TestUnownedProperty()
        {
            SingleElementStorage<object> testInst = null;

            Thread th = new Thread(() =>
            {
                testInst = new SingleElementStorage<object>(Thread.CurrentThread);
            });

            th.Start();
            th.Join();

            Assert.IsNotNull(testInst);
            Assert.IsTrue(testInst.IsUnowned);
        }
    }
}
