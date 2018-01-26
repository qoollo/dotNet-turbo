using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ServiceStuff
{
    /// <summary>
    /// Делегат для вызова из потока обслуживания
    /// </summary>
    /// <param name="elapsedMs">Прошедшее время с последнего успешного исполнения</param>
    /// <returns>Произведено ли действие (true - сбрасывает счётчик времени)</returns>
    internal delegate bool ManagementThreadControllerCallback(int elapsedMs);

    /// <summary>
    /// Контроллер потока обслуживания вспомогательных задач
    /// </summary>
    internal class ManagementThreadController
    {
        private static ManagementThreadController _instance;
        private static readonly object _syncObject = new object();

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void InitSingleton()
        {
            if (_instance == null)
            {
                lock (_syncObject)
                {
                    if (_instance == null)
                        _instance = new ManagementThreadController();
                }
            }
        }

        /// <summary>
        /// Инстанс синглтона
        /// </summary>
        public static ManagementThreadController Instance
        {
            get
            {
                if (_instance == null)
                    InitSingleton();
                return _instance;
            }
        }

        // =================

        /// <summary>
        /// Получить временной маркер в миллисекундах
        /// </summary>
        /// <returns>Временной маркер</returns>
        private static uint GetTimestamp()
        {
            return (uint)Environment.TickCount;
        }


        /// <summary>
        /// Контейнер для данных подписчика
        /// </summary>
        private class CallbackItem
        {
            private readonly ManagementThreadControllerCallback _callback;
            private uint _startTimeMs;

            public CallbackItem(ManagementThreadControllerCallback callback)
            {
                TurboContract.Requires(callback != null, conditionString: "callback != null");

                _callback = callback;
                _startTimeMs = ManagementThreadController.GetTimestamp();
            }

            public ManagementThreadControllerCallback Callback { get { return _callback; } }
            public uint StartTime { get { return _startTimeMs; } }

            /// <summary>
            /// Рассылает уведомление
            /// </summary>
            /// <param name="currentTimestamp">Текущее время</param>
            public void Notify(uint currentTimestamp)
            {
                uint elapsed = (currentTimestamp - _startTimeMs);
                if (elapsed > int.MaxValue)
                    elapsed = int.MaxValue;

                if (_callback((int)elapsed))
                    _startTimeMs = currentTimestamp;
            }
        }


        // ============

        /// <summary>
        /// Период рассылки уведомлений
        /// </summary>
        public const int SleepPeriod = 500;
        private const int ExtendedSleepPeriod = 2000;

        private readonly Thread _thread;
        private readonly List<CallbackItem> _items;
        private readonly List<ManagementThreadControllerCallback> _pendingAddItems;
        private readonly List<ManagementThreadControllerCallback> _pendingRemoveItems;

        /// <summary>
        /// Конструктор ManagementThreadController
        /// </summary>
        private ManagementThreadController()
        {
            _thread = new Thread(new ThreadStart(ThreadFunc));
            _thread.Name = "Qoollo.Turbo ManagementThreadController thread";
            _thread.IsBackground = true;

            _items = new List<CallbackItem>(4);
            _pendingAddItems = new List<ManagementThreadControllerCallback>(4);
            _pendingRemoveItems = new List<ManagementThreadControllerCallback>(4);


            _thread.Start();
        }

        /// <summary>
        /// Зарегистрировать коллбэк для вызова в потоке менеджмента
        /// </summary>
        /// <param name="callback">Коллбэк</param>
        public void RegisterCallback(ManagementThreadControllerCallback callback)
        {
            TurboContract.Requires(callback != null, conditionString: "callback != null");

            lock (_pendingAddItems)
            {
                _pendingAddItems.Add(callback);
            }
        }
        /// <summary>
        /// Снять регистрацию колбэка
        /// </summary>
        /// <param name="callback">Коллбэк</param>
        public void UnregisterCallback(ManagementThreadControllerCallback callback)
        {
            TurboContract.Requires(callback != null, conditionString: "callback != null");
            lock (_pendingRemoveItems)
            {
                _pendingRemoveItems.Add(callback);
            }
        }


        /// <summary>
        /// Обновление _items из _pendingAddItems и _pendingRemoveItems
        /// </summary>
        private void UpdateItems()
        {
            if (_pendingAddItems.Count > 0)
            {
                lock (_pendingAddItems)
                {
                    foreach (var addItem in _pendingAddItems)
                    {
                        bool found = false;
                        foreach (var existItem in _items)
                        {
                            if (existItem.Callback == addItem)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                            _items.Add(new CallbackItem(addItem));
                    }

                    _pendingAddItems.Clear();
                }
            }
            if (_pendingRemoveItems.Count > 0)
            {
                lock (_pendingRemoveItems)
                {
                    foreach (var removeItem in _pendingRemoveItems)
                    {
                        for (int i = 0; i < _items.Count; i++)
                        {
                            if (_items[i].Callback == removeItem)
                            {
                                _items.RemoveAt(i);
                                break;
                            }
                        }
                    }

                    _pendingRemoveItems.Clear();
                }
            }
        }

        /// <summary>
        /// Функция потока
        /// </summary>
        private void ThreadFunc()
        {
            while (true)
            {
                UpdateItems();

                var timestamp = GetTimestamp();
                foreach (var item in _items)
                    item.Notify(timestamp);


                if (_items.Count > 0)
                    Thread.Sleep(SleepPeriod);
                else
                    Thread.Sleep(ExtendedSleepPeriod);
            }
        }
    }
}
