using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Helpers;
using Qoollo.Turbo.IoC.ServiceStuff;

namespace Qoollo.Turbo.IoC.Lifetime
{
    /// <summary>
    /// Контейнер для синглтона с отложенным созданием объекта
    /// </summary>
    public class DeferedSingletonLifetime: LifetimeBase
    {
        private readonly Func<IInjectionResolver, object> _createInstanceFunc;
        private volatile object _obj;
        private readonly object _lockObj = new object();
        private volatile bool _isInited;

        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_createInstanceFunc != null);
            Contract.Invariant(_lockObj != null);
        }

        /// <summary>
        /// Конструктор DeferedSingletonLifetime
        /// </summary>
        /// <param name="createInstanceFunc">Функция создания объекта</param>
        /// <param name="objType">Тип создаваемого объекта</param>
        public DeferedSingletonLifetime(Func<IInjectionResolver, object> createInstanceFunc, Type objType)
            : base(objType)
        {
            Contract.Requires<ArgumentNullException>(createInstanceFunc != null, "createInstanceFunc");

            _isInited = false;
            _createInstanceFunc = createInstanceFunc;
        }

        /// <summary>
        /// Возвращает объект, которым управляет данный контейнер
        /// </summary>
        /// <param name="resolver">Резолвер инъекций</param>
        /// <returns>Полученный объект</returns>
        public sealed override object GetInstance(IInjectionResolver resolver)
        {
            if (!_isInited)
            {
                lock (_lockObj)
                {
                    if (!_isInited)
                    {
                        _obj = _createInstanceFunc(resolver);
                        _isInited = true;
                    }
                }
            }
            return _obj;
        }

        /// <summary>
        /// Внутренний метод освобождения ресурсов
        /// </summary>
        /// <param name="isUserCall">Был ли вызван пользователем (false - вызван деструктором)</param>
        protected override void Dispose(bool isUserCall)
        {
            IDisposable objDisp = _obj as IDisposable;
            if (objDisp != null)
                objDisp.Dispose();

            base.Dispose(isUserCall);
        }
    }
}
