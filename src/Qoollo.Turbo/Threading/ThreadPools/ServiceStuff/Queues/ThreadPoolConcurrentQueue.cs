using Qoollo.Turbo.Threading.ThreadPools.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadPools.ServiceStuff
{
#pragma warning disable 0420

    /// <summary>
    /// Многопоточная lock-free очередь
    /// </summary>
    internal class ThreadPoolConcurrentQueue
    {
        /// <summary>
        /// Сегмент очереди
        /// </summary>
        private class Segment
        {
            private const int SegmentSize = 256;

            private readonly ThreadPoolConcurrentQueue _parent;
            /// <summary>
            /// Индекс головы (меньше или равен tail)
            /// </summary>
            private volatile int _head;
            /// <summary>
            /// Индекс хвоста
            /// </summary>
            private volatile int _tail;
            private readonly ThreadPoolWorkItem[] _data;
            private volatile Segment _next;

            /// <summary>
            /// Конструктор Segment
            /// </summary>
            /// <param name="parent">Очередь - владелец сегмента</param>
            public Segment(ThreadPoolConcurrentQueue parent)
            {
                TurboContract.Requires(parent != null, conditionString: "parent != null");

                _parent = parent;
                _data = new ThreadPoolWorkItem[SegmentSize];
                _head = 0;
                _tail = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool HasElements(int head, int tail)
            {
                return head < SegmentSize && head < tail;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool HasFreeSpace(int tail)
            {
                return tail < SegmentSize;
            }


            public Segment Next { get { return _next; } }
            public bool IsEmpty { get { return !HasElements(_head, _tail); } }


            /// <summary>
            /// Добавить сегмент за нами
            /// </summary>
            private void Grow()
            {
                var tmp = new Segment(_parent);
                TurboContract.Assert(_next == null, conditionString: "_next == null");
                _next = tmp;
                TurboContract.Assert(_parent._tail == this, conditionString: "_parent._tail == this");
                _parent._tail = tmp;
            }


            /// <summary>
            /// Попробовать добавить элемент в сегмент
            /// </summary>
            /// <param name="item">Элемент</param>
            /// <returns>Удалось ли добавить</returns>
            public bool TryAdd(ThreadPoolWorkItem item)
            {
                TurboContract.Requires(item != null, conditionString: "item != null");

                if (!HasFreeSpace(_tail))
                    return false;

                bool result = false;
                try { }
                finally
                {
                    int tail = Interlocked.Increment(ref _tail) - 1;
                    if (HasFreeSpace(tail))
                    {
                        Volatile.Write(ref _data[tail], item);
                        if (tail == SegmentSize - 1)
                            Grow();
                      
                        result = true;
                    }
                }

                return result;
            }

            /// <summary>
            /// Попытаться выбрать элемент из сегмента
            /// </summary>
            /// <param name="item">Выбранный элемент</param>
            /// <returns>Удалось ли сделать выборку</returns>
            public bool TryTake(out ThreadPoolWorkItem item)
            {
                TurboContract.Ensures(TurboContract.Result<bool>() == false || TurboContract.ValueAtReturn(out item) != null);

                SpinWait sw = new SpinWait();
                int head = _head;
                while (HasElements(head, _tail))
                {
                    if (Interlocked.CompareExchange(ref _head, head + 1, head) == head)
                    {
                        SpinWait itemSw = new SpinWait();
                        ThreadPoolWorkItem localItem = null;
                        while ((localItem = Volatile.Read(ref _data[head])) == null)
                            itemSw.SpinOnce();
                        item = localItem;
                        Volatile.Write(ref _data[head], null);

                        if (head + 1 >= SegmentSize)
                        {
                            SpinWait headSw = new SpinWait();
                            while (_next == null)
                                headSw.SpinOnce();
                            _parent._head = _next;
                        }

                        return true;
                    }

                    sw.SpinOnce();
                    head = _head;
                }

                item = null;
                return false;
            }
        }


        // ==================

        private volatile Segment _head;
        private volatile Segment _tail;

        [ContractInvariantMethod]
        private void Invariant()
        {
            TurboContract.Invariant(_head != null);
            TurboContract.Invariant(_tail != null);
        }

        /// <summary>
        /// Конструктор ThreadPoolConcurrentQueue
        /// </summary>
        public ThreadPoolConcurrentQueue()
        {
            var tmp = new Segment(this);
            _head = tmp;
            _tail = tmp;
        }

        /// <summary>
        /// Пуста ли очередь
        /// </summary>
        public bool IsEmpty { get { return !this.HasElements(); } }

        /// <summary>
        /// Проверка наличия элементов
        /// </summary>
        private bool HasElements()
        {
            Segment head = _head;
            if (!head.IsEmpty)
                return true;
            if (head.Next == null)
                return false;


            SpinWait spinWait = new SpinWait();
            head = _head;

            while (head.IsEmpty)
            {
                if (head.Next == null)
                    return false;

                spinWait.SpinOnce();
                head = _head;
            }
            return true;
        }


        /// <summary>
        /// Добавить элемент в очередь
        /// </summary>
        /// <param name="item">Элемент</param>
        public void Add(ThreadPoolWorkItem item)
        {
            TurboContract.Requires(item != null, conditionString: "item != null");

            SpinWait sw = new SpinWait();
            Segment tail = _tail;

            while (!tail.TryAdd(item))
            {
                sw.SpinOnce();
                tail = _tail;
            }
        }

        /// <summary>
        /// Попробовать достать элемент из очереди
        /// </summary>
        /// <param name="item">Выбранный элемент</param>
        /// <returns>Удалось ли выбрать</returns>
        public bool TryTake(out ThreadPoolWorkItem item)
        {
            TurboContract.Ensures(TurboContract.Result<bool>() == false || TurboContract.ValueAtReturn(out item) != null);

            while (this.HasElements())
            {
                Segment head = _head;
                if (head.TryTake(out item))
                    return true;
            }
            item = null;
            return false;
        }
    }

#pragma warning restore 0420
}
