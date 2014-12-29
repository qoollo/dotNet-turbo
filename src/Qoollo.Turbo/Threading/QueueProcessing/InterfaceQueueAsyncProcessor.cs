using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Collections.Concurrent;

namespace Qoollo.Turbo.Threading.QueueProcessing
{
    /// <summary>
    /// Асинхронная обработка на интерфейсе.
    /// </summary>
    /// <typeparam name="T">Тип обрабатываемого элемента</typeparam>
    public class InterfaceQueueAsyncProcessor<T> : QueueAsyncProcessor<T>
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_logic != null);
        }

        private readonly IQueueAsyncProcessorLogic<T> _logic;

        /// <summary>
        /// Конструктор InterfaceQueueAsyncProcessor
        /// </summary>
        /// <param name="logic">Интерфейс с логикой обработки данных</param>
        /// <param name="processorCount">Число потоков обработки</param>
        /// <param name="maxQueueSize">Максимальный размер очереди</param>
        /// <param name="name">Имя, присваемое потокам</param>
        /// <param name="isBackground">Будут ли потоки работать в фоновом режиме</param>
        public InterfaceQueueAsyncProcessor(IQueueAsyncProcessorLogic<T> logic, int processorCount, int maxQueueSize, string name, bool isBackground)
            : base(processorCount, maxQueueSize, name, isBackground)
        {
            Contract.Requires<ArgumentNullException>(logic != null, "logic");

            _logic = logic;
        }
        /// <summary>
        /// Конструктор InterfaceQueueAsyncProcessor
        /// </summary>
        /// <param name="logic">Интерфейс с логикой обработки данных</param>
        /// <param name="processorCount">Число потоков обработки</param>
        /// <param name="maxQueueSize">Максимальный размер очереди</param>
        /// <param name="name">Имя, присваемое потокам</param>
        public InterfaceQueueAsyncProcessor(IQueueAsyncProcessorLogic<T> logic, int processorCount, int maxQueueSize, string name)
            : this(logic, processorCount, maxQueueSize, name, false)
        {
        }
        /// <summary>
        /// Конструктор InterfaceQueueAsyncProcessor
        /// </summary>
        /// <param name="logic">Интерфейс с логикой обработки данных</param>
        /// <param name="processorCount">Число потоков обработки</param>
        /// <param name="name">Имя, присваемое потокам</param>
        public InterfaceQueueAsyncProcessor(IQueueAsyncProcessorLogic<T> logic, int processorCount, string name)
            : this(logic, processorCount, -1, name, false)
        {
        }
        /// <summary>
        /// Конструктор InterfaceQueueAsyncProcessor
        /// </summary>
        /// <param name="logic">Интерфейс с логикой обработки данных</param>
        /// <param name="processorCount">Число потоков обработки</param>
        public InterfaceQueueAsyncProcessor(IQueueAsyncProcessorLogic<T> logic, int processorCount)
            : this(logic, processorCount, -1, null, false)
        {
        }

        /// <summary>
        /// Создание объекта состояния на поток.
        /// Вызывается при старте для каждого потока
        /// </summary>
        /// <returns>Объект состояния</returns>
        protected override object Prepare()
        {
            if (_logic is IQueueAsyncProcessorLogicExt<T>)
                return (_logic as IQueueAsyncProcessorLogicExt<T>).Prepare();

            return base.Prepare();
        }

        /// <summary>
        /// Освобождение объекта состояния потока
        /// </summary>
        /// <param name="state">Объект состояния</param>
        protected override void Finalize(object state)
        {
            if (_logic is IQueueAsyncProcessorLogicExt<T>)
                (_logic as IQueueAsyncProcessorLogicExt<T>).Finalize(state);
            else
                base.Finalize(state);
        }

        /// <summary>
        /// Основной метод обработки элементов
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <param name="state">Объект состояния, инициализированный в методе Prepare()</param>
        /// <param name="token">Токен для отмены обработки</param>
        protected override void Process(T element, object state, CancellationToken token)
        {
            _logic.Process(element, state, token);
        }

        /// <summary>
        /// Обработка исключений. 
        /// Чтобы исключение было проброшено наверх, нужно выбросить новое исключение внутри метода.
        /// </summary>
        /// <param name="ex">Исключение</param>
        /// <returns>Игнорировать ли исключение (false - поток завершает работу)</returns>
        protected override bool ProcessThreadException(Exception ex)
        {
            if (!_logic.ProcessThreadException(ex))
                return base.ProcessThreadException(ex);

            return true;
        }
    }


    /// <summary>
    /// Интерфейс с логикой асинхронной обработки данных
    /// </summary>
    /// <typeparam name="T">Тип обрабатываемого элемента</typeparam>
    [ContractClass(typeof(IQueueAsyncProcessorLogicCodeContractCheck<>))]
    public interface IQueueAsyncProcessorLogic<T>
    {
        /// <summary>
        /// Основной метод обработки элементов
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <param name="state">Объект состояния, инициализированный в методе Prepare()</param>
        /// <param name="token">Токен для отмены обработки</param>
        void Process(T element, object state, CancellationToken token);

        /// <summary>
        /// Обработка исключений. 
        /// Чтобы исключение было проброшено наверх, нужно вернуть false, либо самостоятельно выбросить новое исключение внутри метода.
        /// </summary>
        /// <param name="ex">Исключение</param>
        /// <returns>Игнорировать ли исключение (false - поток завершает работу)</returns>
        bool ProcessThreadException(Exception ex);
    }



    /// <summary>
    /// Интерфейс с расширенной логикой асинхронной обработки данных
    /// </summary>
    /// <typeparam name="T">Тип обрабатываемого элемента</typeparam>
    [ContractClass(typeof(IQueueAsyncProcessorLogicExtCodeContractCheck<>))]
    public interface IQueueAsyncProcessorLogicExt<T> : IQueueAsyncProcessorLogic<T>
    {
        /// <summary>
        /// Создание объекта состояния на поток.
        /// Вызывается при старте для каждого потока
        /// </summary>
        /// <returns>Объект состояния</returns>
        object Prepare();
        /// <summary>
        /// Освобождение объекта состояния потока
        /// </summary>
        /// <param name="state">Объект состояния</param>
        void Finalize(object state);
    }






    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(IQueueAsyncProcessorLogic<>))]
    abstract class IQueueAsyncProcessorLogicCodeContractCheck<T> : IQueueAsyncProcessorLogic<T>
    {
        /// <summary>Контракты</summary>
        private IQueueAsyncProcessorLogicCodeContractCheck() { }

        /// <summary>Контракты</summary>
        public bool ProcessThreadException(Exception ex)
        {
            Contract.Requires(ex != null);

            throw new NotImplementedException();
        }
        /// <summary>Контракты</summary>
        public void Process(T element, object state, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }




    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(IQueueAsyncProcessorLogicExt<>))]
    abstract class IQueueAsyncProcessorLogicExtCodeContractCheck<T> : IQueueAsyncProcessorLogicExt<T>
    {
        /// <summary>Контракты</summary>
        private IQueueAsyncProcessorLogicExtCodeContractCheck() { }


        /// <summary>Контракты</summary>
        public object Prepare()
        {
            throw new NotImplementedException();
        }
        /// <summary>Контракты</summary>
        public void Finalize(object state)
        {
            throw new NotImplementedException();
        }
        /// <summary>Контракты</summary>
        public void Process(T element, object state, CancellationToken token)
        {
            throw new NotImplementedException();
        }
        /// <summary>Контракты</summary>
        public bool ProcessThreadException(Exception ex)
        {
            throw new NotImplementedException();
        }
    }
}
