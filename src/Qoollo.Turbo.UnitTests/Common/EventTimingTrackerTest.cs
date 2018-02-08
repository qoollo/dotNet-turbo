using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Common
{
    [TestClass]
    public class EventTimingTrackerTest : TestClassBase
    {
        [TestMethod]
        public void TestRegisterWork()
        {
            EventTimingTracker inst = new EventTimingTracker(10000);
            Assert.IsFalse(inst.IsEventRegistered);
            Assert.IsTrue(inst.IsPeriodPassed);

            bool needReact = inst.Register();
            Assert.IsTrue(needReact, "needReact");

            Assert.IsTrue(inst.IsEventRegistered);
            Assert.IsFalse(inst.IsPeriodPassed);
        }

        [TestMethod]
        public void TestRegisterReactions()
        {
            EventTimingTracker inst = new EventTimingTracker(150);
            Assert.IsFalse(inst.IsEventRegistered);
            Assert.IsTrue(inst.IsPeriodPassed);

            bool needReact = inst.Register();
            Assert.IsTrue(needReact, "needReact");

            Assert.IsTrue(inst.IsEventRegistered);
            Assert.IsFalse(inst.IsPeriodPassed);

            bool needReact2 = inst.Register();
            Assert.IsFalse(needReact2, "needReact2");

            Assert.IsTrue(inst.IsEventRegistered);
            Assert.IsFalse(inst.IsPeriodPassed);


            System.Threading.Thread.Sleep(300);

            Assert.IsTrue(inst.IsEventRegistered);
            Assert.IsTrue(inst.IsPeriodPassed);

            bool needReact3 = inst.Register();
            Assert.IsTrue(needReact3, "needReact3");

            Assert.IsTrue(inst.IsEventRegistered);
            Assert.IsFalse(inst.IsPeriodPassed);
        }


        [TestMethod]
        public void TestRegisterWithFirstTimeWork()
        {
            EventTimingTracker inst = new EventTimingTracker(10000);
            Assert.IsFalse(inst.IsEventRegistered);
            Assert.IsTrue(inst.IsPeriodPassed);

            bool isFirstTime = false;
            bool needReact = inst.Register(out isFirstTime);
            Assert.IsTrue(needReact, "needReact");
            Assert.IsTrue(isFirstTime, "isFirstTime");

            Assert.IsTrue(inst.IsEventRegistered);
            Assert.IsFalse(inst.IsPeriodPassed);
        }


        [TestMethod]
        public void TestRegisterWithFirstTimeReactions()
        {
            EventTimingTracker inst = new EventTimingTracker(150);
            Assert.IsFalse(inst.IsEventRegistered);
            Assert.IsTrue(inst.IsPeriodPassed);

            bool isFirstTime = false;
            bool needReact = inst.Register(out isFirstTime);
            Assert.IsTrue(needReact, "needReact");
            Assert.IsTrue(isFirstTime, "isFirstTime");

            Assert.IsTrue(inst.IsEventRegistered);
            Assert.IsFalse(inst.IsPeriodPassed);

            bool isFirstTime2 = false;
            bool needReact2 = inst.Register(out isFirstTime2);
            Assert.IsFalse(needReact2, "needReact2");
            Assert.IsFalse(isFirstTime2, "isFirstTime2");

            Assert.IsTrue(inst.IsEventRegistered);
            Assert.IsFalse(inst.IsPeriodPassed);


            System.Threading.Thread.Sleep(300);

            Assert.IsTrue(inst.IsEventRegistered);
            Assert.IsTrue(inst.IsPeriodPassed);

            bool isFirstTime3 = false;
            bool needReact3 = inst.Register(out isFirstTime3);
            Assert.IsTrue(needReact3, "needReact3");
            Assert.IsFalse(isFirstTime3, "isFirstTime3");

            Assert.IsTrue(inst.IsEventRegistered);
            Assert.IsFalse(inst.IsPeriodPassed);
        }


        [TestMethod]
        public void TestRegisterWithDelegateWork()
        {
            EventTimingTracker inst = new EventTimingTracker(10000);
            Assert.IsFalse(inst.IsEventRegistered);
            Assert.IsTrue(inst.IsPeriodPassed);

            bool wasCalled = false;
            bool isFirstTime = false;
            inst.Register(f =>
                {
                    wasCalled = true;
                    isFirstTime = f;
                });

            Assert.IsTrue(wasCalled, "wasCalled");
            Assert.IsTrue(isFirstTime, "isFirstTime");

            Assert.IsTrue(inst.IsEventRegistered);
            Assert.IsFalse(inst.IsPeriodPassed);
        }


        [TestMethod]
        public void TestRegisterWithDelegateReactions()
        {
            EventTimingTracker inst = new EventTimingTracker(150);
            Assert.IsFalse(inst.IsEventRegistered);
            Assert.IsTrue(inst.IsPeriodPassed);

            bool wasCalled = false;
            bool isFirstTime = false;
            inst.Register(f =>
            {
                wasCalled = true;
                isFirstTime = f;
            });

            Assert.IsTrue(wasCalled, "wasCalled");
            Assert.IsTrue(isFirstTime, "isFirstTime");


            Assert.IsTrue(inst.IsEventRegistered);
            Assert.IsFalse(inst.IsPeriodPassed);

            wasCalled = false;
            isFirstTime = false;
            inst.Register(f =>
            {
                wasCalled = true;
                isFirstTime = f;
            });

            Assert.IsFalse(wasCalled, "wasCalled2");
            Assert.IsFalse(isFirstTime, "isFirstTime2");

            Assert.IsTrue(inst.IsEventRegistered);
            Assert.IsFalse(inst.IsPeriodPassed);


            System.Threading.Thread.Sleep(300);

            Assert.IsTrue(inst.IsEventRegistered);
            Assert.IsTrue(inst.IsPeriodPassed);

            wasCalled = false;
            isFirstTime = false;
            inst.Register(f =>
            {
                wasCalled = true;
                isFirstTime = f;
            });

            Assert.IsTrue(wasCalled, "wasCalled3");
            Assert.IsFalse(isFirstTime, "isFirstTime3");

            Assert.IsTrue(inst.IsEventRegistered);
            Assert.IsFalse(inst.IsPeriodPassed);
        }

        [TestMethod]
        public void TestResetWork()
        {
            EventTimingTracker inst = new EventTimingTracker(150);
            Assert.IsFalse(inst.IsEventRegistered);
            Assert.IsTrue(inst.IsPeriodPassed);

            inst.Reset();
            Assert.IsFalse(inst.IsEventRegistered);
            Assert.IsTrue(inst.IsPeriodPassed);

            inst.Register();
            Assert.IsTrue(inst.IsEventRegistered);
            Assert.IsFalse(inst.IsPeriodPassed);


            inst.Reset();
            Assert.IsFalse(inst.IsEventRegistered);
            Assert.IsTrue(inst.IsPeriodPassed);
        }
    }
}
