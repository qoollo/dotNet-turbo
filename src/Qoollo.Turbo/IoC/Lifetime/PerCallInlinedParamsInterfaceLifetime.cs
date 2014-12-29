using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.ServiceStuff;

namespace Qoollo.Turbo.IoC.Lifetime
{
    /// <summary>
    /// Контейнер для создания нового объекта при каждом вызове. 
    /// Параметры зашиты и не выбираются каждый раз из резолвера инъекций
    /// </summary>
    public class PerCallInlinedParamsInterfaceLifetime : LifetimeBase
    {
        private readonly IInstanceCreatorNoParam _createInstanceObj;

        /// <summary>
        /// Конструктор PerCallInlinedParamsInterfaceLifetime
        /// </summary>
        /// <param name="outType">Тип созаваемого объекта</param>
        /// <param name="createInstObj">Интерфейс, который умеет создавать объект</param>
        public PerCallInlinedParamsInterfaceLifetime(Type outType, IInstanceCreatorNoParam createInstObj)
            : base(outType)
        {
            Contract.Requires<ArgumentNullException>(createInstObj != null, "createInstObj");

            _createInstanceObj = createInstObj;
        }

        /// <summary>
        /// Возвращает объект, которым управляет данный контейнер
        /// </summary>
        /// <param name="resolver">Резолвер инъекций</param>
        /// <returns>Полученный объект</returns>
        public sealed override object GetInstance(IInjectionResolver resolver)
        {
            return _createInstanceObj.CreateInstance();
        }
    }
}
