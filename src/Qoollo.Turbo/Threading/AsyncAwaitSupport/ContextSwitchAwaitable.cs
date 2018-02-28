using Qoollo.Turbo.Threading.AsyncAwaitSupport;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading
{
    /// <summary>
    /// Provides an awaitable context for switching into a target context
    /// </summary>
    public struct ContextSwitchAwaitable
    {
        private IContextSwitchSupplier _supplier;

        /// <summary>
        /// ContextSwitchAwaitable constructor
        /// </summary>
        /// <param name="supplier">Context switch supplier</param>
        public ContextSwitchAwaitable(IContextSwitchSupplier supplier)
        {
            TurboContract.Requires(supplier != null, conditionString: "supplier != null");

            _supplier = supplier;
        }

        /// <summary>
        /// ContextSwitchAwaitable constructor
        /// </summary>
        /// <param name="targetContext">Target synchronization context</param>
        public ContextSwitchAwaitable(SynchronizationContext targetContext)
        {
            TurboContract.Requires(targetContext != null, conditionString: "targetContext != null");

            _supplier = new ContextSwitchFromSynchroContextSupplier(targetContext);
        }

        /// <summary>
        /// Gets an awaiter for this <see cref="ContextSwitchAwaitable"/>
        /// </summary>
        /// <returns>An awaiter for this awaitable</returns>
        public ContextSwitchAwaiter GetAwaiter()
        {
            return new ContextSwitchAwaiter(_supplier);
        }

        /// <summary>
        /// Provides an awaiter that switches into a target context
        /// </summary>
        public struct ContextSwitchAwaiter : INotifyCompletion, ICriticalNotifyCompletion
        {
            private IContextSwitchSupplier _supplier;

            /// <summary>
            /// ContextSwitchAwaiter constructor
            /// </summary>
            /// <param name="supplier">Context switch supplier</param>
            public ContextSwitchAwaiter(IContextSwitchSupplier supplier)
            {
                _supplier = supplier;
            }

            /// <summary>
            /// Whether an operation already completed (always false)
            /// </summary>
            public bool IsCompleted
            {
                get { return false; }
            }

            /// <summary>
            /// Ends the await operation
            /// </summary>
            public void GetResult()
            {
            }

            /// <summary>
            /// Performs context switching
            /// </summary>
            /// <param name="continuation">Continuation that will be executed in new context</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation)
            {
                TurboContract.Assert(continuation != null, conditionString: "continuation != null");

                if (_supplier != null)
                    _supplier.Run(continuation, true);
                else
                    continuation();
            }

            /// <summary>
            /// Performs context switching (without flowing data from current context)
            /// </summary>
            /// <param name="continuation">Continuation that will be executed in new context</param>
            [SecurityCritical]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnsafeOnCompleted(Action continuation)
            {
                TurboContract.Assert(continuation != null, conditionString: "continuation != null");

                if (_supplier != null)
                    _supplier.Run(continuation, false);
                else
                    continuation();
            }
        }
    }
}
