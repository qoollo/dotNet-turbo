using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools.Common
{
    /// <summary>
    /// Wrapper for ObjectPool elements which contains additional information necessary for the ObjectPool logic
    /// </summary>
    [DebuggerDisplay("IsBusy = {IsBusy}, IsDestroyed = {IsElementDestroyed}")]
    public class PoolElementWrapper
    {
        private readonly object _owner;

        private int _isBusy;
        private volatile bool _isElementdDestroyed;
        private volatile bool _isRemoved;

        /// <summary>
        /// <see cref="PoolElementWrapper"/> constructor
        /// </summary>
        /// <param name="owner">The owner of the wrapper (ObjectPool instance)</param>
        public PoolElementWrapper(object owner)
        {
            _owner = owner;
            _isBusy = 0;
            _isRemoved = false;
            _isElementdDestroyed = false;
            ThisIndex = -1;
            NextIndex = -1;
        }
        /// <summary>
        /// <see cref="PoolElementWrapper"/> constructor
        /// </summary>
        public PoolElementWrapper()
            : this(null)
        {
        }


        /// <summary>
        /// This item index inside <see cref="ServiceStuff.ElementCollections.IndexedStackElementStorage{T}"/>
        /// </summary>
        internal volatile int ThisIndex;
        /// <summary>
        /// Next item index inside <see cref="ServiceStuff.ElementCollections.IndexedStackElementStorage{T}"/>
        /// </summary>
        internal volatile int NextIndex;



        /// <summary>
        /// Indicates whether the element is rented by user
        /// </summary>
        public bool IsBusy { get { return Volatile.Read(ref _isBusy) != 0; } }
        /// <summary>
        /// Indicates whether the element is removed from the owner ObjectPool
        /// </summary>
        public bool IsRemoved { get { return _isRemoved; } }
        /// <summary>
        /// Indicates whether the element is fully destroyed
        /// </summary>
        public bool IsElementDestroyed { get { return _isElementdDestroyed; } }
        /// <summary>
        /// The owner of the current wrapper
        /// </summary>
        public object Owner { get { return _owner; } }



        /// <summary>
        /// Marks element as Destroyed
        /// </summary>
        public void MarkElementDestroyed()
        {
            TurboContract.Assert(!this.IsElementDestroyed, "Can't destroy element 2 times");
            _isElementdDestroyed = true;
        }
        /// <summary>
        /// Marks element as Removed
        /// </summary>
        public void MarkRemoved()
        {
            TurboContract.Assert(this.IsElementDestroyed, "Trying to remove Pool Element that was not destroyed");
            TurboContract.Assert(!this.IsRemoved, "Can't remove element 2 times");
            _isRemoved = true;
        }



        /// <summary>
        /// Marks element as Busy
        /// </summary>
        protected internal void MakeBusy()
        {
#if DEBUG
            MakeBusyAtomic();
#else

            Debug.Assert(Volatile.Read(ref _isBusy) == 0, "Pool Element is already busy");
            Volatile.Write(ref _isBusy, 1);
#endif
        }
        /// <summary>
        /// Marks element as not Busy (returned back to the pool)
        /// </summary>
        protected internal void MakeAvailable()
        {
#if DEBUG
            MakeAvailableAtomic();
#else
            Debug.Assert(Volatile.Read(ref _isBusy) == 1, "Pool Element is already available");
            Volatile.Write(ref _isBusy, 0);
#endif
        }

        /// <summary>
        /// Attempts to mark element as Busy (concurrent version)
        /// </summary>
        /// <returns>True when the element was not Busy and was successfully marked as Busy after this call</returns>
        protected internal bool TryMakeBusyAtomic()
        {
            return Interlocked.CompareExchange(ref _isBusy, 1, 0) == 0;
        }
        /// <summary>
        /// Forcibly marks the element as Busy (concurrent version)
        /// </summary>
        protected internal void MakeBusyAtomic()
        {
            int prevIsBusy = Interlocked.Exchange(ref _isBusy, 1);
            TurboContract.Assert(prevIsBusy == 0, "Pool Element is already busy");
        }
        /// <summary>
        /// Forcibly marks the element as not Busy (concurrent version)
        /// </summary>
        protected internal void MakeAvailableAtomic()
        {
            int prevIsBusy = Interlocked.Exchange(ref _isBusy, 0);
            TurboContract.Assert(prevIsBusy == 1, "Pool Element is already available");
        }

#if DEBUG

        private bool _finalizerReRegistered;

        /// <summary>
        /// Get string with diagnostic info for this wrapper
        /// </summary>
        /// <returns>Diagnostic</returns>
        protected virtual string CollectDiagnosticInfo()
        {
            return string.Empty;
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~PoolElementWrapper()
        {
            if (!this.IsElementDestroyed && !_finalizerReRegistered)
            {
                GC.ReRegisterForFinalize(this);
                if (GC.GetGeneration(this) >= GC.MaxGeneration)
                    _finalizerReRegistered = true;
            }
            else
            {
                TurboContract.Assert(this.IsElementDestroyed, "Element should be destroyed before removing the PoolElementWrapper. Probably you forget to call Dispose on the owner pool. Info: " + this.CollectDiagnosticInfo());
            }
        }
#endif
    }


    /// <summary>
    /// Generic wrapper for ObjectPool elements which contains additional information necessary for the ObjectPool logic
    /// </summary>
    /// <typeparam name="T">The type of the ObjectPool element</typeparam>
    [DebuggerDisplay("IsValid = {IsValid}, IsDestroyed = {IsElementDestroyed}, IsBusy = {IsBusy}")]
    public class PoolElementWrapper<T> : PoolElementWrapper
    {
        private string _sourcePoolName;
        private readonly T _element;
        private readonly IPoolElementOperationSource<T> _operations;

        private string _lastTimeUsedAtMemberName;
        private string _lastTimeUsedAtFilePath;
        private int _lastTimeUsedAtLineNumber;
        private DateTime _lastTimeUsedAtTime;
        private DateTime _createTime;
        private int _numberOfTimesWasRented;
        private int _numberOfTimesWasReleased;

        private bool _isSourcePoolDisposed;
        private bool _isBusyOnPoolDispose;

        /// <summary>
        /// <see cref="PoolElementWrapper{T}"/> constructor
        /// </summary>
        /// <param name="element">Element to be wrapped</param>
        /// <param name="operations">Provides additional operations on the element (IsValid)</param>
        /// <param name="owner">Owner of the wrapper (ObjectPool instance)</param>
        public PoolElementWrapper(T element, IPoolElementOperationSource<T> operations, object owner)
            : base(owner)
        {
            if (operations == null)
                throw new ArgumentNullException(nameof(operations));

            _element = element;
            _operations = operations;
            _createTime = DateTime.Now;
        }

        /// <summary>
        /// Wrapped element
        /// </summary>
        public T Element { get { return _element; } }
        /// <summary>
        /// Indicates whether the <see cref="Element"/> is valid and can be used for operations
        /// </summary>
        public bool IsValid { get { return !IsElementDestroyed && _operations.IsValid(this); } }
        /// <summary>
        /// Name of the owner pool
        /// </summary>
        internal string SourcePoolName { get { return _sourcePoolName; } }

        /// <summary>
        /// Indicates whether the owner ObjectPool is in Disposed state (for debug purposes)
        /// </summary>
        internal bool IsSourcePoolDisposed { get { return _isSourcePoolDisposed; } }
        /// <summary>
        /// Indicates whether the element was in Busy state when owner ObjectPool becomes Disposed (for debug purposes)
        /// </summary>
        internal bool IsBusyOnPoolDispose { get { return _isBusyOnPoolDispose; } }

        /// <summary>
        /// Name of the caller method that used element the last time (for debug purposes)
        /// </summary>
        internal string LastTimeUsedAtMemberName { get { return _lastTimeUsedAtMemberName; } }
        /// <summary>
        /// Path to the source code file with method that used the element the last time (for debug purposes)
        /// </summary>
        internal string LastTimeUsedAtFilePath { get { return _lastTimeUsedAtFilePath; } }
        /// <summary>
        /// Line number in the source code file with method that used the element the last time (for debug purposes)
        /// </summary>
        internal int LastTimeUsedAtLineNumber { get { return _lastTimeUsedAtLineNumber; } }
        /// <summary>
        /// DateTime when the element was rented the last time
        /// </summary>
        internal DateTime LastTimeUsedAtTime { get { return _lastTimeUsedAtTime; } }
        /// <summary>
        /// Time when the element wrapper was created
        /// </summary>
        internal DateTime CreateTime { get { return _createTime; } }
        /// <summary>
        /// Number of times element was rented
        /// </summary>
        internal int NumberOfTimesWasRented { get { return _numberOfTimesWasRented; } }
        /// <summary>
        /// Number of times element was released
        /// </summary>
        internal int NumberOfTimesWasReleased { get { return _numberOfTimesWasReleased; } }

        /// <summary>
        /// Sets the owner ObjectPool name
        /// </summary>
        /// <param name="name">Name of the owner ObjectPool</param>
        internal void SetPoolName(string name)
        {
            TurboContract.Requires(name != null, conditionString: "name != null");
            _sourcePoolName = name;
        }
        /// <summary>
        /// Marks that the owner pool was disposed (for debug purposes)
        /// </summary>
        internal void SetPoolDisposed()
        {
            _isSourcePoolDisposed = true;
            _isBusyOnPoolDispose = this.IsBusy;
        }

        /// <summary>
        /// Notifies that element was rented with some renting information
        /// </summary>
        /// <param name="memberName">Caller method name</param>
        /// <param name="filePath">Caller source file name</param>
        /// <param name="lineNumber">Caller line number name</param>
        /// <param name="rentTime">DateTime when the element was rented</param>
        internal void UpdateStatOnRent(string memberName, string filePath, int lineNumber, DateTime rentTime)
        {
            _lastTimeUsedAtMemberName = memberName;
            _lastTimeUsedAtFilePath = filePath;
            _lastTimeUsedAtLineNumber = lineNumber;
            _lastTimeUsedAtTime = rentTime;
            Interlocked.Increment(ref _numberOfTimesWasRented);
        }
        /// <summary>
        /// Notifies that element was released
        /// </summary>
        internal void UpdateStatOnRelease()
        {
            Interlocked.Increment(ref _numberOfTimesWasReleased);
        }


#if DEBUG
        /// <summary>
        /// Get string with diagnostic info for this wrapper
        /// </summary>
        /// <returns>Diagnostic</returns>
        protected override string CollectDiagnosticInfo()
        {
            StringBuilder res = new StringBuilder();
            res.AppendFormat("PoolName = '{0}', ", _sourcePoolName ?? "");
            res.AppendFormat("IsSourcePoolDisposed = {0}, ", IsSourcePoolDisposed);
            res.AppendFormat("IsBusy = {0}, ", IsBusy);
            res.AppendFormat("IsBusyOnPoolDispose = {0}, ", IsBusyOnPoolDispose);
            res.AppendFormat("IsRemoved = {0}, ", IsRemoved);
            res.AppendFormat("IsDestroyed = {0}, ", IsElementDestroyed);
            res.AppendFormat("NumberOfTimesRented = {0}, ", _numberOfTimesWasRented);
            res.AppendFormat("NumberOfTimesReleased = {0}", _numberOfTimesWasReleased);
            return res.ToString();
        }
#endif
    }
}
