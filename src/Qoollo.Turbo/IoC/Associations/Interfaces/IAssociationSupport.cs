using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC.Associations
{
    /// <summary>
    /// Interface that indicates the ability to add custom lifetime object containers to the Association Container
    /// </summary>
    /// <typeparam name="TKey">The type of the key in association container</typeparam>
    [ContractClass(typeof(ICustomAssociationSupportCodeContractCheck<>))]
    public interface ICustomAssociationSupport<TKey>
    {
        /// <summary>
        /// Adds a lifetime object container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="lifetimeContainer">Lifetime object container to add</param>
        void AddAssociation(TKey key, Lifetime.LifetimeBase lifetimeContainer);
        /// <summary>
        /// Adds a lifetime object container created by the 'factory' for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="factory">Factory to create a lifetime container for the sepcified 'objType'</param>
        void AddAssociation(TKey key, Type objType, Lifetime.Factories.LifetimeFactory factory);

        /// <summary>
        /// Attempts to add a lifetime object container for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="lifetimeContainer">Lifetime object container to add</param>
        /// <returns>True if AssociationContainer not contains lifetime container with the same key; overwise false</returns>
        bool TryAddAssociation(TKey key, Lifetime.LifetimeBase lifetimeContainer);
        /// <summary>
        /// Attempts to add a lifetime object container created by the 'factory' for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the lifetime container</param>
        /// <param name="factory">Factory to create a lifetime container for the sepcified 'objType'</param>
        /// <returns>True if AssociationContainer not contains lifetime container with the same key; overwise false</returns>
        bool TryAddAssociation(TKey key, Type objType, Lifetime.Factories.LifetimeFactory factory);
    }




    /// <summary>
    /// Interface that indicates the ability to add singleton to the Association Container
    /// </summary>
    /// <typeparam name="TKey">The type of the key in association container</typeparam>
    [ContractClass(typeof(ISingletonAssociationSupportCodeContractCheck<>))]
    public interface ISingletonAssociationSupport<TKey>
    {
        /// <summary>
        /// Adds an object with singleton lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the singleton lifetime container</param>
        void AddSingleton(TKey key, Type objType);
        /// <summary>
        /// Attempts to add an object with singleton lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the singleton lifetime container</param>
        /// <returns>True if AssociationContainer not contains lifetime container with the same key; overwise false</returns>
        bool TryAddSingleton(TKey key, Type objType);
    }




    /// <summary>
    /// Interface that indicates the ability to add already created object as a singleton to the Association Container
    /// </summary>
    /// <typeparam name="TKey">The type of the key in association container</typeparam>
    [ContractClass(typeof(IDirectSingletonAssociationSupportCodeContractCheck<>))]
    public interface IDirectSingletonAssociationSupport<TKey>
    {
        /// <summary>
        /// Adds an object with singleton lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">The object that will be held by the singleton lifetime container</param>
        void AddSingleton(TKey key, object val);
        /// <summary>
        /// Attempts to add an object with singleton lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">The object that will be held by the singleton lifetime container</param>
        /// <returns>True if AssociationContainer not contains lifetime container with the same key; overwise false</returns>
        bool TryAddSingleton(TKey key, object val);
    }




    /// <summary>
    /// Interface that indicates the ability to add a lazily initialized singleton to the Association Container
    /// </summary>
    /// <typeparam name="TKey">The type of the key in association container</typeparam>
    [ContractClass(typeof(IDeferedSingletonAssociationSupportCodeContractCheck<>))]
    public interface IDeferedSingletonAssociationSupport<TKey>
    {
        /// <summary>
        /// Adds an object with lazily initialized singleton lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the defered singleton lifetime container</param>
        void AddDeferedSingleton(TKey key, Type objType);
        /// <summary>
        /// Attempts to add an object with lazily initialized singleton lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the defered singleton lifetime container</param>
        /// <returns>True if AssociationContainer not contains lifetime container with the same key; overwise false</returns>
        bool TryAddDeferedSingleton(TKey key, Type objType);
    }




    /// <summary>
    /// Interface that indicates the ability to add a object created in per thread manner to the Association Container
    /// </summary>
    /// <typeparam name="TKey">The type of the key in association container</typeparam>
    [ContractClass(typeof(IPerThreadAssociationSupportCodeContractCheck<>))]
    public interface IPerThreadAssociationSupport<TKey>
    {
        /// <summary>
        /// Adds an object with per thread lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the PerThread lifetime container</param>
        void AddPerThread(TKey key, Type objType);
        /// <summary>
        /// Attempts to add an object with per thread lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the PerThread lifetime container</param>
        /// <returns>True if AssociationContainer not contains lifetime container with the same key; overwise false</returns>
        bool TryAddPerThread(TKey key, Type objType);
    }



    /// <summary>
    /// Interface that indicates the ability to add a object created in per call manner to the Association Container
    /// </summary>
    /// <typeparam name="TKey">Тип ключа контейнера ассоциаций</typeparam>
    [ContractClass(typeof(IPerCallAssociationSupportCodeContractCheck<>))]
    public interface IPerCallAssociationSupport<TKey>
    {
        /// <summary>
        /// Adds an object with per call lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the PerCall lifetime container</param>
        void AddPerCall(TKey key, Type objType);
        /// <summary>
        /// Attempts to add an object with per call lifetime for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the PerCall lifetime container</param>
        /// <returns>True if AssociationContainer not contains lifetime container with the same key; overwise false</returns>
        bool TryAddPerCall(TKey key, Type objType);
    }


    /// <summary>
    /// Interface that indicates the ability to add a object created in per call manner with inline constructor parameters to the Association Container
    /// </summary>
    /// <typeparam name="TKey">Тип ключа контейнера ассоциаций</typeparam>
    [ContractClass(typeof(IPerCallInlinedParamsAssociationSupportCodeContractCheck<>))]
    public interface IPerCallInlinedParamsAssociationSupport<TKey>
    {
        /// <summary>
        /// Adds an object with per call lifetime with inlined constructor parameters for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the PerCallInlinedParams lifetime container</param>
        void AddPerCallInlinedParams(TKey key, Type objType);
        /// <summary>
        /// Attempts to add an object with per call lifetime with inlined constructor parameters for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="objType">The type of the object that will be held by the PerCallInlinedParams lifetime container</param>
        /// <returns>True if AssociationContainer not contains lifetime container with the same key; overwise false</returns>
        bool TryAddPerCallInlinedParams(TKey key, Type objType);
    }









    /// <summary>
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(ICustomAssociationSupport<>))]
    abstract class ICustomAssociationSupportCodeContractCheck<T> : ICustomAssociationSupport<T>
    {
        /// <summary>Code contracts</summary>
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
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(ISingletonAssociationSupport<>))]
    abstract class ISingletonAssociationSupportCodeContractCheck<T> : ISingletonAssociationSupport<T>
    {
        /// <summary>Code contracts</summary>
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
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(IDirectSingletonAssociationSupport<>))]
    abstract class IDirectSingletonAssociationSupportCodeContractCheck<T> : IDirectSingletonAssociationSupport<T>
    {
        /// <summary>Code contracts</summary>
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
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(IDeferedSingletonAssociationSupport<>))]
    abstract class IDeferedSingletonAssociationSupportCodeContractCheck<T> : IDeferedSingletonAssociationSupport<T>
    {
        /// <summary>Code contracts</summary>
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
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(IPerThreadAssociationSupport<>))]
    abstract class IPerThreadAssociationSupportCodeContractCheck<T> : IPerThreadAssociationSupport<T>
    {
        /// <summary>Code contracts</summary>
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
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(IPerCallAssociationSupport<>))]
    abstract class IPerCallAssociationSupportCodeContractCheck<T> : IPerCallAssociationSupport<T>
    {
        /// <summary>Code contracts</summary>
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
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(IPerCallInlinedParamsAssociationSupport<>))]
    abstract class IPerCallInlinedParamsAssociationSupportCodeContractCheck<T> : IPerCallInlinedParamsAssociationSupport<T>
    {
        /// <summary>Code contracts</summary>
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
