using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC.Injections
{
    /// <summary>
    /// Контейнер инъекций, использующий ConcurrentDictionary для хранения.
    /// Подходит для многопоточных сценариев
    /// </summary>
    /// <typeparam name="TKey">Ключ разрешения инъекций</typeparam>
    public class ConcurrentGenericInjectionContainer<TKey> : GenericInjectionContainerBase<TKey>, IInjectionSource<TKey>
    {
        private readonly ConcurrentDictionary<TKey, object> _injections;
        private readonly bool _disposeInjectionsWithBuiler;

        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_injections != null);
        }

        /// <summary>
        /// Конструктор ConcurrentGenericInjectionContainer
        /// </summary>
        /// <param name="disposeInjectionsWithBuilder">Вызывать ли Dispose у хранимых объектов при уничтожении контейнера</param>
        public ConcurrentGenericInjectionContainer(bool disposeInjectionsWithBuilder)
        {
            _injections = new ConcurrentDictionary<TKey, object>();
            _disposeInjectionsWithBuiler = disposeInjectionsWithBuilder;
        }
        /// <summary>
        /// Конструктор ConcurrentGenericInjectionContainer
        /// </summary>
        public ConcurrentGenericInjectionContainer()
            : this(true)
        {
        }

        /// <summary>
        /// Подходит ли данная инъекция для ключа
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="injection">Инъекция</param>
        /// <returns>Подходит ли</returns>
        protected virtual bool IsGoodInjectionForKey(TKey key, object injection)
        {
            return true;
        }

        /// <summary>
        /// Внутренний метод попытки извлечь инъекцию
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Полученное значение в случа успеха</param>
        /// <returns>Удалось ли извлечь</returns>
        protected sealed override bool TryGetInjectionInner(TKey key, out object val)
        {
            return _injections.TryGetValue(key, out val);
        }

        /// <summary>
        /// Внутренний метод проверки наличия инъекции
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Есть ли инъекция в контейнере</returns>
        protected sealed override bool ContainsInner(TKey key)
        {
            return _injections.ContainsKey(key);
        }

        /// <summary>
        /// Внутренний метод добавления инъекции в контейнер
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Значение</param>
        protected sealed override void AddInjectionInner(TKey key, object val)
        {
            if (!IsGoodInjectionForKey(key, val))
                throw new InjectionIoCException(string.Format("Bad injection ({0}) for key ({1})", val, key));

            _injections[key] = val;
        }

        /// <summary>
        /// Внутренний метод попытки добавить инъекцию в контейнер
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Значение</param>
        /// <returns>Успешность</returns>
        protected sealed override bool TryAddInjectionInner(TKey key, object val)
        {
            if (!IsGoodInjectionForKey(key, val))
                return false;

            return _injections.TryAdd(key, val);
        }

        /// <summary>
        /// Внутренний метод удаления инъекции
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Удалили ли инъекцию</returns>
        protected sealed override bool RemoveInjectionInner(TKey key)
        {
            object val = null;
            return _injections.TryRemove(key, out val);
        }


        /// <summary>
        /// Получение инъекции по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Инъекция</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new object GetInjection(TKey key)
        {
            return _injections[key];
        }
        /// <summary>
        /// Пытается получить инъекцию по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Значение, если найдено</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new bool TryGetInjection(TKey key, out object val)
        {
            return _injections.TryGetValue(key, out val);
        }
        /// <summary>
        /// Содержит ли контейнер инъекцию
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Есть или нет</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new bool Contains(TKey key)
        {
            return _injections.ContainsKey(key);
        }


        /// <summary>
        /// Получение инъекции по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Инъекция</returns>
        object IInjectionSource<TKey>.GetInjection(TKey key)
        {
            return _injections[key];
        }
        /// <summary>
        /// Пытается получить инъекцию по ключу
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <param name="val">Значение, если найдено</param>
        bool IInjectionSource<TKey>.TryGetInjection(TKey key, out object val)
        {
            return _injections.TryGetValue(key, out val);
        }

        /// <summary>
        /// Содержит ли контейнер инъекцию
        /// </summary>
        /// <param name="key">Ключ</param>
        /// <returns>Есть или нет</returns>
        bool IInjectionSource<TKey>.Contains(TKey key)
        {
            return _injections.ContainsKey(key);
        }

        /// <summary>
        /// Внутреннее освобождение ресурсов
        /// </summary>
        /// <param name="isUserCall">True - вызвано пользователем, False - вызвано деструктором</param>
        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                List<KeyValuePair<TKey, object>> toDispose = null;
                if (_disposeInjectionsWithBuiler)
                    toDispose = _injections.ToList();

                _injections.Clear();

                if (toDispose != null)
                {
                    foreach (var elem in toDispose)
                    {
                        IDisposable disp = elem.Value as IDisposable;
                        if (disp != null)
                            disp.Dispose();
                    }
                }
            }
            base.Dispose(isUserCall);
        }
    }
}
