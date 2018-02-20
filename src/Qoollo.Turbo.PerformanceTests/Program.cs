using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.PerformanceTests
{
    class Program
    {
        static void Main(string[] args)
        {
            //SemaphorePerfTest.RunTest();
            //ObjectPoolTest.RunTest();
            //ThreadPoolTests.RunOptimization();
            //ThreadPoolTests.FuncForOptimization();
            //HighConcurrencyLoadTest.RunTest();
            //HighConcurrencyLoadTest.RunOptimization();
            //InliningTest.RunTest();
            //ThreadPoolTaskSpawnPerformanceTest.RunTest();
            //ExecutionContextTest.RunTest();
            //ThreadPoolWorkItemTest.RunTest();
            //ThreadPoolQueueTest.RunTest();
            //ConcurrentQueueTest.RunTest();
            //LocalThreadQueueTest.RunTest();
            //ThreadPoolTests.RunCompareTest();
            //ThreadPoolTests.RunAllTests();
            //LevelingQueueTest.RunTest();
            //EntryCountingEventPerfTest.RunTest();
            //ProfilerInliningTest.RunTest();
            CancellationTokenRegistrationTest.RunTest();
            Console.ReadLine();
        }
    }
}
