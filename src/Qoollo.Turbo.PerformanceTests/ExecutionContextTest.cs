using Qoollo.Turbo.Threading.ServiceStuff;
using Qoollo.Turbo.Threading.ThreadPools.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.PerformanceTests
{
    public static class ExecutionContextTest
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DoWithContext(ExecutionContext d)
        {
            if (d == null)
                throw new Exception();
        }

        public static int Item = 0;
        private static void ExecutionMethod(object state)
        {
            Item++;
        }

        private static void CapturePerfDefault(int elemCount)
        {
            ExecutionContext data = ExecutionContext.Capture();
            ContextCallback clb = new ContextCallback(ExecutionMethod);

            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < elemCount; i++)
            {
                data = ExecutionContext.Capture();
                ExecutionContext.Run(data, clb, null);
                //DoWithContext(data);
            }

            sw.Stop();


            Console.WriteLine("CapturePerfDefault. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }

        enum EnumTst
        {
            A, B, C, D
        }

        private delegate ExecutionContext CaptureInternal();
        private delegate void RunInContext(ExecutionContext context, ContextCallback clb, object state, bool preserveSyncCtx);


        private static CaptureInternal _dynMethod = null;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static CaptureInternal CreateDynamicMethod()
        {
                var method = new DynamicMethod("ExecutionContext_Capture_Internal", typeof(ExecutionContext), Type.EmptyTypes, true);

                var ExecContextMethod = typeof(ExecutionContext).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Single(o => o.Name == "Capture" && o.GetParameters().Length == 2);

                var ilGen = method.GetILGenerator();
                var stackMark = ilGen.DeclareLocal(typeof(int));
                ilGen.Emit(OpCodes.Ldc_I4_1);
                ilGen.Emit(OpCodes.Stloc_0);
                ilGen.Emit(OpCodes.Ldloca_S, stackMark);
                ilGen.Emit(OpCodes.Ldc_I4_3);
                ilGen.Emit(OpCodes.Call, ExecContextMethod);
                ilGen.Emit(OpCodes.Ret);


                return (CaptureInternal)method.CreateDelegate(typeof(CaptureInternal));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ExecutionContext GetContext()
        {
            if (_dynMethod == null)
                _dynMethod = CreateDynamicMethod();
            return _dynMethod();
        }


        private static RunInContext _dynMethodRun = null;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static RunInContext CreateDynamicMethodRun()
        {
            var method = new DynamicMethod("ExecutionContext_Run_Internal", typeof(void), new Type[] { typeof(ExecutionContext), typeof(ContextCallback), typeof(object), typeof(bool) }, true);
            
            var RunContextMethod = typeof(ExecutionContext).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Single(o => o.Name == "RunInternal" && o.GetParameters().Length == 4);

            var ilGen = method.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Ldarg_2);
            ilGen.Emit(OpCodes.Ldarg_3);
            ilGen.Emit(OpCodes.Call, RunContextMethod);
            ilGen.Emit(OpCodes.Ret);


            return (RunInContext)method.CreateDelegate(typeof(RunInContext));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RunInContextMethod(ExecutionContext context, ContextCallback clb, object state, bool preserveSyncCtx)
        {
            if (_dynMethodRun == null)
                _dynMethodRun = CreateDynamicMethodRun();
            _dynMethodRun(context, clb, state, preserveSyncCtx);
        }



        private static void CapturePerfInternal(int elemCount)
        {
            ExecutionContext data = GetContext();
            ContextCallback clb = new ContextCallback(ExecutionMethod);

            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < elemCount; i++)
            {
                data = GetContext();
                RunInContextMethod(data, clb, null, true);
                //DoWithContext(data);
            }

            sw.Stop();

            Console.WriteLine("CapturePerfInternal. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }


        private static void CapturePerfNew(int elemCount)
        {
            ExecutionContext data = ExecutionContextHelper.CaptureContextNoSyncContext();
            ContextCallback clb = new ContextCallback(ExecutionMethod);

            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < elemCount; i++)
            {
                data = ExecutionContextHelper.CaptureContextNoSyncContext();
                ExecutionContextHelper.RunInContext(data, clb, null, true);
                //DoWithContext(data);
            }

            sw.Stop();

            Console.WriteLine("CapturePerfNew. Time = " + sw.ElapsedMilliseconds.ToString() + "ms");
            Console.WriteLine();
        }


        public static void RunTest()
        {
            for (int i = 0; i < 10; i++)
            {
                CapturePerfDefault(10000000);
                CapturePerfInternal(10000000);
                CapturePerfNew(10000000);
            }
        }
    }
}
