using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC
{
    /// <summary>
    /// Интерфейс локатора объектов
    /// </summary>
    /// <typeparam name="TKey">Тип ключа локатора</typeparam>
    [ContractClass(typeof(IObjectLocatorCodeContractCheck<>))]
    public interface IObjectLocator<in TKey>
    {
        /// <summary>
        /// Получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Полученный объект</returns>
        object Resolve(TKey key);
        /// <summary>
        /// Попытаться получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Объект, если удалось получить</param>
        /// <returns>Успешность</returns>
        bool TryResolve(TKey key, out object val);
        /// <summary>
        /// Можно ли получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Можно ли</returns>
        bool CanResolve(TKey key);
    }


    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(IObjectLocator<>))]
    abstract class IObjectLocatorCodeContractCheck<T> : IObjectLocator<T>
    {
        /// <summary>Контракты</summary>
        private IObjectLocatorCodeContractCheck() { }


        public object Resolve(T key)
        {
            Contract.Requires((object)key != null);

            throw new NotImplementedException();
        }

        public bool TryResolve(T key, out object val)
        {
            Contract.Requires((object)key != null);
            Contract.Ensures(Contract.Result<bool>() == true || (Contract.Result<bool>() == false && Contract.ValueAtReturn<object>(out val) == null));

            throw new NotImplementedException();
        }

        public bool CanResolve(T key)
        {
            Contract.Requires((object)key != null);

            throw new NotImplementedException();
        }
    }
}
