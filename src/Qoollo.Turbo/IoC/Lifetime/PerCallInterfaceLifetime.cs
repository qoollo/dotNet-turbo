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
    /// Контейнер для создания нового объекта при каждом вызове
    /// </summary>
    public class PerCallInterfaceLifetime: LifetimeBase
    {
        private readonly IInstanceCreator _createInstanceObj;

        /// <summary>
        /// Конструктор PerCallInterfaceLifetime
        /// </summary>
        /// <param name="outType">Тип созаваемого объекта</param>
        /// <param name="createInstanceObj">Интерфейс, который умеет создавать объект</param>
        public PerCallInterfaceLifetime(Type outType, IInstanceCreator createInstanceObj)
            : base(outType)
        {
            Contract.Requires<ArgumentNullException>(createInstanceObj != null, "createInstanceObj");

            _createInstanceObj = createInstanceObj;
        }

        /// <summary>
        /// Возвращает объект, которым управляет данный контейнер
        /// </summary>
        /// <param name="resolver">Резолвер инъекций</param>
        /// <returns>Полученный объект</returns>
        public sealed override object GetInstance(IInjectionResolver resolver)
        {
            return _createInstanceObj.CreateInstance(resolver);
        }
    }
}
