using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Helpers;
using Qoollo.Turbo.IoC.ServiceStuff;

namespace Qoollo.Turbo.IoC.Lifetime
{
    /// <summary>
    /// Контейнер для хранения копии объекта на каждый поток
    /// </summary>
    public class PerThreadLifetime: LifetimeBase
    {
        private readonly ThreadLocal<object> _obj;
        private readonly Func<IInjectionResolver, object> _createInstFunc;

        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_obj != null);
            Contract.Invariant(_createInstFunc != null);
        }

        /// <summary>
        /// Конструктор PerThreadLifetime
        /// </summary>
        /// <param name="createInstFunc">Функция создания нового объекта</param>
        /// <param name="objType">Тип созаваемого объекта</param>
        public PerThreadLifetime(Func<IInjectionResolver, object> createInstFunc, Type objType)
            : base(objType)
        {
            Contract.Requires<ArgumentNullException>(createInstFunc != null, "createInstFunc");

            _obj = new ThreadLocal<object>(true);
            _createInstFunc = createInstFunc;
        }

        /// <summary>
        /// Возвращает объект, которым управляет данный контейнер
        /// </summary>
        /// <param name="resolver">Резолвер инъекций</param>
        /// <returns>Полученный объект</returns>
        public sealed override object GetInstance(IInjectionResolver resolver)
        {
            if (!_obj.IsValueCreated)
                _obj.Value = _createInstFunc(resolver);

            return _obj.Value;
        }

        /// <summary>
        /// Внутренний метод освобождения ресурсов
        /// </summary>
        /// <param name="isUserCall">Был ли вызван пользователем (false - вызван деструктором)</param>
        protected override void Dispose(bool isUserCall)
        {
            var innerObjects = _obj.Values.ToList();
            _obj.Dispose();

            foreach (var inObj in innerObjects)
            {
                IDisposable inObjDisp = inObj as IDisposable;
                if (inObjDisp != null)
                    inObjDisp.Dispose();
            }

            base.Dispose(isUserCall);
        }
    }
}
