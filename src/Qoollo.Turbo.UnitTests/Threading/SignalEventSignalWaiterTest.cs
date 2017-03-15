using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Threading
{
    [TestClass]
    public class SignalEventSignalWaiterTest
    {
        [TestMethod]
        public void TestSignalReceived()
        {
            using (var ev = new SignalEvent())
            {
                int waiterEntered = 0;
                int waitResult = 0;
                object waiterRef = null;

                var task = Task.Run(() =>
                {
                    using (var waiter = ev.Factory.CreateWaiter())
                    {
                        Interlocked.Exchange(ref waiterRef, waiter);
                        lock (waiter)
                        {
                            Interlocked.Increment(ref waiterEntered);
                            if (waiter.Wait(60000))
                                Interlocked.Exchange(ref waitResult, 1);
                            else
                                Interlocked.Exchange(ref waitResult, 2);
                        }
                    }
                });


                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref waiterEntered));
                lock (waiterRef)
                {
                    ev.Signal();
                }

                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref waitResult));


                task.Wait();
            }
        }


        [TestMethod]
        public void TestWaitingWithPredicate()
        {
            using (var ev = new SignalEvent())
            {
                int waiterEntered = 0;
                int waitResult = 0;
                int waitCondition = 0;
                int condEvaluated = 0;
                object waiterRef = null;

                var task = Task.Run(() =>
                {
                    using (var waiter = ev.Factory.CreateWaiter())
                    {
                        Interlocked.Exchange(ref waiterRef, waiter);
                        lock (waiter)
                        {
                            Interlocked.Increment(ref waiterEntered);
                            if (waiter.Wait(s => { Interlocked.Increment(ref condEvaluated); return Volatile.Read(ref waitCondition) > 0; }, (object)null, 60000))
                                Interlocked.Exchange(ref waitResult, 1);
                            else
                                Interlocked.Exchange(ref waitResult, 2);
                        }
                    }
                });


                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref waiterEntered));
                lock (waiterRef)
                {
                    ev.Signal();
                }
                TimingAssert.AreEqual(10000, 2, () => Volatile.Read(ref condEvaluated));
                lock (waiterRef)
                {
                    Assert.AreEqual(0, Volatile.Read(ref waitResult));
                }
                lock (waiterRef)
                {
                    Interlocked.Increment(ref waitCondition);
                    ev.Signal();
                }
                TimingAssert.AreEqual(10000, 3, () => Volatile.Read(ref condEvaluated));
                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref waitResult));


                task.Wait();
            }
        }


        [TestMethod]
        public void TestWaitingMultipleEvents()
        {
            using (var ev = new SignalEvent())
            using (var ev2 = new SignalEvent())
            using (var ev3 = new SignalEvent())
            {
                int waiterEntered = 0;
                int waitResult = 0;
                int waitCondition = 0;
                int condEvaluated = 0;
                object waiterRef = null;

                var task = Task.Run(() =>
                {
                    var factory = SignalWaiterFactory.Create(ev.Factory, ev2.Factory);
                    var factory2 = SignalWaiterFactory.Create(factory, ev3.Factory);
                    using (var waiter = factory2.CreateWaiter())
                    {
                        Interlocked.Exchange(ref waiterRef, waiter);
                        lock (waiter)
                        {
                            Interlocked.Increment(ref waiterEntered);
                            if (waiter.Wait(s => { Interlocked.Increment(ref condEvaluated); return Volatile.Read(ref waitCondition) > 0; }, (object)null, 60000))
                                Interlocked.Exchange(ref waitResult, 1);
                            else
                                Interlocked.Exchange(ref waitResult, 2);

                            if (waiter.Wait(s => { Interlocked.Increment(ref condEvaluated); return Volatile.Read(ref waitCondition) > 1; }, (object)null, 60000))
                                Interlocked.Exchange(ref waitResult, 3);
                            else
                                Interlocked.Exchange(ref waitResult, 4);

                            if (waiter.Wait(s => { Interlocked.Increment(ref condEvaluated); return Volatile.Read(ref waitCondition) > 2; }, (object)null, 60000))
                                Interlocked.Exchange(ref waitResult, 5);
                            else
                                Interlocked.Exchange(ref waitResult, 6);
                        }
                    }
                });


                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref waiterEntered));
                lock (waiterRef)
                {
                    Interlocked.Increment(ref waitCondition);
                    ev.Signal();
                }
                TimingAssert.AreEqual(10000, 1, () => Volatile.Read(ref waitResult));

                lock (waiterRef)
                {
                    Interlocked.Increment(ref waitCondition);
                    ev2.Signal();
                }
                TimingAssert.AreEqual(10000, 3, () => Volatile.Read(ref waitResult));

                lock (waiterRef)
                {
                    Interlocked.Increment(ref waitCondition);
                    ev2.Signal();
                }
                TimingAssert.AreEqual(10000, 5, () => Volatile.Read(ref waitResult));

                task.Wait();
            }
        }
    }
}
