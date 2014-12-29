using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.AsyncAwaitSupport
{
    /// <summary>
    /// Делегат смены контекста исполнения
    /// </summary>
    /// <param name="act">Действие, которое будет исполнено в новом контексте</param>
    /// <param name="flowEContext">Протаскивать ли параметры текущего контекста</param>
    public delegate void ContextSwitchDelegate(Action act, bool flowEContext);

    /// <summary>
    /// Поставщик смены контекста по делегату, принимающему Action и flowContext
    /// </summary>
    public class SingleDelegateContextSwitchSupplier : IContextSwitchSupplier
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_switchDel != null);
        }

        private ContextSwitchDelegate _switchDel;

        /// <summary>
        /// Конструктор SingleDelegateContextSwitchSupplier
        /// </summary>
        /// <param name="switchDel">Делегат смены контекста</param>
        public SingleDelegateContextSwitchSupplier(ContextSwitchDelegate switchDel)
        {
            Contract.Requires(switchDel != null);

            _switchDel = switchDel;
        }

        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        public void Run(Action act, bool flowContext)
        {
            _switchDel(act, flowContext);
        }
        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="state">Состояние</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        public void RunWithState(Action<object> act, object state, bool flowContext)
        {
            _switchDel(() => act(state), flowContext);
        }
    }


    /// <summary>
    /// Делегат смены контекста исполнения
    /// </summary>
    /// <param name="act">Действие, которое будет исполнено в новом контексте</param>
    /// <param name="state">Состояние</param>
    /// <param name="flowEContext">Протаскивать ли параметры текущего контекста</param>
    public delegate void ContextSwitchWithStateDelegate(Action<object> act, object state, bool flowEContext);


    /// <summary>
    /// Поставщик смены контекста по делегату, принимающему Action, State и flowContext
    /// </summary>
    public class SingleDelegateWithStateContextSwitchSupplier : IContextSwitchSupplier
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_switchDel != null);
        }

        private ContextSwitchWithStateDelegate _switchDel;

        /// <summary>
        /// Конструктор SingleDelegateWithStateContextSwitchSupplier
        /// </summary>
        /// <param name="switchDel">Делегат смены контекста</param>
        public SingleDelegateWithStateContextSwitchSupplier(ContextSwitchWithStateDelegate switchDel)
        {
            Contract.Requires(switchDel != null);

            _switchDel = switchDel;
        }

        /// <summary>
        /// Выполнение действия переданного как состояние
        /// </summary>
        /// <param name="act">Действие</param>
        private static void RunAction(object act)
        {
            Contract.Requires(act != null);

            ((Action)act)();
        }

        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        public void Run(Action act, bool flowContext)
        {
            _switchDel(RunAction, act, flowContext);
        }

        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="state">Состояние</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        public void RunWithState(Action<object> act, object state, bool flowContext)
        {
            _switchDel(act, state, flowContext);
        }
    }


    /// <summary>
    /// Делегат смены контекста исполнения
    /// </summary>
    /// <param name="act">Действие, которое будет исполнено в новом контексте</param>
    public delegate void ContextSwitchNoFlowDelegate(Action act);

    /// <summary>
    /// Поставщик смены контекста по делегату, принимающему Action
    /// </summary>
    public class SingleDelegateNoFlowContextSwitchSupplier : IContextSwitchSupplier
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_switchDel != null);
        }

        private ContextSwitchNoFlowDelegate _switchDel;

        /// <summary>
        /// Конструктор SingleDelegateNoFlowContextSwitchSupplier
        /// </summary>
        /// <param name="switchDel">Делегат смены контекста</param>
        public SingleDelegateNoFlowContextSwitchSupplier(ContextSwitchNoFlowDelegate switchDel)
        {
            Contract.Requires(switchDel != null);

            _switchDel = switchDel;
        }

        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        public void Run(Action act, bool flowContext)
        {
            _switchDel(act);
        }
        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="state">Состояние</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        public void RunWithState(Action<object> act, object state, bool flowContext)
        {
            _switchDel(() => act(state));
        }
    }


    /// <summary>
    /// Делегат смены контекста исполнения
    /// </summary>
    /// <param name="act">Действие, которое будет исполнено в новом контексте</param>
    /// <param name="state">Состояние</param>
    public delegate void ContextSwitchWithStateNoFlowDelegate(Action<object> act, object state);


    /// <summary>
    /// Поставщик смены контекста по делегату, принимающему Action, State и flowContext
    /// </summary>
    public class SingleDelegateWithStateNoFlowContextSwitchSupplier : IContextSwitchSupplier
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_switchDel != null);
        }

        private ContextSwitchWithStateNoFlowDelegate _switchDel;

        /// <summary>
        /// Конструктор SingleDelegateWithStateNoFlowContextSwitchSupplier
        /// </summary>
        /// <param name="switchDel">Делегат смены контекста</param>
        public SingleDelegateWithStateNoFlowContextSwitchSupplier(ContextSwitchWithStateNoFlowDelegate switchDel)
        {
            Contract.Requires(switchDel != null);

            _switchDel = switchDel;
        }

        /// <summary>
        /// Выполнение действия переданного как состояние
        /// </summary>
        /// <param name="act">Действие</param>
        private static void RunAction(object act)
        {
            Contract.Requires(act != null);

            ((Action)act)();
        }

        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        public void Run(Action act, bool flowContext)
        {
            _switchDel(RunAction, act);
        }

        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="state">Состояние</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        public void RunWithState(Action<object> act, object state, bool flowContext)
        {
            _switchDel(act, state);
        }
    }


    /// <summary>
    /// Поставщик смены контекста по контексту синхронизации
    /// </summary>
    public class ContextSwitchFromSynchroContextSupplier : IContextSwitchSupplier
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_syncContext != null);
        }

        private SynchronizationContext _syncContext;

        /// <summary>
        /// Конструктор ContextSwitchFromSynchroContextSupplier
        /// </summary>
        /// <param name="syncContext">Контекст синхронизации</param>
        public ContextSwitchFromSynchroContextSupplier(SynchronizationContext syncContext)
        {
            Contract.Requires(syncContext != null);

            _syncContext = syncContext;
        }

        /// <summary>
        /// Выполнение действия переданного как состояние
        /// </summary>
        /// <param name="act">Действие</param>
        private static void RunAction(object act)
        {
            Contract.Requires(act != null);

            ((Action)act)();
        }

        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        public void Run(Action act, bool flowContext)
        {
            _syncContext.Post(RunAction, act);
        }

        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="state">Состояние</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        public void RunWithState(Action<object> act, object state, bool flowContext)
        {
            _syncContext.Post(new SendOrPostCallback(act), state);
        }
    }
}
