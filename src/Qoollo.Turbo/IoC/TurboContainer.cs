using Qoollo.Turbo.IoC.Associations;
using Qoollo.Turbo.IoC.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC
{
    /// <summary>
    /// IoC container. Stores associations and makes them available to resolve in runtime.
    /// </summary>
    public class TurboContainer : DirectTypeAssociationContainer, IObjectLocator<Type>, IInjectionResolver
    {
        /// <summary>
        /// TurboContainer constructor
        /// </summary>
        public TurboContainer()
        {
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
            return val.Create(objType, this, null);
        }



        /// <summary>
        /// Можно ли получить объект по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Можно ли</returns>
        public bool CanResolve(Type key)
        {
            Contract.Requires(key != null);

            return this.Contains(key);
        }
        /// <summary>
        /// Можно ли получить объект по его типу
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <returns>Можно ли</returns>
        public bool CanResolve<T>()
        {
            return this.Contains(typeof(T));
        }


        private static void ThrowKeyNullException()
        {
            throw new ArgumentNullException("key");
        }
        private static void ThrowObjectCannotBeResolvedException(Type type)
        {
            throw new ObjectCannotBeResolvedException(string.Format("Object of type {0} cannot be resolved by IoC container. Probably this type is not registered.", type));
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
            if (key == null)
                ThrowKeyNullException();

            Lifetime.LifetimeBase life = null;
            if (!this.TryGetAssociation(key, out life))
                ThrowObjectCannotBeResolvedException(key);

            Contract.Assume(life != null);
            return life.GetInstance(this);
        }
        /// <summary>
        /// Получить объект по его типу
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <returns>Полученное значение</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Resolve<T>()
        {
            Lifetime.LifetimeBase life = null;
            if (!this.TryGetAssociation(typeof(T), out life))
                ThrowObjectCannotBeResolvedException(typeof(T));

            Contract.Assume(life != null);
            return (T)life.GetInstance(this);
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
            Contract.Requires(key != null);
            if (key == null)
                ThrowKeyNullException();

            Lifetime.LifetimeBase life = null;

            if (this.TryGetAssociation(key, out life))
            {
                Contract.Assume(life != null);
                val = life.GetInstance(this);
                return true;
            }

            val = null;
            return false;
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
                val = (T)life.GetInstance(this);
                return true;
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
            return InstantiationService.CreateObject<T>(this);
        }



        object IObjectLocator<Type>.Resolve(Type key)
        {
            return this.Resolve(key);
        }

        bool IObjectLocator<Type>.TryResolve(Type key, out object val)
        {
            return this.TryResolve(key, out val);
        }

        bool IObjectLocator<Type>.CanResolve(Type key)
        {
            return this.CanResolve(key);
        }

        object IInjectionResolver.Resolve(Type reqObjectType, string paramName, Type forType, object extData)
        {
            return this.Resolve(reqObjectType);
        }

        T IInjectionResolver.Resolve<T>(Type forType)
        {
            return this.Resolve<T>();
        }
    }
}
