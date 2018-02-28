using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ServiceStuff
{
    /// <summary>
    /// Helper to execute private methods of ExecutionContext
    /// </summary>
    internal static class ExecutionContextHelper
    {
        private delegate ExecutionContext CaptureContextDelegate();
        private delegate void RunInContextDelegate(ExecutionContext context, ContextCallback clb, object state, bool preserveSyncCtx);

        private static CaptureContextDelegate _captureContextDelegate;
        private static RunInContextDelegate _runInContextDelegate;

        private static readonly object _syncObj = new object();


        /// <summary>
        /// Fallback method for CaptureContextNoSyncContext
        /// </summary>
        /// <returns>Captured context</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ExecutionContext CaptureContextMethodFallback()
        {
            return ExecutionContext.Capture();
        }

        /// <summary>
        /// Attempts to generate dynamic method to capture ExecutionContext without SynchronizationContext
        /// </summary>
        /// <returns>Delegate for generated method</returns>
        private static CaptureContextDelegate TryCreateCaptureContextMethod()
        {
            var execContextMethodCandidates = typeof(ExecutionContext).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Where(o => o.Name == "Capture" && o.GetParameters().Length == 2).ToList();
            if (execContextMethodCandidates.Count != 1)
            {
#if NET45 || NET46
                TurboContract.Assert(false, "ExecutionContextHelper.TryCreateCaptureContextMethod should be successful for known runtimes");
#endif
                return null;
            }

            var method = new DynamicMethod("ExecutionContext_Capture_Internal_" + Guid.NewGuid().ToString("N"), typeof(ExecutionContext), Type.EmptyTypes, true);
    
            var ilGen = method.GetILGenerator();
            var stackMark = ilGen.DeclareLocal(typeof(int));
            ilGen.Emit(OpCodes.Ldc_I4_1);
            ilGen.Emit(OpCodes.Stloc_0);
            ilGen.Emit(OpCodes.Ldloca_S, stackMark);
            ilGen.Emit(OpCodes.Ldc_I4_3);
            ilGen.Emit(OpCodes.Call, execContextMethodCandidates[0]);
            ilGen.Emit(OpCodes.Ret);

            return (CaptureContextDelegate)method.CreateDelegate(typeof(CaptureContextDelegate));
        }

        /// <summary>
        /// Initialize CaptureContext delegate
        /// </summary>
        /// <returns>Initialized delegate</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static CaptureContextDelegate InitCaptureContextMethod()
        {
            lock (_syncObj)
            {
                var result = Volatile.Read(ref _captureContextDelegate);
                if (result == null)
                {
                    result = TryCreateCaptureContextMethod() ?? new CaptureContextDelegate(CaptureContextMethodFallback);
                    Volatile.Write(ref _captureContextDelegate, result);
                }
                return result;
            }
        }


        /// <summary>
        /// Fallback for RunInContext
        /// </summary>
        /// <param name="context">Context</param>
        /// <param name="callback">Callback</param>
        /// <param name="state">State object</param>
        /// <param name="preserveSyncCtx">Whether the current synchronization context should be preserved</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RunInContextMethodFallback(ExecutionContext context, ContextCallback callback, object state, bool preserveSyncCtx)
        {
            ExecutionContext.Run(context, callback, state);
        }

        /// <summary>
        /// Attempts to generate dynamic method to run in action in ExceutionContext
        /// </summary>
        /// <returns>Delegate for generated method</returns>
        private static RunInContextDelegate TryCreateRunInContextMethod()
        {
            var runContextMethodCandidates = typeof(ExecutionContext).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Where(o => o.Name == "RunInternal" && o.GetParameters().Length == 4).ToList();
            if (runContextMethodCandidates.Count != 1)
            {
#if NET45 || NET46
                TurboContract.Assert(false, "ExecutionContextHelper.TryCreateRunInContextMethod should be successful for known runtimes");
#endif
                return null;
            }

            var method = new DynamicMethod("ExecutionContext_Run_Internal_" + Guid.NewGuid().ToString("N"), typeof(void), 
                new Type[] { typeof(ExecutionContext), typeof(ContextCallback), typeof(object), typeof(bool) }, true);

            var ilGen = method.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Ldarg_2);
            ilGen.Emit(OpCodes.Ldarg_3);
            ilGen.Emit(OpCodes.Call, runContextMethodCandidates[0]);
            ilGen.Emit(OpCodes.Ret);

            return (RunInContextDelegate)method.CreateDelegate(typeof(RunInContextDelegate));
        }

        /// <summary>
        /// Initialize RunInContext delegate
        /// </summary>
        /// <returns>Initialized delegate</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static RunInContextDelegate InitRunInContextMethod()
        {
            lock (_syncObj)
            {
                var result = Volatile.Read(ref _runInContextDelegate);
                if (result == null)
                {
                    result = TryCreateRunInContextMethod() ?? new RunInContextDelegate(RunInContextMethodFallback);
                    Volatile.Write(ref _runInContextDelegate, result);
                }
                return result;
            }
        }


        /// <summary>
        /// Captures the execution context from the current thread without synchronization context if possible
        /// </summary>
        /// <returns>Captured ExecutionContext</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExecutionContext CaptureContextNoSyncContextIfPossible()
        {
            var action = _captureContextDelegate?? InitCaptureContextMethod();
            return action();
        }
        /// <summary>
        /// Runs a method in a specified execution context on the current thread
        /// </summary>
        /// <param name="context">Execution Context</param>
        /// <param name="callback">Delegate for method that will be run in the provided execution context</param>
        /// <param name="state">The object to pass to the callback method.</param>
        /// <param name="preserveSyncCtx">Whether the current synchronization context should be preserved</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RunInContext(ExecutionContext context, ContextCallback callback, object state, bool preserveSyncCtx)
        {
            TurboContract.Requires(context != null, conditionString: "context != null");
            TurboContract.Requires(callback != null, conditionString: "callback != null");

            var action = _runInContextDelegate ?? InitRunInContextMethod();
            action(context, callback, state, preserveSyncCtx);
        }
    }
}
