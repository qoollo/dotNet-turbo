using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Helpers;
using Qoollo.Turbo.IoC.Lifetime;

namespace Qoollo.Turbo.IoC.Associations
{
    /// <summary>
    /// Контейнер ассоциаций для IoC с произвольным типом ключа.
    /// Подходит для многопоточных сценариев
    /// </summary>
    /// <typeparam name="TKey">Тип ключа</typeparam>
    [ContractClass(typeof(ConcurrentGenericAssociationContainerCodeContractCheck<>))]
    public abstract class ConcurrentGenericAssociationContainer<TKey> : GenericAssociationContainerBase<TKey>, IAssociationSource<TKey>
    {
        private readonly ConcurrentDictionary<TKey, LifetimeBase> _storage;


        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_storage != null);
        }

        /// <summary>
        /// Конструктор ConcurrentGenericAssociationContainer
        /// </summary>
        public ConcurrentGenericAssociationContainer()
        {
            _storage = new ConcurrentDictionary<TKey, LifetimeBase>();
        }

        /// <summary>
        /// Внутренний метод добавления ассоциации
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Контейнер управления жизнью объекта</param>
        protected sealed override void AddAssociationInner(TKey key, Lifetime.LifetimeBase val)
        {
            _storage[key] = val;
        }
        /// <summary>
        /// Внутренний метод попытки добавить ассоциацию в контейнер
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Контейнер управления жизнью объекта</param>
        /// <returns>Успешность</returns>
        protected sealed override bool TryAddAssociationInner(TKey key, Lifetime.LifetimeBase val)
        {
            return _storage.TryAdd(key, val);
        }
        /// <summary>
        /// Внутренний метод добавления ассоциации в контейнер с использованием фабрики формирования Lifetime контейнера
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип объекта, разрешаемый по ключу</param>
        /// <param name="val">Фабрика созадания Lifetime контейнера</param>
        protected sealed override void AddAssociationInner(TKey key, Type objType, Lifetime.Factories.LifetimeFactory val)
        {
            var lfInf = ProduceResolveInfo(key, objType, val);
            _storage[key] = lfInf;
        }

        /// <summary>
        /// Внутренний метод попытки добавления ассоциации в контейнер с использованием фабрики формирования Lifetime контейнера
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="objType">Тип объекта, разрешаемый по ключу</param>
        /// <param name="val">Фабрика созадания Lifetime контейнера</param>
        /// <returns>Успешность</returns>
        protected sealed override bool TryAddAssociationInner(TKey key, Type objType, Lifetime.Factories.LifetimeFactory val)
        {
            if (_storage.ContainsKey(key))
                return false;

            return _storage.TryAdd(key, ProduceResolveInfo(key, objType, val));
        }


        /// <summary>
        /// Внетренний метод попытки получить ассоциацию
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Контейнер управления жизнью объекта, если удалось получить</param>
        /// <returns>Успешность</returns>
        protected sealed override bool TryGetAssociationInner(TKey key, out Lifetime.LifetimeBase val)
        {
            return _storage.TryGetValue(key, out val);
        }
        /// <summary>
        /// Внутренний метод удаления ассоциации из контейнера
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Удалили ли</returns>
        protected sealed override bool RemoveAssociationInner(TKey key)
        {
            LifetimeBase tmp = null;
            return _storage.TryRemove(key, out tmp);
        }
        /// <summary>
        /// Внутренний метод проверки наличия ассоциации в контейнере
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Есть ли</returns>
        protected sealed override bool ContainsInner(TKey key)
        {
            return _storage.ContainsKey(key);
        }

        /// <summary>
        /// Содержит ли контейнер ассоциацию
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Содержит ли</returns>
        public new bool Contains(TKey key)
        {
            return _storage.ContainsKey(key);
        }

        /// <summary>
        /// Получить ассоциацию в виде контейнера управления жизнью объекта
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Контейнер управления жизнью объекта</returns>
        LifetimeBase IAssociationSource<TKey>.GetAssociation(TKey key)
        {
            return _storage[key];
        }

        /// <summary>
        /// Попытаться получить ассоциацию в виде контейнера управления жизнью объекта
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Контейнер управления жизнью объекта, если удалось получить</param>
        /// <returns>Успешность</returns>
        bool IAssociationSource<TKey>.TryGetAssociation(TKey key, out LifetimeBase val)
        {
            return _storage.TryGetValue(key, out val);
        }

        /// <summary>
        /// Содержит ли контейнер ассоциацию
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Содержит ли</returns>
        bool IAssociationSource<TKey>.Contains(TKey key)
        {
            return _storage.ContainsKey(key);
        }

        /// <summary>
        /// Получить ассоциацию в виде контейнера управления жизнью объекта
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Контейнер управления жизнью объекта</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal LifetimeBase GetAssociation(TKey key)
        {
            return _storage[key];
        }

        /// <summary>
        /// Попытаться получить ассоциацию в виде контейнера управления жизнью объекта
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Контейнер управления жизнью объекта, если удалось получить</param>
        /// <returns>Успешность</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryGetAssociation(TKey key, out LifetimeBase val)
        {
            return _storage.TryGetValue(key, out val);
        }


        /// <summary>
        /// Внутреннее освобождение ресурсов
        /// </summary>
        /// <param name="isUserCall">True - вызвано пользователем, False - вызвано деструктором</param>
        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                List<KeyValuePair<TKey, LifetimeBase>> toDispose = _storage.ToList();

                _storage.Clear();

                if (toDispose != null)
                {
                    foreach (var elem in toDispose)
                    {
                        elem.Value.Dispose();
                    }
                }
            }
            base.Dispose(isUserCall);
        }
    }



    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(ConcurrentGenericAssociationContainer<>))]
    abstract class ConcurrentGenericAssociationContainerCodeContractCheck<T> : ConcurrentGenericAssociationContainer<T>
    {
        /// <summary>Контракты</summary>
        private ConcurrentGenericAssociationContainerCodeContractCheck() { }


        protected override LifetimeBase ProduceResolveInfo(T key, Type objType, Lifetime.Factories.LifetimeFactory val)
        {
            throw new NotImplementedException();
        }

        protected override bool IsGoodTypeForKey(T key, Type objType)
        {
            throw new NotImplementedException();
        }
    }
}
