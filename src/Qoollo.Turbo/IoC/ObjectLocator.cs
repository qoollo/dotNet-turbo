using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Associations;
using Qoollo.Turbo.IoC.Injections;
using Qoollo.Turbo.IoC.ServiceStuff;

namespace Qoollo.Turbo.IoC
{
    /// <summary>
    /// Шаблонный класс локатора объектов для упрощения жизни
    /// </summary>
    /// <typeparam name="TInjection">Тип контейнера инъекций</typeparam>
    /// <typeparam name="TInjKey">Тип ключа инъекций</typeparam>
    /// <typeparam name="TAssociation">Тип контейнера ассоциаций</typeparam>
    /// <typeparam name="TAssocKey">Тип ключа ассоциаций</typeparam>
    public abstract class ObjectLocator<TInjection, TInjKey, TAssociation, TAssocKey>: IObjectLocator<TAssocKey>, IDisposable
        where TInjection: IInjectionSource<TInjKey>
        where TAssociation: IAssociationSource<TAssocKey>
    {
        private TInjection _injection;
        private TAssociation _association;
        private IInjectionResolver _resolver;

        /// <summary>
        /// Конструктор ObjectLocator принимающий все необходимые объекты
        /// </summary>
        /// <param name="injection">Контейнер инъекций</param>
        /// <param name="association">Контейнер ассоциаций</param>
        /// <param name="resolver">Резолвер инъекций</param>
        protected ObjectLocator(TInjection injection, TAssociation association, IInjectionResolver resolver)
        {
            Contract.Requires(injection != null);
            Contract.Requires(association != null);
            Contract.Requires(resolver != null);

            _injection = injection;
            _association = association;
            _resolver = resolver;
        }

        /// <summary>
        /// Конструктор ObjectLocator без параметров.
        /// С ним обязателен вызов SetInnerObjects
        /// </summary>
        protected ObjectLocator()
        {
        }

        /// <summary>
        /// Установка значений всех необходимых объектов
        /// </summary>
        /// <param name="injection">Контейнер инъекций</param>
        /// <param name="association">Контейнер ассоциаций</param>
        /// <param name="resolver">Резолвер инъекций</param>
        protected void SetInnerObjects(TInjection injection, TAssociation association, IInjectionResolver resolver)
        {
            Contract.Requires(injection != null);
            Contract.Requires(association != null);
            Contract.Requires(resolver != null);

            _injection = injection;
            _association = association;
            _resolver = resolver;
        }

        /// <summary>
        /// Контейнер инъекций
        /// </summary>
        public TInjection Injection
        {
            get { return _injection; }
        }

        /// <summary>
        /// Контейнер ассоциаций
        /// </summary>
        public TAssociation Association
        {
            get { return _association; }
        }

        /// <summary>
        /// Получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Полученный объект</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Resolve(TAssocKey key)
        {
            Contract.Requires((object)key != null);

            var life = _association.GetAssociation(key);
            Contract.Assume(life != null);
            return life.GetInstance(_resolver);
        }
        /// <summary>
        /// Попытаться получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Объект, если удалось получить</param>
        /// <returns>Успешность</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryResolve(TAssocKey key, out object val)
        {
            Contract.Requires((object)key != null);

            Lifetime.LifetimeBase life = null;

            if (_association.TryGetAssociation(key, out life))
            {
                Contract.Assume(life != null);
                if (life.TryGetInstance(_resolver, out val))
                    return true;
            }

            val = null;
            return false;
        }

        /// <summary>
        /// Можно ли получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Можно ли</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanResolve(TAssocKey key)
        {
            Contract.Requires((object)key != null);

            return Association.Contains(key);
        }

        /// <summary>
        /// Получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Полученный объект</returns>
        object IObjectLocator<TAssocKey>.Resolve(TAssocKey key)
        {
            return this.Resolve(key);
        }

        /// <summary>
        /// Попытаться получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Объект, если удалось получить</param>
        /// <returns>Успешность</returns>
        bool IObjectLocator<TAssocKey>.TryResolve(TAssocKey key, out object val)
        {
            return this.TryResolve(key, out val);
        }

        /// <summary>
        /// Можно ли получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Можно ли</returns>
        bool IObjectLocator<TAssocKey>.CanResolve(TAssocKey key)
        {
            return this.CanResolve(key);
        }

        /// <summary>
        /// Внутреннее освобождение ресурсов
        /// </summary>
        /// <param name="isUserCall">True - вызвано пользователем, False - вызвано деструктором</param>
        protected virtual void Dispose(bool isUserCall)
        {
            if (_association != null && _association is IDisposable)
                (_association as IDisposable).Dispose();

            if (_injection != null && _injection is IDisposable)
                (_injection as IDisposable).Dispose();
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
