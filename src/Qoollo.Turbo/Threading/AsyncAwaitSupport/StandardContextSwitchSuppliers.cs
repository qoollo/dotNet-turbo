using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.AsyncAwaitSupport
{
    /// <summary>
    /// Delegate that provides the ability to switch the context of execution
    /// </summary>
    /// <param name="act">Action that should be executed in another context</param>
    /// <param name="flowEContext">Whether the current ExecutionContext should be flowed</param>
    public delegate void ContextSwitchDelegate(Action act, bool flowEContext);

    /// <summary>
    /// Context switch supplier that works through <see cref="ContextSwitchDelegate"/>
    /// </summary>
    public class SingleDelegateContextSwitchSupplier : IContextSwitchSupplier
    {
        private readonly ContextSwitchDelegate _switchDel;

        /// <summary>
        /// <see cref="SingleDelegateContextSwitchSupplier"/> constructor
        /// </summary>
        /// <param name="switchDel">Delegate that provides the ability to switch the context of execution</param>
        public SingleDelegateContextSwitchSupplier(ContextSwitchDelegate switchDel)
        {
            if (switchDel == null)
                throw new ArgumentNullException(nameof(switchDel));

            _switchDel = switchDel;
        }

        /// <summary>
        /// Runs action in another context
        /// </summary>
        /// <param name="act">Delegate for action to be executed</param>
        /// <param name="flowContext">Whether the ExecutionContext should be flowed</param>
        public void Run(Action act, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            _switchDel(act, flowContext);
        }
        /// <summary>
        /// Runs action in another context
        /// </summary>
        /// <param name="act">Delegate for action to be executed</param>
        /// <param name="state">State object that will be passed to '<paramref name="act"/>' as argument</param>
        /// <param name="flowContext">Whether the ExecutionContext should be flowed</param>
        public void RunWithState(Action<object> act, object state, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            _switchDel(() => act(state), flowContext);
        }
    }


    // =======================================

    /// <summary>
    /// Delegate that provides the ability to switch the context of execution
    /// </summary>
    /// <param name="act">Action that should be executed in another context</param>
    /// <param name="state">State object that will be passed to action</param>
    /// <param name="flowEContext">Whether the current ExecutionContext should be flowed</param>
    public delegate void ContextSwitchWithStateDelegate(Action<object> act, object state, bool flowEContext);


    /// <summary>
    /// Context switch supplier that works through <see cref="ContextSwitchWithStateDelegate"/>
    /// </summary>
    public class SingleDelegateWithStateContextSwitchSupplier : IContextSwitchSupplier
    {
        private readonly ContextSwitchWithStateDelegate _switchDel;

        /// <summary>
        /// <see cref="SingleDelegateWithStateContextSwitchSupplier"/> constructor
        /// </summary>
        /// <param name="switchDel">Delegate that provides the ability to switch the context of execution</param>
        public SingleDelegateWithStateContextSwitchSupplier(ContextSwitchWithStateDelegate switchDel)
        {
            if (switchDel == null)
                throw new ArgumentNullException(nameof(switchDel));

            _switchDel = switchDel;
        }

        /// <summary>
        /// Helper method to run action without state parameter
        /// </summary>
        /// <param name="act">Action</param>
        private static void RunAction(object act)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            ((Action)act)();
        }

        /// <summary>
        /// Runs action in another context
        /// </summary>
        /// <param name="act">Delegate for action to be executed</param>
        /// <param name="flowContext">Whether the ExecutionContext should be flowed</param>
        public void Run(Action act, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            _switchDel(RunAction, act, flowContext);
        }

        /// <summary>
        /// Runs action in another context
        /// </summary>
        /// <param name="act">Delegate for action to be executed</param>
        /// <param name="state">State object that will be passed to '<paramref name="act"/>' as argument</param>
        /// <param name="flowContext">Whether the ExecutionContext should be flowed</param>
        public void RunWithState(Action<object> act, object state, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            _switchDel(act, state, flowContext);
        }
    }


    // =======================================

    /// <summary>
    /// Delegate that provides the ability to switch the context of execution
    /// </summary>
    /// <param name="act">Action that should be executed in another context</param>
    public delegate void ContextSwitchNoFlowDelegate(Action act);

    /// <summary>
    /// Context switch supplier that works through <see cref="ContextSwitchNoFlowDelegate"/>
    /// </summary>
    public class SingleDelegateNoFlowContextSwitchSupplier : IContextSwitchSupplier
    {
        private readonly ContextSwitchNoFlowDelegate _switchDel;

        /// <summary>
        /// <see cref="SingleDelegateNoFlowContextSwitchSupplier"/> constructor
        /// </summary>
        /// <param name="switchDel">Delegate that provides the ability to switch the context of execution</param>
        public SingleDelegateNoFlowContextSwitchSupplier(ContextSwitchNoFlowDelegate switchDel)
        {
            if (switchDel == null)
                throw new ArgumentNullException(nameof(switchDel));

            _switchDel = switchDel;
        }

        /// <summary>
        /// Runs action in another context
        /// </summary>
        /// <param name="act">Delegate for action to be executed</param>
        /// <param name="flowContext">Whether the ExecutionContext should be flowed</param>
        public void Run(Action act, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            _switchDel(act);
        }
        /// <summary>
        /// Runs action in another context
        /// </summary>
        /// <param name="act">Delegate for action to be executed</param>
        /// <param name="state">State object that will be passed to '<paramref name="act"/>' as argument</param>
        /// <param name="flowContext">Whether the ExecutionContext should be flowed</param>
        public void RunWithState(Action<object> act, object state, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            _switchDel(() => act(state));
        }
    }


    // =======================================

    /// <summary>
    /// Delegate that provides the ability to switch the context of execution
    /// </summary>
    /// <param name="act">Action that should be executed in another context</param>
    /// <param name="state">State object that will be passed to action</param>
    public delegate void ContextSwitchWithStateNoFlowDelegate(Action<object> act, object state);


    /// <summary>
    /// Context switch supplier that works through <see cref="ContextSwitchWithStateNoFlowDelegate"/>
    /// </summary>
    public class SingleDelegateWithStateNoFlowContextSwitchSupplier : IContextSwitchSupplier
    {
        private readonly ContextSwitchWithStateNoFlowDelegate _switchDel;

        /// <summary>
        /// <see cref="SingleDelegateWithStateNoFlowContextSwitchSupplier"/> constructor
        /// </summary>
        /// <param name="switchDel">Delegate that provides the ability to switch the context of execution</param>
        public SingleDelegateWithStateNoFlowContextSwitchSupplier(ContextSwitchWithStateNoFlowDelegate switchDel)
        {
            if (switchDel == null)
                throw new ArgumentNullException(nameof(switchDel));


            _switchDel = switchDel;
        }

        /// <summary>
        /// Helper method to run action without state parameter
        /// </summary>
        /// <param name="act">Action</param>
        private static void RunAction(object act)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            ((Action)act)();
        }

        /// <summary>
        /// Runs action in another context
        /// </summary>
        /// <param name="act">Delegate for action to be executed</param>
        /// <param name="flowContext">Whether the ExecutionContext should be flowed</param>
        public void Run(Action act, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            _switchDel(RunAction, act);
        }

        /// <summary>
        /// Runs action in another context
        /// </summary>
        /// <param name="act">Delegate for action to be executed</param>
        /// <param name="state">State object that will be passed to '<paramref name="act"/>' as argument</param>
        /// <param name="flowContext">Whether the ExecutionContext should be flowed</param>
        public void RunWithState(Action<object> act, object state, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            _switchDel(act, state);
        }
    }


    // =======================================

    /// <summary>
    /// Context switch supplier that works through <see cref="SynchronizationContext"/>
    /// </summary>
    public class ContextSwitchFromSynchroContextSupplier : IContextSwitchSupplier
    {
        private readonly SynchronizationContext _syncContext;

        /// <summary>
        /// <see cref="ContextSwitchFromSynchroContextSupplier"/> constructor
        /// </summary>
        /// <param name="syncContext">Synchronization context</param>
        public ContextSwitchFromSynchroContextSupplier(SynchronizationContext syncContext)
        {
            if (syncContext == null)
                throw new ArgumentNullException(nameof(syncContext));

            _syncContext = syncContext;
        }

        /// <summary>
        /// Helper method to run action without state parameter
        /// </summary>
        /// <param name="act">Action</param>
        private static void RunAction(object act)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            ((Action)act)();
        }

        /// <summary>
        /// Runs action in another context
        /// </summary>
        /// <param name="act">Delegate for action to be executed</param>
        /// <param name="flowContext">Whether the ExecutionContext should be flowed</param>
        public void Run(Action act, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            _syncContext.Post(RunAction, act);
        }

        /// <summary>
        /// Runs action in another context
        /// </summary>
        /// <param name="act">Delegate for action to be executed</param>
        /// <param name="state">State object that will be passed to '<paramref name="act"/>' as argument</param>
        /// <param name="flowContext">Whether the ExecutionContext should be flowed</param>
        public void RunWithState(Action<object> act, object state, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            _syncContext.Post(new SendOrPostCallback(act), state);
        }
    }
}
