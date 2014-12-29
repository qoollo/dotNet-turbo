using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ServiceStuff
{
    /// <summary>
    /// Помошник в работе с контекстом исполнения
    /// </summary>
    internal static class ExecutionContextHelper
    {
        private delegate ExecutionContext CaptureContextDelegate();
        private delegate void RunInContextDelegate(ExecutionContext context, ContextCallback clb, object state, bool preserveSyncCtx);

        private static CaptureContextDelegate _captureContextDelegate;
        private static RunInContextDelegate _runInContextDelegate;

        private static readonly object _syncObj = new object();

        /// <summary>
        /// Создаёт динамчисекий метод для захвата ExecutionContext по ускоренному сценарию
        /// </summary>
        /// <returns>Делегат для вызова динамического метода</returns>
        private static CaptureContextDelegate CreateCaptureContextMethod()
        {
            var ExecContextMethod = typeof(ExecutionContext).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Single(o => o.Name == "Capture" && o.GetParameters().Length == 2);

            var method = new DynamicMethod("ExecutionContext_Capture_Internal_" + Guid.NewGuid().ToString("N"), typeof(ExecutionContext), Type.EmptyTypes, true);

            var ilGen = method.GetILGenerator();
            var stackMark = ilGen.DeclareLocal(typeof(int));
            ilGen.Emit(OpCodes.Ldc_I4_1);
            ilGen.Emit(OpCodes.Stloc_0);
            ilGen.Emit(OpCodes.Ldloca_S, stackMark);
            ilGen.Emit(OpCodes.Ldc_I4_3);
            ilGen.Emit(OpCodes.Call, ExecContextMethod);
            ilGen.Emit(OpCodes.Ret);

            return (CaptureContextDelegate)method.CreateDelegate(typeof(CaptureContextDelegate));
        }

        /// <summary>
        /// Создаёт динамический метод для выполнения задачи в рамках ExecutionContext
        /// </summary>
        /// <returns>Делегат для вызова динамического метода</returns>
        private static RunInContextDelegate CreateRunInContextMethod()
        {
            var RunContextMethod = typeof(ExecutionContext).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Single(o => o.Name == "RunInternal" && o.GetParameters().Length == 4);

            var method = new DynamicMethod("ExecutionContext_Run_Internal_" + Guid.NewGuid().ToString("N"), typeof(void), 
                new Type[] { typeof(ExecutionContext), typeof(ContextCallback), typeof(object), typeof(bool) }, true);

            var ilGen = method.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Ldarg_2);
            ilGen.Emit(OpCodes.Ldarg_3);
            ilGen.Emit(OpCodes.Call, RunContextMethod);
            ilGen.Emit(OpCodes.Ret);

            return (RunInContextDelegate)method.CreateDelegate(typeof(RunInContextDelegate));
        }

        /// <summary>
        /// Выполнить инициализацию динамчиеских методов
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void InitDynamicMethods()
        {
            lock (_syncObj)
            {
                if (_captureContextDelegate == null || _runInContextDelegate == null)
                {
                    _captureContextDelegate = CreateCaptureContextMethod();
                    _runInContextDelegate = CreateRunInContextMethod();
                }
            }
        }

        /// <summary>
        /// Захватить контекст исполнения без проброса контекста синхронизации
        /// </summary>
        /// <returns>Захваченный контекст исполнения</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ExecutionContext CaptureContextNoSyncContext()
        {
            var action = _captureContextDelegate;
            if (action == null)
            {
                InitDynamicMethods();
                action = _captureContextDelegate;
            }
            return action();
        }
        /// <summary>
        /// Запустить делегат в ExecutionContext 
        /// </summary>
        /// <param name="context">Контекст</param>
        /// <param name="callback">Исполняемый делегат</param>
        /// <param name="state">Объект состояния</param>
        /// <param name="preserveSyncCtx">Сохранять ли текущий контекст синхронизации</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RunInContext(ExecutionContext context, ContextCallback callback, object state, bool preserveSyncCtx)
        {
            Contract.Requires(context != null);
            Contract.Requires(callback != null);

            var action = _runInContextDelegate;
            if (action == null)
            {
                InitDynamicMethods();
                action = _runInContextDelegate;
            }
            action(context, callback, state, preserveSyncCtx);
        }
    }
}
