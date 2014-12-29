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
    /// Контейнер для синглтона
    /// </summary>
    public class SingletonLifetime: LifetimeBase
    {
        private readonly object _obj;
        private readonly bool _disposeInnerObject;

        /// <summary>
        /// Конструктор SingletonLifetime
        /// </summary>
        /// <param name="obj">Объект, который будет хранится в контейнере</param>
        public SingletonLifetime(object obj)
            : base(obj.GetType())
        {
            _obj = obj;
            _disposeInnerObject = true;
        }
        /// <summary>
        /// Конструктор SingletonLifetime
        /// </summary>
        /// <param name="obj">Объект, который будет хранится в контейнере</param>
        /// <param name="disposeInnerObject">Вызывать ли Dispose у хранимого объекта при уничтожении контейнера</param>
        public SingletonLifetime(object obj, bool disposeInnerObject)
            : base(obj.GetType())
        {
            _obj = obj;
            _disposeInnerObject = disposeInnerObject;
        }

        /// <summary>
        /// Возвращает объект, которым управляет данный контейнер
        /// </summary>
        /// <param name="resolver">Резолвер инъекций</param>
        /// <returns>Полученный объект</returns>
        public sealed override object GetInstance(IInjectionResolver resolver)
        {
            return _obj;
        }

        /// <summary>
        /// Возвращает объект, которым управляет данный контейнер
        /// </summary>
        /// <returns>Полученный объект</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public object GetInstance()
        {
            return _obj;
        }

        /// <summary>
        /// Внутренний метод освобождения ресурсов
        /// </summary>
        /// <param name="isUserCall">Был ли вызван пользователем (false - вызван деструктором)</param>
        protected override void Dispose(bool isUserCall)
        {
            if (_disposeInnerObject)
            {
                IDisposable objDisp = _obj as IDisposable;
                if (objDisp != null)
                    objDisp.Dispose();
            }

            base.Dispose(isUserCall);
        }
    }
}
