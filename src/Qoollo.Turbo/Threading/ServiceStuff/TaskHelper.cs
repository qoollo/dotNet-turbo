using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ServiceStuff
{
    /// <summary>
    /// Помошник для работы с Task
    /// </summary>
    internal static class TaskHelper
    {
        private delegate void SetTaskSchedulerDelegate(Task task, TaskScheduler scheduler);
        private delegate bool ExecuteTaskEntryDelegate(Task task, bool preventDoubleExecution);
        private delegate bool CancelTaskDelegate(Task task, bool cancelNonExecutingOnly);

        private static SetTaskSchedulerDelegate _setTaskSchedulerDelegate;
        private static ExecuteTaskEntryDelegate _executeTaskEntryDelegate;
        private static CancelTaskDelegate _cancelTaskDelegate;

        private static readonly object _syncObj = new object();

        /// <summary>
        /// Создаёт динамчисекий метод для установки TaskScheduler
        /// </summary>
        /// <returns>Делегат для вызова динамического метода</returns>
        private static SetTaskSchedulerDelegate CreateSetTaskSchedulerMethod()
        {
            var TaskSchedulerField = typeof(Task).GetField("m_taskScheduler", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (TaskSchedulerField == null)
                throw new InvalidProgramException("Field 'm_taskScheduler' not found in task.");

            var method = new DynamicMethod("Task_SetTaskScheduler_Internal_" + Guid.NewGuid().ToString("N"),
                typeof(void), new Type[] { typeof(Task), typeof(TaskScheduler) }, true);

            var ilGen = method.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Stfld, TaskSchedulerField);
            ilGen.Emit(OpCodes.Ret);

            return (SetTaskSchedulerDelegate)method.CreateDelegate(typeof(SetTaskSchedulerDelegate));
        }

        /// <summary>
        /// Создаёт динамический метод для выполнения внутренностей Task
        /// </summary>
        /// <returns>Делегат для вызова динамического метода</returns>
        private static ExecuteTaskEntryDelegate CreateExecuteTaskEntryMethod()
        {
            var ExecuteEntryMethod = typeof(Task).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Single(o => o.Name == "ExecuteEntry" && o.GetParameters().Length == 1);

            var method = new DynamicMethod("Task_ExecuteTaskEntry_Internal_" + Guid.NewGuid().ToString("N"), typeof(bool),
                new Type[] { typeof(Task), typeof(bool) }, true);

            var ilGen = method.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Callvirt, ExecuteEntryMethod);
            ilGen.Emit(OpCodes.Ret);

            return (ExecuteTaskEntryDelegate)method.CreateDelegate(typeof(ExecuteTaskEntryDelegate));
        }

        /// <summary>
        /// Создаёт динамический метод для отмены Task
        /// </summary>
        /// <returns>Делегат для вызова динамического метода</returns>
        private static CancelTaskDelegate CreateCancelTaskMethod()
        {
            var InternalCancelMethod = typeof(Task).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Single(o => o.Name == "InternalCancel" && o.GetParameters().Length == 1);

            var method = new DynamicMethod("Task_InternalCancel_Internal_" + Guid.NewGuid().ToString("N"), typeof(bool),
                new Type[] { typeof(Task), typeof(bool) }, true);

            var ilGen = method.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Callvirt, InternalCancelMethod);
            ilGen.Emit(OpCodes.Ret);

            return (CancelTaskDelegate)method.CreateDelegate(typeof(CancelTaskDelegate));
        }

        /// <summary>
        /// Выполнить инициализацию динамчиеских методов
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void InitDynamicMethods()
        {
            lock (_syncObj)
            {
                if (_setTaskSchedulerDelegate == null || _executeTaskEntryDelegate == null || _cancelTaskDelegate == null)
                {
                    _setTaskSchedulerDelegate = CreateSetTaskSchedulerMethod();
                    _executeTaskEntryDelegate = CreateExecuteTaskEntryMethod();
                    _cancelTaskDelegate = CreateCancelTaskMethod();
                }
            }
        }


        /// <summary>
        /// Выполнить внутренности Task
        /// </summary>
        /// <param name="task">Task</param>
        /// <param name="preventDoubleExecution">Защита от повторного выполнения</param>
        /// <returns>Успешность запуска</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ExecuteTaskEntry(Task task, bool preventDoubleExecution)
        {
            Contract.Requires(task != null);

            var action = _executeTaskEntryDelegate;
            if (action == null)
            {
                InitDynamicMethods();
                action = _executeTaskEntryDelegate;
            }
            return action(task, preventDoubleExecution);
        }
        /// <summary>
        /// Отменить Task
        /// </summary>
        /// <param name="task">Task</param>
        /// <param name="cancelNonExecutingOnly">Отменить только не начавший выполняться Task</param>
        /// <returns>Удалось ли отменить</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CancelTask(Task task, bool cancelNonExecutingOnly)
        {
            Contract.Requires(task != null);

            var action = _cancelTaskDelegate;
            if (action == null)
            {
                InitDynamicMethods();
                action = _cancelTaskDelegate;
            }
            return action(task, cancelNonExecutingOnly);
        }
        /// <summary>
        /// Установить scheduler для task
        /// </summary>
        /// <param name="task">Task</param>
        /// <param name="scheduler">TaskScheduler</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTaskScheduler(Task task, TaskScheduler scheduler)
        {
            Contract.Requires(task != null);

            var action = _setTaskSchedulerDelegate;
            if (action == null)
            {
                InitDynamicMethods();
                action = _setTaskSchedulerDelegate;
            }

            action(task, scheduler);
        }
    }
}
