using System;
using System.Collections.Generic;
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
        private readonly ContextSwitchDelegate _switchDel;

        /// <summary>
        /// Конструктор SingleDelegateContextSwitchSupplier
        /// </summary>
        /// <param name="switchDel">Делегат смены контекста</param>
        public SingleDelegateContextSwitchSupplier(ContextSwitchDelegate switchDel)
        {
            if (switchDel == null)
                throw new ArgumentNullException(nameof(switchDel));

            _switchDel = switchDel;
        }

        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        public void Run(Action act, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

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
            TurboContract.Requires(act != null, conditionString: "act != null");

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
        private readonly ContextSwitchWithStateDelegate _switchDel;

        /// <summary>
        /// Конструктор SingleDelegateWithStateContextSwitchSupplier
        /// </summary>
        /// <param name="switchDel">Делегат смены контекста</param>
        public SingleDelegateWithStateContextSwitchSupplier(ContextSwitchWithStateDelegate switchDel)
        {
            if (switchDel == null)
                throw new ArgumentNullException(nameof(switchDel));

            _switchDel = switchDel;
        }

        /// <summary>
        /// Выполнение действия переданного как состояние
        /// </summary>
        /// <param name="act">Действие</param>
        private static void RunAction(object act)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            ((Action)act)();
        }

        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        public void Run(Action act, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

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
            TurboContract.Requires(act != null, conditionString: "act != null");

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
        private readonly ContextSwitchNoFlowDelegate _switchDel;

        /// <summary>
        /// Конструктор SingleDelegateNoFlowContextSwitchSupplier
        /// </summary>
        /// <param name="switchDel">Делегат смены контекста</param>
        public SingleDelegateNoFlowContextSwitchSupplier(ContextSwitchNoFlowDelegate switchDel)
        {
            if (switchDel == null)
                throw new ArgumentNullException(nameof(switchDel));

            _switchDel = switchDel;
        }

        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        public void Run(Action act, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

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
            TurboContract.Requires(act != null, conditionString: "act != null");

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
        private readonly ContextSwitchWithStateNoFlowDelegate _switchDel;

        /// <summary>
        /// Конструктор SingleDelegateWithStateNoFlowContextSwitchSupplier
        /// </summary>
        /// <param name="switchDel">Делегат смены контекста</param>
        public SingleDelegateWithStateNoFlowContextSwitchSupplier(ContextSwitchWithStateNoFlowDelegate switchDel)
        {
            if (switchDel == null)
                throw new ArgumentNullException(nameof(switchDel));


            _switchDel = switchDel;
        }

        /// <summary>
        /// Выполнение действия переданного как состояние
        /// </summary>
        /// <param name="act">Действие</param>
        private static void RunAction(object act)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            ((Action)act)();
        }

        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        public void Run(Action act, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

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
            TurboContract.Requires(act != null, conditionString: "act != null");

            _switchDel(act, state);
        }
    }


    /// <summary>
    /// Поставщик смены контекста по контексту синхронизации
    /// </summary>
    public class ContextSwitchFromSynchroContextSupplier : IContextSwitchSupplier
    {
        private readonly SynchronizationContext _syncContext;

        /// <summary>
        /// Конструктор ContextSwitchFromSynchroContextSupplier
        /// </summary>
        /// <param name="syncContext">Контекст синхронизации</param>
        public ContextSwitchFromSynchroContextSupplier(SynchronizationContext syncContext)
        {
            if (syncContext == null)
                throw new ArgumentNullException(nameof(syncContext));

            _syncContext = syncContext;
        }

        /// <summary>
        /// Выполнение действия переданного как состояние
        /// </summary>
        /// <param name="act">Действие</param>
        private static void RunAction(object act)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

            ((Action)act)();
        }

        /// <summary>
        /// Запустить в другом контексте
        /// </summary>
        /// <param name="act">Действие</param>
        /// <param name="flowContext">Протаскивать ли ExecutionContext</param>
        public void Run(Action act, bool flowContext)
        {
            TurboContract.Requires(act != null, conditionString: "act != null");

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
            TurboContract.Requires(act != null, conditionString: "act != null");

            _syncContext.Post(new SendOrPostCallback(act), state);
        }
    }
}
