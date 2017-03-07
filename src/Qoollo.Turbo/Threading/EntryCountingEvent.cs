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
    /// Примитив для использования using с EntryCountingEvent
    /// </summary>
    public struct EntryCountingEventGuard: IDisposable
    {
        private EntryCountingEvent _srcCounter;

        /// <summary>
        /// Конструктор EntryCountingEventGuard
        /// </summary>
        /// <param name="srcCounter">EntryCountingEvent</param>
        internal EntryCountingEventGuard(EntryCountingEvent srcCounter)
        {
            _srcCounter = srcCounter;
        }

        /// <summary>
        /// Удалось ли войти в экранируемый блок
        /// </summary>
        public bool IsAcquired
        {
            get { return _srcCounter != null; }
        }

        /// <summary>
        /// Выход из экранируемого блока
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
    /// Примитив для ожидания завершения исполнения всех экранируемых блоков
    /// </summary>
    public class EntryCountingEvent: IDisposable
    {
		private int _currentCountInner;
		private readonly ManualResetEventSlim _event;
        private volatile bool _isTerminateRequested;
		private bool _isDisposed;

        /// <summary>
        /// Констрктор EntryCountingEvent
        /// </summary>
        public EntryCountingEvent()
		{
			_currentCountInner = 1;
			_event = new ManualResetEventSlim();
		}

        /// <summary>
        /// Текущее количество входов
        /// </summary>
        public int CurrentCount { get { return Math.Max(0, Volatile.Read(ref _currentCountInner) - 1); } }
        /// <summary>
        /// Запрошена ли остановка
        /// </summary>
        public bool IsTerminateRequested { get { return _isTerminateRequested; } }
        /// <summary>
        /// Выполнена ли остановка полностью
        /// </summary>
        public bool IsTerminated { get { return _isTerminateRequested && Volatile.Read(ref _currentCountInner) <= 0; } }

        /// <summary>
        /// Объект ожидания
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
        /// Попробовать зайти в экранируемый блок
        /// </summary>
        /// <returns>Удалось ли зайти в блок</returns>
        public bool TryEnterClient()
        {
            if (_isDisposed || _isTerminateRequested)
                return false;

            int newCount = Interlocked.Increment(ref _currentCountInner);
            Debug.Assert(newCount > 0);

            if (_isDisposed || _isTerminateRequested)
            {
                Interlocked.Decrement(ref _currentCountInner);
                return false;
            }

            return true;
        }
        /// <summary>
        /// Выполнить заход в экранируемый блок (исключение, если не удалось войти)
        /// </summary>
        /// <exception cref="ObjectDisposedException">Освобождён</exception>
        /// <exception cref="InvalidOperationException">Завершён</exception>
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
        /// Попытаться войти в жкранируемый блок с guard объектом
        /// </summary>
        /// <returns></returns>
        public EntryCountingEventGuard TryEnterClientGuarded()
        {
            if (!TryEnterClient())
                return new EntryCountingEventGuard();
            return new EntryCountingEventGuard(this);
        }
        /// <summary>
        /// Войти в блок с guard объектом (исключение, если не удалось войти)
        /// </summary>
        /// <returns>Guard</returns>
        /// <exception cref="ObjectDisposedException">Освобождён</exception>
        /// <exception cref="InvalidOperationException">Завершён</exception>
        public EntryCountingEventGuard EnterClientGuarded()
        {
            EnterClient();
            return new EntryCountingEventGuard(this);
        }


        /// <summary>
        /// Войти в блок с guard объектом и, если не удалось, то выбросить пользовательское исключение
        /// </summary>
        /// <typeparam name="TException">Тип исключения</typeparam>
        /// <param name="message">Сообщение, которое будет записано в исключении</param>
        /// <returns>Guard</returns>
        public EntryCountingEventGuard EnterClientGuarded<TException>(string message) where TException: Exception
        {
            if (!TryEnterClient())
                TurboException.Throw<TException>(message);
            return new EntryCountingEventGuard(this);
        }
        /// <summary>
        /// Войти в блок с guard объектом и, если не удалось, то выбросить пользовательское исключение
        /// </summary>
        /// <typeparam name="TException">Тип исключения</typeparam>
        /// <returns>Guard</returns>
        public EntryCountingEventGuard EnterClientGuarded<TException>() where TException : Exception
        {
            if (!TryEnterClient())
                TurboException.Throw<TException>();
            return new EntryCountingEventGuard(this);
        }

        /// <summary>
        /// Попробовать зайти в экранируемый блок с дополнительным условием
        /// </summary>
        /// <param name="condition">Условие</param>
        /// <returns>Удалось ли войти</returns>
        public bool TryEnterClientConditional(Func<bool> condition)
        {
            Contract.Requires<ArgumentNullException>(condition != null);

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
        /// Дополнительные действия при выходе клиента для возведения флага окончания
        /// </summary>
        /// <param name="newCount">Новое значение после декремента числа клиентов</param>
        private void ExitClientAdditionalActions(int newCount)
        {
            if (newCount < 0)
                throw new InvalidOperationException("ExitClient called more times then EnterClien.");
            if (newCount == 0 && !_isTerminateRequested)
                throw new InvalidOperationException("ExitClient called more times then EnterClien.");

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
        /// Выйти из экранируемого блока
        /// </summary>
        public void ExitClient()
        {
            int newCount = Interlocked.Decrement(ref this._currentCountInner);

            Debug.Assert((newCount >= 0 && _isTerminateRequested) || (newCount > 0 && !_isTerminateRequested));
            if (newCount <= 0)
                ExitClientAdditionalActions(newCount);
        }


        /// <summary>
        /// Выполнить остановку (новые клиенты не смогут войти)
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
        /// Дождаться завершения исполнения всех блоков
        /// </summary>
		public void Wait()
		{
			this.Wait(-1, CancellationToken.None);
		}

        /// <summary>
        /// Дождаться завершения исполнения всех блоков
        /// </summary>
        /// <param name="cancellationToken">Токен отмены</param>
		public void Wait(CancellationToken cancellationToken)
		{
			this.Wait(-1, cancellationToken);
		}

        /// <summary>
        /// Дождаться завершения исполнения всех блоков
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <returns>false - вышли по таймауту</returns>
		public bool Wait(TimeSpan timeout)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (num < -1L || num > 2147483647L)
				throw new ArgumentOutOfRangeException("timeout");

            return this.Wait((int)num, CancellationToken.None);
		}

        /// <summary>
        /// Дождаться завершения исполнения всех блоков
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>false - вышли по таймауту</returns>
		public bool Wait(TimeSpan timeout, CancellationToken cancellationToken)
		{
			long num = (long)timeout.TotalMilliseconds;
			if (num < -1L || num > 2147483647L)
				throw new ArgumentOutOfRangeException("timeout");

			return this.Wait((int)num, cancellationToken);
		}

        /// <summary>
        /// Дождаться завершения исполнения всех блоков
        /// </summary>
        /// <param name="millisecondsTimeout">Таймаут</param>
        /// <returns>false - вышли по таймауту</returns>
		public bool Wait(int millisecondsTimeout)
		{
            return this.Wait(millisecondsTimeout, CancellationToken.None);
		}

        /// <summary>
        /// Дождаться завершения исполнения всех блоков
        /// </summary>
        /// <param name="millisecondsTimeout">Таймаут</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>false - вышли по таймауту</returns>
		public bool Wait(int millisecondsTimeout, CancellationToken cancellationToken)
		{
            Contract.Requires<ArgumentOutOfRangeException>(millisecondsTimeout >= -1);
            if (this._isDisposed)
                throw new ObjectDisposedException(this.GetType().Name);	
			cancellationToken.ThrowIfCancellationRequested();

            if (!IsTerminateRequested)
                return false;

            return this._event.Wait(millisecondsTimeout, cancellationToken);
		}


        /// <summary>
        /// Остановить и дождаться завершения исполнения всех блоков
        /// </summary>
        public void TerminateAndWait()
        {
            Terminate();
            Wait();
        }
        /// <summary>
        /// Остановить и дождаться завершения исполнения всех блоков
        /// </summary>
        /// <param name="cancellationToken">Токен отмены</param>
        public void TerminateAndWait(CancellationToken cancellationToken)
        {
            Terminate();
            Wait(cancellationToken);
        }
        /// <summary>
        /// Остановить и дождаться завершения исполнения всех блоков
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <returns>false - вышли по таймауту</returns>
        public void TerminateAndWait(TimeSpan timeout)
        {
            Terminate();
            Wait(timeout);
        }
        /// <summary>
        /// Остановить и дождаться завершения исполнения всех блоков
        /// </summary>
        /// <param name="timeout">Таймаут</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>false - вышли по таймауту</returns>
        public void TerminateAndWait(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Terminate();
            Wait(timeout, cancellationToken);
        }
        /// <summary>
        /// Остановить и дождаться завершения исполнения всех блоков
        /// </summary>
        /// <param name="millisecondsTimeout">Таймаут</param>
        /// <returns>false - вышли по таймауту</returns>
        public void TerminateAndWait(int millisecondsTimeout)
        {
            Terminate();
            Wait(millisecondsTimeout);
        }
        /// <summary>
        /// Остановить и дождаться завершения исполнения всех блоков
        /// </summary>
        /// <param name="millisecondsTimeout">Таймаут</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>false - вышли по таймауту</returns>
        public void TerminateAndWait(int millisecondsTimeout, CancellationToken cancellationToken)
        {
            Terminate();
            Wait(millisecondsTimeout, cancellationToken);
        }


        /// <summary>
        /// Освободить ресурсы
        /// </summary>
        /// <param name="isUserCall">Вызвано ли явно</param>
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
        /// Освободить ресурсы
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
