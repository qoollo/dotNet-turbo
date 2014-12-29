using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadManagement
{
    /// <summary>
    /// Делегат для запуска потока с передачей токена отмены
    /// </summary>
    /// <param name="token">Токен отмены</param>
    public delegate void TokenThreadStart(CancellationToken token);
    /// <summary>
    /// Делегат для запуска потока с передачей состояния и токена отмены
    /// </summary>
    /// <param name="state">Объект состояния</param>
    /// <param name="token">Токен отмены</param>
    public delegate void ParametrizedTokenThreadStart(object state, CancellationToken token);
    /// <summary>
    /// Делегат для запуска потока с передачей уникального id потока, объекта состояния и токена отмены
    /// </summary>
    /// <param name="threadUID">ID потока</param>
    /// <param name="state">Объект состояния</param>
    /// <param name="token">Токен отмены</param>
    public delegate void ParametrizedIdTokenThreadStart(int threadUID, object state, CancellationToken token);


    /// <summary>
    /// Менеджер группы потоков
    /// </summary>
    public sealed class DelegateThreadSetManager: ThreadSetManager
    {
        private readonly ThreadStart _threadStartAction;
        private readonly ParameterizedThreadStart _parametrizedThreadStartAction;
        private readonly TokenThreadStart _tokenThreadStartAction;
        private readonly ParametrizedTokenThreadStart _stateTokenThreadStartAction;
        private readonly ParametrizedIdTokenThreadStart _idStateTokenThreadStartAction;

        private object _state;

        /// <summary>
        /// Конструктор DelegateThreadManager
        /// </summary>
        /// <param name="threadStartAction">Делегат запуска задачи для потока</param>
        /// <param name="threadCount">Число потоков</param>
        /// <param name="name">Имя для потоков</param>
        public DelegateThreadSetManager(int threadCount, string name, ThreadStart threadStartAction)
            : base(threadCount, name)
        {
            Contract.Requires<ArgumentNullException>(threadStartAction != null);

            _threadStartAction = threadStartAction;
        }
        /// <summary>
        /// Конструктор DelegateThreadManager
        /// </summary>
        /// <param name="parametrizedThreadStartAction">Делегат запуска задачи для потока</param>
        /// <param name="threadCount">Число потоков</param>
        /// <param name="name">Имя для потоков</param>
        public DelegateThreadSetManager(int threadCount, string name, ParameterizedThreadStart parametrizedThreadStartAction)
            : base(threadCount, name)
        {
            Contract.Requires<ArgumentNullException>(parametrizedThreadStartAction != null);

            _parametrizedThreadStartAction = parametrizedThreadStartAction;
        }
        /// <summary>
        /// Конструктор DelegateThreadManager
        /// </summary>
        /// <param name="tokenThreadStartAction">Делегат запуска задачи для потока</param>
        /// <param name="threadCount">Число потоков</param>
        /// <param name="name">Имя для потоков</param>
        public DelegateThreadSetManager(int threadCount, string name, TokenThreadStart tokenThreadStartAction)
            : base(threadCount, name)
        {
            Contract.Requires<ArgumentNullException>(tokenThreadStartAction != null);

            _tokenThreadStartAction = tokenThreadStartAction;
        }
        /// <summary>
        /// Конструктор DelegateThreadManager
        /// </summary>
        /// <param name="stateTokenThreadStartAction">Делегат запуска задачи для потока</param>
        /// <param name="threadCount">Число потоков</param>
        /// <param name="name">Имя для потоков</param>
        public DelegateThreadSetManager(int threadCount, string name, ParametrizedTokenThreadStart stateTokenThreadStartAction)
            : base(threadCount, name)
        {
            Contract.Requires<ArgumentNullException>(stateTokenThreadStartAction != null);

            _stateTokenThreadStartAction = stateTokenThreadStartAction;
        }
        /// <summary>
        /// Конструктор DelegateThreadManager
        /// </summary>
        /// <param name="idStateTokenThreadStartAction">Делегат запуска задачи для потока</param>
        /// <param name="threadCount">Число потоков</param>
        /// <param name="name">Имя для потоков</param>
        public DelegateThreadSetManager(int threadCount, string name, ParametrizedIdTokenThreadStart idStateTokenThreadStartAction)
            : base(threadCount, name)
        {
            Contract.Requires<ArgumentNullException>(idStateTokenThreadStartAction != null);

            _idStateTokenThreadStartAction = idStateTokenThreadStartAction;
        }


        /// <summary>
        /// Запуск обработчиков
        /// </summary>
        /// <param name="state">Объект состояния, передаваемый во все потоки</param>
        public void Start(object state)
        {
            _state = state;
            this.Start();
        }

        /// <summary>
        /// Основной метод обработки
        /// </summary>
        /// <param name="state">Объект состояния, инициализированный в методе Prepare()</param>
        /// <param name="token">Токен для отмены обработки при вызове Stop</param>
        protected override void Process(object state, CancellationToken token)
        {
            if (_threadStartAction != null)
                _threadStartAction();
            else if (_parametrizedThreadStartAction != null)
                _parametrizedThreadStartAction(_state);
            else if (_tokenThreadStartAction != null)
                _tokenThreadStartAction(token);
            else if (_stateTokenThreadStartAction != null)
                _stateTokenThreadStartAction(_state, token);
            else if (_idStateTokenThreadStartAction != null)
                _idStateTokenThreadStartAction(this.GetThreadId(), _state, token);
            else
                throw new InvalidOperationException("ThreadStart delegates not initialized");
        }


        /// <summary>
        /// Остановка и освобождение ресурсов
        /// </summary>
        /// <param name="waitForStop">Ожидать остановки</param>
        public override void Stop(bool waitForStop)
        {
            base.Stop(waitForStop);
            _state = null;
        }

        /// <summary>
        /// Основной код освобождения ресурсов
        /// </summary>
        /// <param name="isUserCall">Вызвано ли освобождение пользователем. False - деструктор</param>
        protected override void Dispose(bool isUserCall)
        {
            base.Dispose(isUserCall);

            if (isUserCall)
                _state = null;
        }
    }
}
