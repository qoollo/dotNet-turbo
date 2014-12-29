using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.QueueProcessing
{
    /// <summary>
    /// Интерфейс помошник для QueueAsyncProcessorExtension
    /// </summary>
    public interface IQueueAsyncProcessorStartStopHelper: IDisposable
    {
        /// <summary>
        /// Запустить QueueAsyncProcessor
        /// </summary>
        void Start();
        /// <summary>
        /// Остановка и освобождение ресурсов
        /// </summary>
        /// <param name="waitForStop">Ожидать остановки</param>
        /// <param name="letFinishProcess">Позволить обработать всю очередь</param>
        /// <param name="completeAdding">Запретить добавление новых элементов</param>
        void Stop(bool waitForStop, bool letFinishProcess, bool completeAdding);
        /// <summary>
        /// Остановка и освобождение ресурсов
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// Расширения для QueueAsyncProcessor
    /// </summary>
    public static class QueueAsyncProcessorExtensions
    {
        /// <summary>
        /// Fluent start
        /// </summary>
        /// <typeparam name="TQueueProc">Тип QueueAsyncProcessor</typeparam>
        /// <param name="proc">Сам обработчик</param>
        /// <returns>Переданный обработчик</returns>
        public static TQueueProc ThenStart<TQueueProc>(this TQueueProc proc) where TQueueProc : IQueueAsyncProcessorStartStopHelper
        {
            Contract.Requires<ArgumentNullException>(proc != null);

            proc.Start();
            return proc;
        }
    }
}
