using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.PerformanceTests
{
    public static class ThreadPoolTests
    {
        private static int CalcRandSleep(Random rnd, double ms)
        {
            if (ms <= 0)
                return (int)ms;

            int full = (int)Math.Floor(ms);
            double rest = ms - full;

            lock (rnd)
            {
                if (rnd.NextDouble() < rest)
                    full++;
            }

            return full;
        }

        private static TimeSpan CalcRandSleep(Random rnd, TimeSpan src)
        {
            if (src <= TimeSpan.Zero)
                return src;

            return TimeSpan.FromMilliseconds(CalcRandSleep(rnd, src.TotalMilliseconds));
        }


        private struct TestConfiguration
        {
            public TestConfiguration(int poolQueueSize, int poolThreadCount, int taskCount, TimeSpan taskActiveAvgTime, TimeSpan taskPassiveAvgTime,
                int spawnThreadCount, TimeSpan spawnPeriod, bool spawnFromPool, bool useDeviance)
            {
                PoolQueueSize = poolQueueSize;
                PoolThreadCount = poolThreadCount;

                TaskCount = taskCount;
                ActiveTaskAvgTime = taskActiveAvgTime;
                PassiveTaskAvgTime = taskPassiveAvgTime;

                SpawnThreadCount = spawnThreadCount;
                SpawnPeriod = spawnPeriod;

                SpawnFromPool = spawnFromPool;

                UseDeviance = useDeviance;
            }

            public int PoolQueueSize;
            public int PoolThreadCount;

            public int TaskCount;
            public TimeSpan ActiveTaskAvgTime;
            public TimeSpan PassiveTaskAvgTime;

            public int SpawnThreadCount;
            public TimeSpan SpawnPeriod;

            public bool SpawnFromPool;

            public bool UseDeviance;

            public override string ToString()
            {
                StringBuilder data = new StringBuilder();
                data.Append("[").
                    Append("UseDeviance=" + UseDeviance.ToString() + ", ").
                    Append("SpawnFromPool=" + SpawnFromPool.ToString() + ", ").
                    Append("SpawnThreadCount=" + SpawnThreadCount.ToString() + ", ").
                    Append("SpawnPeriod=" + SpawnPeriod.ToString() + ", ").
                    Append("TaskCount=" + TaskCount.ToString() + ", ").
                    Append("ActiveTaskAvgTime=" + ActiveTaskAvgTime.ToString() + ", ").
                    Append("PassiveTaskAvgTime=" + PassiveTaskAvgTime.ToString() + ", ").
                    Append("QueueSize=" + PoolQueueSize.ToString() + ", ").
                    Append("ThreadCount=" + PoolThreadCount.ToString()).
                    Append("]");
                return data.ToString();
            }
        }


        private static TimeSpan RunTestOnPool(Qoollo.Turbo.Threading.ThreadPools.ThreadPoolBase pool, TestConfiguration config, bool waitForCompletion = true)
        {
            var taskActiveTimeDev = (int)(config.ActiveTaskAvgTime.Ticks / 4);
            var taskPassiveTimeDev = (int)(config.PassiveTaskAvgTime.Ticks / 4);
            Random rndGenerator = new Random();

            int executedTaskCounter = 0;
            int completedTaskCount = 0;


            Action taskAction = null;
            taskAction = () =>
            {
                if (config.PassiveTaskAvgTime > TimeSpan.Zero)
                {
                    TimeSpan taskTime = config.PassiveTaskAvgTime;
                    if (config.UseDeviance)
                        lock (rndGenerator)
                            taskTime += TimeSpan.FromTicks(rndGenerator.Next(-taskPassiveTimeDev, taskPassiveTimeDev));

                    int taskTimeMs = CalcRandSleep(rndGenerator, taskTime.TotalMilliseconds);

                    if (taskTimeMs > 0)
                        Thread.Sleep(taskTimeMs);
                    else if (taskTimeMs == 0)
                        Thread.Yield();
                }

                // ----
                if (config.ActiveTaskAvgTime > TimeSpan.Zero)
                {
                    TimeSpan taskTime = config.ActiveTaskAvgTime;
                    if (config.UseDeviance)
                        lock (rndGenerator)
                            taskTime += TimeSpan.FromTicks(rndGenerator.Next(-taskActiveTimeDev, taskActiveTimeDev));

                    int taskTimeMs = CalcRandSleep(rndGenerator, taskTime.TotalMilliseconds);

                    if (taskTimeMs > 0)
                    {
                        Stopwatch sw111 = Stopwatch.StartNew();
                        while (sw111.ElapsedMilliseconds < taskTimeMs)
                            Thread.SpinWait(10000);
                    }
                    else if (taskTimeMs == 0)
                        Thread.Yield();
                }

                // ----

                if (config.SpawnFromPool)
                {
                    if (Interlocked.Increment(ref executedTaskCounter) <= config.TaskCount)
                    {
                        //pool.RunAsTask(taskAction);
                        pool.Run(taskAction);
                    }
                }

                Interlocked.Increment(ref completedTaskCount);
            };

            Barrier bar = new Barrier(config.SpawnThreadCount + 1);

            Random spawnRndGenerator = new Random();
            int spawnTimeDev = (int)(config.SpawnPeriod.Ticks / 4);
            Thread[] spawnThreads = new Thread[config.SpawnThreadCount];
            ThreadStart spawnAction = () =>
            {
                bar.SignalAndWait();

                long expectedSleep = 0;
                long realSleep = 0;
                int curSpawned = 0;
                while ((curSpawned = Interlocked.Increment(ref executedTaskCounter)) <= config.TaskCount)
                {
                    //pool.RunAsTask(taskAction);
                    pool.Run(taskAction);

                    TimeSpan spawnSleep = config.SpawnPeriod;
                    if (config.UseDeviance)
                        lock (spawnRndGenerator)
                            spawnSleep += TimeSpan.FromTicks(spawnRndGenerator.Next(-spawnTimeDev, spawnTimeDev));

                    int spawnSleepMs = CalcRandSleep(spawnRndGenerator, spawnSleep.TotalMilliseconds);

                    if (spawnSleepMs > 0)
                    {
                        expectedSleep += spawnSleepMs;

                        int startTick = Environment.TickCount;
                        if (realSleep <= expectedSleep)
                            Thread.Sleep(spawnSleepMs);

                        realSleep += (Environment.TickCount - startTick);
                    }
                }
            };


            for (int i = 0; i < spawnThreads.Length; i++)
                spawnThreads[i] = new Thread(spawnAction);

            for (int i = 0; i < spawnThreads.Length; i++)
                spawnThreads[i].Start();

          
            bar.SignalAndWait();
            Stopwatch sw = Stopwatch.StartNew();

            if (waitForCompletion)
                SpinWait.SpinUntil(() => Volatile.Read(ref completedTaskCount) >= config.TaskCount);
            else
                SpinWait.SpinUntil(() => Volatile.Read(ref executedTaskCounter) >= config.TaskCount);

            sw.Stop();

            if (waitForCompletion && completedTaskCount != config.TaskCount)
                throw new Exception("completedTaskCount != config.TaskCount");

            return sw.Elapsed;
        }


        private static void RunSingleTestSet(TestConfiguration config, string fileName, Func<TestConfiguration, Qoollo.Turbo.Threading.ThreadPools.ThreadPoolBase> poolConstructor, bool waitForCompletion = true, bool doDispose = true)
        {
            Qoollo.Turbo.Threading.ThreadPools.ThreadPoolBase pool = null;
            try
            {
                pool = poolConstructor(config);
                {
                    var testRes = RunTestOnPool(pool, config, waitForCompletion);
                    string msg = pool.GetType().Name + config.ToString() + "  ==  " + testRes.TotalMilliseconds.ToString() + "ms";
                    Console.WriteLine(msg);
                    if (fileName != null)
                        File.AppendAllLines(fileName, new string[] { msg });
                }
            }
            finally
            {
                if (doDispose && pool != null)
                    pool.Dispose();
            }
        }

        private static void RunAllTestSets(
            Func<TestConfiguration, Qoollo.Turbo.Threading.ThreadPools.ThreadPoolBase> poolConstructor2,     
            string fileName, bool useDeviance = true)
        {
            bool[] spawnFromPoolParams = { false, true };

            TestConfiguration[] configs = 
            {
                new TestConfiguration()
                {
                    TaskCount = 3000,
                    ActiveTaskAvgTime = TimeSpan.Zero,
                    PassiveTaskAvgTime = TimeSpan.FromMilliseconds(10),
                    SpawnThreadCount = 1,
                    SpawnPeriod = TimeSpan.FromTicks(500)
                },
                new TestConfiguration()
                {
                    TaskCount = 10000,
                    ActiveTaskAvgTime = TimeSpan.FromMilliseconds(1),
                    PassiveTaskAvgTime = TimeSpan.Zero,                 
                    SpawnThreadCount = 1,
                    SpawnPeriod = TimeSpan.FromTicks(500)
                },
                //new TestConfiguration()
                //{
                //    TaskCount = 1000000,
                //    ActiveTaskAvgTime = TimeSpan.Zero,
                //    PassiveTaskAvgTime = TimeSpan.Zero,
                //    SpawnThreadCount = 1,
                //    SpawnPeriod = TimeSpan.Zero
                //},

                new TestConfiguration()
                {
                    TaskCount = 3000,
                    ActiveTaskAvgTime = TimeSpan.Zero,
                    PassiveTaskAvgTime = TimeSpan.FromMilliseconds(10),
                    SpawnThreadCount = 4,
                    SpawnPeriod = TimeSpan.FromTicks(500)
                },
                new TestConfiguration()
                {
                    TaskCount = 10000,
                    ActiveTaskAvgTime = TimeSpan.FromMilliseconds(1),
                    PassiveTaskAvgTime = TimeSpan.Zero,
                    SpawnThreadCount = 4,
                    SpawnPeriod = TimeSpan.FromTicks(500)
                },
                new TestConfiguration()
                {
                    TaskCount = 1000000,
                    ActiveTaskAvgTime = TimeSpan.Zero,
                    PassiveTaskAvgTime = TimeSpan.Zero,
                    SpawnThreadCount = 4,
                    SpawnPeriod = TimeSpan.Zero
                },
            };


            int[] queueSizes = { -1, 0, 1000 };
            int[] threadCountOpt = Enumerable.Range(1, 16).Concat(Enumerable.Range(1, 10).Select(o => o * 4 + 16)).ToArray();

            foreach (var spawnFromPool in spawnFromPoolParams)
            {
                foreach (var config in configs)
                {
                    foreach (var queueSize in queueSizes)
                    {
                        foreach (var threadCount in threadCountOpt)
                        {
                            var realConfig = config;
                            realConfig.SpawnFromPool = spawnFromPool;
                            realConfig.PoolQueueSize = queueSize != 0 ? queueSize : threadCount;
                            realConfig.PoolThreadCount = threadCount;
                            realConfig.UseDeviance = useDeviance;

                            RunSingleTestSet(realConfig, fileName, poolConstructor2);
                        }

                        Console.WriteLine();
                        if (fileName != null)
                            File.AppendAllLines(fileName, new String[] { "" });
                    }
                }
            }
        }


        private static void RunOnSystemThreadPool()
        {
            RunAllTestSets(cfg => new Qoollo.Turbo.Threading.ThreadPools.SystemThreadPool(), @"f:\sysThPool.txt", true);
        }
        private static void RunOnStaticThreadPool()
        {
            RunAllTestSets(cfg => new Qoollo.Turbo.Threading.ThreadPools.StaticThreadPool(cfg.PoolThreadCount, cfg.PoolQueueSize, "123"), @"f:\statThPool.txt", true);
        }
        private static void RunOnDynamicThreadPool()
        {
            RunAllTestSets(cfg => new Qoollo.Turbo.Threading.ThreadPools.DynamicThreadPool(0, cfg.PoolThreadCount, cfg.PoolQueueSize, "234"), @"f:\dynThPool.txt", true);
        }



        private static void RunOnSystemThreadPoolNew(TestConfiguration config)
        {
            RunSingleTestSet(config, null, cfg => new Qoollo.Turbo.Threading.ThreadPools.SystemThreadPool());
        }
        private static void RunOnStaticThreadPoolNew(TestConfiguration config)
        {
            RunSingleTestSet(config, null, cfg => new Qoollo.Turbo.Threading.ThreadPools.StaticThreadPool(cfg.PoolThreadCount, cfg.PoolQueueSize, "123"));
        }
        private static void RunOnStaticThreadPoolNew(TestConfiguration config, Qoollo.Turbo.Threading.ThreadPools.StaticThreadPool pool, bool waitForCompletion)
        {
            RunSingleTestSet(config, null, cfg => pool, waitForCompletion, false);
        }
        private static void RunOnDynamicThreadPoolNew(TestConfiguration config)
        {
            RunSingleTestSet(config, null, cfg => new Qoollo.Turbo.Threading.ThreadPools.DynamicThreadPool(0, cfg.PoolThreadCount, cfg.PoolQueueSize, "123"));
        }
        private static void RunOnDynamicThreadPoolNew(TestConfiguration config, Qoollo.Turbo.Threading.ThreadPools.DynamicThreadPool pool, bool waitForCompletion)
        {
            RunSingleTestSet(config, null, cfg => pool, waitForCompletion,  false);
        }



        public static void RunCompareTest()
        {
            TestConfiguration config = new TestConfiguration()
            {
                PoolQueueSize = 1000,
                PoolThreadCount = 32,

                TaskCount = 100000,
                ActiveTaskAvgTime = TimeSpan.Zero,
                PassiveTaskAvgTime = TimeSpan.FromMilliseconds(1),

                SpawnThreadCount = 4,
                SpawnPeriod = TimeSpan.Zero,

                SpawnFromPool = false,

                UseDeviance = false
            };
            TestConfiguration config2 = new TestConfiguration()
            {
                PoolQueueSize = -1,
                PoolThreadCount = 2,

                TaskCount = 100000,
                ActiveTaskAvgTime = TimeSpan.FromMilliseconds(1), //TimeSpan.FromMilliseconds(1),
                PassiveTaskAvgTime = TimeSpan.Zero,

                SpawnThreadCount = 8,
                SpawnPeriod = TimeSpan.Zero, //TimeSpan.FromMilliseconds(1),

                SpawnFromPool = false,

                UseDeviance = false
            };
            TestConfiguration config3 = new TestConfiguration()
            {
                PoolQueueSize = -1,
                PoolThreadCount = 2,

                TaskCount = 100000,
                ActiveTaskAvgTime = TimeSpan.FromMilliseconds(1),
                PassiveTaskAvgTime = TimeSpan.FromMilliseconds(1),

                SpawnThreadCount = 8,
                SpawnPeriod = TimeSpan.Zero,

                SpawnFromPool = false,

                UseDeviance = false
            };

            //RunOnSystemThreadPool(config2);
            //RunOnStaticThreadPool(config2);
            //RunOnDynamicThreadPool(config);

            //Console.WriteLine();

            //RunOnSystemThreadPoolNew(config);
            //for (int i = 0; i < 3; i++)
            //RunOnStaticThreadPoolNew(config2);
            //config2.PoolQueueSize = -1;
            //RunOnStaticThreadPoolNew(config2);

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < 2; i++)
            {
                RunOnSystemThreadPoolNew(config);
                RunOnSystemThreadPoolNew(config3);
                RunOnSystemThreadPoolNew(config2);
                RunOnSystemThreadPoolNew(config3);
            }

            sw.Stop();
            Console.WriteLine("=================");
            Console.WriteLine("!!!!!!!!!!!!!!!!!!SYSTEM = " + sw.Elapsed.ToString());
            Console.WriteLine("=================");
            Console.WriteLine();




            //Qoollo.Turbo.Threading.ThreadPools.StaticThreadPool staticPool = new Threading.ThreadPools.StaticThreadPool(Environment.ProcessorCount, 4000, "name");

            //sw = Stopwatch.StartNew();

            //for (int i = 0; i < 2; i++)
            //{
            //    RunOnStaticThreadPoolNew(config, staticPool, true);
            //    RunOnStaticThreadPoolNew(config3, staticPool, true);
            //    RunOnStaticThreadPoolNew(config2, staticPool, true);
            //    RunOnStaticThreadPoolNew(config3, staticPool, true);
            //}

            //staticPool.Dispose(true, true, false);
            //sw.Stop();
            //Console.WriteLine("=================");
            //Console.WriteLine("!!!!!!!!!!!!!!!!!!STATIC = " + sw.Elapsed.ToString());
            //Console.WriteLine("=================");
            //Console.WriteLine();






            Qoollo.Turbo.Threading.ThreadPools.DynamicThreadPool pool = new Threading.ThreadPools.DynamicThreadPool(0, 128, 10000, "name");

            sw = Stopwatch.StartNew();

            for (int i = 0; i < 2; i++)
            {
                //RunOnDynamicThreadPoolNew(config, pool);
                //RunOnDynamicThreadPoolNew(config, pool);
                //RunOnDynamicThreadPoolNew(config2, pool);

                RunOnDynamicThreadPoolNew(config, pool, true);
                RunOnDynamicThreadPoolNew(config3, pool, true);
                RunOnDynamicThreadPoolNew(config2, pool, true);
                RunOnDynamicThreadPoolNew(config3, pool, true);
            }

            pool.Dispose(true, true, false);
            sw.Stop();
            Console.WriteLine("=================");
            Console.WriteLine("!!!!!!!!!!!!!!!!!!DYNAMIC = " + sw.Elapsed.ToString());
            Console.WriteLine("=================");
            Console.WriteLine();
        }




        public static double FuncForOptimization(double[] optVals = null)
        {
            TestConfiguration config = new TestConfiguration()
            {
                PoolQueueSize = 1000,
                PoolThreadCount = 32,

                TaskCount = 50000,
                ActiveTaskAvgTime = TimeSpan.Zero,
                PassiveTaskAvgTime = TimeSpan.FromMilliseconds(1),

                SpawnThreadCount = 4,
                SpawnPeriod = TimeSpan.Zero,

                SpawnFromPool = false,

                UseDeviance = false
            };
            TestConfiguration config2 = new TestConfiguration()
            {
                PoolQueueSize = -1,
                PoolThreadCount = 2,

                TaskCount = 50000,
                ActiveTaskAvgTime = TimeSpan.FromMilliseconds(1),
                PassiveTaskAvgTime = TimeSpan.Zero,

                SpawnThreadCount = 8,
                SpawnPeriod = TimeSpan.Zero,

                SpawnFromPool = false,

                UseDeviance = false
            };
            TestConfiguration config3 = new TestConfiguration()
            {
                PoolQueueSize = -1,
                PoolThreadCount = 2,

                TaskCount = 50000,
                ActiveTaskAvgTime = TimeSpan.FromMilliseconds(1),
                PassiveTaskAvgTime = TimeSpan.FromMilliseconds(1),

                SpawnThreadCount = 8,
                SpawnPeriod = TimeSpan.Zero,

                SpawnFromPool = false,

                UseDeviance = false
            };


            if (optVals != null)
            {
                //Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.OptimalStateTime = (int)optVals[0];
                //Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.OptimalStateAverageCaptureLocalFluctuationDiffCoef = optVals[1];
                //Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.OptimalStateFluctuationDiffCoef = optVals[2];

                //Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.FindBestDirectionSuggestIncreaseDiff = optVals[3];
                //Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.FindBestDirectionSuggestDecreaseDiff = optVals[4];

                //Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.IncreaseDirectionLocGoodDiff = optVals[5];
                //Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.IncreaseDirectionAvgGoodDiff = optVals[6];
                //Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.IncreaseDirectionLocDropDiff = optVals[7];
                //Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.IncreaseDirectionAvgDropDiff = optVals[8];

                //Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.DecreaseDirectionLocDropDiff = optVals[9];
                //Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.DecreaseDirectionAvgDropDiff = optVals[10];
                //Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.DecreaseDirectionLocFluctuationNearBaselineCoef = optVals[11];
                //Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.DecreaseDirectionAvgFluctuationNearBaselineCoef = optVals[12];
            }


            double[] measures = new double[5];


            for (int measureId = 0; measureId < measures.Length; measureId++)
            {
                Stopwatch sw = Stopwatch.StartNew();
                Qoollo.Turbo.Threading.ThreadPools.DynamicThreadPool.DisableCritical = true;
                Qoollo.Turbo.Threading.ThreadPools.DynamicThreadPool pool = new Threading.ThreadPools.DynamicThreadPool(0, 80, 2000, "name");

                for (int i = 0; i < 2; i++)
                {
                    RunOnDynamicThreadPoolNew(config, pool, true);
                    RunOnDynamicThreadPoolNew(config3, pool, true);
                    RunOnDynamicThreadPoolNew(config2, pool, true);
                    RunOnDynamicThreadPoolNew(config3, pool, true);
                }

                pool.Dispose(true, true, false);

                sw.Stop();
                measures[measureId] = sw.ElapsedMilliseconds / 1000;
            }

            Array.Sort(measures);

            double elapsed = measures.Skip(1).Take(measures.Length - 2).Average();


            string printString = "ELAPSED = " + elapsed.ToString("0.00") + "s";

            if (optVals != null)
            {
                printString += ", values = [";
                for (int i = 0; i < optVals.Length; i++)
                    printString += (optVals[i].ToString() + ", ");
                printString += "]";
            }

            Console.WriteLine(printString);
            Console.WriteLine();

            return (int)elapsed;
        }


        private static double RunRangeOptimization(double[] bestValues, double[] minValues, double[] maxValues, int stepCount, int[] argsIndex)
        {
            double[] stepSize = new double[argsIndex.Length];
            int[] stepNum = new int[argsIndex.Length];

            for (int i = 0; i < argsIndex.Length; i++)
                stepSize[i] = (maxValues[argsIndex[i]] - minValues[argsIndex[i]]) / (stepCount - 1);

            double[] curValues = bestValues.ToArray();
            double bestResult = double.MaxValue;

            int step = 0;
            int totalSteps = (int)Math.Pow(stepCount, argsIndex.Length);

            while (true)
            {
                for (int i = 0; i < argsIndex.Length; i++)
                    curValues[argsIndex[i]] = minValues[argsIndex[i]] + stepSize[i] * stepNum[i];

                step++;
                Console.WriteLine("=> STEP = " + step.ToString() + " / " + totalSteps.ToString());

                double localResult = FuncForOptimization(curValues);
                if (localResult < bestResult)
                {
                    for (int i = 0; i < bestValues.Length; i++)
                        bestValues[i] = curValues[i];
                    bestResult = localResult;
                }

                bool hasOverflow = false;
                for (int i = 0; i < stepNum.Length; i++)
                {
                    hasOverflow = false;
                    stepNum[i]++;
                    if (stepNum[i] < stepCount)
                        break;

                    hasOverflow = true;
                    stepNum[i] = 0;
                }

                if (hasOverflow)
                    break;
            }

            return bestResult;
        }


        public static void RunOptimization()
        {
            var bestValues = new double[]
                    {
                        Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.OptimalStateTime,
                        Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.OptimalStateAverageCaptureLocalFluctuationDiffCoef,
                        Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.OptimalStateFluctuationDiffCoef,
                        
                        Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.FindBestDirectionSuggestIncreaseDiff,
                        Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.FindBestDirectionSuggestDecreaseDiff,
                        
                        Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.IncreaseDirectionLocGoodDiff,
                        Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.IncreaseDirectionAvgGoodDiff,
                        Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.IncreaseDirectionLocDropDiff,
                        Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.IncreaseDirectionAvgDropDiff,
                        
                        Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.DecreaseDirectionLocDropDiff,
                        Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.DecreaseDirectionAvgDropDiff,
                        Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.DecreaseDirectionLocFluctuationNearBaselineCoef,
                        Qoollo.Turbo.Threading.ThreadPools.ServiceStuff.ExecutionThroughoutTrackerUpDownCorrection.DecreaseDirectionAvgFluctuationNearBaselineCoef
                    };

            var minValues = new double[]
                    {
                        1000, 0, 0,
                        0.1, 0,
                        0.05, 0.05, -0.1, -0.1,
                        0.7, 0.7, 0.01, 0.01
                    };

            var maxValues = new double[]
                    {
                        30000, 1, 0.25,
                        0.6, 0.3,
                        0.15, 0.15, 0.1, 0.1,
                        1.2, 1.2, 0.5, 0.5
                    };

            Console.WriteLine("Optimization of {9, 10} in range {[0.7, 1.2], [0.7, 1.2]}");
            var result = RunRangeOptimization(bestValues, minValues, maxValues, 10, new int[] { 9, 10 });
            Console.WriteLine("============= OPTIMIZED =============");
            Console.WriteLine("Result = " + result.ToString());
            Console.WriteLine("=====================================");

            for (int i = 0; i < bestValues.Length; i++)
                Console.WriteLine(bestValues[i].ToString());


            //using (NLoptNet.NLoptSolver solver = new NLoptNet.NLoptSolver(NLoptNet.NLoptAlgorithm.LN_SBPLX, (uint)bestValues.Length, 0, 1000))
            //{
            //    solver.SetLowerBounds(minValues);
            //    solver.SetUpperBounds(maxValues);
            //    solver.SetMinObjective(FuncForOptimization);

            //    double? score;
            //    var result = solver.Optimize(bestValues, out score);


            //    Console.WriteLine("============= OPTIMIZED =============");
            //    Console.WriteLine("Result = " + result.ToString());
            //    Console.WriteLine("Score = " + (score ?? -1).ToString());
            //    Console.WriteLine("=====================================");

            //    for (int i = 0; i < bestValues.Length; i++)
            //        Console.WriteLine(bestValues[i].ToString());
            //}

            Console.ReadLine();
            Console.ReadLine();
        }


        public static void RunAllTests()
        {
            RunOnSystemThreadPool();
            RunOnStaticThreadPool();
            RunOnDynamicThreadPool();
        }
    }
}
