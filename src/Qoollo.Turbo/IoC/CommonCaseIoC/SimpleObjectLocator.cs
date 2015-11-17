using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Associations;
using Qoollo.Turbo.IoC.ServiceStuff;

namespace Qoollo.Turbo.IoC
{
    /// <summary>
    /// Простой локатор объектов
    /// Инъекции имитируются синглтонами
    /// </summary>
    [Obsolete("This container is obsolete. Please, use TurboContainer instead.")]
    public class SimpleObjectLocator : DirectTypeAssociationContainer, IObjectLocator<Type>
    {
        private readonly IInjectionResolver _resolver;

        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_resolver != null);
        }

        /// <summary>
        /// Объект для резолва инъекций
        /// </summary>
        private class InnerInjectionResolver : IInjectionResolver
        {
            private readonly SimpleObjectLocator _locator;

            [ContractInvariantMethod]
            private void Invariant()
            {
                Contract.Invariant(_locator != null);
            }

            /// <summary>
            /// Конструктор InnerInjectionResolver
            /// </summary>
            /// <param name="locator">Локатор</param>
            public InnerInjectionResolver(SimpleObjectLocator locator)
            {
                Contract.Requires(locator != null);

                _locator = locator;
            }

            /// <summary>
            /// Разрешить зависимость на основе подробной информации
            /// </summary>
            /// <param name="reqObjectType">Тип объекта, который требуется вернуть</param>
            /// <param name="paramName">Имя параметра, для которого разрешается зависимость (если применимо)</param>
            /// <param name="forType">Тип, для которого разрешается зависимость (если применимо)</param>
            /// <param name="extData">Расширенные данные для разрешения зависимости (если есть)</param>
            /// <returns>Найденный объект запрашиваемого типа</returns>
            public object Resolve(Type reqObjectType, string paramName, Type forType, object extData)
            {
                return _locator.Resolve(reqObjectType);
            }

            /// <summary>
            /// Упрощённое разрешение зависимости
            /// </summary>
            /// <typeparam name="T">Тип объекта, который требуется вернуть</typeparam>
            /// <param name="forType">Тип, для которого разрешается зависимость</param>
            /// <returns>Найденный объект запрашиваемого типа</returns>
            public T Resolve<T>(Type forType)
            {
                return _locator.Resolve<T>();
            }
        }

        /// <summary>
        /// Конструктор SimpleObjectLocator
        /// </summary>
        public SimpleObjectLocator()
        {
            _resolver = new InnerInjectionResolver(this);
        }

        /// <summary>
        /// Creates a lifetime container by object type and lifetime factory
        /// </summary>
        /// <param name="key">The key that will be used to store lifetime container inside the current AssociationContainer</param>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="val">Factory to create a lifetime container for the sepcified 'objType'</param>
        /// <returns>Created lifetime container</returns>
        protected override Lifetime.LifetimeBase ProduceResolveInfo(Type key, Type objType, Lifetime.Factories.LifetimeFactory val)
        {
            return val.Create(objType, _resolver, null);
        }


        /// <summary>
        /// Получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Полученный объект</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Resolve(Type key)
        {
            Contract.Requires(key != null);

            var life = this.GetAssociation(key);
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
        public bool TryResolve(Type key, out object val)
        {
            Contract.Requires((object)key != null);

            Lifetime.LifetimeBase life = null;

            if (this.TryGetAssociation(key, out life))
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
        public bool CanResolve(Type key)
        {
            Contract.Requires(key != null);

            return this.Contains(key);
        }


        /// <summary>
        /// Получить объект по его типу
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <returns>Полученное значение</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Resolve<T>()
        {
            var life = this.GetAssociation(typeof(T));
            Contract.Assume(life != null);
            return (T)life.GetInstance(_resolver);
        }

        /// <summary>
        /// Попытаться получить объект по его типу
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="val">Полученное значение в случае успеха</param>
        /// <returns>Успешность</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryResolve<T>(out T val)
        {
            Lifetime.LifetimeBase life = null;

            if (this.TryGetAssociation(typeof(T), out life))
            {
                Contract.Assume(life != null);
                object tmp = null;
                if (life.TryGetInstance(_resolver, out tmp))
                {
                    if (tmp is T)
                    {
                        val = (T)tmp;
                        return true;
                    }
                }
            }

            val = default(T);
            return false;
        }


        /// <summary>
        /// Создаёт объект типа T с использованием инъекций
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <returns>Созданный объект</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T CreateObject<T>()
        {
            return InstantiationService.CreateObject<T>(_resolver);
        }

        /// <summary>
        /// Можно ли получить объект по его типу
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <returns>Можно ли</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanResolve<T>()
        {
            return this.Contains(typeof(T));
        }


        /// <summary>
        /// Получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Полученный объект</returns>
        object IObjectLocator<Type>.Resolve(Type key)
        {
            return this.Resolve(key);
        }

        /// <summary>
        /// Попытаться получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Объект, если удалось получить</param>
        /// <returns>Успешность</returns>
        bool IObjectLocator<Type>.TryResolve(Type key, out object val)
        {
            return this.TryResolve(key, out val);
        }

        /// <summary>
        /// Можно ли получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Можно ли</returns>
        bool IObjectLocator<Type>.CanResolve(Type key)
        {
            return this.CanResolve(key);
        }
    }
}
