using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Collections.Concurrent;
using System.Threading;

namespace Qoollo.Turbo.Threading.QueueProcessing
{
    /// <summary>
    /// Асинхронная обработка на делегатах.
    /// Лучше не использовать, а самостоятельно наследоваться от QueueAsyncProcessor
    /// </summary>
    /// <typeparam name="T">Тип обрабатываемого элемента</typeparam>
    public class DeleageQueueAsyncProcessor<T> : QueueAsyncProcessor<T>
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_processing != null);
        }

        private readonly Action<T, CancellationToken> _processing;
        private readonly Action<Exception> _exceptionProc;


        /// <summary>
        /// Конструктор DeleageQueueAsyncProcessor
        /// </summary>
        /// <param name="processorCount">Число потоков</param>
        /// <param name="maxQueueSize">Максимальный размер очереди</param>
        /// <param name="name">Имя для потоков</param>
        /// <param name="isBackground">Будут ли потоки работать в фоновом режиме</param>
        /// <param name="processing">Делегат обработки элементов</param>
        /// <param name="exceptionAct">Делегат обработки исключений</param>
        public DeleageQueueAsyncProcessor(int processorCount, int maxQueueSize, string name, bool isBackground, Action<T, CancellationToken> processing, Action<Exception> exceptionAct)
            : base(processorCount, maxQueueSize, name, isBackground)
        {
            Contract.Requires<ArgumentNullException>(processing != null, "processing");

            _processing = processing;
            _exceptionProc = exceptionAct;
        }
        /// <summary>
        /// Конструктор DeleageQueueAsyncProcessor
        /// </summary>
        /// <param name="processorCount">Число потоков</param>
        /// <param name="maxQueueSize">Максимальный размер очереди</param>
        /// <param name="name">Имя для потоков</param>
        /// <param name="processing">Делегат обработки элементов</param>
        /// <param name="exceptionAct">Делегат обработки исключений</param>
        public DeleageQueueAsyncProcessor(int processorCount, int maxQueueSize, string name, Action<T, CancellationToken> processing, Action<Exception> exceptionAct)
            : this(processorCount, maxQueueSize, name, false, processing, exceptionAct)
        {
        }
        /// <summary>
        /// Конструктор DeleageQueueAsyncProcessor
        /// </summary>
        /// <param name="processorCount">Число потоков</param>
        /// <param name="maxQueueSize">Максимальный размер очереди</param>
        /// <param name="name">Имя для потоков</param>
        /// <param name="processing">Делегат обработки элементов</param>
        public DeleageQueueAsyncProcessor(int processorCount, int maxQueueSize, string name, Action<T, CancellationToken> processing)
            : this(processorCount, maxQueueSize, name, false, processing, null)
        {
        }
        /// <summary>
        /// Конструктор DeleageQueueAsyncProcessor
        /// </summary>
        /// <param name="processorCount">Число потоков</param>
        /// <param name="name">Имя для потоков</param>
        /// <param name="processing">Делегат обработки элементов</param>
        /// <param name="exceptionAct">Делегат обработки исключений</param>
        public DeleageQueueAsyncProcessor(int processorCount, string name, Action<T, CancellationToken> processing, Action<Exception> exceptionAct)
            : this(processorCount, -1, name, false, processing, exceptionAct)
        {
        }
        /// <summary>
        /// Конструктор DeleageQueueAsyncProcessor
        /// </summary>
        /// <param name="processorCount">Число потоков</param>
        /// <param name="name">Имя для потоков</param>
        /// <param name="processing">Делегат обработки элементов</param>
        public DeleageQueueAsyncProcessor(int processorCount, string name, Action<T, CancellationToken> processing)
            : this(processorCount, -1, name, false, processing, null)
        {
        }
        /// <summary>
        /// Конструктор DeleageQueueAsyncProcessor
        /// </summary>
        /// <param name="processorCount">Число потоков</param>
        /// <param name="processing">Делегат обработки элементов</param>
        public DeleageQueueAsyncProcessor(int processorCount, Action<T, CancellationToken> processing)
            : this(processorCount, -1, null, false, processing, null)
        {
        }

        /// <summary>
        /// Основной метод обработки элементов
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <param name="state">Объект состояния, инициализированный в методе Prepare()</param>
        /// <param name="token">Токен для отмены обработки</param>
        protected override void Process(T element, object state, CancellationToken token)
        {
            _processing(element, token);
        }


        /// <summary>
        /// Обработка исключений. 
        /// Чтобы исключение было проброшено наверх, нужно выбросить новое исключение внутри метода.
        /// </summary>
        /// <param name="ex">Исключение</param>
        /// <returns>Игнорировать ли исключение (false - поток завершает работу)</returns>
        protected override bool ProcessThreadException(Exception ex)
        {
            if (_exceptionProc != null)
                _exceptionProc(ex);

            return base.ProcessThreadException(ex);
        }
    }
}
