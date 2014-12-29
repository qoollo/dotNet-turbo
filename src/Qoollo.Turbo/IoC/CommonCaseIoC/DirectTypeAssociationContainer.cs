using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.ServiceStuff;

namespace Qoollo.Turbo.IoC.Associations
{
    /// <summary>
    /// Контейнер ассоциаций, в котором ключ - тип, являющийся базовым для типа значения (делает разрешение по иерархии наследования)
    /// </summary>
    public class DirectTypeAssociationContainer: TypeStrictAssociationContainer
    {
        private readonly IInjectionResolver _injResolver;

        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_injResolver != null);
        }

        /// <summary>
        /// Конструктор DirectTypeAssociationContainer
        /// </summary>
        /// <param name="injResolver">Резолвер инъекций</param>
        public DirectTypeAssociationContainer(IInjectionResolver injResolver)
        {
            Contract.Requires<ArgumentNullException>(injResolver != null);

            _injResolver = injResolver;
        }

        /// <summary>
        /// Сформировать Lifetime контейнер по типу и фабрике 
        /// </summary>
        /// <param name="key">Ключ, по которому будет сохранён контейнер</param>
        /// <param name="objType">Тип объекта, который будет обрабатывать Lifetime контейнер</param>
        /// <param name="val">Фабрика для создания Lifetime контейнера</param>
        /// <returns>Lifetime контейнер</returns>
        protected override Lifetime.LifetimeBase ProduceResolveInfo(Type key, Type objType, Lifetime.Factories.LifetimeFactory val)
        {
            return val.Create(objType, _injResolver, null);
        }

        /// <summary>
        /// Подходит ли переданный тип для заданного ключа
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип объекта</param>
        /// <returns>Подходит ли</returns>
        protected override bool IsGoodTypeForKey(Type key, Type objType)
        {
            return (key == objType) || (key.IsAssignableFrom(objType));
        }


        /// <summary>
        /// Добавить синглтон
        /// </summary>
        /// <typeparam name="TType">Тип синглтона</typeparam>
        /// <param name="val">Значение синглтона</param>
        public void AddSingleton<TType>(TType val)
        {
            Contract.Requires<ArgumentNullException>((object)val != null);

            base.AddSingleton(typeof(TType), (object)val);
        }

        /// <summary>
        /// Добавить синглтон
        /// </summary>
        /// <typeparam name="TType">Тип синглтона</typeparam>
        /// <param name="val">Значение синглтона</param>
        /// <param name="disposeWithContainer">Освобождать ли объект синглтона вместе с контейнером</param>
        public void AddSingleton<TType>(TType val, bool disposeWithContainer)
        {
            Contract.Requires<ArgumentNullException>((object)val != null);

            base.AddSingleton(typeof(TType), (object)val, disposeWithContainer);
        }

        /// <summary>
        /// Попытаться добавить синглтон
        /// </summary>
        /// <typeparam name="TType">Тип синглтона</typeparam>
        /// <param name="val">Значение синглтона</param>
        /// <returns>Успешность</returns>
        public bool TryAddSingleton<TType>(TType val)
        {
            Contract.Requires<ArgumentNullException>((object)val != null);

            return base.TryAddSingleton(typeof(TType), (object)val);
        }

        /// <summary>
        /// Попытаться добавить синглтон
        /// </summary>
        /// <typeparam name="TType">Тип синглтона</typeparam>
        /// <param name="val">Значение синглтона</param>
        /// <param name="disposeWithContainer">Освобождать ли объект синглтона вместе с контейнером</param>
        /// <returns>Успешность</returns>
        public bool TryAddSingleton<TType>(TType val, bool disposeWithContainer)
        {
            Contract.Requires<ArgumentNullException>((object)val != null);

            return base.TryAddSingleton(typeof(TType), (object)val, disposeWithContainer);
        }



        /// <summary>
        /// Добавить ассоциацию
        /// </summary>
        /// <typeparam name="TKey">Тип, используемый в качестве ключа</typeparam>
        /// <param name="lifetimeContainer">Lifetime контейнер</param>
        public void AddAssociation<TKey>(Lifetime.LifetimeBase lifetimeContainer)
        {
            Contract.Requires<ArgumentNullException>(lifetimeContainer != null);

            base.AddAssociation(typeof(TKey), lifetimeContainer);
        }

        /// <summary>
        /// Добавить ассоциацию
        /// </summary>
        /// <typeparam name="TKey">Тип, используемый в качестве ключа</typeparam>
        /// <param name="objType">Тип объекта, которым будет управлять Lifetime контейнер</param>
        /// <param name="factory">Фабрика</param>
        public void AddAssociation<TKey>(Type objType, Lifetime.Factories.LifetimeFactory factory)
        {
            Contract.Requires<ArgumentNullException>(objType != null);
            Contract.Requires<ArgumentNullException>(factory != null);

            base.AddAssociation(typeof(TKey), objType, factory);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию
        /// </summary>
        /// <typeparam name="TKey">Тип, используемый в качестве ключа</typeparam>
        /// <param name="lifetimeContainer">Lifetime контейнер</param>
        /// <returns>Успешность</returns>
        public bool TryAddAssociation<TKey>(Lifetime.LifetimeBase lifetimeContainer)
        {
            Contract.Requires<ArgumentNullException>(lifetimeContainer != null);

            return base.TryAddAssociation(typeof(TKey), lifetimeContainer);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию
        /// </summary>
        /// <typeparam name="TKey">Тип, используемый в качестве ключа</typeparam>
        /// <param name="objType">Тип объекта, которым будет управлять Lifetime контейнер</param>
        /// <param name="factory">Фабрика</param>
        /// <returns>Успешность</returns>
        public bool TryAddAssociation<TKey>(Type objType, Lifetime.Factories.LifetimeFactory factory)
        {
            Contract.Requires<ArgumentNullException>(objType != null);
            Contract.Requires<ArgumentNullException>(factory != null);

            return base.TryAddAssociation(typeof(TKey), objType, factory);
        }

        /// <summary>
        /// Добавить ассоциацию для заданного типа и фабрики создания Lifetime контейнера
        /// </summary>
        /// <typeparam name="TKey">Тип, используемый в качестве ключа</typeparam>
        /// <typeparam name="TValue">Тип объекта, которым будет управлять Lifetime контейнер</typeparam>
        /// <param name="factory">Фабрика</param>
        public void AddAssociation<TKey, TValue>(Lifetime.Factories.LifetimeFactory factory)
        {
            Contract.Requires<ArgumentNullException>(factory != null);

            base.AddAssociation(typeof(TKey), typeof(TValue), factory);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию для заданного типа и фабрики создания Lifetime контейнера
        /// </summary>
        /// <typeparam name="TKey">Тип, используемый в качестве ключа</typeparam>
        /// <typeparam name="TValue">Тип объекта, которым будет управлять Lifetime контейнер</typeparam>
        /// <param name="factory">Фабрика</param>
        /// <returns>Успешность</returns>
        public bool TryAddAssociation<TKey, TValue>(Lifetime.Factories.LifetimeFactory factory)
        {
            Contract.Requires<ArgumentNullException>(factory != null);

            return base.TryAddAssociation(typeof(TKey), typeof(TValue), factory);
        }









        /// <summary>
        /// Добавить ассоциацию типа 'синглтон'
        /// </summary>
        /// <typeparam name="TKey">Тип, используемый в качестве ключа</typeparam>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        public void AddSingleton<TKey>(Type objType)
        {
            Contract.Requires<ArgumentNullException>(objType != null);

            base.AddAssociation(typeof(TKey), objType, LifetimeFactories.Singleton);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию типа 'синглтон'
        /// </summary>
        /// <typeparam name="TKey">Тип, используемый в качестве ключа</typeparam>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        /// <returns>Успешность</returns>
        public bool TryAddSingleton<TKey>(Type objType)
        {
            Contract.Requires<ArgumentNullException>(objType != null);

            return base.TryAddAssociation(typeof(TKey), objType, LifetimeFactories.Singleton);
        }

        /// <summary>
        /// Добавить ассоциацию типа 'синглтон'
        /// </summary>
        /// <typeparam name="TKey">Тип ключа</typeparam>
        /// <typeparam name="TValue">Тип инстанцируемого объекта</typeparam>
        public void AddSingleton<TKey, TValue>()
            where TValue : TKey
        {
            base.AddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.Singleton);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию типа 'синглтон'
        /// </summary>
        /// <typeparam name="TKey">Тип ключа</typeparam>
        /// <typeparam name="TValue">Тип инстанцируемого объекта</typeparam>
        /// <returns>Успешность</returns>
        public bool TryAddSingleton<TKey, TValue>()
            where TValue : TKey
        {
            return base.TryAddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.Singleton);
        }

        /// <summary>
        /// Добавить ассоциацию типа 'отложенный синглтон'
        /// </summary>
        /// <typeparam name="TKey">Тип, используемый в качестве ключа</typeparam>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        public void AddDeferedSingleton<TKey>(Type objType)
        {
            Contract.Requires<ArgumentNullException>(objType != null);

            base.AddAssociation(typeof(TKey), objType, LifetimeFactories.DeferedSingleton);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию типа 'отложенный синглтон'
        /// </summary>
        /// <typeparam name="TKey">Тип, используемый в качестве ключа</typeparam>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        /// <returns>Успешность</returns>
        public bool TryAddDeferedSingleton<TKey>(Type objType)
        {
            Contract.Requires<ArgumentNullException>(objType != null);

            return base.TryAddAssociation(typeof(TKey), objType, LifetimeFactories.DeferedSingleton);
        }

        /// <summary>
        /// Добавить ассоциацию типа 'отложенный синглтон'
        /// </summary>
        /// <typeparam name="TKey">Тип ключа</typeparam>
        /// <typeparam name="TValue">Тип инстанцируемого объекта</typeparam>
        public void AddDeferedSingleton<TKey, TValue>()
            where TValue : TKey
        {
            base.AddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.DeferedSingleton);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию типа 'отложенный синглтон'
        /// </summary>
        /// <typeparam name="TKey">Тип ключа</typeparam>
        /// <typeparam name="TValue">Тип инстанцируемого объекта</typeparam>
        /// <returns>Успешность</returns>
        public bool TryAddDeferedSingleton<TKey, TValue>()
            where TValue : TKey
        {
            return base.TryAddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.DeferedSingleton);
        }

        /// <summary>
        /// Добавить ассоциацию типа 'экземпляр на поток'
        /// </summary>
        /// <typeparam name="TKey">Тип, используемый в качестве ключа</typeparam>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        public void AddPerThread<TKey>(Type objType)
        {
            Contract.Requires<ArgumentNullException>(objType != null);

            base.AddAssociation(typeof(TKey), objType, LifetimeFactories.PerThread);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию типа 'экземпляр на поток'
        /// </summary>
        /// <typeparam name="TKey">Тип, используемый в качестве ключа</typeparam>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        /// <returns>Успешность</returns>
        public bool TryAddPerThread<TKey>(Type objType)
        {
            Contract.Requires<ArgumentNullException>(objType != null);

            return base.TryAddAssociation(typeof(TKey), objType, LifetimeFactories.PerThread);
        }

        /// <summary>
        /// Добавить ассоциацию типа 'экземпляр на поток'
        /// </summary>
        /// <typeparam name="TKey">Тип ключа</typeparam>
        /// <typeparam name="TValue">Тип инстанцируемого объекта</typeparam>
        public void AddPerThread<TKey, TValue>()
            where TValue : TKey
        {
            base.AddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.PerThread);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию типа 'экземпляр на поток'
        /// </summary>
        /// <typeparam name="TKey">Тип ключа</typeparam>
        /// <typeparam name="TValue">Тип инстанцируемого объекта</typeparam>
        /// <returns>Успешность</returns>
        public bool TryAddPerThread<TKey, TValue>()
            where TValue : TKey
        {
            return base.TryAddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.PerThread);
        }

        /// <summary>
        /// Добавить ассоциацию типа 'экземпляр на каждый вызов'
        /// </summary>
        /// <typeparam name="TKey">Тип, используемый в качестве ключа</typeparam>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        public void AddPerCall<TKey>(Type objType)
        {
            Contract.Requires<ArgumentNullException>(objType != null);

            base.AddAssociation(typeof(TKey), objType, LifetimeFactories.PerCall);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию типа 'экземпляр на каждый вызов'
        /// </summary>
        /// <typeparam name="TKey">Тип, используемый в качестве ключа</typeparam>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        /// <returns>Успешность</returns>
        public bool TryAddPerCall<TKey>(Type objType)
        {
            Contract.Requires<ArgumentNullException>(objType != null);

            return base.TryAddAssociation(typeof(TKey), objType, LifetimeFactories.PerCall);
        }

        /// <summary>
        /// Добавить ассоциацию типа 'экземпляр на каждый вызов'
        /// </summary>
        /// <typeparam name="TKey">Тип ключа</typeparam>
        /// <typeparam name="TValue">Тип инстанцируемого объекта</typeparam>
        public void AddPerCall<TKey, TValue>()
            where TValue : TKey
        {
            base.AddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.PerCall);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию типа 'экземпляр на каждый вызов'
        /// </summary>
        /// <typeparam name="TKey">Тип ключа</typeparam>
        /// <typeparam name="TValue">Тип инстанцируемого объекта</typeparam>
        /// <returns>Успешность</returns>
        public bool TryAddPerCall<TKey, TValue>()
            where TValue : TKey
        {
            return base.TryAddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.PerCall);
        }

        /// <summary>
        /// Добавить ассоциацию типа 'экземпляр на каждый вызов с зашитыми параметрами инстанцирования'
        /// </summary>
        /// <typeparam name="TKey">Тип, используемый в качестве ключа</typeparam>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        public void AddPerCallInlinedParams<TKey>(Type objType)
        {
            Contract.Requires<ArgumentNullException>(objType != null);

            base.AddAssociation(typeof(TKey), objType, LifetimeFactories.PerCallInlinedParams);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию типа 'экземпляр на каждый вызов с зашитыми параметрами инстанцирования'
        /// </summary>
        /// <typeparam name="TKey">Тип, используемый в качестве ключа</typeparam>
        /// <param name="objType">Тип инстанцируемого объекта</param>
        /// <returns>Успешность</returns>
        public bool TryAddPerCallInlinedParams<TKey>(Type objType)
        {
            Contract.Requires<ArgumentNullException>(objType != null);

            return base.TryAddAssociation(typeof(TKey), objType, LifetimeFactories.PerCallInlinedParams);
        }

        /// <summary>
        /// Добавить ассоциацию типа 'экземпляр на каждый вызов с зашитыми параметрами инстанцирования'
        /// </summary>
        /// <typeparam name="TKey">Тип ключа</typeparam>
        /// <typeparam name="TValue">Тип инстанцируемого объекта</typeparam>
        public void AddPerCallInlinedParams<TKey, TValue>()
            where TValue : TKey
        {
            base.AddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.PerCallInlinedParams);
        }

        /// <summary>
        /// Попытаться добавить ассоциацию типа 'экземпляр на каждый вызов с зашитыми параметрами инстанцирования'
        /// </summary>
        /// <typeparam name="TKey">Тип ключа</typeparam>
        /// <typeparam name="TValue">Тип инстанцируемого объекта</typeparam>
        /// <returns>Успешность</returns>
        public bool TryAddPerCallInlinedParams<TKey, TValue>()
            where TValue : TKey
        {
            return base.TryAddAssociation(typeof(TKey), typeof(TValue), LifetimeFactories.PerCallInlinedParams);
        }
    }
}
