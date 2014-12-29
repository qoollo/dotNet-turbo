using Qoollo.Turbo.Threading.ThreadPools.Common;
using Qoollo.Turbo.Threading.ThreadPools.ServiceStuff;
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

    public static class ThreadPoolWorkItemTest
    {
        private static int TestValue;
        private static void TestAction()
        {
            TestValue++;
        }
        private static void TestActionWithState(int val)
        {
            TestValue += val;
        }


        /*
        private static void TestWorkItem1(int count)
        {
            TestValue = 0;

            ThreadPoolWorkItem[] items = new ThreadPoolWorkItem[count];
            for (int i = 0; i < items.Length; i++)
                items[i] = null;

            Stopwatch swTotal = Stopwatch.StartNew();
            Stopwatch swCreation = Stopwatch.StartNew();

            for (int i = 0; i < items.Length; i++)
                items[i] = new ThreadPoolWorkItemForAction(TestAction);

            swCreation.Stop();

            Stopwatch swExecution = Stopwatch.StartNew();

            for (int i = 0; i < items.Length; i++)
                items[i].Run();

            swExecution.Stop();
            swTotal.Stop();

            if (TestValue != count)
                throw new Exception();

            Console.WriteLine("TestWorkItem1. TotalTime = " + swTotal.ElapsedMilliseconds.ToString() + "ms, CreationTime = " + swCreation.ElapsedMilliseconds.ToString() + "ms, ExecutionTime = " + swExecution.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }


        private static void TestWorkItem2(int count)
        {
            TestValue = 0;

            ThreadPoolWorkItem2[] items = new ThreadPoolWorkItem2[count];
            for (int i = 0; i < items.Length; i++)
                items[i] = null;

            Stopwatch swTotal = Stopwatch.StartNew();
            Stopwatch swCreation = Stopwatch.StartNew();

            for (int i = 0; i < items.Length; i++)
                items[i] = new ThreadPoolWorkItem2(TestAction);

            swCreation.Stop();

            Stopwatch swExecution = Stopwatch.StartNew();

            for (int i = 0; i < items.Length; i++)
                items[i].Run();

            swExecution.Stop();
            swTotal.Stop();

            if (TestValue != count)
                throw new Exception();

            Console.WriteLine("TestWorkItem2. TotalTime = " + swTotal.ElapsedMilliseconds.ToString() + "ms, CreationTime = " + swCreation.ElapsedMilliseconds.ToString() + "ms, ExecutionTime = " + swExecution.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }

        private static void TestWorkItem3(int count)
        {
            TestValue = 0;

            ThreadPoolWorkItem3[] items = new ThreadPoolWorkItem3[count];
            for (int i = 0; i < items.Length; i++)
                items[i] = new ThreadPoolWorkItem3();

            Stopwatch swTotal = Stopwatch.StartNew();
            Stopwatch swCreation = Stopwatch.StartNew();

            for (int i = 0; i < items.Length; i++)
                items[i] = new ThreadPoolWorkItem3(TestAction);

            swCreation.Stop();

            Stopwatch swExecution = Stopwatch.StartNew();

            for (int i = 0; i < items.Length; i++)
                items[i].Run();

            swExecution.Stop();
            swTotal.Stop();

            if (TestValue != count)
                throw new Exception();

            Console.WriteLine("TestWorkItem3. TotalTime = " + swTotal.ElapsedMilliseconds.ToString() + "ms, CreationTime = " + swCreation.ElapsedMilliseconds.ToString() + "ms, ExecutionTime = " + swExecution.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }



        private static void TestWorkItem1CreateAndRun(int count)
        {
            TestValue = 0;

            Stopwatch swTotal = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                var elem = new ThreadPoolWorkItemForAction(TestAction);
                elem.Run();
            }

            swTotal.Stop();

            if (TestValue != count)
                throw new Exception();

            Console.WriteLine("TestWorkItem1CreateAndRun. TotalTime = " + swTotal.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }
        private static void TestWorkItem2CreateAndRun(int count)
        {
            TestValue = 0;

            Stopwatch swTotal = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                var elem = new ThreadPoolWorkItem2(TestAction);
                elem.Run();
            }

            swTotal.Stop();

            if (TestValue != count)
                throw new Exception();

            Console.WriteLine("TestWorkItem2CreateAndRun. TotalTime = " + swTotal.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }
        private static void TestWorkItem3CreateAndRun(int count)
        {
            TestValue = 0;

            Stopwatch swTotal = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                var elem = new ThreadPoolWorkItem3(TestAction);
                elem.Run();
            }

            swTotal.Stop();

            if (TestValue != count)
                throw new Exception();

            Console.WriteLine("TestWorkItem3CreateAndRun. TotalTime = " + swTotal.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }




        private static void TestWorkItem1WithState(int count)
        {
            TestValue = 0;

            Action<int> act = new Action<int>(TestActionWithState);

            ThreadPoolWorkItem[] items = new ThreadPoolWorkItem[count];
            for (int i = 0; i < items.Length; i++)
                items[i] = null;

            Stopwatch swTotal = Stopwatch.StartNew();
            Stopwatch swCreation = Stopwatch.StartNew();

            for (int i = 0; i < items.Length; i++)
                items[i] = new ThreadPoolWorkItemForActionWithState<int>(TestActionWithState, 1);

            swCreation.Stop();

            Stopwatch swExecution = Stopwatch.StartNew();

            for (int i = 0; i < items.Length; i++)
                items[i].Run();

            swExecution.Stop();
            swTotal.Stop();

            if (TestValue != count)
                throw new Exception();

            Console.WriteLine("TestWorkItem1WithState. TotalTime = " + swTotal.ElapsedMilliseconds.ToString() + "ms, CreationTime = " + swCreation.ElapsedMilliseconds.ToString() + "ms, ExecutionTime = " + swExecution.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }

        private static void TestWorkItem2WithState(int count)
        {
            TestValue = 0;

            ThreadPoolWorkItem2[] items = new ThreadPoolWorkItem2[count];
            for (int i = 0; i < items.Length; i++)
                items[i] = null;

            Stopwatch swTotal = Stopwatch.StartNew();
            Stopwatch swCreation = Stopwatch.StartNew();

            for (int i = 0; i < items.Length; i++)
                items[i] = new ThreadPoolWorkItem2ForActionWithState<int>(TestActionWithState, 1);

            swCreation.Stop();

            Stopwatch swExecution = Stopwatch.StartNew();

            for (int i = 0; i < items.Length; i++)
                items[i].Run();

            swExecution.Stop();
            swTotal.Stop();

            if (TestValue != count)
                throw new Exception();

            Console.WriteLine("TestWorkItem2WithState. TotalTime = " + swTotal.ElapsedMilliseconds.ToString() + "ms, CreationTime = " + swCreation.ElapsedMilliseconds.ToString() + "ms, ExecutionTime = " + swExecution.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }

        private static ThreadPoolWorkItem3 FuckTheLogic(int val)
        {
            return new ThreadPoolWorkItem3(() => TestActionWithState(val));
        }

        private static void TestWorkItem3WithState(int count)
        {
            TestValue = 0;

            ThreadPoolWorkItem3[] items = new ThreadPoolWorkItem3[count];
            for (int i = 0; i < items.Length; i++)
                items[i] = new ThreadPoolWorkItem3();

            Stopwatch swTotal = Stopwatch.StartNew();
            Stopwatch swCreation = Stopwatch.StartNew();

            for (int i = 0; i < items.Length; i++)
                items[i] = FuckTheLogic(1); //new ThreadPoolWorkItem3(() => TestActionWithState(IncCount));

            swCreation.Stop();

            Stopwatch swExecution = Stopwatch.StartNew();

            for (int i = 0; i < items.Length; i++)
                items[i].Run();

            swExecution.Stop();
            swTotal.Stop();

            if (TestValue != count)
                throw new Exception();

            Console.WriteLine("TestWorkItem3WithState. TotalTime = " + swTotal.ElapsedMilliseconds.ToString() + "ms, CreationTime = " + swCreation.ElapsedMilliseconds.ToString() + "ms, ExecutionTime = " + swExecution.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }




        private static void TestWorkItem1CreateAndRunWithState(int count)
        {
            TestValue = 0;

            Stopwatch swTotal = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                var elem = new ThreadPoolWorkItemForActionWithState<int>(TestActionWithState, 1);
                elem.Run();
            }

            swTotal.Stop();

            if (TestValue != count)
                throw new Exception();

            Console.WriteLine("TestWorkItem1CreateAndRunWithState. TotalTime = " + swTotal.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }
        private static void TestWorkItem2CreateAndRunWithState(int count)
        {
            TestValue = 0;

            Stopwatch swTotal = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                var elem = new ThreadPoolWorkItem2ForActionWithState<int>(TestActionWithState, 1);
                elem.Run();
            }

            swTotal.Stop();

            if (TestValue != count)
                throw new Exception();

            Console.WriteLine("TestWorkItem2CreateAndRunWithState. TotalTime = " + swTotal.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }
        private static void TestWorkItem3CreateAndRunWithState(int count)
        {
            TestValue = 0;

            Stopwatch swTotal = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                var elem = new ThreadPoolWorkItem3(() => TestActionWithState(1));
                elem.Run();
            }

            swTotal.Stop();

            if (TestValue != count)
                throw new Exception();

            Console.WriteLine("TestWorkItem3CreateAndRunWithState. TotalTime = " + swTotal.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }



        private static void AddItemToCollection(BlockingCollection<ThreadPoolWorkItem> col)
        {
            var item = new ThreadPoolWorkItemForAction(TestAction);
            //item._action = TestAction;
            col.Add(item);
        }

        private static void TestWorkItem1ThoughQueue(int count)
        {
            BlockingCollection<ThreadPoolWorkItem> col = new BlockingCollection<ThreadPoolWorkItem>();
            CancellationTokenSource cSrc = new CancellationTokenSource();

            Thread consumeThread = new Thread(new ThreadStart(() =>
                {
                    try
                    {
                        while (true)
                        {
                            var elem = col.Take(cSrc.Token);
                            elem.Run();
                        }
                    }
                    catch (OperationCanceledException)
                    {

                    }

                    ThreadPoolWorkItem elem2;
                    while (col.TryTake(out elem2))
                        elem2.Run();
                }));


            consumeThread.Start();

            Thread.Sleep(1000);

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
                AddItemToCollection(col);

            cSrc.Cancel();
            //consumeThread.Join();

            sw.Stop();


            Console.WriteLine("TestWorkItem1ThoughQueue. TotalTime = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }



        private static void AddItemToCollection(BlockingCollection<ThreadPoolWorkItem2> col)
        {
            col.Add(new ThreadPoolWorkItem2(TestAction));
        }

        private static void TestWorkItem2ThoughQueue(int count)
        {
            BlockingCollection<ThreadPoolWorkItem2> col = new BlockingCollection<ThreadPoolWorkItem2>();
            CancellationTokenSource cSrc = new CancellationTokenSource();

            Thread consumeThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    while (true)
                    {
                        var elem = col.Take(cSrc.Token);
                        elem.Run();
                    }
                }
                catch (OperationCanceledException)
                {

                }

                ThreadPoolWorkItem2 elem2;
                while (col.TryTake(out elem2))
                    elem2.Run();
            }));


            consumeThread.Start();

            Thread.Sleep(1000);

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
                AddItemToCollection(col);

            cSrc.Cancel();
            //consumeThread.Join();

            sw.Stop();


            Console.WriteLine("TestWorkItem2ThoughQueue. TotalTime = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }


        private static void AddItemToCollection(BlockingCollection<ThreadPoolWorkItem3> col)
        {
            col.Add(new ThreadPoolWorkItem3(TestAction));
        }

        private static void TestWorkItem3ThoughQueue(int count)
        {
            BlockingCollection<ThreadPoolWorkItem3> col = new BlockingCollection<ThreadPoolWorkItem3>();
            CancellationTokenSource cSrc = new CancellationTokenSource();

            Thread consumeThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    while (true)
                    {
                        var elem = col.Take(cSrc.Token);
                        elem.Run();
                    }
                }
                catch (OperationCanceledException)
                {

                }

                ThreadPoolWorkItem3 elem2;
                while (col.TryTake(out elem2))
                    elem2.Run();
            }));


            consumeThread.Start();

            Thread.Sleep(1000);

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
                AddItemToCollection(col);

            cSrc.Cancel();
            //consumeThread.Join();

            sw.Stop();

            


            Console.WriteLine("TestWorkItem3ThoughQueue. TotalTime = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }

        private static void AddItemToCollection(BlockingCollection<ThreadPoolWorkItem4> col)
        {
            col.Add(new ThreadPoolWorkItem4(TestAction));
        }

        private static void TestWorkItem4ThoughQueue(int count)
        {
            BlockingCollection<ThreadPoolWorkItem4> col = new BlockingCollection<ThreadPoolWorkItem4>();
            CancellationTokenSource cSrc = new CancellationTokenSource();

            Thread consumeThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    while (true)
                    {
                        var elem = col.Take(cSrc.Token);
                        elem.Run();
                    }
                }
                catch (OperationCanceledException)
                {

                }

                ThreadPoolWorkItem4 elem2;
                while (col.TryTake(out elem2))
                    elem2.Run();
            }));


            consumeThread.Start();

            Thread.Sleep(1000);

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
                AddItemToCollection(col);

            cSrc.Cancel();
            //consumeThread.Join();

            sw.Stop();




            Console.WriteLine("TestWorkItem4ThoughQueue. TotalTime = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }


        private static void TestActionWithState(object state)
        {
            TestAction();
        }

        private static void TestSystemThreadPool(int count)
        {

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
                ThreadPool.QueueUserWorkItem(TestActionWithState, null);

            sw.Stop();




            Console.WriteLine("TestSystemThreadPool. TotalTime = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }


        private static void AddItemToCollection(ThreadPoolQueue col, Action act)
        {
            col.Add(new ThreadPoolWorkItemForAction(act), null);
        }


        private static void TestWorkItem1ThoughQueueX(int count)
        {
            ThreadPoolQueue col = new ThreadPoolQueue(-1, -1);
            CancellationTokenSource cSrc = new CancellationTokenSource();

            Thread consumeThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    while (true)
                    {
                        var elem = col.Take(null, null, cSrc.Token);
                        ((ThreadPoolWorkItem)elem).Run();
                    }
                }
                catch (OperationCanceledException)
                {

                }

                object elem2;
                while (col.TryTake(null, null, out elem2, 0, CancellationToken.None))
                    ((ThreadPoolWorkItem)elem2).Run();
            }));


            consumeThread.Start();

            Thread.Sleep(1000);

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
                AddItemToCollection(col, () => TestAction());

            //cSrc.Cancel();
            //consumeThread.Join();

            sw.Stop();

            cSrc.Cancel();

            Console.WriteLine("TestWorkItem1ThoughQueueX. TotalTime = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }

        */

        public static void RunTest()
        {
            //for (int i = 0; i < 10; i++)
            //{
            //    TestWorkItem1ThoughQueue(10000000);

            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();

            //    Thread.Sleep(1000);

            //    TestWorkItem2ThoughQueue(10000000);

            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();

            //    Thread.Sleep(1000);

            //    TestWorkItem3ThoughQueue(10000000);

            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();

            //    Thread.Sleep(1000);

            //    TestWorkItem4ThoughQueue(10000000);

            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();

            //    Thread.Sleep(1000);

            //    TestSystemThreadPool(10000000);

            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();

            //    Thread.Sleep(1000);

            //    TestWorkItem1ThoughQueueX(10000000);

            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();

            //    Thread.Sleep(1000);

            //    Console.WriteLine();
            //}


            //for (int i = 0; i < 10; i++)
            //{
            //    TestWorkItem1WithState(10000000);

            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();

            //    TestWorkItem2WithState(10000000);

            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();

            //    TestWorkItem3WithState(10000000);

            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();

            //    Console.WriteLine();
            //}


            //for (int i = 0; i < 10; i++)
            //{
            //    TestWorkItem1CreateAndRunWithState(40000000);

            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();

            //    TestWorkItem2CreateAndRunWithState(40000000);

            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();

            //    TestWorkItem3CreateAndRunWithState(40000000);

            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();

            //    Console.WriteLine();
            //}

            //for (int i = 0; i < 10; i++)
            //{
            //    TestWorkItem1CreateAndRun(40000000);

            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();

            //    TestWorkItem2CreateAndRun(40000000);

            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();

            //    TestWorkItem3CreateAndRun(40000000);

            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();

            //    Console.WriteLine();
            //}

            //for (int i = 0; i < 10; i++)
            //{
            //    TestWorkItem1(10000000);

            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();

            //    TestWorkItem2(10000000);

            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();

            //    TestWorkItem3(10000000);

            //    GC.Collect();
            //    GC.WaitForPendingFinalizers();
            //    GC.Collect();

            //    Console.WriteLine();
            //}
        }
    }
}
