using Qoollo.Turbo.ObjectPools.Common;
using Qoollo.Turbo.ObjectPools.ServiceStuff.ElementContainers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools
{
    /// <summary>
    /// Object pool where elements must be added manually by the user
    /// </summary>
    /// <typeparam name="TElem">The type of elements stored in Pool</typeparam>
    public class StaticPoolManager<TElem> : ObjectPoolManager<TElem>, IPoolElementOperationSource<TElem>
    {
        /// <summary>
        /// Calls Dispose for the <paramref name="element"/> if it implements <see cref="IDisposable"/> interface
        /// </summary>
        /// <param name="element">Pool element</param>
        private static void CallElementDispose(TElem element)
        {
            if (element is IDisposable disposableElem)
                disposableElem.Dispose();
        }


        private readonly string _name;
        private readonly Action<TElem> _destroyAction;

        private readonly SimpleElementsContainer<TElem> _elementsContainer;

        private readonly ManualResetEventSlim _stoppedEvent;
        private readonly CancellationTokenSource _disposeCancellation;


        /// <summary>
        /// <see cref="StaticPoolManager{TElem}"/> constructor
        /// </summary>
        /// <param name="name">Name for the current <see cref="StaticPoolManager{TElem}"/> instance</param>
        /// <param name="destroyAction">The action that will be performed to destroy each element after removal ('null' means no action)</param>
        public StaticPoolManager(string name, Action<TElem> destroyAction)
        {
            _name = name ?? this.GetType().GetCSFullName();
            _destroyAction = destroyAction;

            _elementsContainer = new SimpleElementsContainer<TElem>();
            _stoppedEvent = new ManualResetEventSlim(false);
            _disposeCancellation = new CancellationTokenSource();

            Profiling.Profiler.ObjectPoolCreated(this.Name);
        }
        /// <summary>
        /// <see cref="StaticPoolManager{TElem}"/> constructor
        /// </summary>
        /// <param name="name">Name for the current <see cref="StaticPoolManager{TElem}"/> instance</param>
        /// <param name="disposeElementOnDestroy">Whether the <see cref="IDisposable.Dispose"/> should be called for element after removal</param>
        public StaticPoolManager(string name, bool disposeElementOnDestroy)
            : this(name, disposeElementOnDestroy ? new Action<TElem>(CallElementDispose) : null)
        {
        }
        /// <summary>
        /// <see cref="StaticPoolManager{TElem}"/> constructor
        /// </summary>
        /// <param name="name">Name for the current <see cref="StaticPoolManager{TElem}"/> instance</param>
        public StaticPoolManager(string name)
            : this(name, null)
        {
        }
        /// <summary>
        /// <see cref="StaticPoolManager{TElem}"/> constructor
        /// </summary>
        public StaticPoolManager()
            : this(null, null)
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
        /// Number of elements stored in ObjectPool
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
        /// Adds new element to the ObjectPool
        /// </summary>
        /// <param name="elem">New element</param>
        /// <exception cref="ObjectDisposedException">ObjectPool is in disposed state</exception>
        /// <exception cref="InvalidOperationException">Too many elements inside ObjectPool</exception>
        public void AddElement(TElem elem)
        {
            if (_disposeCancellation.IsCancellationRequested)
                throw new ObjectDisposedException(this.GetType().Name);
            if (this.ElementCount >= (1 << 16) - 2)
                throw new InvalidOperationException("Max supported ElementCount is 65534");

            var addedElem = _elementsContainer.Add(elem, this, true);
            addedElem.SetPoolName(this.Name);
            Profiling.Profiler.ObjectPoolElementCreated(this.Name, this.ElementCount);
        }


        /// <summary>
        /// Removes the element from ObjectPool (the element should be rented before removing)
        /// </summary>
        /// <param name="elemMonitor">Rented element monitor for the element to remove</param>
        /// <exception cref="ArgumentException">Invalid <paramref name="elemMonitor"/></exception>
        public void RemoveElement(RentedElementMonitor<TElem> elemMonitor)
        {
            if (elemMonitor.IsDisposed)
                throw new ArgumentException("Element from 'elemMonitor' already returned to ObjectPool (" + this.Name + ")", nameof(elemMonitor));
            if (!object.ReferenceEquals(this, elemMonitor.SourcePool))
                throw new ArgumentException("RentedElementMonitor is not belog to the current ObjectPool (" + this.Name + ")", nameof(elemMonitor));

            DestroyElementInner(elemMonitor.ElementWrapper);
            Profiling.Profiler.ObjectPoolElementDestroyed(this.Name, this.ElementCount);
        }



        /// <summary>
        /// Destoroys element when it is removed from pool
        /// </summary>
        /// <param name="elem">Element to be destroyed</param>
        protected virtual void DestroyElement(TElem elem)
        {
            _destroyAction?.Invoke(elem);
        }

        /// <summary>
        /// Performs actions required to destroy the element
        /// </summary>
        /// <param name="element">Element</param>
        private void DestroyElementInner(PoolElementWrapper<TElem> element)
        {
            TurboContract.Requires(element != null, "element != null");
            TurboContract.Requires(element.IsBusy, "element.IsBusy");
            TurboContract.Requires(!element.IsElementDestroyed, "!element.IsElementDestroyed");

            DestroyElement(element.Element);
            element.MarkElementDestroyed();
        }

        /// <summary>
        /// Destroys element and removes it from the Pool
        /// </summary>
        /// <param name="element">Element</param>
        private void DestroyAndRemoveElement(PoolElementWrapper<TElem> element)
        {
            TurboContract.Requires(element != null, "element != null");
            TurboContract.Requires(element.IsBusy, "element.IsBusy");
            TurboContract.Requires(!element.IsElementDestroyed, "!element.IsElementDestroyed");

            try
            {
                DestroyElementInner(element);
            }
            finally
            {
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
        /// Rents element from pool
        /// </summary>
        /// <param name="timeout">Waiting timeout in milliseconds (-1 - infinity)</param>
        /// <param name="token">Token to cancel waiting for element availability</param>
        /// <param name="throwOnUnavail">True - throws <see cref="CantRetrieveElementException"/> when element can't be rented, otherwise returns null</param>
        /// <returns>Wrapper for rented element</returns>
        protected sealed override PoolElementWrapper<TElem> RentElement(int timeout, CancellationToken token, bool throwOnUnavail)
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


            PoolElementWrapper<TElem> result = null;
            bool elemWasTaken = false;

            if (timeout < 0)
                timeout = Timeout.Infinite;

            CancellationTokenSource linkedTokenSource = null;

            try
            {
                elemWasTaken = _elementsContainer.TryTake(out result, 0, new CancellationToken());

                if (!elemWasTaken && timeout != 0)
                {
                    if (!token.CanBeCanceled)
                    {
                        elemWasTaken = _elementsContainer.TryTake(out result, timeout, _disposeCancellation.Token);
                    }
                    else
                    {
                        linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, _disposeCancellation.Token);
                        elemWasTaken = _elementsContainer.TryTake(out result, timeout, linkedTokenSource.Token);
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


            if (throwOnUnavail && !elemWasTaken)
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
            TurboContract.Requires(element != null, "element != null");

            if (!element.IsBusy)
                throw new InvalidOperationException("Trying to release same element several times in Pool: " + this.Name);

            if (_disposeCancellation.IsCancellationRequested)
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

            Profiling.Profiler.ObjectPoolElementReleased(this.Name, this.RentedElementCount);
        }




        /// <summary>
        /// Checks whether the element is valid and can be used for operations (always true for <see cref="StaticPoolManager{TElem}"/>)
        /// </summary>
        /// <param name="container">Element wrapper</param>
        /// <returns>Whether the element is valid</returns>
        bool IPoolElementOperationSource<TElem>.IsValid(PoolElementWrapper<TElem> container)
        {
            TurboContract.Requires(container != null, "container != null");

            return true;
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
                        TurboContract.Assert(--count >= 0, "--count >= 0");

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
                    TurboContract.Assert(false, "StaticPoolManager should be Disposed by user! PoolName: " + this.Name);

                elementsContainer.ProcessFreeElements(o => o.MarkElementDestroyed());
#endif
            }

            base.Dispose(isUserCall);
        }

#if DEBUG
        /// <summary>
        /// Finalizer
        /// </summary>
        ~StaticPoolManager()
        {
            Dispose(false);
        }
#endif
    }
}
