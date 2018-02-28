using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Common
{
    [TestClass]
    public class WeakDelegateTests : TestClassBase
    {
        class TargetClass
        {
            public int CallCount = 0;
            public void TestMethod()
            {
                System.Threading.Interlocked.Increment(ref CallCount);
            }
        }

        [TestMethod]
        public void TestEmptyDelegateNotFail()
        {
            MulticastWeakDelegate<Action> testInst = new MulticastWeakDelegate<Action>();
            var d = testInst.GetDelegate();
            Assert.IsNull(d);
        }

        [TestMethod]
        public void TestCallStrong()
        {
            int callCount = 0;

            MulticastWeakDelegate<Action> testInst = new MulticastWeakDelegate<Action>();
            testInst.Add(() => callCount++);

            var d = testInst.GetDelegate();
            Assert.IsNotNull(d);
            d();

            Assert.AreEqual(1, callCount);
        }
        [TestMethod]
        public void TestCallStrongTwice()
        {
            int callCount = 0;

            MulticastWeakDelegate<Action> testInst = new MulticastWeakDelegate<Action>();
            testInst.Add(() => callCount++);
            testInst.Add(() => callCount++);

            var d = testInst.GetDelegate();
            Assert.IsNotNull(d);
            d();

            Assert.AreEqual(2, callCount);
        }


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void Subscribe(MulticastWeakDelegate<Action> weakDeleg)
        {
            TargetClass target = new TargetClass();
            weakDeleg.Add(new Action(target.TestMethod));
        }

        [TestMethod]
        public void TestWeakRef()
        {
            MulticastWeakDelegate<Action> testInst = new MulticastWeakDelegate<Action>();
            Subscribe(testInst);

            for (int i = 0; i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            var d = testInst.GetDelegate();
            Assert.IsNull(d);
        }


        [TestMethod]
        public void TestUnsubscribeWorks()
        {
            TargetClass target = new TargetClass();
            MulticastWeakDelegate<Action> testInst = new MulticastWeakDelegate<Action>();

            testInst.Add(target.TestMethod);
            testInst.GetDelegate()();
            Assert.AreEqual(1, target.CallCount);

            testInst.Remove(target.TestMethod);
            var d = testInst.GetDelegate();
            Assert.IsNull(d);
        }

        [TestMethod]
        public void TestDoubleSubscribeRemove()
        {
            TargetClass target = new TargetClass();
            MulticastWeakDelegate<Action> testInst = new MulticastWeakDelegate<Action>();

            testInst.Add(target.TestMethod);
            testInst.Add(target.TestMethod);
            testInst.Remove(target.TestMethod);
            testInst.GetDelegate()();
            Assert.AreEqual(1, target.CallCount);
        }
    }
}
