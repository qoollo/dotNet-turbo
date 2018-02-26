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
    /// Helper to execute private methods of Task
    /// </summary>
    internal static class TaskHelper
    {
        private delegate void SetTaskSchedulerDelegate(Task task, TaskScheduler scheduler);
        private delegate bool ExecuteTaskEntryDelegate(Task task);
        private delegate bool CancelTaskDelegate(Task task, bool cancelNonExecutingOnly);

        private static SetTaskSchedulerDelegate _setTaskSchedulerDelegate;
        private static ExecuteTaskEntryDelegate _executeTaskEntryDelegate;
        private static CancelTaskDelegate _cancelTaskDelegate;

        private static readonly object _syncObj = new object();

        /// <summary>
        /// Creates dynamic method to set TaskScheduler on Task (set m_taskScheduler field)
        /// </summary>
        /// <returns>Delegate to set TaskScheduler</returns>
        private static SetTaskSchedulerDelegate CreateSetTaskSchedulerMethod()
        {
            var taskSchedulerField = typeof(Task).GetField("m_taskScheduler", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (taskSchedulerField == null)
                throw new NotSupportedException("Field 'm_taskScheduler' not found in Task");

            var method = new DynamicMethod("Task_SetTaskScheduler_Internal_" + Guid.NewGuid().ToString("N"),
                typeof(void), new Type[] { typeof(Task), typeof(TaskScheduler) }, true);

            var ilGen = method.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Stfld, taskSchedulerField);
            ilGen.Emit(OpCodes.Ret);

            return (SetTaskSchedulerDelegate)method.CreateDelegate(typeof(SetTaskSchedulerDelegate));
        }

        /// <summary>
        /// Initialize SetTaskScheduler dynamic method
        /// </summary>
        /// <returns>Delegate to initialized method</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SetTaskSchedulerDelegate InitSetTaskSchedulerMethod()
        {
            lock (_syncObj)
            {
                var result = Volatile.Read(ref _setTaskSchedulerDelegate);
                if (result == null)
                {
                    result = CreateSetTaskSchedulerMethod();
                    Volatile.Write(ref _setTaskSchedulerDelegate, result);
                }
                return result;
            }
        }

        /// <summary>
        /// Creates dynamic method to execute Task synchronously (ExecuteEntry method)
        /// </summary>
        /// <returns>Delegate to execute Task</returns>
        private static ExecuteTaskEntryDelegate CreateExecuteTaskEntryMethod()
        {
            var executeEntryMethodCandidates = typeof(Task).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Where(o => o.Name == "ExecuteEntry").ToList();
            if (executeEntryMethodCandidates.Count != 1)
                throw new NotSupportedException("Task does not contains method 'ExecuteEntry'");

            var executeEntryMethod = executeEntryMethodCandidates[0];
            var executeEntryMethodParameters = executeEntryMethod.GetParameters();
            if (executeEntryMethodParameters.Length != 0 && !(executeEntryMethodParameters.Length == 1 && executeEntryMethodParameters[0].ParameterType == typeof(bool)))
                throw new NotSupportedException("Task does not contains method 'ExecuteEntry'");

            var method = new DynamicMethod("Task_ExecuteTaskEntry_Internal_" + Guid.NewGuid().ToString("N"), typeof(bool),
                new Type[] { typeof(Task) }, true);

            var ilGen = method.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            if (executeEntryMethodParameters.Length == 1)
                ilGen.Emit(OpCodes.Ldc_I4_1);
            ilGen.Emit(OpCodes.Callvirt, executeEntryMethod);
            ilGen.Emit(OpCodes.Ret);

            return (ExecuteTaskEntryDelegate)method.CreateDelegate(typeof(ExecuteTaskEntryDelegate));
        }

        /// <summary>
        /// Initialize ExecuteTaskEntry dynamic method
        /// </summary>
        /// <returns>Delegate to initialized method</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ExecuteTaskEntryDelegate InitExecuteTaskEntryMethod()
        {
            lock (_syncObj)
            {
                var result = Volatile.Read(ref _executeTaskEntryDelegate);
                if (result == null)
                {
                    result = CreateExecuteTaskEntryMethod();
                    Volatile.Write(ref _executeTaskEntryDelegate, result);
                }
                return result;
            }
        }

        /// <summary>
        /// Creates dynamic method to cancel Task (InternalCancel method)
        /// </summary>
        /// <returns>Delegate to cancel Task</returns>
        private static CancelTaskDelegate CreateCancelTaskMethod()
        {
            var internalCancelMethodCandidates = typeof(Task).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Where(o => o.Name == "InternalCancel" && o.GetParameters().Length == 1).ToList();
            if (internalCancelMethodCandidates.Count != 1)
                throw new NotSupportedException("'InternalCancel' method not found in Task");

            var method = new DynamicMethod("Task_InternalCancel_Internal_" + Guid.NewGuid().ToString("N"), typeof(bool),
                new Type[] { typeof(Task), typeof(bool) }, true);

            var ilGen = method.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Callvirt, internalCancelMethodCandidates[0]);
            ilGen.Emit(OpCodes.Ret);

            return (CancelTaskDelegate)method.CreateDelegate(typeof(CancelTaskDelegate));
        }

        /// <summary>
        /// Initialize CancelTask dynamic method
        /// </summary>
        /// <returns>Delegate to initialized method</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static CancelTaskDelegate InitCancelTaskMethod()
        {
            lock (_syncObj)
            {
                var result = Volatile.Read(ref _cancelTaskDelegate);
                if (result == null)
                {
                    result = CreateCancelTaskMethod();
                    Volatile.Write(ref _cancelTaskDelegate, result);
                }
                return result;
            }
        }


        /// <summary>
        /// Executes work associated with Task synchronously
        /// </summary>
        /// <param name="task">Task</param>
        /// <returns>Is executed successfully</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ExecuteTaskEntry(Task task)
        {
            TurboContract.Requires(task != null, conditionString: "task != null");

            var action = _executeTaskEntryDelegate ?? InitExecuteTaskEntryMethod();
            return action(task);
        }
        /// <summary>
        /// Cancels Task
        /// </summary>
        /// <param name="task">Task</param>
        /// <param name="cancelNonExecutingOnly">Cancel only non executing task</param>
        /// <returns>Is cancelled sucessfully</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CancelTask(Task task, bool cancelNonExecutingOnly)
        {
            TurboContract.Requires(task != null, conditionString: "task != null");

            var action = _cancelTaskDelegate ?? InitCancelTaskMethod();
            return action(task, cancelNonExecutingOnly);
        }
        /// <summary>
        /// Sets TaskScheduler on Task object
        /// </summary>
        /// <param name="task">Task</param>
        /// <param name="scheduler">TaskScheduler</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTaskScheduler(Task task, TaskScheduler scheduler)
        {
            TurboContract.Requires(task != null, conditionString: "task != null");

            var action = _setTaskSchedulerDelegate ?? InitSetTaskSchedulerMethod();
            action(task, scheduler);
        }
    }
}
