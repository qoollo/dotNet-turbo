using Qoollo.Turbo.ObjectPools.Common;
using Qoollo.Turbo.ObjectPools.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools
{
    /// <summary>
    /// Пул объектов
    /// </summary>
    /// <typeparam name="TElem">Тип элементов пула</typeparam>
    [ContractClass(typeof(ObjectPoolManagerCodeContract<>))]
    public abstract class ObjectPoolManager<TElem>: IDisposable
    {
        /// <summary>
        /// Общее число элементов
        /// </summary>
        public abstract int ElementCount
        {
            get;
        }

#if DEBUG
        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <returns>Обёртка над элементом пула</returns>
        /// <exception cref="CantRetrieveElementException">Если не удалось получить элемент</exception>
        /// <param name="memberName">Иия метода, в котором произошло получение элемента пула</param>
        /// <param name="sourceFilePath">Путь до файла, в котором произошло получение элемента пула</param>
        /// <param name="sourceLineNumber">Строка файла, в которой произошло получение элемента пула</param>
        public RentedElementMonitor<TElem> Rent([CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            return new RentedElementMonitor<TElem>(RentElement(-1, new CancellationToken(), true), this, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="throwOnUnavail">Выбрасывать исключение при недоступности элемента</param>
        /// <returns>Обёртка над элементом пула</returns>
        /// <exception cref="CantRetrieveElementException">Если не удалось получить элемент и throwOnUnavail == true</exception>
        /// <param name="memberName">Иия метода, в котором произошло получение элемента пула</param>
        /// <param name="sourceFilePath">Путь до файла, в котором произошло получение элемента пула</param>
        /// <param name="sourceLineNumber">Строка файла, в которой произошло получение элемента пула</param>
        public RentedElementMonitor<TElem> Rent(bool throwOnUnavail, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            return new RentedElementMonitor<TElem>(RentElement(-1, new CancellationToken(), throwOnUnavail), this, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="token">Токен для отмены</param>
        /// <returns>Обёртка над элементом пула</returns>
        /// <exception cref="CantRetrieveElementException">Если не удалось получить элемент</exception>
        /// <param name="memberName">Иия метода, в котором произошло получение элемента пула</param>
        /// <param name="sourceFilePath">Путь до файла, в котором произошло получение элемента пула</param>
        /// <param name="sourceLineNumber">Строка файла, в которой произошло получение элемента пула</param>
        public RentedElementMonitor<TElem> Rent(CancellationToken token, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            return new RentedElementMonitor<TElem>(RentElement(-1, token, true), this, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="token">Токен для отмены</param>
        /// <param name="throwOnUnavail">Выбрасывать исключение при недоступности элемента</param>
        /// <returns>Обёртка над элементом пула</returns>
        /// <exception cref="CantRetrieveElementException">Если не удалось получить элемент и throwOnUnavail == true</exception>
        /// <param name="memberName">Иия метода, в котором произошло получение элемента пула</param>
        /// <param name="sourceFilePath">Путь до файла, в котором произошло получение элемента пула</param>
        /// <param name="sourceLineNumber">Строка файла, в которой произошло получение элемента пула</param>
        public RentedElementMonitor<TElem> Rent(CancellationToken token, bool throwOnUnavail, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            return new RentedElementMonitor<TElem>(RentElement(-1, token, throwOnUnavail), this, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <returns>Обёртка над элементом пула</returns>
        /// <param name="memberName">Иия метода, в котором произошло получение элемента пула</param>
        /// <param name="sourceFilePath">Путь до файла, в котором произошло получение элемента пула</param>
        /// <param name="sourceLineNumber">Строка файла, в которой произошло получение элемента пула</param>
        public RentedElementMonitor<TElem> Rent(int timeout, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            return new RentedElementMonitor<TElem>(RentElement(timeout, new CancellationToken(), true), this, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <param name="throwOnUnavail">Выбрасывать исключение при недоступности элемента</param>
        /// <returns>Обёртка над элементом пула</returns>
        /// <param name="memberName">Иия метода, в котором произошло получение элемента пула</param>
        /// <param name="sourceFilePath">Путь до файла, в котором произошло получение элемента пула</param>
        /// <param name="sourceLineNumber">Строка файла, в которой произошло получение элемента пула</param>
        public RentedElementMonitor<TElem> Rent(int timeout, bool throwOnUnavail, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            return new RentedElementMonitor<TElem>(RentElement(timeout, new CancellationToken(), throwOnUnavail), this, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="timeout">Таймаут (-1 - бесконечно, 0 - быстрая проверка, > 0 - полная проверка)</param>
        /// <param name="token">Токен для отмены</param>
        /// <param name="throwOnUnavail">Выбрасывать исключение при недоступности элемента</param>
        /// <returns>Обёртка над элементом пула</returns>
        /// <exception cref="CantRetrieveElementException">Если не удалось получить элемент и throwOnUnavail == true</exception>
        /// <param name="memberName">Иия метода, в котором произошло получение элемента пула</param>
        /// <param name="sourceFilePath">Путь до файла, в котором произошло получение элемента пула</param>
        /// <param name="sourceLineNumber">Строка файла, в которой произошло получение элемента пула</param>
        public RentedElementMonitor<TElem> Rent(int timeout, CancellationToken token, bool throwOnUnavail, [CallerMemberName] string memberName = null, [CallerFilePath] string sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = 0)
        {
            return new RentedElementMonitor<TElem>(RentElement(timeout, token, throwOnUnavail), this, memberName, sourceFilePath, sourceLineNumber);
        }
#else
        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <returns>Обёртка над элементом пула</returns>
        /// <exception cref="CantRetrieveElementException">Если не удалось получить элемент</exception>
        public RentedElementMonitor<TElem> Rent()
        {
            return new RentedElementMonitor<TElem>(RentElement(-1, new CancellationToken(), true), this);
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="throwOnUnavail">Выбрасывать исключение при недоступности элемента</param>
        /// <returns>Обёртка над элементом пула</returns>
        /// <exception cref="CantRetrieveElementException">Если не удалось получить элемент и throwOnUnavail == true</exception>
        public RentedElementMonitor<TElem> Rent(bool throwOnUnavail)
        {
            return new RentedElementMonitor<TElem>(RentElement(-1, new CancellationToken(), throwOnUnavail), this);
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="token">Токен для отмены</param>
        /// <returns>Обёртка над элементом пула</returns>
        /// <exception cref="CantRetrieveElementException">Если не удалось получить элемент</exception>
        public RentedElementMonitor<TElem> Rent(CancellationToken token)
        {
            return new RentedElementMonitor<TElem>(RentElement(-1, token, true), this);
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="token">Токен для отмены</param>
        /// <param name="throwOnUnavail">Выбрасывать исключение при недоступности элемента</param>
        /// <returns>Обёртка над элементом пула</returns>
        /// <exception cref="CantRetrieveElementException">Если не удалось получить элемент и throwOnUnavail == true</exception>
        public RentedElementMonitor<TElem> Rent(CancellationToken token, bool throwOnUnavail)
        {
            return new RentedElementMonitor<TElem>(RentElement(-1, token, throwOnUnavail), this);
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <returns>Обёртка над элементом пула</returns>
        public RentedElementMonitor<TElem> Rent(int timeout)
        {
            return new RentedElementMonitor<TElem>(RentElement(timeout, new CancellationToken(), true), this);
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <param name="throwOnUnavail">Выбрасывать исключение при недоступности элемента</param>
        /// <returns>Обёртка над элементом пула</returns>
        public RentedElementMonitor<TElem> Rent(int timeout, bool throwOnUnavail)
        {
            return new RentedElementMonitor<TElem>(RentElement(timeout, new CancellationToken(), throwOnUnavail), this);
        }

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="timeout">Таймаут (-1 - бесконечно, 0 - быстрая проверка, > 0 - полная проверка)</param>
        /// <param name="token">Токен для отмены</param>
        /// <param name="throwOnUnavail">Выбрасывать исключение при недоступности элемента</param>
        /// <returns>Обёртка над элементом пула</returns>
        /// <exception cref="CantRetrieveElementException">Если не удалось получить элемент и throwOnUnavail == true</exception>
        public RentedElementMonitor<TElem> Rent(int timeout, CancellationToken token, bool throwOnUnavail)
        {
            return new RentedElementMonitor<TElem>(RentElement(timeout, token, throwOnUnavail), this);
        }
#endif

        /// <summary>
        /// Арендовать элемент
        /// </summary>
        /// <param name="timeout">Таймаут (-1 - бесконечно, 0 - быстрая проверка, > 0 - полная проверка)</param>
        /// <param name="token">Токен для отмены</param>
        /// <param name="throwOnUnavail">Выбрасывать исключение при недоступности элемента</param>
        /// <returns>Обёртка над элементом пула</returns>
        protected abstract PoolElementWrapper<TElem> RentElement(int timeout, CancellationToken token, bool throwOnUnavail);

        /// <summary>
        /// Вернуть элемент в пул
        /// </summary>
        /// <param name="element">Элемент</param>
        protected internal abstract void ReleaseElement(PoolElementWrapper<TElem> element);



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





    [ContractClassFor(typeof(ObjectPoolManager<>))]
    internal abstract class ObjectPoolManagerCodeContract<TElem>: ObjectPoolManager<TElem>
    {
        public override int ElementCount
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);

                throw new NotImplementedException();
            }
        }

        protected override PoolElementWrapper<TElem> RentElement(int timeout, CancellationToken token, bool throwOnUnavail)
        {
            throw new NotImplementedException();
        }

        protected internal override void ReleaseElement(PoolElementWrapper<TElem> element)
        {
            Contract.Requires(element != null);

            throw new NotImplementedException();
        }
    }
}
