using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC.Injections
{
    /// <summary>
    /// Контейнер инъекций с произвольным типом ключа
    /// </summary>
    /// <typeparam name="TKey">Тип ключа</typeparam>
    [ContractClass(typeof(GenericInjectionContainerBaseCodeContractCheck<>))]
    public abstract class GenericInjectionContainerBase<TKey> : IInjectionSource<TKey>, IFreezable, IDisposable
    {
        private volatile bool _isFrozen = false;
        private volatile bool _isDisposed = false;

        /// <summary>
        /// Внутренний метод попытки извлечь инъекцию
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Полученное значение в случа успеха</param>
        /// <returns>Удалось ли извлечь</returns>
        protected abstract bool TryGetInjectionInner(TKey key, out object val);
        /// <summary>
        /// Внутренний метод проверки наличия инъекции
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Есть ли инъекция в контейнере</returns>
        [Pure]
        protected abstract bool ContainsInner(TKey key);

        /// <summary>
        /// Внутренний метод добавления инъекции в контейнер
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Значение</param>
        protected abstract void AddInjectionInner(TKey key, object val);
        /// <summary>
        /// Внутренний метод попытки добавить инъекцию в контейнер
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Значение</param>
        /// <returns>Успешность</returns>
        protected abstract bool TryAddInjectionInner(TKey key, object val);
        /// <summary>
        /// Внутренний метод удаления инъекции
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Удалили ли инъекцию</returns>
        protected abstract bool RemoveInjectionInner(TKey key);


        /// <summary>
        /// Проверяет состояние контейнера при выполнении какого-либо действия.
        /// Если состояние не соостветствует действию, то выбрасывается исключение
        /// </summary>
        /// <param name="onEdit">Действие делает изменения в контейнере</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void CheckContainerState(bool onEdit)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("GenericInjectionBuilderBase");

            if (onEdit && _isFrozen)
                throw new ObjectFrozenException("GenericInjectionBuilderBase is frozen");
        }

        /// <summary>
        /// Проверяет состояние контейнера при выполнении какого-либо действия.
        /// Сообщает, возможно ли выполнить данное действие.
        /// </summary>
        /// <param name="onEdit">Действие делает изменения в контейнере</param>
        /// <returns>Можно ли выполнить действие</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool CheckContainerStateBool(bool onEdit)
        {
            return !(_isDisposed || (onEdit && _isFrozen));
        }

        /// <summary>
        /// Получение инъекции по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Инъекция</returns>
        /// <exception cref="InjectionIoCException">При отсутствии ключа</exception>
        /// <exception cref="ObjectDisposedException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object GetInjection(TKey key)
        {
            Contract.Requires((object)key != null);

            CheckContainerState(false);

            object res = null;
            if (!TryGetInjectionInner(key, out res))
                throw new KeyNotFoundException("Injection in GenericInjectionBuilderBase not found");

            return res;
        }
        /// <summary>
        /// Пытается получить инъекцию по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Значение, если найдено</param>
        /// <returns>Удалось ли получить значение</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetInjection(TKey key, out object val)
        {
            Contract.Requires((object)key != null);

            if (_isDisposed)
            {
                val = null;
                return false;
            }

            return TryGetInjectionInner(key, out val);
        }

        /// <summary>
        /// Содержит ли контейнер инъекцию
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Есть или нет</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(TKey key)
        {
            Contract.Requires((object)key != null);

            if (_isDisposed)
                return false;

            return ContainsInner(key);
        }

        /// <summary>
        /// Добавляет инъекцию в контейнер. Если она уже там есть, то перезаписывает.
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Значение</param>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ObjectFrozenException"></exception>
        public void AddInjection(TKey key, object val)
        {
            Contract.Requires((object)key != null);
            Contract.Ensures(this.ContainsInner(key));

            CheckContainerState(true);

            AddInjectionInner(key, val);
        }

        /// <summary>
        /// Пытается добавить инъекцию.
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Значение</param>
        /// <returns>True если удалось, False - если инъекция с таким ключём уже была, либо если неверное состояние контейнера</returns>
        public bool TryAddInjection(TKey key, object val)
        {
            Contract.Requires((object)key != null);
            Contract.Ensures(Contract.Result<bool>() == false || this.ContainsInner(key));

            if (_isDisposed || _isFrozen)
                return false;

            return TryAddInjectionInner(key, val);
        }

        /// <summary>
        /// Удаляет инъекцию из контейнера
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Произошло ли удаление</returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ObjectFrozenException"></exception>
        public bool RemoveInjection(TKey key)
        {
            Contract.Requires((object)key != null);
            Contract.Ensures(!this.ContainsInner(key));

            CheckContainerState(true);

            return RemoveInjectionInner(key);
        }


        /// <summary>
        /// Получение инъекции по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Инъекция</returns>
        /// <exception cref="InjectionIoCException">При отсутствии ключа</exception>
        /// <exception cref="ObjectDisposedException"></exception>
        object IInjectionSource<TKey>.GetInjection(TKey key)
        {
            return GetInjection(key);
        }

        /// <summary>
        /// Пытается получить инъекцию по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Значение, если найдено</param>
        /// <returns>Удалось ли получить значение</returns>
        bool IInjectionSource<TKey>.TryGetInjection(TKey key, out object val)
        {
            return TryGetInjection(key, out val);
        }

        /// <summary>
        /// Содержит ли контейнер инъекцию
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Есть или нет</returns>
        bool IInjectionSource<TKey>.Contains(TKey key)
        {
            return Contains(key);
        }


        /// <summary>
        /// Заморозить контейнер инъекций
        /// </summary>
        public void Freeze()
        {
            _isFrozen = true;
        }

        /// <summary>
        /// Заморожен ли контейнер инъекций
        /// </summary>
        public bool IsFrozen
        {
            get { return _isFrozen; }
        }

        /// <summary>
        /// Освобождены ли ресурсы контейнера инъекций
        /// </summary>
        protected bool IsDisposed
        {
            get { return _isDisposed; }
        }

        /// <summary>
        /// Внутреннее освобождение ресурсов
        /// </summary>
        /// <param name="isUserCall">True - вызвано пользователем, False - вызвано деструктором</param>
        protected virtual void Dispose(bool isUserCall)
        {
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
    }



    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(GenericInjectionContainerBase<>))]
    abstract class GenericInjectionContainerBaseCodeContractCheck<T> : GenericInjectionContainerBase<T>
    {
        /// <summary>Контракты</summary>
        private GenericInjectionContainerBaseCodeContractCheck() { }

        /// <summary>Контракты</summary>
        protected override bool TryGetInjectionInner(T key, out object val)
        {
            Contract.Requires((object)key != null);

            throw new NotImplementedException();
        }

        /// <summary>Контракты</summary>
        protected override bool ContainsInner(T key)
        {
            Contract.Requires((object)key != null);

            throw new NotImplementedException();
        }

        /// <summary>Контракты</summary>
        protected override void AddInjectionInner(T key, object val)
        {
            Contract.Requires((object)key != null);

            throw new NotImplementedException();
        }

        /// <summary>Контракты</summary>
        protected override bool TryAddInjectionInner(T key, object val)
        {
            Contract.Requires((object)key != null);

            throw new NotImplementedException();
        }

        /// <summary>Контракты</summary>
        protected override bool RemoveInjectionInner(T key)
        {
            Contract.Requires((object)key != null);

            throw new NotImplementedException();
        }
    }
}
