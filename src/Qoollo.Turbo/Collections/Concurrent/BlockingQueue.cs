using Qoollo.Turbo.Threading;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections.Concurrent
{
#pragma warning disable 0420
    /// <summary>
    /// Блокирующая очередь
    /// </summary>
    /// <typeparam name="T">Тип элементов очереди</typeparam>
    [DebuggerDisplay("Count = {Count}")]
    public class BlockingQueue<T>: ICollection, IEnumerable<T>, IDisposable
    {
        private const int COMPLETE_ADDING_ON_MASK = 1 << 31;

        private readonly ConcurrentQueue<T> _innerQueue;
        private volatile int _boundedCapacity;
        private volatile int _delayedBoundedCapacityDecrease;
        
        private readonly SemaphoreLight _freeNodes;
        private readonly SemaphoreLight _occupiedNodes;
        
        private readonly CancellationTokenSource _consumersCancellationTokenSource;
        private readonly CancellationTokenSource _producersCancellationTokenSource;
 
        private volatile int _currentAdders;

        private volatile bool _isDisposed;


        /// <summary>
        /// Конструктор BlockingQueue 
        /// </summary>
        /// <param name="boundedCapacity">Верхнее ограничение по размеру (нет ограничения, если меньше 0)</param>
        public BlockingQueue(int boundedCapacity)
        {
            _innerQueue = new ConcurrentQueue<T>();
            _boundedCapacity = boundedCapacity >= 0 ? boundedCapacity : -1;
            _delayedBoundedCapacityDecrease = 0;
            _consumersCancellationTokenSource = new CancellationTokenSource();
            _producersCancellationTokenSource = new CancellationTokenSource();
            _isDisposed = false;

            if (boundedCapacity > 0)
                _freeNodes = new SemaphoreLight(boundedCapacity);

            _occupiedNodes = new SemaphoreLight(0);
        }
        /// <summary>
        /// Конструктор BlockingQueue
        /// </summary>
        public BlockingQueue()
            : this(-1)
        {
        }


        /// <summary>
        /// Ограничение максимального размера очереди
        /// </summary>
        public int BoundedCapacity { get { return _boundedCapacity; } }
        /// <summary>
        /// Завершено ли добавление в очередь
        /// </summary>
        public bool IsAddingCompleted { get { return _currentAdders == COMPLETE_ADDING_ON_MASK; } }
        /// <summary>
        /// Завершена ли полная обработка очереди (нельзя добавлять и нечего удалять)
        /// </summary>
        public bool IsCompleted { get { return (IsAddingCompleted && (_occupiedNodes.CurrentCount == 0)); } }
        /// <summary>
        /// Количество элементов в очереди
        /// </summary>
        public int Count { get { return _occupiedNodes.CurrentCount; } }


        /// <summary>
        /// Проверка, что очередь не освобождена
        /// </summary>
        private void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("BlockingQueue");
        }

        /// <summary>
        /// Увеличить расширить ограничение на вместимость очереди
        /// </summary>
        /// <param name="increaseValue">Величина расширения</param>
        public void IncreaseBoundedCapacity(int increaseValue)
        {
            CheckDisposed();
            if (increaseValue < 0)
                throw new ArgumentException("increaseValue", "increaseValue should be positive");

            if (_freeNodes == null)
                return;

            if (increaseValue == 0)
                return;

            try { }
            finally
            {
                _freeNodes.Release(increaseValue);
                Interlocked.Add(ref _boundedCapacity, increaseValue);
            }
        }

        /// <summary>
        /// Обновить значение поля с числом отложенных уменьшений вместимости у семафора
        /// </summary>
        /// <param name="decreaseValue">Величина, на которую вместимость должна уменьшиться</param>
        private void UpdateDelayedBoundedCapacityDecreaseField(int decreaseValue)
        {
            Contract.Requires(decreaseValue >= 0);

            SpinWait sw = new SpinWait();
            int delayedBoundedCapacityDecrease = _delayedBoundedCapacityDecrease;
            while (Interlocked.CompareExchange(ref _delayedBoundedCapacityDecrease, Math.Max(0, delayedBoundedCapacityDecrease) + decreaseValue, delayedBoundedCapacityDecrease) != delayedBoundedCapacityDecrease)
            {
                sw.SpinOnce();
                delayedBoundedCapacityDecrease = _delayedBoundedCapacityDecrease;
            }
        }
        /// <summary>
        /// Уменьшить величину максимальной вместимости очереди
        /// </summary>
        /// <param name="decreaseValue">Величина уменьшения</param>
        public void DecreaseBoundedCapacity(int decreaseValue)
        {
            CheckDisposed();
            if (decreaseValue < 0)
                throw new ArgumentException("decreaseValue", "decreaseValue should be positive");

            if (_freeNodes == null)
                return;

            if (decreaseValue == 0)
                return;

            try { }
            finally
            {
                SpinWait sw = new SpinWait();
                int boundedCapacity = _boundedCapacity;
                int newBoundedCapacity = Math.Max(0, boundedCapacity - decreaseValue);
                while (Interlocked.CompareExchange(ref _boundedCapacity, newBoundedCapacity, boundedCapacity) != boundedCapacity)
                {
                    sw.SpinOnce();
                    boundedCapacity = _boundedCapacity;
                    newBoundedCapacity = Math.Max(0, boundedCapacity - decreaseValue);
                }

                int realDiff = boundedCapacity - newBoundedCapacity;
                UpdateDelayedBoundedCapacityDecreaseField(realDiff);


                while (_delayedBoundedCapacityDecrease > 0 && _freeNodes.Wait(0))
                {
                    if (Interlocked.Decrement(ref _delayedBoundedCapacityDecrease) < 0)
                    {
                        _freeNodes.Release();
                        break;
                    }
                }
            }
        }
        /// <summary>
        /// Задать новую вместимость очереди
        /// </summary>
        /// <param name="newBoundedCapacity">Новая вместимость</param>
        public void SetBoundedCapacity(int newBoundedCapacity)
        {
            CheckDisposed();
            if (newBoundedCapacity < 0)
                throw new ArgumentException("newBoundedCapacity", "newBoundedCapacity should be positive");

            if (_freeNodes == null)
                return;

            int boundedCapacity = _boundedCapacity;
            if (boundedCapacity < newBoundedCapacity)
                IncreaseBoundedCapacity(newBoundedCapacity - boundedCapacity);
            else if (boundedCapacity > newBoundedCapacity)
                DecreaseBoundedCapacity(boundedCapacity - newBoundedCapacity);
        }








        /// <summary>
        /// Форсированное добавление в очередь (даже если превышен максимальный её размер)
        /// </summary>
        /// <param name="item">Элемент</param>
        public void EnqueueForced(T item)
        {
            CheckDisposed();

            if (IsAddingCompleted)
                throw new InvalidOperationException("Adding was completed for BlockingQueue");

            try { }
            finally
            {
                _innerQueue.Enqueue(item);
                if (_freeNodes != null)
                    UpdateDelayedBoundedCapacityDecreaseField(1);
                _occupiedNodes.Release();
            }
        }



        /// <summary>
        /// Быстрый метод добавления элемента в очередь
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Удалось ли добавить</returns>
        internal bool TryEnqueueFast(T item)
        {
            CheckDisposed();

            if (IsAddingCompleted)
                throw new InvalidOperationException("Adding was completed for BlockingQueue");


            if (_freeNodes != null && (_freeNodes.CurrentCount == 0 || !_freeNodes.Wait(0)))
                return false;

            try
            {
                _innerQueue.Enqueue(item);
            }
            catch
            {
                if (_freeNodes != null)
                    _freeNodes.Release();
                throw;
            }
            _occupiedNodes.Release();

            return true;
        }


        /// <summary>
        /// Внутренний метод добавления элемента в очередь
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Успешность</returns>
        private bool TryEnqueueInner(T item, int timeout, CancellationToken token)
        {
            CheckDisposed();

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            if (IsAddingCompleted)
                throw new InvalidOperationException("Adding was completed for BlockingQueue");

            bool waitForSemaphoreWasSuccessful = true;

            if (_freeNodes != null)
            {
                CancellationTokenSource linkedTokenSource = null;
                try
                {
                    waitForSemaphoreWasSuccessful = _freeNodes.Wait(0);
                    if (waitForSemaphoreWasSuccessful == false && timeout != 0)
                    {
                        if (token.CanBeCanceled)
                        {
                            linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, _producersCancellationTokenSource.Token);
                            waitForSemaphoreWasSuccessful = _freeNodes.Wait(timeout, linkedTokenSource.Token);
                        }
                        else
                        {
                            waitForSemaphoreWasSuccessful = _freeNodes.Wait(timeout, _consumersCancellationTokenSource.Token);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    if (token.IsCancellationRequested)
                        throw new OperationCanceledException(token);

                    throw new OperationInterruptedException("Add operation in BlockingQueue was interrupted by CompleteAdd");
                }
                finally
                {
                    if (linkedTokenSource != null)
                        linkedTokenSource.Dispose();
                }
            }

            if (!waitForSemaphoreWasSuccessful)
                return false;


            bool currentAddersUpdated = false;
            bool elementWasTaken = false;
            try
            {
                int currentAdders = _currentAdders;
                if ((currentAdders & COMPLETE_ADDING_ON_MASK) != 0)
                {
                    SpinWait completeSw = new SpinWait();
                    while (_currentAdders != COMPLETE_ADDING_ON_MASK)
                        completeSw.SpinOnce();

                    throw new OperationInterruptedException("Add operation in BlockingQueue was interrupted by CompleteAdd");
                }

                currentAdders = Interlocked.Increment(ref _currentAdders);
                currentAddersUpdated = true;

                if ((currentAdders & COMPLETE_ADDING_ON_MASK) != 0)
                {
                    SpinWait completeSw = new SpinWait();
                    while (_currentAdders != COMPLETE_ADDING_ON_MASK)
                        completeSw.SpinOnce();

                    throw new OperationInterruptedException("Add operation in BlockingQueue was interrupted by CompleteAdd");
                }


                token.ThrowIfCancellationRequested();
                _innerQueue.Enqueue(item);
                elementWasTaken = true;

                _occupiedNodes.Release();
            }
            finally
            {
                if (!elementWasTaken && _freeNodes != null)
                {
                    _freeNodes.Release();
                }

                if (currentAddersUpdated)
                {
                    Contract.Assert((_currentAdders & ~COMPLETE_ADDING_ON_MASK) > 0);
                    Interlocked.Decrement(ref _currentAdders);
                }
            }

            return true;
        }


        /// <summary>
        /// Добавить элемент в очередь
        /// </summary>
        /// <param name="item">Элемент</param>
        public void Enqueue(T item)
        {
            bool addResult = TryEnqueueInner(item, Timeout.Infinite, new CancellationToken());
            Contract.Assert(addResult);
        }
        /// <summary>
        /// Добавить элемент в очередь
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <param name="token">Токен отмены</param>
        public void Enqueue(T item, CancellationToken token)
        {
            bool addResult = TryEnqueueInner(item, Timeout.Infinite, token);
            Contract.Assert(addResult);
        }

        /// <summary>
        /// Попытаться добавить элемент в очередь
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Удалось ли добавить</returns>
        public bool TryEnqueue(T item)
        {
            return TryEnqueueInner(item, 0, new CancellationToken());
        }
        /// <summary>
        /// Попытаться добавить элемент в очередь
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <returns>Удалось ли добавить</returns>
        public bool TryEnqueue(T item, TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException("timeout");
            if (timeoutMs < 0)
                timeoutMs = Timeout.Infinite;

            return TryEnqueueInner(item, (int)timeoutMs, new CancellationToken());
        }
        /// <summary>
        /// Попытаться добавить элемент в очередь
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <returns>Удалось ли добавить</returns>
        public bool TryEnqueue(T item, int timeout)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryEnqueueInner(item, timeout, new CancellationToken());
        }
        /// <summary>
        /// Попытаться добавить элемент в очередь
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Удалось ли добавить</returns>
        public bool TryEnqueue(T item, int timeout, CancellationToken token)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryEnqueueInner(item, timeout, token);
        }



        /// <summary>
        /// Попытаться выбрать элемент с минимальной задержкой
        /// </summary>
        /// <param name="item">Выбранный элемент</param>
        /// <returns>Успешность</returns>
        internal bool TryDequeueFast(out T item)
        {
            CheckDisposed();
            item = default(T);

            if (_occupiedNodes.CurrentCount == 0)
                return false;

            if (!_occupiedNodes.Wait(0))
                return false;

            bool removeSucceeded = false;
            bool removeFaulted = true;
            try
            {
                removeSucceeded = _innerQueue.TryDequeue(out item);
                Contract.Assert(removeSucceeded, "Take from underlying collection return false");
                removeFaulted = false;
            }
            finally
            {
                if (removeSucceeded)
                {
                    if (_freeNodes != null)
                    {
                        if (_delayedBoundedCapacityDecrease <= 0 || Interlocked.Decrement(ref _delayedBoundedCapacityDecrease) < 0)
                            _freeNodes.Release();
                    }
                }
                else if (removeFaulted)
                {
                    _occupiedNodes.Release();
                }

                if (IsCompleted)
                    _consumersCancellationTokenSource.Cancel();
            }

            return removeSucceeded;
        }


        /// <summary>
        /// Внутренний метод извлечения элемента из очереди
        /// </summary>
        /// <param name="item">Извлечённый элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <param name="throwOnCancellation">Выбрасывать исключение при отмене по токену</param>
        /// <returns>Удалось ли извлечь элемент</returns>
        private bool TryDequeueInner(out T item, int timeout, CancellationToken token, bool throwOnCancellation)
        {
            CheckDisposed();
            item = default(T);

            if (token.IsCancellationRequested)
            {
                if (!throwOnCancellation)
                    return false;

                token.ThrowIfCancellationRequested();
            }

            if (IsCompleted)
                return false;


            bool waitForSemaphoreWasSuccessful = false;

            CancellationTokenSource linkedTokenSource = null;
            try
            {
                waitForSemaphoreWasSuccessful = _occupiedNodes.Wait(0);
                if (waitForSemaphoreWasSuccessful == false && timeout != 0)
                {
                    if (token.CanBeCanceled)
                    {
                        linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, _consumersCancellationTokenSource.Token);
                        waitForSemaphoreWasSuccessful = _occupiedNodes.Wait(timeout, linkedTokenSource.Token);
                    }
                    else
                    {
                        waitForSemaphoreWasSuccessful = _occupiedNodes.Wait(timeout, _consumersCancellationTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (token.IsCancellationRequested && throwOnCancellation)
                    throw new OperationCanceledException(token);

                return false;
            }
            finally
            {
                if (linkedTokenSource != null)
                    linkedTokenSource.Dispose();
            }

            if (!waitForSemaphoreWasSuccessful)
                return false;

            bool removeSucceeded = false;
            bool removeFaulted = true;
            try
            {
                if (token.IsCancellationRequested)
                {
                    if (!throwOnCancellation)
                        return false;

                    token.ThrowIfCancellationRequested();
                }
                removeSucceeded = _innerQueue.TryDequeue(out item);
                Contract.Assert(removeSucceeded, "Take from underlying collection return false");
                removeFaulted = false;
            }
            finally
            {
                if (removeSucceeded)
                {
                    if (_freeNodes != null)
                    {
                        if (_delayedBoundedCapacityDecrease <= 0 || Interlocked.Decrement(ref _delayedBoundedCapacityDecrease) < 0)
                            _freeNodes.Release();
                    }
                }
                else if (removeFaulted)
                {
                    _occupiedNodes.Release();
                }

                if (IsCompleted)
                    _consumersCancellationTokenSource.Cancel();
            }

            return removeSucceeded;
        }




        /// <summary>
        /// Извлечь элемент из очереди
        /// </summary>
        /// <returns>Элемент</returns>
        public T Dequeue()
        {
            T result;
            bool takeResult = TryDequeueInner(out result, Timeout.Infinite, new CancellationToken(), true);
            Contract.Assert(takeResult);
 
            return result;
        }
        /// <summary>
        /// Извлечь элемент из очереди
        /// </summary>
        /// <param name="token">Токен отмены</param>
        /// <returns>Элемент</returns>
        public T Dequeue(CancellationToken token)
        {
            T result;
            bool takeResult = TryDequeueInner(out result, Timeout.Infinite, token, true);
            Contract.Assert(takeResult);

            return result;
        }
 
        /// <summary>
        /// Попытаться извлечь элемент из головы очереди
        /// </summary>
        /// <param name="item">Извлечённый элемент (если удалось)</param>
        /// <returns>Успешность извлечения</returns>
        public bool TryDequeue(out T item)
        {
            return TryDequeueInner(out item, 0, CancellationToken.None, true);
        }
        /// <summary>
        /// Попытаться извлечь элемент из головы очереди
        /// </summary>
        /// <param name="item">Извлечённый элемент (если удалось)</param>
        /// <param name="timeout">Таймаут</param>
        /// <returns>Успешность извлечения</returns>
        public bool TryDequeue(out T item, TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException("timeout");
            if (timeoutMs < 0)
                timeoutMs = Timeout.Infinite;

            return TryDequeueInner(out item, (int)timeoutMs, new CancellationToken(), true);
        }
        /// <summary>
        /// Попытаться извлечь элемент из головы очереди
        /// </summary>
        /// <param name="item">Извлечённый элемент (если удалось)</param>
        /// <param name="timeout">Таймаут в миллисекундах</param>
        /// <returns>Успешность извлечения</returns>
        public bool TryDequeue(out T item, int timeout)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryDequeueInner(out item, timeout, new CancellationToken(), true);
        }
        /// <summary>
        /// Попытаться извлечь элемент из головы очереди
        /// </summary>
        /// <param name="item">Извлечённый элемент (если удалось)</param>
        /// <param name="timeout">Таймаут в миллисекундах</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Успешность извлечения</returns>
        public bool TryDequeue(out T item, int timeout, CancellationToken token)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryDequeueInner(out item, timeout, token, true);
        }
        /// <summary>
        /// Попытаться извлечь элемент из головы очереди
        /// </summary>
        /// <param name="item">Извлечённый элемент (если удалось)</param>
        /// <param name="timeout">Таймаут в миллисекундах</param>
        /// <param name="token">Токен отмены</param>
        /// <param name="throwOnCancellation">Выбрасывать ли исключение при отмене</param>
        /// <returns>Успешность извлечения</returns>
        internal bool TryDequeue(out T item, int timeout, CancellationToken token, bool throwOnCancellation)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;
            return TryDequeueInner(out item, timeout, token, throwOnCancellation);
        }





        /// <summary>
        /// Внутренний метод чтения элемента из головы очереди без удаления
        /// </summary>
        /// <param name="item">Ситанный элемент</param>
        /// <param name="timeout">Таймаут</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Успешность</returns>
        private bool TryPeekInner(out T item, int timeout, CancellationToken token)
        {
            CheckDisposed();
            item = default(T);

            if (token.IsCancellationRequested)
                throw new OperationCanceledException(token);

            if (IsCompleted)
                return false;

            if (_innerQueue.TryPeek(out item))
                return true;


            bool waitForSemaphoreWasSuccessful = false;

            CancellationTokenSource linkedTokenSource = null;
            try
            {
                waitForSemaphoreWasSuccessful = _occupiedNodes.Wait(0);
                if (waitForSemaphoreWasSuccessful == false && timeout != 0)
                {
                    if (token.CanBeCanceled)
                    {
                        linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, _consumersCancellationTokenSource.Token);
                        waitForSemaphoreWasSuccessful = _occupiedNodes.Wait(timeout, linkedTokenSource.Token);
                    }
                    else
                    {
                        waitForSemaphoreWasSuccessful = _occupiedNodes.Wait(timeout, _consumersCancellationTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException(token);

                return false;
            }
            finally
            {
                if (linkedTokenSource != null)
                    linkedTokenSource.Dispose();
            }

            if (!waitForSemaphoreWasSuccessful)
                return false;

            try
            {
                token.ThrowIfCancellationRequested();
                return _innerQueue.TryPeek(out item);
            }
            finally
            {
                _occupiedNodes.Release();
            }
        }




        /// <summary>
        /// Прочитать элемент из головы очереди без удаления.
        /// Если элемента нет, то ждёт его появления
        /// </summary>
        /// <returns>Считанный элемент</returns>
        public T Peek()
        {
            T result;
            bool takeResult = TryPeekInner(out result, Timeout.Infinite, new CancellationToken());
            Contract.Assert(takeResult);

            return result;
        }
        /// <summary>
        /// Прочитать элемент из головы очереди без удаления.
        /// Если элемента нет, то ждёт его появления
        /// </summary>
        /// <param name="token">Токен отмены</param>
        /// <returns>Считанный элемент</returns>
        public T Peek(CancellationToken token)
        {
            T result;
            bool takeResult = TryPeekInner(out result, Timeout.Infinite, token);
            Contract.Assert(takeResult);

            return result;
        }

        /// <summary>
        /// Попытаться прочитать элемент из головы очереди без удаления
        /// </summary>
        /// <param name="item">Считанный элемент</param>
        /// <returns>Успешность считывания</returns>
        public bool TryPeek(out T item)
        {
            return _innerQueue.TryPeek(out item);
        }
        /// <summary>
        /// Попытаться прочитать элемент из головы очереди без удаления
        /// </summary>
        /// <param name="item">Считанный элемент</param>
        /// <param name="timeout">Таймаут ожидания элемента</param>
        /// <returns>Успешность считывания</returns>
        public bool TryPeek(out T item, TimeSpan timeout)
        {
            long timeoutMs = (long)timeout.TotalMilliseconds;
            if (timeoutMs > int.MaxValue)
                throw new ArgumentOutOfRangeException("timeout");
            if (timeoutMs < 0)
                timeoutMs = Timeout.Infinite;

            if (timeoutMs == 0)
                return _innerQueue.TryPeek(out item);

            return TryPeekInner(out item, (int)timeoutMs, new CancellationToken());
        }
        /// <summary>
        /// Попытаться прочитать элемент из головы очереди без удаления
        /// </summary>
        /// <param name="item">Считанный элемент</param>
        /// <param name="timeout">Таймаут ожидания элемента в миллисекундах</param>
        /// <returns>Успешность считывания</returns>
        public bool TryPeek(out T item, int timeout)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;

            if (timeout == 0)
                return _innerQueue.TryPeek(out item);

            return TryPeekInner(out item, timeout, new CancellationToken());
        }
        /// <summary>
        /// Попытаться прочитать элемент из головы очереди без удаления
        /// </summary>
        /// <param name="item">Считанный элемент</param>
        /// <param name="timeout">Таймаут ожидания элемента в миллисекундах</param>
        /// <param name="token">Токен отмены</param>
        /// <returns>Успешность считывания</returns>
        public bool TryPeek(out T item, int timeout, CancellationToken token)
        {
            if (timeout < 0)
                timeout = Timeout.Infinite;

            if (timeout == 0 && !token.IsCancellationRequested)
                return _innerQueue.TryPeek(out item);

            return TryPeekInner(out item, timeout, token);
        }


 
 
        /// <summary>
        /// Завершить добавление элементов
        /// </summary>
        public void CompleteAdding()
        {
            CheckDisposed();
 
            if (IsAddingCompleted)
                return;
 
            SpinWait sw = new SpinWait();
            int currentAdders = _currentAdders;

            while (true)
            {
                if ((currentAdders & COMPLETE_ADDING_ON_MASK) != 0)
                {
                    SpinWait completeSw = new SpinWait();
                    while (_currentAdders != COMPLETE_ADDING_ON_MASK) 
                        completeSw.SpinOnce();
                    return;
                }

                if (Interlocked.CompareExchange(ref _currentAdders, currentAdders | COMPLETE_ADDING_ON_MASK, currentAdders) == currentAdders)
                {
                    SpinWait completeSw = new SpinWait();
                    while (_currentAdders != COMPLETE_ADDING_ON_MASK)
                        completeSw.SpinOnce();

                    if (Count == 0)
                        _consumersCancellationTokenSource.Cancel();

                    _producersCancellationTokenSource.Cancel();
                    return;
 
                }

                sw.SpinOnce();
                currentAdders = _currentAdders;
            }
        } 
 
 
        /// <summary>
        /// Создать массив из элементов очереди
        /// </summary>
        /// <returns>Массив</returns>
        public T[] ToArray()
        {
            return _innerQueue.ToArray();
        }
 
        /// <summary>
        /// Скопировать данные очереди в массив
        /// </summary>
        /// <param name="array">Массив, в который копируем</param>
        /// <param name="index">Начальный индекс внутри массива</param>
        public void CopyTo(T[] array, int index)
        {
            Contract.Requires(array != null);
            Contract.Requires(index >= 0 && index < array.Length);

            _innerQueue.CopyTo(array, index);
        }
 

        /// <summary>
        /// Получить Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            CheckDisposed();
            return _innerQueue.GetEnumerator();
 
        }
        /// <summary>
        /// Получить Enumerator
        /// </summary>
        /// <returns>Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        /// <summary>
        /// Синхронизированная ли коллекция
        /// </summary>
        bool ICollection.IsSynchronized { get { return false; } }
        /// <summary>
        /// Объект синхронизации (не поддерживается)
        /// </summary>
        object ICollection.SyncRoot
        {
            get
            {
                throw new NotSupportedException("SyncRoot is not supported for BlockingQueue");
            }
        }

        /// <summary>
        /// Скопировать элементы очереди в массив
        /// </summary>
        /// <param name="array">Массив</param>
        /// <param name="index">Начальный индекс</param>
        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException("index");

            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0 || index >= array.Length)
                throw new ArgumentOutOfRangeException("index");

 
            T[] localArray = _innerQueue.ToArray();
            if (array.Length - index < localArray.Length)
                throw new ArgumentException("Not enough space in target array");


            Array.Copy(localArray, 0, array, index, localArray.Length);
        }



        /// <summary>
        /// Внутренний код освобождения ресурсов
        /// </summary>
        /// <param name="isUserCall">Вызвано ли пользователем</param>
        protected virtual void Dispose(bool isUserCall)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
            }
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

#pragma warning restore 0420


}
