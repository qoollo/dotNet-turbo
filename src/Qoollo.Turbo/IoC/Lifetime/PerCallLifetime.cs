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
    public class PerCallLifetime : LifetimeBase
    {
        private readonly Func<IInjectionResolver, object> _createInstanceFunc;

        /// <summary>
        /// Конструктор PerCallLifetime
        /// </summary>
        /// <param name="outType">Тип созаваемого объекта</param>
        /// <param name="createInstanceFunc">Функция создания нового объекта</param>
        public PerCallLifetime(Type outType, Func<IInjectionResolver, object> createInstanceFunc)
            : base(outType)
        {
            Contract.Requires<ArgumentNullException>(createInstanceFunc != null, "createInstanceFunc");

            _createInstanceFunc = createInstanceFunc;
        }

        /// <summary>
        /// Возвращает объект, которым управляет данный контейнер
        /// </summary>
        /// <param name="resolver">Резолвер инъекций</param>
        /// <returns>Полученный объект</returns>
        public sealed override object GetInstance(IInjectionResolver resolver)
        {
            return _createInstanceFunc(resolver);
        }
    }
}
