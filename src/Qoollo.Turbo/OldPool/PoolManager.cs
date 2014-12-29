using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics.Contracts;

namespace Qoollo.Turbo.OldPool
{
    /// <summary>
    /// Базовый класс для пулов
    /// </summary>
    /// <typeparam name="T">Тип элемента пула</typeparam>
    /// <typeparam name="PE">Тип обёртки для элемента пула</typeparam>
    [ContractClass(typeof(PoolManagerBaseCodeContractCheck<,>))]
    internal abstract class PoolManagerBase<T, PE> : IDisposable
        where T : class
        where PE : PoolElement<T>
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(ElementCount >= 0);
            Contract.Invariant(FreeElementCount >= 0);
        }


        /// <summary>
        /// Общее число элементов
        /// </summary>
        public abstract int ElementCount
        {
            get;
        }

        /// <summary>
        /// Число свободных в данный момент элементов
        /// </summary>
        public abstract int FreeElementCount
        {
            get;
        }

        /// <summary>
        /// Число арендованных элементов в данный момент
        /// </summary>
        public abstract int RentedElementCount
        {
            get;
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <returns>Обёртка над элементом пула</returns>
        /// <exception cref="CantRetrieveElementException">Если не удалось получить элемент</exception>
        public PE Rent()
        {
            return Rent(-1, CancellationToken.None, true);
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="throwOnUnavail">Выбрасывать исключение при недоступности элемента</param>
        /// <returns>Обёртка над элементом пула</returns>
        /// <exception cref="CantRetrieveElementException">Если не удалось получить элемент и throwOnUnavail == true</exception>
        public PE Rent(bool throwOnUnavail)
        {
            return Rent(-1, CancellationToken.None, throwOnUnavail);
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="token">Токен для отмены</param>
        /// <returns>Обёртка над элементом пула</returns>
        /// <exception cref="CantRetrieveElementException">Если не удалось получить элемент</exception>
        public PE Rent(CancellationToken token)
        {
            return Rent(-1, token, true);
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="token">Токен для отмены</param>
        /// <param name="throwOnUnavail">Выбрасывать исключение при недоступности элемента</param>
        /// <returns>Обёртка над элементом пула</returns>
        /// <exception cref="CantRetrieveElementException">Если не удалось получить элемент и throwOnUnavail == true</exception>
        public PE Rent(CancellationToken token, bool throwOnUnavail)
        {
            return Rent(-1, token, throwOnUnavail);
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <returns>Обёртка над элементом пула</returns>
        public PE Rent(int timeout)
        {
            return Rent(timeout, CancellationToken.None, true);
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <param name="throwOnUnavail">Выбрасывать исключение при недоступности элемента</param>
        /// <returns>Обёртка над элементом пула</returns>
        public PE Rent(int timeout, bool throwOnUnavail)
        {
            return Rent(timeout, CancellationToken.None, throwOnUnavail);
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="timeout">Таймаут (-1 - бесконечно, 0 - быстрая проверка, > 0 - полная проверка)</param>
        /// <param name="token">Токен для отмены</param>
        /// <param name="throwOnUnavail">Выбрасывать исключение при недоступности элемента</param>
        /// <returns>Обёртка над элементом пула</returns>
        /// <exception cref="CantRetrieveElementException">Если не удалось получить элемент и throwOnUnavail == true</exception>
        public abstract PE Rent(int timeout, CancellationToken token, bool throwOnUnavail);

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


    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(PoolManagerBase<,>))]
    abstract class PoolManagerBaseCodeContractCheck<T, PE> : PoolManagerBase<T, PE>
        where T : class
        where PE : PoolElement<T>
    {
        /// <summary>Контракты</summary>
        private PoolManagerBaseCodeContractCheck() { }


        /// <summary>Контракты</summary>
        public override int ElementCount
        {
            get { throw new NotImplementedException(); }
        }
        /// <summary>Контракты</summary>
        public override int FreeElementCount
        {
            get { throw new NotImplementedException(); }
        }
        /// <summary>Контракты</summary>
        public override PE Rent(int timeout, CancellationToken token, bool throwOnUnavail)
        {
            Contract.Ensures(Contract.Result<PE>() != null);

            throw new NotImplementedException();
        }

        /// <summary>Контракты</summary>
        public override int RentedElementCount
        {
            get { throw new NotImplementedException(); }
        }
    }
}
