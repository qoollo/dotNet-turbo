using Qoollo.Turbo.ObjectPools.Common;
using Qoollo.Turbo.ObjectPools.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools
{
    /// <summary>
    /// Controlls the life-time of the element rented from ObjectPool. Can be used within 'using' statement
    /// </summary>
    /// <typeparam name="TElem">The type of the element controlled by the monitor</typeparam>
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
        private DateTime _rentTime;
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
        /// RentedElementMonitor consturctor
        /// </summary>
        /// <param name="element">Wrapper for pool element</param>
        /// <param name="sourcePool">Source object pool</param>
        internal RentedElementMonitor(PoolElementWrapper<TElem> element, ObjectPoolManager<TElem> sourcePool)
        {
            TurboContract.Requires(element == null || (element != null && sourcePool != null), conditionString: "element == null || (element != null && sourcePool != null)");

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


#if DEBUG
        /// <summary>
        /// RentedElementMonitor constructor
        /// </summary>
        /// <param name="element">Wrapper for pool element</param>
        /// <param name="sourcePool">Source object pool</param>
        /// <param name="memberName">Method name in which the element is rented (for debug purposes)</param>
        /// <param name="filePath">Source code file path in which the element is rented (for debug purposes)</param>
        /// <param name="lineNumber">Source code line number in which the element is rented (for debug purposes)</param>
        internal RentedElementMonitor(PoolElementWrapper<TElem> element, ObjectPoolManager<TElem> sourcePool, string memberName, string filePath, int lineNumber)
            : this(element, sourcePool)
        {
            _memberName = memberName;
            _filePath = filePath;
            _lineNumber = lineNumber;
            _rentTime = DateTime.Now;

            if (element != null)
                element.UpdateStatOnRent(memberName, filePath, lineNumber, _rentTime);
        }
#endif

#if DEBUG
        /// <summary>
        /// Method name in which the element is rented
        /// </summary>
        internal string MemberName { get { return _memberName; } }
        /// <summary>
        /// Source code file path in which the element is rented
        /// </summary>
        internal string FilePath { get { return _filePath; } }
        /// <summary>
        /// Source code line number in which the element is rented
        /// </summary>
        internal int LineNumber { get { return _lineNumber; } }
        /// <summary>
        /// The time when element was rented (RentedElementModitor was created)
        /// </summary>
        internal DateTime RentTime { get { return _rentTime; } }
#endif

        /// <summary>
        /// Source pool
        /// </summary>
        internal ObjectPoolManager<TElem> SourcePool { get { return _sourcePool; } }
        /// <summary>
        /// Element wrapper
        /// </summary>
        internal PoolElementWrapper<TElem> ElementWrapper { get { return _elementWrapper; } }
        /// <summary>
        /// Indicates whether the RentedElementMonitor is in disposed state
        /// </summary>
        internal bool IsDisposed { get { return _elementWrapper == null; } }


        /// <summary>
        /// Indicates whether the <see cref="Element"/> is valid and can be used for operations
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
        /// Throws excpetion that the <see cref="RentedElementMonitor{TElem}"/> is in disposed state
        /// </summary>
        private void ThrowElementDisposedException()
        {
            throw new ObjectDisposedException("RentedElementMonitor", "Rented element was returned to the Pool");
        }

        /// <summary>
        /// Rented element
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
        /// Returns rented element back to the object pool
        /// </summary>
        public void Dispose()
        {
            var wrapperCopy = _elementWrapper;
            var sourcePool = _sourcePool;

            _elementWrapper = null;
            _sourcePool = null;

            TurboContract.Assert((wrapperCopy == null && sourcePool == null) || (wrapperCopy != null && sourcePool != null), conditionString: "(wrapperCopy == null && sourcePool == null) || (wrapperCopy != null && sourcePool != null)");

            if (wrapperCopy != null && sourcePool != null)
            {
                sourcePool.ReleaseElement(wrapperCopy);
                Profiling.Profiler.ObjectPoolElementRentedTime(wrapperCopy.SourcePoolName, this.StopActiveTime());
            }

#if DEBUG
            if (wrapperCopy != null)
                wrapperCopy.UpdateStatOnRelease();

            GC.SuppressFinalize(this);
#endif
        }


#if DEBUG
        /// <summary>
        /// Finalizer
        /// </summary>
        ~RentedElementMonitor()
        {
            if (_elementWrapper == null && _sourcePool == null)
                return;

            string poolName = "<null>";
            if (_elementWrapper != null && _elementWrapper.SourcePoolName != null)
                poolName = _elementWrapper.SourcePoolName;
            else if (_sourcePool != null)
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

            TurboContract.Assert(false, "Rented element should be disposed by user call. Finalizer is not allowed. PoolName: " + poolName + rentedAt);

            if (_elementWrapper != null && _sourcePool != null)
                _sourcePool.ReleaseElement(_elementWrapper);
        }
#endif
    }
}
