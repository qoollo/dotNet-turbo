using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;

namespace Qoollo.Turbo.Threading.QueueProcessing
{
    /// <summary>
    /// Базовый класс для асинхронных обработчиков
    /// </summary>
    /// <typeparam name="T">Тип обрабатываемого элемента</typeparam>
    public abstract class QueueAsyncProcessorBase<T>: IConsumer<T>, IDisposable
    {
        /// <summary>
        /// Добавить элемент на обработку
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <param name="timeout">Таймаут добавления в миллисекундах</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Успешность (удалось ли добавить до истечения таймаута)</returns>
        public abstract bool Add(T element, int timeout, CancellationToken token);
        /// <summary>
        /// Добавить элемент на обработку
        /// </summary>
        /// <param name="element">Элемент</param>
        public void Add(T element)
        {
            bool result = Add(element, Timeout.Infinite, new CancellationToken());
            Debug.Assert(result);
        }
        /// <summary>
        /// Добавить элемент на обработку
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <param name="token">Токен отмены</param>
        public void Add(T element, CancellationToken token)
        {
            bool result = Add(element, Timeout.Infinite, token);
            Debug.Assert(result);
        }
        /// <summary>
        /// Добавить элемент на обработку
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <param name="timeout">Таймаут добавления в миллисекундах</param>
        /// <returns>Успешность (удалось ли добавить до истечения таймаута)</returns>
        public bool Add(T element, int timeout)
        {
            return Add(element, timeout, new CancellationToken());
        }
        /// <summary>
        /// Попытаться добавить элемент на обработку
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <returns>Удалось ли добавить</returns>
        public bool TryAdd(T element)
        {
            return Add(element, 0, new CancellationToken());
        }



        /// <summary>
        /// Добавить элемент
        /// </summary>
        /// <param name="item">Элемент</param>
        void IConsumer<T>.Add(T item)
        {
            this.Add(item);
        }
        /// <summary>
        /// Попытаться добавить элемент
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Успешность</returns>
        bool IConsumer<T>.TryAdd(T item)
        {
            return this.TryAdd(item);
        }

        /// <summary>
        /// Основной код освобождения ресурсов
        /// </summary>
        /// <param name="isUserCall">Вызвано ли освобождение пользователем. False - деструктор</param>
        protected virtual void Dispose(bool isUserCall)
        { 
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
