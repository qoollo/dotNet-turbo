using Qoollo.Turbo.ObjectPools.Common;
using Qoollo.Turbo.ObjectPools.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools
{
    /// <summary>
    /// Контроллер арендованных элементов пула
    /// </summary>
    /// <typeparam name="TElem">Тип элемента</typeparam>
#if DEBUG
    public class RentedElementMonitor<TElem>: IDisposable
#else
    public struct RentedElementMonitor<TElem>: IDisposable
#endif
    {
        private PoolElementWrapper<TElem> _elementWrapper;
        private ObjectPoolManager<TElem> _sourcePool;

#if DEBUG
        private string _memberName;
        private string _filePath;
        private int _lineNumber;
#endif

#if SERVICE_CLASSES_PROFILING && SERVICE_CLASSES_PROFILING_TIME
        private Profiling.ProfilingTimer _activeTime;

        private void StartActiveTime()
        {
            _activeTime.StartTime();
        }
        private TimeSpan StopActiveTime()
        {
            return _activeTime.StopTime();
        }
#else
        [System.Diagnostics.Conditional("SERVICE_CLASSES_PROFILING_TIME")]
        private void StartActiveTime()
        {
        }
        private TimeSpan StopActiveTime()
        {
            return TimeSpan.Zero;
        }
#endif


#if DEBUG
        internal RentedElementMonitor() { }
#endif

        /// <summary>
        /// Конструктор RentedElementMonitor
        /// </summary>
        /// <param name="element">Обёртка над элементом</param>
        /// <param name="sourcePool">Пул-источник</param>
        internal RentedElementMonitor(PoolElementWrapper<TElem> element, ObjectPoolManager<TElem> sourcePool)
        {
            Contract.Requires(element == null || (element != null && sourcePool != null));

            if (element == null)
            {
                _elementWrapper = null;
                _sourcePool = null;
            }
            else
            {
                _elementWrapper = element;
                _sourcePool = sourcePool;
                this.StartActiveTime();
            }
        }

        /// <summary>
        /// Конструктор RentedElementMonitor
        /// </summary>
        /// <param name="element">Обёртка над элементом</param>
        /// <param name="sourcePool">Пул-источник</param>
        /// <param name="memberName">Иия метода, в котором произошло получение элемента пула</param>
        /// <param name="filePath">Путь до файла, в котором произошло получение элемента пула</param>
        /// <param name="lineNumber">Строка файла, в которой произошло получение элемента пула</param>
        internal RentedElementMonitor(PoolElementWrapper<TElem> element, ObjectPoolManager<TElem> sourcePool, string memberName, string filePath, int lineNumber)
            : this(element, sourcePool)
        {
#if DEBUG
            _memberName = memberName;
            _filePath = filePath;
            _lineNumber = lineNumber;
#endif
        }

#if DEBUG
        /// <summary>
        /// Иия метода, в котором произошло получение элемента пула
        /// </summary>
        internal string MemberName { get { return _memberName; } }
        /// <summary>
        /// Путь до файла, в котором произошло получение элемента пула
        /// </summary>
        internal string FilePath { get { return _filePath; } }
        /// <summary>
        /// Строка файла, в которой произошло получение элемента пула
        /// </summary>
        internal int LineNumber { get { return _lineNumber; } }
#endif

        /// <summary>
        /// Исходный пул
        /// </summary>
        internal ObjectPoolManager<TElem> SourcePool { get { return _sourcePool; } }
        /// <summary>
        /// Обёртка над элементом
        /// </summary>
        internal PoolElementWrapper<TElem> ElementWrapper { get { return _elementWrapper; } }
        /// <summary>
        /// Освобождён ли RentedElementMonitor
        /// </summary>
        internal bool IsDisposed { get { return _elementWrapper == null; } }


        /// <summary>
        /// Является ли элемент валидным
        /// </summary>
        public bool IsValid
        {
            get
            {
                var wrapperCopy = _elementWrapper;
                return wrapperCopy != null && wrapperCopy.IsValid;
            }
        }

        /// <summary>
        /// Выбросить исключение, что элемент освобождён
        /// </summary>
        private void ThrowElementDisposedException()
        {
            throw new ObjectDisposedException("RentedElementMonitor", "Rented element was returned to the Pool");
        }

        /// <summary>
        /// Элемент пула
        /// </summary>
        public TElem Element
        {
            get
            {
                var wrapperCopy = _elementWrapper;
                if (wrapperCopy == null)
                    ThrowElementDisposedException();

                return wrapperCopy.Element;
            }
        }


        /// <summary>
        /// Возврат элемента в пул
        /// </summary>
        public void Dispose()
        {
            var wrapperCopy = _elementWrapper;
            var sourcePool = _sourcePool;

            _elementWrapper = null;
            _sourcePool = null;

            Contract.Assert((wrapperCopy == null && sourcePool == null) || (wrapperCopy != null && sourcePool != null));

            if (wrapperCopy != null && sourcePool != null)
            {
                sourcePool.ReleaseElement(wrapperCopy);
                Profiling.Profiler.ObjectPoolElementRentedTime(wrapperCopy.SourcePoolName, this.StopActiveTime());
            }

#if DEBUG
            GC.SuppressFinalize(this);
#endif
        }


#if DEBUG
        /// <summary>
        /// Финализатор
        /// </summary>
        ~RentedElementMonitor()
        {
            string poolName = "<null>";
            if (_sourcePool != null)
                poolName = _sourcePool.ToString();

            string rentedAt = "";
            if (_memberName != null)
                rentedAt += ". MemberName: " + _memberName;
            if (_filePath != null)
                rentedAt += ". FilePath: " + _filePath;
            if (_lineNumber > 0)
                rentedAt += ". LineNumber: " + _lineNumber.ToString();
            if (rentedAt == "")
                rentedAt = ". RentedAt: <unknown>";

            Contract.Assert(false, "Rented element should be disposed by user call. Finalizer is not allowed. PoolName: " + poolName + rentedAt);

            if (_elementWrapper != null && _sourcePool != null)
                _sourcePool.ReleaseElement(_elementWrapper);
        }
#endif
    }
}
