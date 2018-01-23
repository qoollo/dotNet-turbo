using Qoollo.Turbo.ObjectPools.Common;
using Qoollo.Turbo.ObjectPools.ServiceStuff;
using Qoollo.Turbo.ObjectPools.ServiceStuff.ElementContainers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools
{
    /// <summary>
    /// Base class for object pool in which the number of elements is balanced according to the load
    /// </summary>
    /// <typeparam name="TElem">The type of elements stored in Pool</typeparam>
    [ContractClass(typeof(DynamicPoolManagerCodeContract<>))]
    public abstract class DynamicPoolManager<TElem> : ObjectPoolManager<TElem>, IPoolElementOperationSource<TElem>
    {
        /// <summary>
        /// Returns the current timestamp in milliseconds
        /// </summary>
        /// <returns>Timestamp</returns>
        private static uint GetTimestamp()
        {
            return (uint)Environment.TickCount;
        }
        /// <summary>
        /// Updates the timeout value based on elapsed time
        /// </summary>
        /// <param name="startTime">Timestamp for the moment when processing started</param>
        /// <param name="originalTimeout">Original timeout value</param>
        /// <returns>Rest time in milliseconds</returns>
        private static int UpdateTimeout(uint startTime, int originalTimeout)
        {
            if (originalTimeout < 0)
                return Timeout.Infinite;

            uint elapsed = GetTimestamp() - startTime;
            if (elapsed > (uint)int.MaxValue)
                return 0;

            int rest = originalTimeout - (int)elapsed;
            if (rest <= 0)
                return 0;

            return rest;
        }

        // ===================


        private const int DefaultGetRetryTimeout = 2000;
        private const int DefaultTrimPeriod = 60 * 1000;

        private readonly string _name;
        private readonly int _minElementCount;
        private readonly int _maxElementCount;
        private readonly int _getRetryTimeout;
        private readonly int _trimPeriod;

        private readonly UsedElementTracker _usedElementTracker;
        private readonly SimpleElementsContainer<TElem> _elementsContainer;
        private readonly ManualResetEventSlim _stoppedEvent;
        private readonly CancellationTokenSource _disposeCancellation;

        private int _reservedCount;

        /// <summary>
        /// <see cref="DynamicPoolManager{TElem}"/> constructor
        /// </summary>
        /// <param name="minElementCount">Minimum number of elements that should always be in pool</param>
        /// <param name="maxElementCount">Maximum number of elements in pool</param>
        /// <param name="name">Name for the current <see cref="DynamicPoolManager{TElem}"/> instance</param>
        /// <param name="trimPeriod">Period in milliseconds to estimate the load level and to reduce the number of elements in accordance with it (-1 - do not preform contraction)</param>
        /// <param name="getRetryTimeout">Interval in milliseconds between attempts to create a new element (used when a new element fails to create)</param>
        public DynamicPoolManager(int minElementCount, int maxElementCount, string name, int trimPeriod, int getRetryTimeout)
        {
            if (minElementCount < 0)
                throw new ArgumentOutOfRangeException(nameof(minElementCount));
            if (maxElementCount <= 0 || maxElementCount >= ((1 << 16) - 1))
                throw new ArgumentOutOfRangeException(nameof(maxElementCount));
            if (maxElementCount < minElementCount)
                throw new ArgumentException($"{nameof(maxElementCount)} cannot be less than minElementCount", nameof(maxElementCount));
            if (getRetryTimeout <= 0)
                throw new ArgumentException($"{nameof(getRetryTimeout)} should be positive", nameof(getRetryTimeout));

            _name = name ?? this.GetType().GetCSFullName();
            _minElementCount = minElementCount;
            _maxElementCount = maxElementCount;
            _getRetryTimeout = getRetryTimeout;
            _trimPeriod = trimPeriod > 0 ? trimPeriod : int.MaxValue;

            _reservedCount = 0;

            _usedElementTracker = new UsedElementTracker(_trimPeriod);
            _elementsContainer = new SimpleElementsContainer<TElem>();
            _stoppedEvent = new ManualResetEventSlim(false);
            _disposeCancellation = new CancellationTokenSource();

            Profiling.Profiler.ObjectPoolCreated(this.Name);
        }
        /// <summary>
        /// <see cref="DynamicPoolManager{TElem}"/> constructor
        /// </summary>
        /// <param name="minElementCount">Minimum number of elements that should always be in pool</param>
        /// <param name="maxElementCount">Maximum number of elements in pool</param>
        /// <param name="name">Name for the current <see cref="DynamicPoolManager{TElem}"/> instance</param>
        /// <param name="trimPeriod">Period in milliseconds to estimate the load level and to reduce the number of elements in accordance with it (-1 - do not preform contraction)</param>
        public DynamicPoolManager(int minElementCount, int maxElementCount, string name, int trimPeriod)
            : this(minElementCount, maxElementCount, name, trimPeriod, DefaultGetRetryTimeout)
        {
        }
        /// <summary>
        /// <see cref="DynamicPoolManager{TElem}"/> constructor
        /// </summary>
        /// <param name="minElementCount">Minimum number of elements that should always be in pool</param>
        /// <param name="maxElementCount">Maximum number of elements in pool</param>
        /// <param name="name">Name for the current <see cref="DynamicPoolManager{TElem}"/> instance</param>
        public DynamicPoolManager(int minElementCount, int maxElementCount, string name)
            : this(minElementCount, maxElementCount, name, DefaultTrimPeriod, DefaultGetRetryTimeout)
        {
        }
        /// <summary>
        /// <see cref="DynamicPoolManager{TElem}"/> constructor
        /// </summary>
        /// <param name="maxElementCount">Maximum number of elements in pool</param>
        /// <param name="name">Name for the current <see cref="DynamicPoolManager{TElem}"/> instance</param>
        public DynamicPoolManager(int maxElementCount, string name)
            : this(0, maxElementCount, name, DefaultTrimPeriod, DefaultGetRetryTimeout)
        {
        }
        /// <summary>
        /// <see cref="DynamicPoolManager{TElem}"/> constructor
        /// </summary>
        /// <param name="maxElementCount">Maximum number of elements in pool</param>
        public DynamicPoolManager(int maxElementCount)
            : this(0, maxElementCount, null, DefaultTrimPeriod, DefaultGetRetryTimeout)
        {
        }


        /// <summary>
        /// The name of the current ObjectPool instance
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Minimum number of elements that should always be in pool
        /// </summary>
        public int MinElementCount
        {
            get { return _minElementCount; }
        }
        /// <summary>
        /// Maximum number of elements in pool
        /// </summary>
        public int MaxElementCount
        {
            get { return _maxElementCount; }
        }
        /// <summary>
        /// Current number of elements stored in ObjectPool
        /// </summary>
        public override int ElementCount
        {
            get { return _elementsContainer.Count; }
        }

        /// <summary>
        /// Number of elements available for rent
        /// </summary>
        public int FreeElementCount
        {
            get { return _elementsContainer.AvailableCount; }
        }
        /// <summary>
        /// Number of rented elements
        /// </summary>
        private int RentedElementCount
        {
            get { return this.ElementCount - this.FreeElementCount; }
        }



        /// <summary>
        /// Creates a new element for the object pool.
        /// When creation is not possible it should return 'false'.
        /// </summary>
        /// <param name="elem">Created element in case of success</param>
        /// <param name="timeout">Creation timeout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Whether the creation was successful</returns>
        protected abstract bool CreateElement(out TElem elem, int timeout, CancellationToken token);
        /// <summary>
        /// Checks whether the element is valid and can be used for operations
        /// </summary>
        /// <param name="elem">Element to check</param>
        /// <returns>Whether the element is valid</returns>
        protected abstract bool IsValidElement(TElem elem);
        /// <summary>
        /// Destoroys element when it is removed from pool
        /// </summary>
        /// <param name="elem">Element to be destroyed</param>
        protected abstract void DestroyElement(TElem elem);




        /// <summary>
        /// Warms-up pool by adding new elements up to the specified '<paramref name="count"/>'
        /// </summary>
        /// <param name="count">The number of elements that must be present in the pool after the call</param>
        public void FillPoolUpTo(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (_disposeCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(this.GetType().Name);

            count = Math.Min(count, this.MaxElementCount);
            int restCount = count - _reservedCount;

            PoolElementWrapper<TElem> initedElem = null;
            for (int i = 0; i < restCount; i++)
            {
                try
                {
                    initedElem = this.TryCreateNewElement(-1, new CancellationToken());
                }
                finally
                {
                    if (initedElem != null)
                    {
                        this.ReleaseElement(initedElem);
                        initedElem = null;
                    }
                }
            }
        }


        /// <summary>
        /// Destroys element and removes it from the Pool
        /// </summary>
        /// <param name="element">Element</param>
        private void DestroyAndRemoveElement(PoolElementWrapper<TElem> element)
        {
            TurboContract.Requires(element != null, conditionString: "element != null");
            TurboContract.Requires(element.IsBusy, conditionString: "element.IsBusy");
            TurboContract.Requires(!element.IsElementDestroyed, conditionString: "!element.IsElementDestroyed");

            try
            {
                this.DestroyElement(element.Element);
                element.MarkElementDestroyed();
            }
            finally
            {
                if (element.IsElementDestroyed)
                    Interlocked.Decrement(ref _reservedCount);

                _elementsContainer.Release(element);
            }

            Profiling.Profiler.ObjectPoolElementDestroyed(this.Name, this.ElementCount);
        }

        /// <summary>
        /// Takes element from the <see cref="_elementsContainer"/> then destroys it and removes from the Pool
        /// </summary>
        /// <returns>Whether the element was presented in <see cref="_elementsContainer"/></returns>
        private bool TakeDestroyAndRemoveElement()
        {
            PoolElementWrapper<TElem> element = null;
            if (_elementsContainer.TryTake(out element, 0, new CancellationToken()))
            {
                DestroyAndRemoveElement(element);
                return true;
            }

            return false;
        }


        /// <summary>
        /// Attempts to create a new element and registers it in pool.
        /// Returns 'null' in case of failure.
        /// </summary>
        /// <param name="timeout">Creation timeout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Created element in case of success, otherwise null</returns>
        private PoolElementWrapper<TElem> TryCreateNewElement(int timeout, CancellationToken token)
        {
            if (_disposeCancellation.IsCancellationRequested || token.IsCancellationRequested)
                return null;
            if (Volatile.Read(ref _reservedCount) >= _maxElementCount)
                return null;

            _usedElementTracker.Reset();

            PoolElementWrapper<TElem> result = null;

            try
            {
                if (Interlocked.Increment(ref _reservedCount) > _maxElementCount)
                    return null;

                TElem element = default(TElem);
                if (CreateElement(out element, timeout, token))
                {
                    result = _elementsContainer.Add(element, this, false);
                    result.SetPoolName(this.Name);
                }
                else
                {
                    Profiling.Profiler.ObjectPoolElementFaulted(this.Name, this.ElementCount);
                }
            }
            finally
            {
                if (result == null)
                    Interlocked.Decrement(ref _reservedCount);
            }


            if (result != null)
                Profiling.Profiler.ObjectPoolElementCreated(this.Name, this.ElementCount);

            return result;
        }


        /// <summary>
        /// Processes taken element (checks whether it is valid)
        /// </summary>
        /// <param name="element">Element</param>
        /// <returns>Can the element be used in operations</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ProcessTakenElement(ref PoolElementWrapper<TElem> element)
        {
            TurboContract.Requires(element != null, conditionString: "element != null");

            if (this.IsValidElement(element.Element))
                return true;

            DestroyAndRemoveElement(element);
            element = null;

            Profiling.Profiler.ObjectPoolElementFaulted(this.Name, this.ElementCount);
            return false;
        }

        /// <summary>
        /// Gets an existing element from pool or creates a new element if required.
        /// </summary>
        /// <param name="timeout">Timeout</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>Element in case of success. Otherwise null.</returns>
        private PoolElementWrapper<TElem> TryGetElement(int timeout, CancellationToken token)
        {
            if (_disposeCancellation.IsCancellationRequested || token.IsCancellationRequested)
                return null;

            PoolElementWrapper<TElem> result = null;
            while (_elementsContainer.TryTake(out result, 0, new CancellationToken()))
            {
                if (ProcessTakenElement(ref result))
                    return result;
            }

            if (timeout == 0)
                return null;

            uint startTime = timeout > 0 ? GetTimestamp() : 0;
            int restTime = timeout;

            while (timeout < 0 || restTime > 0)
            {
                if (_disposeCancellation.IsCancellationRequested || token.IsCancellationRequested)
                    return null;

                result = TryCreateNewElement(restTime, token);
                if (result != null)
                    return result;

                restTime = UpdateTimeout(startTime, timeout);
                restTime = restTime > 0 ? Math.Min(_getRetryTimeout, restTime) : _getRetryTimeout;

                if (_elementsContainer.TryTake(out result, restTime, token))
                {
                    if (ProcessTakenElement(ref result))
                        return result;
                }

                restTime = UpdateTimeout(startTime, timeout);
            }
          
            return null;
        }






        /// <summary>
        /// Rents element from pool
        /// </summary>
        /// <param name="timeout">Waiting timeout in milliseconds (-1 - infinity)</param>
        /// <param name="token">Token to cancel waiting for element availability</param>
        /// <param name="throwOnUnavail">True - throws <see cref="CantRetrieveElementException"/> when element can't be rented, otherwise returns null</param>
        /// <returns>Wrapper for rented element</returns>
        protected sealed override PoolElementWrapper<TElem> RentElement(int timeout, System.Threading.CancellationToken token, bool throwOnUnavail)
        {
            if (_disposeCancellation.IsCancellationRequested)
            {
                if (throwOnUnavail)
                    throw new CantRetrieveElementException("Rent from pool failed. Dispose was called.", new ObjectDisposedException(this.GetType().Name));

                return null;
            }

            if (token.IsCancellationRequested)
            {
                if (throwOnUnavail)
                    token.ThrowIfCancellationRequested();

                return null;
            }


            if (timeout < 0)
                timeout = Timeout.Infinite;

            PoolElementWrapper<TElem> result = null;
            CancellationTokenSource linkedTokenSource = null;

            try
            {
                result = TryGetElement(0, new CancellationToken());

                if (result == null && timeout != 0)
                {
                    if (!token.CanBeCanceled)
                    {
                        result = TryGetElement(timeout, _disposeCancellation.Token);
                    }
                    else
                    {
                        linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, _disposeCancellation.Token);
                        result = TryGetElement(timeout, linkedTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (linkedTokenSource != null)
                    linkedTokenSource.Dispose();
            }


            // Обновление числа занятых элементов (для удаления неиспользуемых)
            _usedElementTracker.UpdateMinFreeElementCount(this.FreeElementCount);


            if (throwOnUnavail && result == null)
            {
                if (_disposeCancellation.IsCancellationRequested)
                {
                    // We should attempt to destroy element that was potentially occupied for small amout of time
                    this.TakeDestroyAndRemoveElement();
                    if (_elementsContainer.Count == 0)
                        _stoppedEvent.Set();

                    throw new CantRetrieveElementException("Rent from pool failed. Dispose was called.", new ObjectDisposedException(this.GetType().Name));
                }

                token.ThrowIfCancellationRequested();

                if (timeout >= 0)
                    throw new TimeoutException(string.Format("Pool 'Rent' operation has timeouted. Pool: {0}. Timeout value: {1}ms", this.Name, timeout));

                TurboContract.Assert(false, "Element in pool is not available. Reason: UNKNOWN!");
                throw new CantRetrieveElementException("Rent from pool failed");
            }


            Profiling.Profiler.ObjectPoolElementRented(this.Name, this.RentedElementCount);
            return result;
        }



        /// <summary>
        /// Releases element back to the pool. Normally should be called from <see cref="RentedElementMonitor{TElem}"/>
        /// </summary>
        /// <param name="element">Element wrapper to be released</param>
        protected internal sealed override void ReleaseElement(PoolElementWrapper<TElem> element)
        {
            TurboContract.Requires(element != null, conditionString: "element != null");

            if (!element.IsBusy)
                throw new InvalidOperationException("Trying to release same element several times in Pool: " + this.Name);

            _usedElementTracker.UpdateState();

            bool doTrim = _elementsContainer.Count > _minElementCount && _usedElementTracker.RequestElementToDestroy();
            bool isValid = this.IsValidElement(element.Element);

            if (_disposeCancellation.IsCancellationRequested || doTrim || !isValid)
            {
                DestroyAndRemoveElement(element);
            }
            else
            {
                _elementsContainer.Release(element);

                if (_disposeCancellation.IsCancellationRequested)
                    TakeDestroyAndRemoveElement();
            }

            if (_disposeCancellation.IsCancellationRequested && _elementsContainer.Count == 0)
                _stoppedEvent.Set();

            if (!isValid)
                Profiling.Profiler.ObjectPoolElementFaulted(this.Name, this.ElementCount);

            Profiling.Profiler.ObjectPoolElementReleased(this.Name, this.RentedElementCount);
        }



        /// <summary>
        /// Checks whether the element is valid and can be used for operations (redirects call to <see cref="IsValidElement(TElem)"/>)
        /// </summary>
        /// <param name="container">Element wrapper</param>
        /// <returns>Whether the element is valid</returns>
        bool IPoolElementOperationSource<TElem>.IsValid(PoolElementWrapper<TElem> container)
        {
            TurboContract.Requires(container != null, conditionString: "container != null");
            return this.IsValidElement(container.Element);
        }



        /// <summary>
        /// Blocks the current thread and waits for full completion (all elements should be returned back to the pool)
        /// </summary>
        public void WaitUntilStop()
        {
            if (_disposeCancellation.IsCancellationRequested && _elementsContainer.Count == 0)
                return;

            _stoppedEvent.Wait();
        }

        /// <summary>
        /// Blocks the current thread and waits for full completion (all elements should be returned back to the pool)
        /// </summary>
        /// <param name="timeout">Waiting timeout in milliseconds</param>
        /// <returns>True when pool was disposed and all its elements was destroyed in time</returns>
        public bool WaitUntilStop(int timeout)
        {
            if (_disposeCancellation.IsCancellationRequested && _elementsContainer.Count == 0)
                return true;

            return _stoppedEvent.Wait(timeout);
        }



        /// <summary>
        /// Pool clean-up (core method)
        /// </summary>
        /// <param name="waitForRelease">Should it wait for full completion</param>
        private void DisposePool(bool waitForRelease)
        {
            if (!_disposeCancellation.IsCancellationRequested)
            {
#if DEBUG
                _elementsContainer.ProcessAllElements(o => o.SetPoolDisposed());
#endif

                _disposeCancellation.Cancel();

                try { }
                finally
                {
                    int count = _elementsContainer.Count;
                    while (TakeDestroyAndRemoveElement())
                        TurboContract.Assert(--count >= 0, conditionString: "--count >= 0");

                    if (_elementsContainer.Count == 0)
                        _stoppedEvent.Set();
                }

                Profiling.Profiler.ObjectPoolDisposed(this.Name, false);
            }
            else
            {
                if (_elementsContainer.Count == 0)
                    _stoppedEvent.Set();
            }

            if (waitForRelease)
                this.WaitUntilStop();
        }

        /// <summary>
        /// Cleans-up ObjectPool resources
        /// </summary>
        /// <param name="flags">Flags that controls disposing behaviour</param>
        public virtual void Dispose(DisposeFlags flags)
        {
            bool waitForRelease = (flags & DisposeFlags.WaitForElementsRelease) != DisposeFlags.None;
            this.DisposePool(waitForRelease);
            GC.SuppressFinalize(this);
        }



        /// <summary>
        /// Cleans-up ObjectPool resources
        /// </summary>
        /// <param name="isUserCall">Is it called explicitly by user (False - from finalizer)</param>
        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                this.DisposePool(false);
            }
            else
            {
#if DEBUG
                var elementsContainer = _elementsContainer;
                if (elementsContainer == null)
                    TurboContract.Assert(false, "DynamicPoolManager should be Disposed by user! PoolName: " + this.Name);

                elementsContainer.ProcessFreeElements(o => o.MarkElementDestroyed());
#endif
            }

            base.Dispose(isUserCall);
        }

#if DEBUG
        /// <summary>
        /// Finalizer
        /// </summary>
        ~DynamicPoolManager()
        {
            Dispose(false);
        }
#endif
    }


    /// <summary>
    /// Code contracts
    /// </summary>
    /// <typeparam name="TElem"></typeparam>
    [ContractClassFor(typeof(DynamicPoolManager<>))]
    internal abstract class DynamicPoolManagerCodeContract<TElem>: DynamicPoolManager<TElem>
    {
        private DynamicPoolManagerCodeContract() : base(1) { }

        protected override bool CreateElement(out TElem elem, int timeout, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected override bool IsValidElement(TElem elem)
        {
            Contract.EnsuresOnThrow<Exception>(false, "DynamicPoolManager.IsValidElement should not throw any exception");

            throw new NotImplementedException();
        }

        protected override void DestroyElement(TElem elem)
        {
            throw new NotImplementedException();
        }
    }
}
