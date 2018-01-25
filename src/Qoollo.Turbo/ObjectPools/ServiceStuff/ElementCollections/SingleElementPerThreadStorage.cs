using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools.ServiceStuff.ElementCollections
{
    /// <summary>
    /// Хранилище SingleElementStorage на поток
    /// </summary>
    /// <typeparam name="T">Тип элементов</typeparam>
    [System.Diagnostics.DebuggerTypeProxy(typeof(SingleElementPerThreadStorageDebugView<>))]
    internal class SingleElementPerThreadStorage<T> where T: class
    {
        private readonly ThreadLocal<SingleElementStorage<T>> _perThreadData;
        private readonly object _stackAccessLock;

        private volatile SingleElementStorage<T> _stackHead;
        private volatile int _containerCount;

        /// <summary>
        /// Конструктор SingleElementPerThreadStorage
        /// </summary>
        public SingleElementPerThreadStorage()
        {
            _stackAccessLock = new object();
            _stackHead = null;
            _perThreadData = new ThreadLocal<SingleElementStorage<T>>(false);
            _containerCount = 0;
        }

        /// <summary>
        /// Число контейнеров SingleElementStorage
        /// </summary>
        public int ContainerCount { get { return _containerCount; } }

        /// <summary>
        /// Посчитать число элементов во всех контейнерах
        /// </summary>
        /// <returns>Число элементов</returns>
        public int CalculateElementCount()
        {
            int result = 0;
            for (var curListItem = this._stackHead; curListItem != null; curListItem = curListItem.Next)
            {
                if (curListItem.HasElement)
                    result++;
            }
            return result;
        }

        /// <summary>
        /// Преобразовать в массив (для целей отладки!!!)
        /// </summary>
        /// <returns>Массив SingleElementStorage</returns>
        internal SingleElementStorage<T>[] ToArray()
        {
            List<SingleElementStorage<T>> result = new List<SingleElementStorage<T>>(_containerCount);

            for (var curListItem = this._stackHead; curListItem != null; curListItem = curListItem.Next)
                result.Add(curListItem);

            return result.ToArray();
        }


        /// <summary>
        /// Есть ли хранилища, чьи потоки завершились (SingleElementStorage без владельца)
        /// </summary>
        /// <returns>Есть ли такие</returns>
        public bool HasUnownedLocalStorage()
        {
            for (var curListItem = this._stackHead; curListItem != null; curListItem = curListItem.Next)
            {
                if (curListItem.IsUnowned)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Освободить элементы, не находящиеся в чьём-либо владении (владеющие потоки завершились)
        /// </summary>
        /// <returns>Список вытащенных элементов</returns>
        public List<T> ReleaseUnownedElements()
        {
            List<T> result = new List<T>(_containerCount);

            for (var curListItem = this._stackHead; curListItem != null; curListItem = curListItem.Next)
            {
                if (curListItem.IsUnowned)
                {
                    if (curListItem.TryTake(out T elem))
                        result.Add(elem);
                }
            }

            return result;
        }


        /// <summary>
        /// Получить незанятый контейнер
        /// </summary>
        /// <returns>Незанятый контейнер (null, если все заняты)</returns>
        private SingleElementStorage<T> GetUnownedLocalStorage()
        {
            for (var curListItem = this._stackHead; curListItem != null; curListItem = curListItem.Next)
            {
                if (curListItem.IsUnowned)
                {
                    curListItem.SetOwnerThread(Thread.CurrentThread);
                    return curListItem;
                }
            }
            return null;
        }

        /// <summary>
        /// Создать контейнер для текущего потока
        /// </summary>
        /// <returns>Созданный контейнер</returns>
        private SingleElementStorage<T> CreateThreadLocalStorage()
        {
            lock (_stackAccessLock)
            {
                var threadLocalStorage = GetUnownedLocalStorage();
                if (threadLocalStorage == null)
                {
                    threadLocalStorage = new SingleElementStorage<T>(Thread.CurrentThread);
                    threadLocalStorage.Next = _stackHead;
                    _stackHead = threadLocalStorage;
                    _containerCount++;
                }

                _perThreadData.Value = threadLocalStorage;
                return threadLocalStorage;
            }
        }


        /// <summary>
        /// Получить контейнер для текущего потока
        /// </summary>
        /// <param name="forceCreate">Создать, если отсутствует</param>
        /// <returns>Контейнер для текущего потока</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SingleElementStorage<T> GetThreadLocalStorage(bool forceCreate)
        {
            var threadLocalStorage = _perThreadData.Value;
            if (threadLocalStorage != null)
                return threadLocalStorage;

            if (!forceCreate)
                return null;

            return CreateThreadLocalStorage();
        }


        /// <summary>
        /// Попытаться добавить элемент в локльное хранилище потока
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <returns>Удалось ли добавить (false, если контейнер уже занят)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAddLocal(T element)
        {
            TurboContract.Requires(element != null, conditionString: "element != null");

            var storage = GetThreadLocalStorage(true);
            return storage.TryAdd(element);
        }


        /// <summary>
        /// Попытаться получить элемент из локального хранилища потока
        /// </summary>
        /// <param name="element">Вытащенный элемент</param>
        /// <returns>Удалось ли вытащить</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryTakeLocal(out T element)
        {
            var storage = GetThreadLocalStorage(false);
            if (storage != null && storage.TryTake(out element))
                return true;

            element = null;
            return false;
        }

        /// <summary>
        /// Сделать похищение из контейнеров других потоков
        /// </summary>
        /// <param name="element">Похищенный элемент</param>
        /// <returns>Удалось ли найти элемент и похитить</returns>
        public bool TrySteal(out T element)
        {
            for (var curListItem = this._stackHead; curListItem != null; curListItem = curListItem.Next)
            {
                if (curListItem.TryTake(out element))
                    return true;
            }

            element = null;
            return false;
        }
    }


    /// <summary>
    /// Debug-view для отладки
    /// </summary>
    internal class SingleElementPerThreadStorageDebugView<T> where T: class
    {
        private SingleElementPerThreadStorage<T> _original;

        public SingleElementPerThreadStorageDebugView(SingleElementPerThreadStorage<T> original)
        {
            _original = original;
        }

        public int ContainerCount { get { return _original.ContainerCount; } }
        public int Count { get { return _original.CalculateElementCount(); } }

        public SingleElementStorage<T>[] SingleElementStorages { get { return _original.ToArray(); } }
        public SingleElementStorage<T>[] NonEmptyElementStorages { get { return _original.ToArray().Where(o => o.HasElement).ToArray(); } }
    }
}
