using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC.Associations
{
    /// <summary>
    /// Интерфейс для поддержки добавления произвольных Lifetime контейнеров в контейнер ассоциаций
    /// </summary>
    /// <typeparam name="TKey">Тип ключа контейнера ассоциаций</typeparam>
    [ContractClass(typeof(ICustomAssociationSupportCodeContractCheck<>))]
    public interface ICustomAssociationSupport<in TKey>
    {
        /// <summary>
        /// Добавить ассоциацию с заднным Lifetime контейнером
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="lifetimeContainer">Lifetime контейнер</param>
        void AddAssociation(TKey key, Lifetime.LifetimeBase lifetimeContainer);
        /// <summary>
        /// Добавить ассоциацию для заданного типа и фабрики создания Lifetime контейнера
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип объекта, которым будет управлять Lifetime контейнер</param>
        /// <param name="factory">Фабрика</param>
        void AddAssociation(TKey key, Type objType, Lifetime.Factories.LifetimeFactory factory);

        /// <summary>
        /// Попытаться добавить ассоциацию с заднным Lifetime контейнером
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="lifetimeContainer">Lifetime контейнер</param>
        /// <returns>Успешность</returns>
        bool TryAddAssociation(TKey key, Lifetime.LifetimeBase lifetimeContainer);
        /// <summary>
        /// Попытаться добавить ассоциацию для заданного типа и фабрики создания Lifetime контейнера
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип объекта, которым будет управлять Lifetime контейнер</param>
        /// <param name="factory">Фабрика</param>
        /// <returns>Успешность</returns>
        bool TryAddAssociation(TKey key, Type objType, Lifetime.Factories.LifetimeFactory factory);
    }




    /// <summary>
    /// Интерфейс поддержки добавления синглтонов в контейнер ассоциаций
    /// </summary>
    /// <typeparam name="TKey">Тип ключа контейнера ассоциаций</typeparam>
    [ContractClass(typeof(ISingletonAssociationSupportCodeContractCheck<>))]
    public interface ISingletonAssociationSupport<in TKey>
    {
        /// <summary>
        /// Добавить синглтон
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип синглтона</param>
        void AddSingleton(TKey key, Type objType);
        /// <summary>
        /// Попытаться добавить синглтон
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип синглтона</param>
        /// <returns>Успешность</returns>
        bool TryAddSingleton(TKey key, Type objType);
    }




    /// <summary>
    /// Интерфейс поддержки добавления синглтонов как уже созданный объектов в контейнер ассоциаций
    /// </summary>
    /// <typeparam name="TKey">Тип ключа контейнера ассоциаций</typeparam>
    [ContractClass(typeof(IDirectSingletonAssociationSupportCodeContractCheck<>))]
    public interface IDirectSingletonAssociationSupport<in TKey>
    {
        /// <summary>
        /// Добавить синглтон
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Объект синглтона</param>
        void AddSingleton(TKey key, object val);
        /// <summary>
        /// Попытаться добавить синглтон
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Объект синглтона</param>
        /// <returns>Успешность</returns>
        bool TryAddSingleton(TKey key, object val);
    }




    /// <summary>
    /// Интерфейс поддержки добавления синглтонов с отложенной инициализацией в контейнер ассоциаций
    /// </summary>
    /// <typeparam name="TKey">Тип ключа контейнера ассоциаций</typeparam>
    [ContractClass(typeof(IDeferedSingletonAssociationSupportCodeContractCheck<>))]
    public interface IDeferedSingletonAssociationSupport<in TKey>
    {
        /// <summary>
        /// Добавить синглтон отложенной инициализации
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип синглтона</param>
        void AddDeferedSingleton(TKey key, Type objType);
        /// <summary>
        /// Попытаться добавить синглтон отложенной инициализации
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип синглтона</param>
        /// <returns>Успешность</returns>
        bool TryAddDeferedSingleton(TKey key, Type objType);
    }




    /// <summary>
    /// Интерфейс поддержки добавления PerThread объектов в контейнер ассоциаций
    /// </summary>
    /// <typeparam name="TKey">Тип ключа контейнера ассоциаций</typeparam>
    [ContractClass(typeof(IPerThreadAssociationSupportCodeContractCheck<>))]
    public interface IPerThreadAssociationSupport<in TKey>
    {
        /// <summary>
        /// Добавить PerThread
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип объекта</param>
        void AddPerThread(TKey key, Type objType);
        /// <summary>
        /// Попытаться добавить PerThread
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип объекта</param>
        /// <returns>Успешность</returns>
        bool TryAddPerThread(TKey key, Type objType);
    }



    /// <summary>
    /// Интерфейс поддержки добавления PerCall объектов в контейнер ассоциаций
    /// </summary>
    /// <typeparam name="TKey">Тип ключа контейнера ассоциаций</typeparam>
    [ContractClass(typeof(IPerCallAssociationSupportCodeContractCheck<>))]
    public interface IPerCallAssociationSupport<in TKey>
    {
        /// <summary>
        /// Добавить PerCall
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип объекта</param>
        void AddPerCall(TKey key, Type objType);
        /// <summary>
        /// Попытаться добавить PerCall
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип объекта</param>
        /// <returns>Успешность</returns>
        bool TryAddPerCall(TKey key, Type objType);
    }


    /// <summary>
    /// Интерфейс поддержки добавления PerCall объектов с зашитыми параметрами созданий в контейнер ассоциаций
    /// </summary>
    /// <typeparam name="TKey">Тип ключа контейнера ассоциаций</typeparam>
    [ContractClass(typeof(IPerCallInlinedParamsAssociationSupportCodeContractCheck<>))]
    public interface IPerCallInlinedParamsAssociationSupport<in TKey>
    {
        /// <summary>
        /// Добавить PerCall с зашитыми параметрами создания
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип объекта</param>
        void AddPerCallInlinedParams(TKey key, Type objType);
        /// <summary>
        /// Попытаться добавить PerCall с зашитыми параметрами создания
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип объекта</param>
        /// <returns>Успешность</returns>
        bool TryAddPerCallInlinedParams(TKey key, Type objType);
    }









    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(ICustomAssociationSupport<>))]
    abstract class ICustomAssociationSupportCodeContractCheck<T> : ICustomAssociationSupport<T>
    {
        /// <summary>Контракты</summary>
        private ICustomAssociationSupportCodeContractCheck() { }


        public void AddAssociation(T key, Lifetime.LifetimeBase lifetimeContainer)
        {
            Contract.Requires<ArgumentNullException>((object)key != null);
            Contract.Requires<ArgumentNullException>(lifetimeContainer != null);

            throw new NotImplementedException();
        }

        public void AddAssociation(T key, Type objType, Lifetime.Factories.LifetimeFactory factory)
        {
            Contract.Requires<ArgumentNullException>((object)key != null);
            Contract.Requires<ArgumentNullException>(objType != null);
            Contract.Requires<ArgumentNullException>(factory != null);

            throw new NotImplementedException();
        }

        public bool TryAddAssociation(T key, Lifetime.LifetimeBase lifetimeContainer)
        {
            Contract.Requires<ArgumentNullException>((object)key != null);
            Contract.Requires<ArgumentNullException>(lifetimeContainer != null);

            throw new NotImplementedException();
        }

        public bool TryAddAssociation(T key, Type objType, Lifetime.Factories.LifetimeFactory factory)
        {
            Contract.Requires<ArgumentNullException>((object)key != null);
            Contract.Requires<ArgumentNullException>(objType != null);
            Contract.Requires<ArgumentNullException>(factory != null);

            throw new NotImplementedException();
        }
    }



    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(ISingletonAssociationSupport<>))]
    abstract class ISingletonAssociationSupportCodeContractCheck<T> : ISingletonAssociationSupport<T>
    {
        /// <summary>Контракты</summary>
        private ISingletonAssociationSupportCodeContractCheck() { }

        public void AddSingleton(T key, Type objType)
        {
            Contract.Requires<ArgumentNullException>((object)key != null);
            Contract.Requires<ArgumentNullException>(objType != null);

            throw new NotImplementedException();
        }

        public bool TryAddSingleton(T key, Type objType)
        {
            Contract.Requires<ArgumentNullException>((object)key != null);
            Contract.Requires<ArgumentNullException>(objType != null);

            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(IDirectSingletonAssociationSupport<>))]
    abstract class IDirectSingletonAssociationSupportCodeContractCheck<T> : IDirectSingletonAssociationSupport<T>
    {
        /// <summary>Контракты</summary>
        private IDirectSingletonAssociationSupportCodeContractCheck() { }

        public void AddSingleton(T key, object val)
        {
            Contract.Requires<ArgumentNullException>((object)key != null);

            throw new NotImplementedException();
        }

        public bool TryAddSingleton(T key, object val)
        {
            Contract.Requires<ArgumentNullException>((object)key != null);

            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(IDeferedSingletonAssociationSupport<>))]
    abstract class IDeferedSingletonAssociationSupportCodeContractCheck<T> : IDeferedSingletonAssociationSupport<T>
    {
        /// <summary>Контракты</summary>
        private IDeferedSingletonAssociationSupportCodeContractCheck() { }

        public void AddDeferedSingleton(T key, Type objType)
        {
            Contract.Requires<ArgumentNullException>((object)key != null);
            Contract.Requires<ArgumentNullException>(objType != null);

            throw new NotImplementedException();
        }

        public bool TryAddDeferedSingleton(T key, Type objType)
        {
            Contract.Requires<ArgumentNullException>((object)key != null);
            Contract.Requires<ArgumentNullException>(objType != null);

            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(IPerThreadAssociationSupport<>))]
    abstract class IPerThreadAssociationSupportCodeContractCheck<T> : IPerThreadAssociationSupport<T>
    {
        /// <summary>Контракты</summary>
        private IPerThreadAssociationSupportCodeContractCheck() { }

        public void AddPerThread(T key, Type objType)
        {
            Contract.Requires<ArgumentNullException>((object)key != null);
            Contract.Requires<ArgumentNullException>(objType != null);

            throw new NotImplementedException();
        }

        public bool TryAddPerThread(T key, Type objType)
        {
            Contract.Requires<ArgumentNullException>((object)key != null);
            Contract.Requires<ArgumentNullException>(objType != null);

            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(IPerCallAssociationSupport<>))]
    abstract class IPerCallAssociationSupportCodeContractCheck<T> : IPerCallAssociationSupport<T>
    {
        /// <summary>Контракты</summary>
        private IPerCallAssociationSupportCodeContractCheck() { }

        public void AddPerCall(T key, Type objType)
        {
            Contract.Requires<ArgumentNullException>((object)key != null);
            Contract.Requires<ArgumentNullException>(objType != null);

            throw new NotImplementedException();
        }

        public bool TryAddPerCall(T key, Type objType)
        {
            Contract.Requires<ArgumentNullException>((object)key != null);
            Contract.Requires<ArgumentNullException>(objType != null);

            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(IPerCallInlinedParamsAssociationSupport<>))]
    abstract class IPerCallInlinedParamsAssociationSupportCodeContractCheck<T> : IPerCallInlinedParamsAssociationSupport<T>
    {
        /// <summary>Контракты</summary>
        private IPerCallInlinedParamsAssociationSupportCodeContractCheck() { }


        public void AddPerCallInlinedParams(T key, Type objType)
        {
            Contract.Requires<ArgumentNullException>((object)key != null);
            Contract.Requires<ArgumentNullException>(objType != null);

            throw new NotImplementedException();
        }

        public bool TryAddPerCallInlinedParams(T key, Type objType)
        {
            Contract.Requires<ArgumentNullException>((object)key != null);
            Contract.Requires<ArgumentNullException>(objType != null);

            throw new NotImplementedException();
        }
    }

}
