using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Helpers;
using Qoollo.Turbo.IoC.Lifetime;

namespace Qoollo.Turbo.IoC.Associations
{
    /// <summary>
    /// Источник ассоциаций для IoC
    /// </summary>
    /// <typeparam name="TKey">Тип ключа для получения ассоциаций</typeparam>
    [ContractClass(typeof(IAssociationSourceCodeContractCheck<>))]
    public interface IAssociationSource<in TKey>
    {
        /// <summary>
        /// Получить ассоциацию в виде контейнера управления жизнью объекта
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Контейнер управления жизнью объекта</returns>
        LifetimeBase GetAssociation(TKey key);
        /// <summary>
        /// Попытаться получить ассоциацию в виде контейнера управления жизнью объекта
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Контейнер управления жизнью объекта, если удалось получить</param>
        /// <returns>Успешность</returns>
        bool TryGetAssociation(TKey key, out LifetimeBase val);
        /// <summary>
        /// Содержит ли контейнер ассоциацию
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Содержит ли</returns>
        bool Contains(TKey key);
    }


    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(IAssociationSource<>))]
    abstract class IAssociationSourceCodeContractCheck<T> : IAssociationSource<T>
    {
        /// <summary>Контракты</summary>
        private IAssociationSourceCodeContractCheck() { }

        /// <summary>Контракты</summary>
        public LifetimeBase GetAssociation(T key)
        {
            Contract.Ensures(Contract.Result<LifetimeBase>() != null);

            throw new NotImplementedException();
        }

        /// <summary>Контракты</summary>
        public bool TryGetAssociation(T key, out LifetimeBase val)
        {
            Contract.Ensures((Contract.Result<bool>() == true && Contract.ValueAtReturn<LifetimeBase>(out val) != null) ||
                (Contract.Result<bool>() == false && Contract.ValueAtReturn<LifetimeBase>(out val) == null));

            throw new NotImplementedException();
        }

        /// <summary>Контракты</summary>
        public bool Contains(T key)
        {
            throw new NotImplementedException();
        }
    }
}
