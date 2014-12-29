using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC.ServiceStuff
{
    /// <summary>
    /// Интерфейс для разрешения зависимостей
    /// </summary>
    [ContractClass(typeof(IInjectionResolverCodeContractCheck))]
    public interface IInjectionResolver
    {
        /// <summary>
        /// Разрешить зависимость на основе подробной информации
        /// </summary>
        /// <param name="reqObjectType">Тип объекта, который требуется вернуть</param>
        /// <param name="paramName">Имя параметра, для которого разрешается зависимость (если применимо)</param>
        /// <param name="forType">Тип, для которого разрешается зависимость (если применимо)</param>
        /// <param name="extData">Расширенные данные для разрешения зависимости (если есть)</param>
        /// <returns>Найденный объект запрашиваемого типа</returns>
        object Resolve(Type reqObjectType, string paramName, Type forType, object extData);
        /// <summary>
        /// Упрощённое разрешение зависимости
        /// </summary>
        /// <typeparam name="T">Тип объекта, который требуется вернуть</typeparam>
        /// <param name="forType">Тип, для которого разрешается зависимость</param>
        /// <returns>Найденный объект запрашиваемого типа</returns>
        T Resolve<T>(Type forType);
    }


    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(IInjectionResolver))]
    abstract class IInjectionResolverCodeContractCheck : IInjectionResolver
    {
        /// <summary>Контракты</summary>
        private IInjectionResolverCodeContractCheck() { }


        /// <summary>Контракты</summary>
        public object Resolve(Type reqObjectType, string paramName, Type forType, object extData)
        {
            Contract.Requires(reqObjectType != null);

            throw new NotImplementedException();
        }
        /// <summary>Контракты</summary>
        public T Resolve<T>(Type forType)
        {
            throw new NotImplementedException();
        }
    }
}
