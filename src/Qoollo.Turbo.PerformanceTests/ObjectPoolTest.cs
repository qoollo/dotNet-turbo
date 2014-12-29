using Qoollo.Turbo.Collections.Concurrent;
using Qoollo.Turbo.ObjectPools.Common;
using Qoollo.Turbo.ObjectPools.ServiceStuff;
using Qoollo.Turbo.ObjectPools.ServiceStuff.ElementCollections;
using Qoollo.Turbo.ObjectPools.ServiceStuff.ElementContainers;
using Qoollo.Turbo.OldPool;
using Qoollo.Turbo.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.PerformanceTests
{
    public class ObjectPoolTest
    {
        public class PoolElem {}
        private class PoolElemOpSup: IPoolElementOperationSource<PoolElem>
        {
            public bool IsValid(PoolElementWrapper<PoolElem> container)
            {
                return true;
            }

            public int GetPriority(PoolElementWrapper<PoolElem> container)
            {
                return 0;
            }
        }

        private class PoolElemComparer: IComparer<PoolElem>
        {
            public int Compare(PoolElem x, PoolElem y)
            {
                return 0;
            }
        }

        private class PoolElemComparerNew: PoolElementComparer<PoolElem>
        {
            public override int Compare(PoolElementWrapper<PoolElem> a, PoolElementWrapper<PoolElem> b, out bool stopHere)
            {
                stopHere = false;
                return 0;
            }
        }

        public class ObjectPoolBCL<T>
        {
            private BlockingCollection<T> _objects = new BlockingCollection<T>();

            public T GetObject()
            {
                return _objects.Take();
            }

            public void PutObject(T item)
            {
                _objects.Add(item);
            }
        }

        public class ObjectPool<T>
        {
            //private BlockingCollection<T> _objects;
            //private BlockingQueue<T> _objects;
            private ConcurrentStack<T> _objects;
            private SemaphoreLight _sem = new SemaphoreLight(0);

            public ObjectPool()
            {
                _objects = new ConcurrentStack<T>();
                //_objects = new BlockingCollection<T>(new ConcurrentQueue<T>());
                //_objects = new BlockingQueue<T>();
            }

            public T GetObject()
            {
                if (!_sem.Wait(0, new CancellationToken()))
                    _sem.Wait();
                T item;
                _objects.TryPop(out item);
                return item;
                //return _objects.TryTake();
                //return _objects.Take();
            }

            public void PutObject(T item)
            {
                _objects.Push(item);
                _sem.Release();
                //_objects.Enqueue(item);
                //_objects.Add(item);
            }
        }


        internal class OldDynPool : DynamicSizePoolManager<PoolElem, PoolElement<PoolElem>>
        {
            public OldDynPool(int cnt) : base(cnt) { }

            protected override bool CreateElement(out PoolElem elem, int timeout, CancellationToken token)
            {
                elem = new PoolElem();
                return true;
            }

            protected override bool IsValidElement(PoolElem elem)
            {
                return true;
            }

            protected override void DestroyElement(PoolElem elem)
            {
            }

            protected override PoolElement<PoolElem> CreatePoolElement(PoolElem elem)
            {
                return new PoolElement<PoolElem>(this, elem);
            }
        }

        internal sealed class OldBalDynPool : BalancingDynamicSizePoolManager<PoolElem, PoolElement<PoolElem>>
        {
            public OldBalDynPool(int cnt) : base(cnt, new PoolElemComparer()) { }

            protected override bool CreateElement(out PoolElem elem, int timeout, CancellationToken token)
            {
                elem = new PoolElem();
                return true;
            }

            protected override bool IsValidElement(PoolElem elem)
            {
                return true;
            }

            protected override void DestroyElement(PoolElem elem)
            {
            }

            protected override PoolElement<PoolElem> CreatePoolElement(PoolElem elem)
            {
                return new PoolElement<PoolElem>(this, elem);
            }
        }

        public sealed class NewDynPool: Qoollo.Turbo.ObjectPools.DynamicPoolManager<PoolElem>
        {
            public NewDynPool(int cnt) : base(cnt) { }

            protected override bool CreateElement(out PoolElem elem, int timeout, CancellationToken token)
            {
                elem = new PoolElem();
                return true;
            }

            protected override bool IsValidElement(PoolElem elem)
            {
                return true;
            }

            protected override void DestroyElement(PoolElem elem)
            {
            }
        }

        public sealed class NewBalDynPool: Qoollo.Turbo.ObjectPools.BalancingDynamicPoolManager<PoolElem>
        {
            public NewBalDynPool(int cnt) : base(cnt) { }

            protected override bool CreateElement(out PoolElem elem, int timeout, CancellationToken token)
            {
                elem = new PoolElem();
                return true;
            }

            protected override bool IsValidElement(PoolElem elem)
            {
                return true;
            }

            protected override void DestroyElement(PoolElem elem)
            {
            }

            protected override int CompareElements(PoolElem a, PoolElem b, out bool stopHere)
            {
                stopHere = false;
                return 0;
            }
        }


        private static TimeSpan TestBCL(ObjectPoolBCL<PoolElem> pool, int threadCount, int opCount, int pauseSpin)
        {
            Thread[] threads = new Thread[threadCount];
            Barrier startBar = new Barrier(threadCount + 1);

            int opCountPerThread = opCount / threadCount;

            Action thAct = () =>
            {
                startBar.SignalAndWait();
                int execOp = 0;
                while (execOp++ < opCountPerThread)
                {
                    PoolElem el = null;
                    try
                    {
                        el = pool.GetObject();
                        //Thread.Sleep(pauseSpin);
                        Thread.SpinWait(pauseSpin);
                    }
                    finally
                    {
                        pool.PutObject(el);
                    }
                }
            };


            for (int i = 0; i < threads.Length; i++)
                threads[i] = new Thread(new ThreadStart(thAct));

            for (int i = 0; i < threads.Length; i++)
                threads[i].Start();

            startBar.SignalAndWait();
            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();

            sw.Stop();

            Console.WriteLine("BCL ObjPool. Elapsed = " + sw.ElapsedMilliseconds.ToString() + "ms");

            return sw.Elapsed;
        }

        private static void RunTestBCL(int threadCount, int elemCount, int opCount, int pauseSpin)
        {
            ObjectPoolBCL<PoolElem> pool = new ObjectPoolBCL<PoolElem>();
            for (int i = 0; i < elemCount; i++)
                pool.PutObject(new PoolElem());

            TestBCL(pool, threadCount, opCount, pauseSpin);
        }

        private static TimeSpan TestSimple(ObjectPool<PoolElem> pool, int threadCount, int opCount, int pauseSpin)
        {
            Thread[] threads = new Thread[threadCount];
            Barrier startBar = new Barrier(threadCount + 1);

            int opCountPerThread = opCount / threadCount;

            Action thAct = () =>
            {
                startBar.SignalAndWait();
                int execOp = 0;
                while (execOp++ < opCountPerThread)
                {
                    PoolElem el = null;
                    try
                    {
                        el = pool.GetObject();
                        //Thread.Sleep(pauseSpin);
                        Thread.SpinWait(pauseSpin);
                    }
                    finally
                    {
                        pool.PutObject(el);
                    }
                }
            };


            for (int i = 0; i < threads.Length; i++)
                threads[i] = new Thread(new ThreadStart(thAct));

            for (int i = 0; i < threads.Length; i++)
                threads[i].Start();

            startBar.SignalAndWait();
            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();

            sw.Stop();

            Console.WriteLine("SimpleObjPool. Elapsed = " + sw.ElapsedMilliseconds.ToString() + "ms");

            return sw.Elapsed;
        }

        private static void RunTestSimple(int threadCount, int elemCount, int opCount, int pauseSpin)
        {
            ObjectPool<PoolElem> pool = new ObjectPool<PoolElem>();
            for (int i = 0; i < elemCount; i++)
                pool.PutObject(new PoolElem());

            TestSimple(pool, threadCount, opCount, pauseSpin);
        }


        private static TimeSpan TestObjectPoolWithLListSimple(SimpleElementsContainer<PoolElem> pool, int threadCount, int opCount, int pauseSpin)
        {
            Thread[] threads = new Thread[threadCount];
            Barrier startBar = new Barrier(threadCount + 1);

            int opCountPerThread = opCount / threadCount;

            Action thAct = () =>
            {
                startBar.SignalAndWait();

                int execOp = 0;
                while (execOp++ < opCountPerThread)
                {
                    PoolElementWrapper<PoolElem> el = null;
                    try
                    {
                        el = pool.Take();
                        //Thread.Sleep(pauseSpin);
                        Thread.SpinWait(pauseSpin);
                    }
                    finally
                    {
                        pool.Release(el);
                    }
                }
            };


            for (int i = 0; i < threads.Length; i++)
                threads[i] = new Thread(new ThreadStart(thAct));

            for (int i = 0; i < threads.Length; i++)
                threads[i].Start();

            startBar.SignalAndWait();
            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();

            sw.Stop();

            Console.WriteLine("ObjectPoolWithLListSimple. Elapsed = " + sw.ElapsedMilliseconds.ToString() + "ms");

            return sw.Elapsed;
        }

        private static void RunTestObjectPoolWithLListSimple(int threadCount, int elemCount, int opCount, int pauseSpin)
        {
            SimpleElementsContainer<PoolElem> pool = new SimpleElementsContainer<PoolElem>();
            for (int i = 0; i < elemCount; i++)
                pool.Add(new PoolElem(), new PoolElemOpSup(), true);

            TestObjectPoolWithLListSimple(pool, threadCount, opCount, pauseSpin);

            PoolElementWrapper<PoolElem> tmp;
            while (pool.TryTake(out tmp, 0, new CancellationToken()))
            {
                tmp.MarkElementDestroyed();
                pool.Release(tmp);
            }
        }




        private static TimeSpan TestObjectPoolWithSyncPrior(PrioritizedElementsContainer<PoolElem> pool, int threadCount, int opCount, int pauseSpin)
        {
            Thread[] threads = new Thread[threadCount];
            Barrier startBar = new Barrier(threadCount + 1);

            int opCountPerThread = opCount / threadCount;

            Action thAct = () =>
            {
                startBar.SignalAndWait();

                int execOp = 0;
                while (execOp++ < opCountPerThread)
                {
                    PoolElementWrapper<PoolElem> el = null;
                    try
                    {
                        el = pool.Take();
                        //Thread.Sleep(pauseSpin);
                        Thread.SpinWait(pauseSpin);
                    }
                    finally
                    {
                        pool.Release(el);
                    }
                }
            };


            for (int i = 0; i < threads.Length; i++)
                threads[i] = new Thread(new ThreadStart(thAct));

            for (int i = 0; i < threads.Length; i++)
                threads[i].Start();

            startBar.SignalAndWait();
            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();

            sw.Stop();

            Console.WriteLine("PrioritizedElementsContainer. Elapsed = " + sw.ElapsedMilliseconds.ToString() + "ms");

            return sw.Elapsed;
        }

        private static void RunTestObjectPoolWithSyncPrior(int threadCount, int elemCount, int opCount, int pauseSpin)
        {
            PrioritizedElementsContainer<PoolElem> pool = new PrioritizedElementsContainer<PoolElem>(new PoolElemComparerNew());
            for (int i = 0; i < elemCount; i++)
                pool.Add(new PoolElem(), new PoolElemOpSup(), true);

            TestObjectPoolWithSyncPrior(pool, threadCount, opCount, pauseSpin);

            PoolElementWrapper<PoolElem> tmp;
            while (pool.TryTake(out tmp, 0, new CancellationToken()))
            {
                tmp.MarkElementDestroyed();
                pool.Release(tmp);
            }
        }





        private static TimeSpan TestPool(PoolManagerBase<PoolElem, PoolElement<PoolElem>> pool, int threadCount, int opCount, int pauseSpin)
        {
            Thread[] threads = new Thread[threadCount];
            Barrier startBar = new Barrier(threadCount + 1);

            int opCountPerThread = opCount / threadCount;

            Action thAct = () =>
                {
                    startBar.SignalAndWait();

                    int execOp = 0;
                    while (execOp++ < opCountPerThread)
                    {
                        using (var el = pool.Rent())
                        {
                            //Thread.Sleep(pauseSpin);
                            Thread.SpinWait(pauseSpin);
                        }
                    }
                };


            for (int i = 0; i < threads.Length; i++)
                threads[i] = new Thread(new ThreadStart(thAct));

            for (int i = 0; i < threads.Length; i++)
                threads[i].Start();

            startBar.SignalAndWait();
            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();

            sw.Stop();

            Console.WriteLine(pool.ToString() + ". Elapsed = " + sw.ElapsedMilliseconds.ToString() + "ms");

            return sw.Elapsed;
        }



        private static void TestStaticPool(int threadCount, int elemCount, int opCount, int pauseSpin)
        {
            using (var pool = new StaticPoolManager<PoolElem>("static"))
            {
                for (int i = 0; i < elemCount; i++)
                    pool.AddNewElement(new PoolElem());

                TestPool(pool, threadCount, opCount, pauseSpin);
            }
        }

        private static void TestDynamicPool(int threadCount, int elemCount, int opCount, int pauseSpin)
        {
            using (var pool = new OldDynPool(elemCount))
            {
                //pool.FillPoolUpTo(elemCount);

                TestPool(pool, threadCount, opCount, pauseSpin);
            }
        }


        private static void TestBalancingStaticPool(int threadCount, int elemCount, int opCount, int pauseSpin)
        {
            using (var pool = new BalancingStaticPoolManager<PoolElem>(new PoolElemComparer(), "static"))
            {
                for (int i = 0; i < elemCount; i++)
                    pool.AddNewElement(new PoolElem());

                TestPool(pool, threadCount, opCount, pauseSpin);
            }
        }

        private static void TestBalancingDynamicPool(int threadCount, int elemCount, int opCount, int pauseSpin)
        {
            using (var pool = new OldBalDynPool(elemCount))
            {
                //pool.FillPoolUpTo(elemCount);

                TestPool(pool, threadCount, opCount, pauseSpin);
            }
        }



        private static TimeSpan TestNewPool(Qoollo.Turbo.ObjectPools.ObjectPoolManager<PoolElem> pool, int threadCount, int opCount, int pauseSpin)
        {
            Thread[] threads = new Thread[threadCount];
            Barrier startBar = new Barrier(threadCount + 1);

            int opCountPerThread = opCount / threadCount;

            Action thAct = () =>
            {
                startBar.SignalAndWait();

                int execOp = 0;
                while (execOp++ < opCountPerThread)
                {
                    using (var el = pool.Rent())
                    {
                        //Thread.Sleep(pauseSpin);
                        Thread.SpinWait(pauseSpin);
                    }
                }
            };


            for (int i = 0; i < threads.Length; i++)
                threads[i] = new Thread(new ThreadStart(thAct));

            for (int i = 0; i < threads.Length; i++)
                threads[i].Start();

            startBar.SignalAndWait();
            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < threads.Length; i++)
                threads[i].Join();

            sw.Stop();

            Console.WriteLine(pool.ToString() + ". Elapsed = " + sw.ElapsedMilliseconds.ToString() + "ms");

            return sw.Elapsed;
        }



        private static void TestNewStaticPool(int threadCount, int elemCount, int opCount, int pauseSpin)
        {
            using (var pool = new Qoollo.Turbo.ObjectPools.StaticPoolManager<PoolElem>("static"))
            {
                for (int i = 0; i < elemCount; i++)
                    pool.AddElement(new PoolElem());

                TestNewPool(pool, threadCount, opCount, pauseSpin);
            }
        }

        private static void TestNewDynamicPool(int threadCount, int elemCount, int opCount, int pauseSpin)
        {
            using (var pool = new NewDynPool(elemCount))
            {
                pool.FillPoolUpTo(elemCount);

                TestNewPool(pool, threadCount, opCount, pauseSpin);
            }
        }


        private static void TestNewBalancingStaticPool(int threadCount, int elemCount, int opCount, int pauseSpin)
        {
            using (var pool = new Qoollo.Turbo.ObjectPools.BalancingStaticPoolManager<PoolElem>(new PoolElemComparerNew(), "static"))
            {
                for (int i = 0; i < elemCount; i++)
                    pool.AddElement(new PoolElem());

                TestNewPool(pool, threadCount, opCount, pauseSpin);
            }
        }

        private static void TestNewBalancingDynamicPool(int threadCount, int elemCount, int opCount, int pauseSpin)
        {
            using (var pool = new NewBalDynPool(elemCount))
            {
                //pool.FillPoolUpTo(elemCount);

                TestNewPool(pool, threadCount, opCount, pauseSpin);
            }
        }

        public static void RunTest()
        {
            //TestStaticThreadPool(4, 4, 10000000, 1);
            //TestStaticThreadPool(4, 4, 10000000, 1);
            //TestStaticThreadPool(4, 4, 10000000, 1);

            for (int i = 0; i < 3; i++)
                RunTestBCL(8, 1, 10000000, 100);

            for (int i = 0; i < 3; i++)
                TestNewDynamicPool(8, 1, 10000000, 100);

            //for (int i = 0; i < 3; i++)
            //    TestNewStaticPool(8, 1, 10000000, 100);

            //for (int i = 0; i < 1; i++)
            //    RunTestSimple(8, 16, 10000000, 10);

            //for (int i = 0; i < 1; i++)
            //    RunTestObjectPoolWithLListSimple(8, 16, 10000000, 10);

            //for (int i = 0; i < 1; i++)
            //    RunTestObjectPoolWithSyncPrior(8, 16, 10000000, 10);


            //for (int i = 0; i < 1; i++)
            //    TestStaticPool(8, 16, 10000000, 10);

            //for (int i = 0; i < 1; i++)
            //    TestNewStaticPool(8, 4, 10000000, 10);

            //for (int i = 0; i < 1; i++)
            //    TestDynamicPool(8, 2, 10000000, 10);

            //for (int i = 0; i < 3; i++)
            //    TestNewDynamicPool(8, 4, 10000000, 10);

            //for (int i = 0; i < 1; i++)
            //    TestBalancingStaticPool(8, 16, 10000000, 10);

            //for (int i = 0; i < 1; i++)
            //    TestBalancingDynamicPool(8, 160, 10000000, 10);

            //for (int i = 0; i < 3; i++)
            //    TestNewBalancingStaticPool(8, 1, 10000000, 100);

            //for (int i = 0; i < 4; i++)
            //    TestNewBalancingDynamicPool(8, 16, 10000000, 10);

            //TestStaticThreadPool(8, 4, 10000, 1);
            //TestStaticThreadPool(8, 4, 10000, 1);
            //TestStaticThreadPool(8, 4, 10000, 1);

            // 5152 vs 5500

            // 9600 vs 9400


            // 3500 vs 3500
            // 3800 vs 3800

            // 2700 vs 3100

            // 1100

            // 4500
        }
    }
}
