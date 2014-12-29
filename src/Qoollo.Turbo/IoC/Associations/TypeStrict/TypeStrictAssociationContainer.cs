using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC.Associations
{
    /// <summary>
    /// Контейнер ассоциаций с ключём типа Type.
    /// Поддерживает многопоточность
    /// </summary>
    public abstract class TypeStrictAssociationContainer : ConcurrentGenericAssociationContainer<Type>,
        ISingletonAssociationSupport<Type>, IDeferedSingletonAssociationSupport<Type>,
        IPerThreadAssociationSupport<Type>, IPerCallAssociationSupport<Type>, IPerCallInlinedParamsAssociationSupport<Type>,
        IDirectSingletonAssociationSupport<Type>, ICustomAssociationSupport<Type>
    {

        /// <summary>
        /// Добавить синглтон
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Объект синглтона</param>
        public void AddSingleton(Type key, object val)
        {
            base.AddAssociation(key, new Lifetime.SingletonLifetime(val));
        }

        /// <summary>
        /// Добавить синглтон
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Объект синглтона</param>
        /// <param name="disposeWithContainer">Освобождать ли объект синглтона вместе с контейнером</param>
        public void AddSingleton(Type key, object val, bool disposeWithContainer)
        {
            Contract.Requires<ArgumentNullException>(key != null);
            Contract.Requires<ArgumentNullException>(val != null);

            base.AddAssociation(key, new Lifetime.SingletonLifetime(val, disposeWithContainer));
        }

        /// <summary>
        /// Попытаться добавить синглтон
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Объект синглтона</param>
        /// <returns>Успешность</returns>
        public bool TryAddSingleton(Type key, object val)
        {
            return base.TryAddAssociation(key, new Lifetime.SingletonLifetime(val));
        }

        /// <summary>
        /// Попытаться добавить синглтон
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Объект синглтона</param>
        /// <param name="disposeWithContainer">Освобождать ли объект синглтона вместе с контейнером</param>
        /// <returns>Успешность</returns>
        public bool TryAddSingleton(Type key, object val, bool disposeWithContainer)
        {
            Contract.Requires<ArgumentNullException>(key != null);
            Contract.Requires<ArgumentNullException>(val != null);

            return base.TryAddAssociation(key, new Lifetime.SingletonLifetime(val, disposeWithContainer));
        }




        /// <summary>
        /// Добавить ассоциацию с заднным Lifetime контейнером
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="lifetimeContainer">Lifetime контейнер</param>
        public new void AddAssociation(Type key, Lifetime.LifetimeBase lifetimeContainer)
        {
            base.AddAssociation(key, lifetimeContainer);
        }

        /// <summary>
        /// Добавить ассоциацию для заданного типа и фабрики создания Lifetime контейнера
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип объекта, которым будет управлять Lifetime контейнер</param>
        /// <param name="factory">Фабрика</param>
        public new void AddAssociation(Type key, Type objType, Lifetime.Factories.LifetimeFactory factory)
        {
            base.AddAssociation(key, objType, factory);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию с заднным Lifetime контейнером
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="lifetimeContainer">Lifetime контейнер</param>
        /// <returns>Успешность</returns>
        public new bool TryAddAssociation(Type key, Lifetime.LifetimeBase lifetimeContainer)
        {
            return base.TryAddAssociation(key, lifetimeContainer);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию для заданного типа и фабрики создания Lifetime контейнера
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип объекта, которым будет управлять Lifetime контейнер</param>
        /// <param name="factory">Фабрика</param>
        /// <returns>Успешность</returns>
        public new bool TryAddAssociation(Type key, Type objType, Lifetime.Factories.LifetimeFactory factory)
        {
            return base.TryAddAssociation(key, objType, factory);
        }


        /// <summary>
        /// Добавить ассоциацию типа 'синглтон'
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        public void AddSingleton(Type key, Type objType)
        {
            base.AddAssociation(key, objType, LifetimeFactories.Singleton);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию типа 'синглтон'
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        /// <returns>Успешность</returns>
        public bool TryAddSingleton(Type key, Type objType)
        {
            return base.TryAddAssociation(key, objType, LifetimeFactories.Singleton);
        }

        /// <summary>
        /// Добавить ассоциацию типа 'отложенный синглтон'
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        public void AddDeferedSingleton(Type key, Type objType)
        {
            base.AddAssociation(key, objType, LifetimeFactories.DeferedSingleton);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию типа 'отложенный синглтон'
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        /// <returns>Успешность</returns>
        public bool TryAddDeferedSingleton(Type key, Type objType)
        {
            return base.TryAddAssociation(key, objType, LifetimeFactories.DeferedSingleton);
        }

        /// <summary>
        /// Добавить ассоциацию типа 'экземпляр на поток'
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        public void AddPerThread(Type key, Type objType)
        {
            base.AddAssociation(key, objType, LifetimeFactories.PerThread);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию типа 'экземпляр на поток'
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        /// <returns>Успешность</returns>
        public bool TryAddPerThread(Type key, Type objType)
        {
            return base.TryAddAssociation(key, objType, LifetimeFactories.PerThread);
        }

        /// <summary>
        /// Добавить ассоциацию типа 'экземпляр на каждый вызов'
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        public void AddPerCall(Type key, Type objType)
        {
            base.AddAssociation(key, objType, LifetimeFactories.PerCall);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию типа 'экземпляр на каждый вызов'
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        /// <returns>Успешность</returns>
        public bool TryAddPerCall(Type key, Type objType)
        {
            return base.TryAddAssociation(key, objType, LifetimeFactories.PerCall);
        }

        /// <summary>
        /// Добавить ассоциацию типа 'экземпляр на каждый вызов с зашитыми параметрами инстанцирования'
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        public void AddPerCallInlinedParams(Type key, Type objType)
        {
            base.AddAssociation(key, objType, LifetimeFactories.PerCallInlinedParams);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию типа 'экземпляр на каждый вызов с зашитыми параметрами инстанцирования'
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        /// <returns>Успешность</returns>
        public bool TryAddPerCallInlinedParams(Type key, Type objType)
        {
            return base.TryAddAssociation(key, objType, LifetimeFactories.PerCallInlinedParams);
        }
    }
}
