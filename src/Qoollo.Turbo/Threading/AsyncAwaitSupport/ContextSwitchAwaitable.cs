using Qoollo.Turbo.Threading.AsyncAwaitSupport;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading
{
    /// <summary>
    /// Структура поддержки смены контекста
    /// </summary>
    public struct ContextSwitchAwaitable
    {
        private IContextSwitchSupplier _supplier;

        /// <summary>
        /// Конструктор для случая внешней смены контектса
        /// </summary>
        /// <param name="supplier">Поставщик смены контекста</param>
        public ContextSwitchAwaitable(IContextSwitchSupplier supplier)
        {
            Contract.Requires(supplier != null);

            _supplier = supplier;
        }

        /// <summary>
        /// Конструктор, принимающий целевой контекст синхронизации
        /// </summary>
        /// <param name="targetContext">Целевой контекст синхронизации</param>
        public ContextSwitchAwaitable(SynchronizationContext targetContext)
        {
            Contract.Requires(targetContext != null);

            _supplier = new ContextSwitchFromSynchroContextSupplier(targetContext);
        }

        /// <summary>
        /// Получение объекта ожидания смены контекста
        /// </summary>
        /// <returns>Объект ожидания</returns>
        public ContextSwitchAwaiter GetAwaiter()
        {
            return new ContextSwitchAwaiter(_supplier);
        }

        /// <summary>
        /// Структура ожидания смены контекста
        /// </summary>
        public struct ContextSwitchAwaiter : INotifyCompletion, ICriticalNotifyCompletion
        {
            private IContextSwitchSupplier _supplier;

            /// <summary>
            /// Конструктор, принимающий операцию, которя сменит контекст
            /// </summary>
            /// <param name="supplier">Поставщик контекста</param>
            public ContextSwitchAwaiter(IContextSwitchSupplier supplier)
            {
                _supplier = supplier;
            }

            /// <summary>
            /// Завершена ли операция синхронно (всегда false)
            /// </summary>
            public bool IsCompleted
            {
                get { return false; }
            }

            /// <summary>
            /// Получение результата (ничего не делает, т.к. просто меняем контектс)
            /// </summary>
            public void GetResult()
            {
            }

            /// <summary>
            /// Собственно смена контекста
            /// </summary>
            /// <param name="continuation">Продолжение, которое будет выполнено в новом контектсе</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation)
            {
                Contract.Assert(continuation != null);

                if (_supplier != null)
                    _supplier.Run(continuation, true);
                else
                    continuation();
            }

            /// <summary>
            /// Смена контекста без протаскивания данных текущего контекста.
            /// </summary>
            /// <param name="continuation">Продолжение, которое будет выполнено в новом контектсе</param>
            [SecurityCritical]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnsafeOnCompleted(Action continuation)
            {
                Contract.Assert(continuation != null);

                if (_supplier != null)
                    _supplier.Run(continuation, false);
                else
                    continuation();
            }
        }
    }
}
