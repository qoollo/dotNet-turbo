using Qoollo.Turbo.Threading.ThreadPools.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadPools.ServiceStuff
{
    /// <summary>
    /// Локальная очередь-стек для отдельного потока
    /// </summary>
    internal class ThreadPoolLocalQueue
    {
        private const int QueueSize = 32;
        private const int NormalizeThreshold = int.MaxValue / 2;

        /// <summary>
        /// Индекс головы очереди (начала). Должен быть меньше или равен tail
        /// </summary>
        private volatile int _head;
        /// <summary>
        /// Индекс хвоста очереди и стека
        /// </summary>
        private volatile int _tail;
        private readonly ThreadPoolWorkItem[] _data;
        private readonly object _syncObj = new object();

        
        public ThreadPoolLocalQueue()
        {
            _data = new ThreadPoolWorkItem[QueueSize];
            _head = 0;
            _tail = 0;
        }


        //private long head_tail_Upd = 0;
        //private void DoCheck(long val)
        //{
        //    if ((val & ((1L << 32) - 1)) < (val >> 32))
        //        throw new Exception("check fail. head = " + (val >> 32).ToString() + ", tail = " + (val & ((1L << 32) - 1)).ToString());
        //}
        //private void NewElementToTail()
        //{
        //    DoCheck(Interlocked.Increment(ref head_tail_Upd));
        //}
        //private void RemoveElementFromTail()
        //{
        //    DoCheck(Interlocked.Decrement(ref head_tail_Upd));
        //}
        //private void RemoveElementFromHead()
        //{
        //    DoCheck(Interlocked.Add(ref head_tail_Upd, (1L << 32)));
        //}


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasElements(int head, int tail)
        {
            return tail > head;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasFreeSpace(int head, int tail)
        {
            return tail - head < QueueSize - 1;
        }

        /// <summary>
        /// Нормировка индексов
        /// </summary>
        /// <returns>Новый индекс хвоста (tail)</returns>
        private int NormHeadTail()
        {
            int result = 0;
            lock (_syncObj)
            {
                result = _tail;
                if (result > NormalizeThreshold)
                {
                    int head = _head % QueueSize;
                    int tail = _tail % QueueSize;

                    if (tail < head)
                        tail += QueueSize;

                    try { }
                    finally
                    {
                        _head = head;
                        _tail = tail;
                    }
                  
                    result = tail;
                }
            }
            return result;
        }


        /// <summary>
        /// Выполнить локальное добавление (должно вызываться из потока-владельца)
        /// </summary>
        /// <param name="item">Новый элемент</param>
        /// <returns>Удалось ли добавить</returns>
        public bool TryAddLocal(ThreadPoolWorkItem item)
        {
            TurboContract.Requires(item != null, conditionString: "item != null");

            int tail = _tail;
            if (tail > NormalizeThreshold)
                tail = NormHeadTail();

            if (!HasFreeSpace(_head, tail))
                return false;

            Volatile.Write(ref _data[tail % QueueSize], item);
            //NewElementToTail();
            _tail = tail + 1;
            return true;
        }

        /// <summary>
        /// Выборка элемента из хвоста (должно вызываться из потока-владельца)
        /// </summary>
        /// <param name="item">Выбранный элемент</param>
        /// <returns>Удалось ли сделать выборку</returns>
        public bool TryTakeLocal(out ThreadPoolWorkItem item)
        {
            TurboContract.Ensures(TurboContract.Result<bool>() == false || TurboContract.ValueAtReturn(out item) != null);

            int tail = _tail;
            if (HasElements(_head, tail))
            {
                item = Interlocked.Exchange(ref _data[(tail - 1) % QueueSize], null);
                if (item != null)
                {
                    //RemoveElementFromTail();
                    _tail = tail - 1;
                    return true;
                }
            }

            item = null;
            return false;
        }


        /// <summary>
        /// Выборка элемента из головы (может вызываться из любого потока)
        /// </summary>
        /// <param name="item">Выбранный элемент</param>
        /// <returns>Удалось ли сделать выборку</returns>
        public bool TrySteal(out ThreadPoolWorkItem item)
        {
            TurboContract.Ensures(TurboContract.Result<bool>() == false || TurboContract.ValueAtReturn(out item) != null);

            if (HasElements(_head, _tail))
            {
                lock (_syncObj)
                {
                    int head = _head;
                    if (HasElements(head, _tail))
                    {
                        item = Interlocked.Exchange(ref _data[head % QueueSize], null);
                        if (item != null)
                        {
                            //RemoveElementFromHead();
                            _head = head + 1;
                            return true;
                        }
                    }
                }
            }

            item = null;
            return false;
        }
    }
}
