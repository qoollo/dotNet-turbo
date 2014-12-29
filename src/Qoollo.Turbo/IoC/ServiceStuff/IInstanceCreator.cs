using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC.ServiceStuff
{
    /// <summary>
    /// Интерфейс для объекта, который выполняет создание конкретного объекта
    /// </summary>
    [ContractClass(typeof(IInstanceCreatorCodeContractCheck))]
    public interface IInstanceCreator
    {
        /// <summary>
        /// Создаёт объект
        /// </summary>
        /// <param name="resolver">Резолвер зависимостей</param>
        /// <returns>Созданный объект</returns>
        object CreateInstance(IInjectionResolver resolver);
    }

    /// <summary>
    /// Интерфейс для объекта, который выполняет создание конкретного объекта
    /// </summary>
    [ContractClass(typeof(IInstanceCreatorNoParamCodeContractCheck))]
    public interface IInstanceCreatorNoParam
    {
        /// <summary>
        /// Создаёт объект
        /// </summary>
        /// <returns>Созданный объект</returns>
        object CreateInstance();
    }




    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(IInstanceCreator))]
    abstract class IInstanceCreatorCodeContractCheck : IInstanceCreator
    {
        /// <summary>Контракты</summary>
        private IInstanceCreatorCodeContractCheck() { }


        /// <summary>Контракты</summary>
        public object CreateInstance(IInjectionResolver resolver)
        {
            Contract.Requires(resolver != null);
            Contract.Ensures(Contract.Result<object>() != null);

            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(IInstanceCreatorNoParam))]
    abstract class IInstanceCreatorNoParamCodeContractCheck : IInstanceCreatorNoParam
    {
        /// <summary>Контракты</summary>
        private IInstanceCreatorNoParamCodeContractCheck() { }


        /// <summary>Контракты</summary>
        public object CreateInstance()
        {
            Contract.Ensures(Contract.Result<object>() != null);

            throw new NotImplementedException();
        }
    }
}
