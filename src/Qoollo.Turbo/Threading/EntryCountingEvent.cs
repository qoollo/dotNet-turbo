using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading
{
    /// <summary>
    /// Guard primitive for EntryCountingEvent that allows to use it with 'using' statement
    /// </summary>
    public struct EntryCountingEventGuard: IDisposable
    {
        private EntryCountingEvent _srcCounter;

        /// <summary>
        /// EntryCountingEventGuard constructor
        /// </summary>
        /// <param name="srcCounter">EntryCountingEvent</param>
        internal EntryCountingEventGuard(EntryCountingEvent srcCounter)
        {
            _srcCounter = srcCounter;
        }

        /// <summary>
        /// Is entering the protected section was successful
        /// </summary>
        public bool IsAcquired
        {
            get { return _srcCounter != null; }
        }

        /// <summary>
        /// Exits the protected code section
        /// </summary>
        public void Dispose()
        {
            if (_srcCounter != null)
            {
                _srcCounter.ExitClient();
                _srcCounter = null;
            }
        }
    }


    /// <summary>
    /// Primitive that protects the code section from state changing when there are threads executing that section
    /// </summary>
    public class EntryCountingEvent: IDisposable
    {
		private int _currentCountInner;
		private readonly ManualResetEventSlim _event;
        private volatile bool _isTerminateRequested;
		private bool _isDisposed;

        /// <summary>
        /// EntryCountingEvent constructor
        /// </summary>
        public EntryCountingEvent()
		{
			_currentCountInner = 1;
			_event = new ManualResetEventSlim();
		}

        /// <summary>
        /// The current number of entered clients
        /// </summary>
        public int CurrentCount { get { return Math.Max(0, Volatile.Read(ref _currentCountInner) - 1); } }
        /// <summary>
        /// Is terminate requested (prevent new clients to enter the protected section)
        /// </summary>
        public bool IsTerminateRequested { get { return _isTerminateRequested; } }
        /// <summary>
        /// Is terminated successfully (all clients has exited the protected section)
        /// </summary>
        public bool IsTerminated { get { return _isTerminateRequested && Volatile.Read(ref _currentCountInner) <= 0; } }

        /// <summary>
        /// Gets the underlying WaitHandle to wait for termination
        /// </summary>
        public WaitHandle WaitHandle
        {
            get
            {
                if (this._isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);
                return this._event.WaitHandle;
            }
        }


        /// <summary>
        /// Attempts to enter to the protected code section
        /// </summary>
        /// <returns>Is entered successfully</returns>
        public bool TryEnterClient()
        {
            if (_isDisposed || _isTerminateRequested)
                return false;

            int newCount = Interlocked.Increment(ref _currentCountInner);
            Debug.Assert(newCount > 0);

            if (_isDisposed || _isTerminateRequested)
            {
                int newCountDec = Interlocked.Decrement(ref _currentCountInner);
                if (newCount > 1 && newCountDec == 0)
                    ExitClientAdditionalActions(newCountDec);
                return false;
            }

            return true;
        }
        /// <summary>
        /// Enters to the protected code section. Throws <see cref="InvalidOperationException"/> if termination was requested
        /// </summary>
        /// <exception cref="ObjectDisposedException"><see cref="EntryCountingEvent"/> was disposed</exception>
        /// <exception cref="InvalidOperationException"><see cref="EntryCountingEvent"/> was terminated</exception>
        public void EnterClient()
        {
            if (!this.TryEnterClient())
            {
                if (this._isDisposed)
                    throw new ObjectDisposedException(this.GetType().Name);

                if (this.IsTerminateRequested)
                    throw new InvalidOperationException(this.GetType().Name + " is terminated");
            }
        }

        /// <summary>
        /// Attemts to enter to the protected code section
        /// </summary>
        /// <returns>Guard primitive to track the protected section scope with 'using' statement</returns>
        public EntryCountingEventGuard TryEnterClientGuarded()
        {
            if (!TryEnterClient())
                return new EntryCountingEventGuard();
            return new EntryCountingEventGuard(this);
        }
        /// <summary>
        /// Enters to the protected code section. Throws <see cref="InvalidOperationException"/> if termination was requested
        /// </summary>
        /// <returns>Guard primitive to track the protected section scope with 'using' statement</returns>
        /// <exception cref="ObjectDisposedException"><see cref="EntryCountingEvent"/> was disposed</exception>
        /// <exception cref="InvalidOperationException"><see cref="EntryCountingEvent"/> was terminated</exception>
        public EntryCountingEventGuard EnterClientGuarded()
        {
            EnterClient();
            return new EntryCountingEventGuard(this);
        }


        /// <summary>
        /// Enters to the protected code section. Throws user exception if termination was requested
        /// </summary>
        /// <typeparam name="TException">The type of exception to throw when attempt was unsuccessful</typeparam>
        /// <param name="message">Message, that will be passed to Exception constructor</param>
        /// <returns>Guard primitive to track the protected section scope with 'using' statement</returns>
        public EntryCountingEventGuard EnterClientGuarded<TException>(string message) where TException: Exception
        {
            if (!TryEnterClient())
                TurboException.Throw<TException>(message);
            return new EntryCountingEventGuard(this);
        }
        /// <summary>
        /// Enters to the protected code section. Throws user exception if termination was requested
        /// </summary>
        /// <typeparam name="TException">The type of exception to throw when attempt was unsuccessful</typeparam>
        /// <returns>Guard primitive to track the protected section scope with 'using' statement</returns>
        public EntryCountingEventGuard EnterClientGuarded<TException>() where TException : Exception
        {
            if (!TryEnterClient())
                TurboException.Throw<TException>();
            return new EntryCountingEventGuard(this);
        }

        /// <summary>
        /// Attempts to enter the protected section with additional user-specified condition
        /// </summary>
        /// <param name="condition">User-sepcified condition</param>
        /// <returns>Is entered successfully</returns>
        public bool TryEnterClientConditional(Func<bool> condition)
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            if (condition())
            {
                if (TryEnterClient())
                {
                    if (condition())
                        return true;
                    else
                        ExitClient();
                }
            }

            return false;
        }


        /// <summary>
        /// Additional action on client exit when he was the last client
        /// </summary>
        /// <param name="newCount">Number of clients</param>
        private void ExitClientAdditionalActions(int newCount)
        {
            if (newCount < 0)
                throw new InvalidOperationException("ExitClient called more times then EnterClient. EntryCountingEvent is in desynced state.");
            if (newCount == 0 && !_isTerminateRequested)
                throw new InvalidOperationException("ExitClient called more times then EnterClient. EntryCountingEvent is in desynced state.");

            if (newCount == 0)
            {
                lock (this._event)
                {
                    if (!_isDisposed)
                        this._event.Set();
                }
            }
        }

        /// <summary>
        /// Exits the protected code section (better to use <see cref="EntryCountingEventGuard"/> for safety)
        /// </summary>
        public void ExitClient()
        {
            int newCount = Interlocked.Decrement(ref this._currentCountInner);

            if (newCount <= 0) // Throws exception when negative
                ExitClientAdditionalActions(newCount);
        }


        /// <summary>
        /// Stops new clients from entering the protected code section
        /// </summary>
        public void Terminate()
        {
            if (!_isTerminateRequested)
            {
                _isTerminateRequested = true;
                ExitClient();
            }
        }

        /// <summary>
        /// Attemts to reset the <see cref="EntryCountingEvent"/> to the initial state.
        /// Can be done only in <see cref="IsTerminated"/> state.
        /// </summary>
        /// <returns>Is reseted succesfully</returns>
        public bool TryReset()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (!IsTerminated)
                return false;

            lock (this._event)
            {
                if (!_isDisposed)
                {
                    int numberOfClients = Interlocked.Increment(ref _currentCountInner);
                    if (numberOfClients != 1)
                    {
                        Interlocked.Decrement(ref _currentCountInner);
                        return false;
                    }
                    _isTerminateRequested = false;
                    this._event.Reset();
                }
            }

            return true;
        }
        /// <summary>
        /// Attemts to reset the <see cref="EntryCountingEvent"/> to the initial state.
        /// Can be done only in <see cref="IsTerminated"/> state.
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="EntryCountingEvent"/> is not in Terminated state</exception>
        /// <exception cref="ObjectDisposedException"><see cref="EntryCountingEvent"/> is disposed</exception>
        public void Reset()
        {
            if (!TryReset())
                throw new InvalidOperationException("Reset can be performed only when full termination performed");
        }


        /// <summary>
        /// Waits until all clients leave the protected code sections
        /// </summary>
		public void Wait()
		{
			this.Wait(-1, CancellationToken.None);
		}

        /// <summary>
        /// Waits until all clients leave the protected code sections
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
		public void Wait(CancellationToken cancellationToken)
		{
			this.Wait(-1, cancellationToken);
		}

        /// <summary>
        /// Waits until all clients leave the protected code sections
        /// </summary>
        /// <param name="timeout">Timeout</param>
        /// <returns>True if all clients leaved the protected section in specified timeout</returns>
		public bool Wait(TimeSpan timeout)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (num < -1L || num > 2147483647L)
				throw new ArgumentOutOfRangeException("timeout");

            return this.Wait((int)num, CancellationToken.None);
		}

        /// <summary>
        /// Waits until all clients leave the protected code sections
        /// </summary>
        /// <param name="timeout">Timeout</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if all clients leaved the protected section in specified timeout</returns>
		public bool Wait(TimeSpan timeout, CancellationToken cancellationToken)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (num < -1L || num > 2147483647L)
				throw new ArgumentOutOfRangeException("timeout");

			return this.Wait((int)num, cancellationToken);
		}

        /// <summary>
        /// Waits until all clients leave the protected code sections
        /// </summary>
        /// <param name="millisecondsTimeout">Cancellation token</param>
        /// <returns>True if all clients leaved the protected section in specified timeout</returns>
		public bool Wait(int millisecondsTimeout)
		{
            return this.Wait(millisecondsTimeout, CancellationToken.None);
		}

        /// <summary>
        /// Waits until all clients leave the protected code sections
        /// </summary>
        /// <param name="millisecondsTimeout">Timeout</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if all clients leaved the protected section in specified timeout</returns>
		public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)
		{
            if (millisecondsTimeout < -1)
                throw new ArgumentOutOfRangeException(nameof(millisecondsTimeout));
            if (this._isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);

            if (!IsTerminateRequested)
                return false;

            return this._event.Wait(millisecondsTimeout, cancellationToken);
		}


        /// <summary>
        /// Stops new clients from entering and waits until all already entered clients leave the protected code sections
        /// </summary>
        public void TerminateAndWait()
        {
            Terminate();
            Wait();
        }
        /// <summary>
        /// Stops new clients from entering and waits until all already entered clients leave the protected code sections
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        public void TerminateAndWait(CancellationToken cancellationToken)
        {
            Terminate();
            Wait(cancellationToken);
        }
        /// <summary>
        /// Stops new clients from entering and waits until all already entered clients leave the protected code sections
        /// </summary>
        /// <param name="timeout">Timeout</param>
        /// <returns>True if all clients leaved the protected section in specified timeout</returns>
        public void TerminateAndWait(TimeSpan timeout)
        {
            Terminate();
            Wait(timeout);
        }
        /// <summary>
        /// Stops new clients from entering and waits until all already entered clients leave the protected code sections
        /// </summary>
        /// <param name="timeout">Timeout</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if all clients leaved the protected section in specified timeout</returns>
        public void TerminateAndWait(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Terminate();
            Wait(timeout, cancellationToken);
        }
        /// <summary>
        /// Stops new clients from entering and waits until all already entered clients leave the protected code sections
        /// </summary>
        /// <param name="millisecondsTimeout">Timeout</param>
        /// <returns>True if all clients leaved the protected section in specified timeout</returns>
        public void TerminateAndWait(int millisecondsTimeout)
        {
            Terminate();
            Wait(millisecondsTimeout);
        }
        /// <summary>
        /// Stops new clients from entering and waits until all already entered clients leave the protected code sections
        /// </summary>
        /// <param name="millisecondsTimeout">Timeout</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if all clients leaved the protected section in specified timeout</returns>
        public void TerminateAndWait(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            Terminate();
            Wait(millisecondsTimeout, cancellationToken);
        }


        /// <summary>
        /// Cleans-up all resources
        /// </summary>
        /// <param name="isUserCall">Is called explicitly by user from Dispose</param>
        protected virtual void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                this._isTerminateRequested = true;
                this._isDisposed = true;

                lock (this._event)
                {
                    this._event.Dispose();
                }
            }
        }

        /// <summary>
        /// Cleans-up all resources
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
