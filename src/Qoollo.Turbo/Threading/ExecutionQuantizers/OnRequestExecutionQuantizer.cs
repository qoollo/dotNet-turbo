using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ExecutionQuantizers
{
    /// <summary>
    /// Реализация ExecutionQuantizer, которая продвигается лишь при явном разрешении
    /// </summary>
    public class OnRequestExecutionQuantizer: ExecutionQuantizer, IDisposable
    {
        private readonly object _processNotifier;
        private volatile bool _processNotifierFlag;
        private readonly ManualResetEventSlim _waiterNotifier;
        private volatile int _tickWaiters;

        /// <summary>
        /// Конструктор OnRequestExecutionQuantizer
        /// </summary>
        public OnRequestExecutionQuantizer()
        {
            _processNotifierFlag = false;
            _processNotifier = new object();
            _waiterNotifier = new ManualResetEventSlim(false);
            _tickWaiters = 0;
        }

        /// <summary>
        /// Количество потоков, ожидающих продвижения
        /// </summary>
        public int TickWaiters { get { return _tickWaiters; } }
        /// <summary>
        /// Будет ли следующий тик пройден без ожидания
        /// </summary>
        public bool IsProcessAllowed { get { return _processNotifierFlag; } }


        /// <summary>
        /// Сообщить, что можно переключится в данном месте.
        /// Подразумевается необходимость ожидания с таймаутом и отмены по токену.
        /// </summary>
        /// <param name="waitTimeout">Таймаут в миллисекундах</param>
        /// <param name="token">Токен отмены</param>
        public override void Tick(int waitTimeout, CancellationToken token)
        {
            ProcessTick();
        }
        /// <summary>
        /// Сообщить, что можно переключится в данном месте.
        /// Подразумевается необходимость ожидания с таймаутом
        /// </summary>
        /// <param name="waitTimeout">Таймаут в миллисекундах</param>
        public override void Tick(int waitTimeout)
        {
            ProcessTick();
        }
        /// <summary>
        /// Сообщить, что можно переключится в данном месте
        /// </summary>
        public override void Tick()
        {
            ProcessTick();
        }

        /// <summary>
        /// Обработчик тика
        /// </summary>
        private void ProcessTick()
        {
            bool lockTaken = false;
            try
            {
                try { }
                finally
                {
                    Monitor.Enter(_processNotifier, ref lockTaken);
                    _tickWaiters++;
                    if (_tickWaiters > 0 && !_waiterNotifier.IsSet)
                        _waiterNotifier.Set();
                }

                while (!_processNotifierFlag)
                    Monitor.Wait(_processNotifier);

                _processNotifierFlag = false;

            }
            finally
            {
                if (lockTaken)
                {
                    _tickWaiters--;
                    if (_tickWaiters == 0 && _waiterNotifier.IsSet)
                        _waiterNotifier.Reset();

                    Monitor.Exit(_processNotifier);
                }
            }
        }


        /// <summary>
        /// Разрешить обработку очередного тика
        /// </summary>
        public void AllowProcess()
        {
            lock (_processNotifier)
            {
                _processNotifierFlag = true;
                Monitor.Pulse(_processNotifier);
            }
        }


        /// <summary>
        /// Дождаться появления тика
        /// </summary>
        public void WaitForTickers()
        {
            _waiterNotifier.Wait();
        }
        /// <summary>
        /// Дождаться появления тика
        /// </summary>
        /// <param name="timeout">Таймаут ожидания</param>
        /// <returns>Удалось ли дождаться</returns>
        public bool WaitForTickers(int timeout)
        {
            return _waiterNotifier.Wait(timeout);
        }
        /// <summary>
        /// Дождаться появления тика
        /// </summary>
        /// <param name="token">Токен отмены ожидания</param>
        public void WaitForTickers(CancellationToken token)
        {
            _waiterNotifier.Wait(token);
        }
        /// <summary>
        /// Дождаться появления тика
        /// </summary>
        /// <param name="timeout">Таймаут ожидания</param>
        /// <param name="token">Токен отмены ожидания</param>
        /// <returns>Удалось ли дождаться</returns>
        public bool WaitForTickers(int timeout, CancellationToken token)
        {
            return _waiterNotifier.Wait(timeout, token);
        }



        /// <summary>
        /// Разрешить обработку очередного тика и дождаться его
        /// </summary>
        public void AllowProcessAndWaitForTickers()
        {
            AllowProcess();
            WaitForTickers();
        }
        /// <summary>
        /// Разрешить обработку очередного тика и дождаться его
        /// </summary>
        /// <param name="timeout">Таймаут ожидания</param>
        /// <returns>Удалось ли дождаться</returns>
        public bool AllowProcessAndWaitForTickers(int timeout)
        {
            AllowProcess();
            return WaitForTickers(timeout);
        }
        /// <summary>
        /// Разрешить обработку очередного тика и дождаться его
        /// </summary>
        /// <param name="token">Токен отмены ожидания</param>
        public void AllowProcessAndWaitForTickers(CancellationToken token)
        {
            AllowProcess();
            WaitForTickers(token);
        }
        /// <summary>
        /// Разрешить обработку очередного тика и дождаться его
        /// </summary>
        /// <param name="timeout">Таймаут ожидания</param>
        /// <param name="token">Токен отмены ожидания</param>
        /// <returns>Удалось ли дождаться</returns>
        public bool AllowProcessAndWaitForTickers(int timeout, CancellationToken token)
        {
            AllowProcess();
            return WaitForTickers(timeout, token);
        }




        /// <summary>
        /// Освободить ресурсы
        /// </summary>
        public void Dispose()
        {
            _waiterNotifier.Dispose();
        }
    }
}
