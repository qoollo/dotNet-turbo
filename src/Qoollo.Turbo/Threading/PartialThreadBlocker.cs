using Qoollo.Turbo.Threading.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading
{
    /// <summary>
    /// Primitive that limits the number of simultaniously executing threads (transfers specified number of threads to Wait state)
    /// </summary>
    [DebuggerDisplay("RealWaiterCount = {RealWaiterCount}, ExpectedWaiterCount = {ExpectedWaiterCount}")]
    internal class PartialThreadBlocker
    {
        private static readonly int _processorCount = Environment.ProcessorCount;

        private static readonly Action<object> _cancellationTokenCanceledEventHandler = new Action<object>(CancellationTokenCanceledEventHandler);
        /// <summary>
        /// CancellationToken cancellation handler
        /// </summary>
        /// <param name="obj"><see cref="PartialThreadBlocker"/> instance</param>
        private static void CancellationTokenCanceledEventHandler(object obj)
        {
            PartialThreadBlocker blocker = obj as PartialThreadBlocker;
            TurboContract.Assert(blocker != null, conditionString: "blocker != null");
            lock (blocker._lockObj)
            {
                Monitor.PulseAll(blocker._lockObj);
            }
        }


        // ======================


        private volatile int _expectedWaiterCount;
        private volatile int _realWaiterCount;

        private readonly object _lockObj = new object();

        /// <summary>
        /// <see cref="PartialThreadBlocker"/> constructor
        /// </summary>
        /// <param name="expectedWaiterCount">Number of threads to be blocked</param>
        public PartialThreadBlocker(int expectedWaiterCount)
        {
            if (expectedWaiterCount < 0)
                throw new ArgumentOutOfRangeException(nameof(expectedWaiterCount), "expectedWaiterCount should be greater or equal to 0");

            _expectedWaiterCount = expectedWaiterCount;
            _realWaiterCount = 0;
        }
        /// <summary>
        /// <see cref="PartialThreadBlocker"/> constructor
        /// </summary>
        public PartialThreadBlocker()
            : this(0)
        {
        }
        
        /// <summary>
        /// Gets the number of threads that should be blocked
        /// </summary>
        public int ExpectedWaiterCount { get { return _expectedWaiterCount; } }
        /// <summary>
        /// Gets the number of threads that already was blocked
        /// </summary>
        public int RealWaiterCount { get { return _realWaiterCount; } }


        /// <summary>
        /// Notifies waiting threads that several of them can continue execution
        /// </summary>
        /// <param name="diff">The number of threads that potentially should be unblocked</param>
        private void WakeUpWaiters(int diff)
        {
            if (diff >= 0)
                return;

            lock (_lockObj)
            {
                if (Math.Abs(diff) == 1)
                    Monitor.Pulse(_lockObj);
                else
                    Monitor.PulseAll(_lockObj);
            }
        }

        /// <summary>
        /// Sets the number of threads that should be blocked (<see cref="ExpectedWaiterCount"/>)
        /// </summary>
        /// <param name="newValue">New value</param>
        /// <returns>Previous value that was stored in <see cref="ExpectedWaiterCount"/></returns>
        public int SetExpectedWaiterCount(int newValue)
        {
            TurboContract.Requires(newValue >= 0, conditionString: "newValue >= 0");

            int prevValue = Interlocked.Exchange(ref _expectedWaiterCount, newValue);
            WakeUpWaiters(newValue - prevValue);
            return prevValue;
        }
        /// <summary>
        /// Increases the number of threads that should be blocked
        /// </summary>
        /// <param name="addValue">The value by which it is necessary to increase the number of blocked threads</param>
        /// <returns>Updated value stored in <see cref="ExpectedWaiterCount"/></returns>
        public int AddExpectedWaiterCount(int addValue)
        {
            SpinWait sw = new SpinWait();
            int expectedWaiterCount = _expectedWaiterCount;
            TurboContract.Assert(expectedWaiterCount + addValue >= 0, "Negative ExpectedWaiterCount. Can be commented");
            int newExpectedWaiterCount = Math.Max(0, expectedWaiterCount + addValue);
            while (Interlocked.CompareExchange(ref _expectedWaiterCount, newExpectedWaiterCount, expectedWaiterCount) != expectedWaiterCount)
            {
                sw.SpinOnce();
                expectedWaiterCount = _expectedWaiterCount;
                TurboContract.Assert(expectedWaiterCount + addValue >= 0, "Negative ExpectedWaiterCount. Can be commented");
                newExpectedWaiterCount = Math.Max(0, expectedWaiterCount + addValue);
            }

            WakeUpWaiters(newExpectedWaiterCount - expectedWaiterCount);

            return newExpectedWaiterCount;
        }
        /// <summary>
        /// Decreases the number of threads that should be blocked
        /// </summary>
        /// <param name="subValue">The value by which it is necessary to decrease the number of blocked threads</param>
        /// <returns>Updated value stored in <see cref="ExpectedWaiterCount"/></returns>
        public int SubstractExpectedWaiterCount(int subValue)
        {
            return AddExpectedWaiterCount(-subValue);
        }




        /// <summary>
        /// Blocks the current thread if it is required
        /// </summary>
        /// <param name="timeout">Waiting timeout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the current thread successfully passed the <see cref="PartialThreadBlocker"/> (false - exited by timeout)</returns>
        public bool Wait(int timeout, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (timeout < 0)
                timeout = Timeout.Infinite;

            if (_realWaiterCount >= _expectedWaiterCount)
                return true;

            if (timeout == 0)
                return false;

            uint startTime = 0;
            int currentTime = Timeout.Infinite;
            if (timeout != Timeout.Infinite)
                startTime = TimeoutHelper.GetTimestamp();

            for (int i = 0; i < 10; i++)
            {
                if (_realWaiterCount >= _expectedWaiterCount)
                    return true;

                if (i == 5)
                    Thread.Yield();
                else
                    Thread.SpinWait(150 + (4 << i));
            }


            using (CancellationTokenHelper.RegisterWithoutECIfPossible(token, _cancellationTokenCanceledEventHandler, this))
            {
                lock (_lockObj)
                {
                    if (_realWaiterCount < _expectedWaiterCount)
                    {
                        try
                        {
                            _realWaiterCount++;

                            while (_realWaiterCount <= _expectedWaiterCount)
                            {
                                token.ThrowIfCancellationRequested();

                                if (timeout != Timeout.Infinite)
                                {
                                    currentTime = TimeoutHelper.UpdateTimeout(startTime, timeout);
                                    if (currentTime <= 0)
                                        return false;
                                }

                                if (!Monitor.Wait(_lockObj, currentTime))
                                    return false;
                            }
                        }
                        finally
                        {
                            _realWaiterCount--;
                        }
                    }
                }
            }
            return true;
        }



        /// <summary>
        /// Blocks the current thread if it is required
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wait()
        {
            bool semaphoreSlotTaken = Wait(Timeout.Infinite, new CancellationToken());
            TurboContract.Assert(semaphoreSlotTaken, conditionString: "semaphoreSlotTaken");
        }
        /// <summary>
        /// Blocks the current thread if it is required
        /// </summary>
        /// <param name="token">Cancellation token</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Wait(CancellationToken token)
        {
            bool semaphoreSlotTaken = Wait(Timeout.Infinite, token);
            TurboContract.Assert(semaphoreSlotTaken, conditionString: "semaphoreSlotTaken");
        }
        /// <summary>
        /// Blocks the current thread if it is required
        /// </summary>
        /// <param name="timeout">Waiting timeout in milliseconds</param>
        /// <returns>True if the current thread successfully passed the <see cref="PartialThreadBlocker"/> (false - exited by timeout)</returns>
        public bool Wait(TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            return Wait((int)timeoutMs, new CancellationToken());
        }
        /// <summary>
        /// Blocks the current thread if it is required
        /// </summary>
        /// <param name="timeout">Waiting timeout in milliseconds</param>
        /// <param name="token">Cancellation token</param>
        /// <returns>True if the current thread successfully passed the <see cref="PartialThreadBlocker"/> (false - exited by timeout)</returns>
        public bool Wait(TimeSpan timeout, CancellationToken token)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(timeout));

            return Wait((int)timeoutMs, token);
        }
        /// <summary>
        /// Blocks the current thread if it is required
        /// </summary>
        /// <param name="timeout">Waiting timeout in milliseconds</param>
        /// <returns>True if the current thread successfully passed the <see cref="PartialThreadBlocker"/> (false - exited by timeout)</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Wait(int timeout)
        {
            return Wait(timeout, new CancellationToken());
        }
    }
}
