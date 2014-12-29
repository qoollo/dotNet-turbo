using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;

namespace Qoollo.Turbo.OldPool
{
    /// <summary>
    /// Базовый класс для обёртки над элементами пула
    /// </summary>
    /// <typeparam name="T">Тип элемента</typeparam>
    internal class PoolElement<T> : IDisposable
        where T: class
    {
#if SERVICE_CLASSES_PROFILING && SERVICE_CLASSES_PROFILING_TIME
        private Profiling.ProfilingTimer _activeTime = Profiling.ProfilingTimer.Create();
        private string _poolName = "not_set";

        internal void StartActiveTime()
        {
            _activeTime.StartTime();
        }
        internal TimeSpan StopActiveTime()
        {
            return _activeTime.StopTime();
        }

        internal void SetPoolName(string name)
        {
            _poolName = name;
        }
        internal string GetPoolName()
        {
            return _poolName;
        }
#else
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        internal void StartActiveTime()
        {
        }
        internal TimeSpan StopActiveTime()
        {
            return TimeSpan.Zero;
        }

        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING")]
        internal void SetPoolName(string name)
        {
        }
        internal string GetPoolName()
        {
            return "not_set";
        }
#endif


        private IPoolElementReleaser<T> _pool;
        private T _element;

        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant((Element == null && !IsValid) || Element != null);
        }

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="pool">Менеджер пула, либо другой объект для освобождения элемента</param>
        /// <param name="elem">Сам элемент</param>
        public PoolElement(IPoolElementReleaser<T> pool, T elem)
        {
            Contract.Requires(pool != null);

            _pool = pool;
            _element = elem;

            if (elem == null)
                GC.SuppressFinalize(this);

            StartActiveTime();
        }

        /// <summary>
        /// Обёрнутый элемент
        /// </summary>
        public T Element
        {
            get { return _element; }
        }

        /// <summary>
        /// Ссылка на пул
        /// </summary>
        internal IPoolElementReleaser<T> Pool
        {
            get { return _pool; }
        }

        /// <summary>
        /// Валиден ли элемент пула. Обязательно надо проверять.
        /// </summary>
        public virtual bool IsValid
        {
            get { return _element != null; }
        }

        /// <summary>
        /// Вызывается при освобождении данного элемента
        /// </summary>
        /// <param name="elem">Освобождаемый элемент</param>
        protected virtual void OnRelease(T elem)
        {
        }


        /// <summary>
        /// Код освобождения элемента
        /// </summary>
        /// <param name="isUserCall">Вызвано ли освобождение явно</param>
        /// <param name="isFreeWrapper">Вызвано из кода очистки обёртки над элементом</param>
        private void Dispose(bool isUserCall, bool isFreeWrapper)
        {
            var elem = System.Threading.Interlocked.Exchange(ref _element, null);
            if (elem == null)
                return;

            if (isUserCall)
            {
                OnRelease(elem);
                Profiling.Profiler.ObjectPoolElementRentedTime(GetPoolName(), StopActiveTime());
            }
            else
            {
                //Profiling.Profiler.PoolElementReleasedInFinalizer(GetPoolName());
            }

            var pool = System.Threading.Interlocked.Exchange(ref _pool, null);
            if (pool == null)
                return;

            if (!isFreeWrapper)
                pool.Release(elem);
        }

        /// <summary>
        /// Удаление элемента из обёртки
        /// </summary>
        internal void FreeWrapperInternal()
        {
            Dispose(true, true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Явное освобождение элемента
        /// </summary>
        public void Dispose()
        {
            Dispose(true, false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Деструктор
        /// </summary>
        ~PoolElement()
        {
            Dispose(false, false);
        }
    }
}
