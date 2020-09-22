using Qoollo.Turbo.ObjectPools.Common;
using Qoollo.Turbo.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools.ServiceStuff.ElementCollections
{
    /// <summary>
    /// Lock-free стек на базе односвязанного списка. 
    /// Стек строится по индексам в SpaceArray. Пока элемент в стеке, его индекс меняться не должен.
    /// </summary>
    [System.Diagnostics.DebuggerTypeProxy(typeof(IndexedStackElementStorageDebugView<>))]
    internal class IndexedStackElementStorage<T>
    {
        /// <summary>
        /// Индекс, который означает, что элементов нет (== -1)
        /// </summary>
        private const int NoElementHeadIndex = (1 << 16) - 1;

        /// <summary>
        /// Хранит индекс головного элемента, а также номер операции для предотвращения ABA
        /// </summary>
        private int _headIndexOp;
        private readonly SparceArrayStorage<PoolElementWrapper<T>> _dataArray;

        /// <summary>
        /// Конструктор IndexedStackElementStorage
        /// </summary>
        /// <param name="dataArray">Массив данных</param>
        public IndexedStackElementStorage(SparceArrayStorage<PoolElementWrapper<T>> dataArray)
        {
            TurboContract.Requires(dataArray != null, conditionString: "dataArray != null");

            _headIndexOp = Repack(NoElementHeadIndex, 1);
            _dataArray = dataArray;
        }

        /// <summary>
        /// Получить индекс головы в headIndexOp
        /// </summary>
        /// <param name="headIndexOp">Значение headIndexOp</param>
        /// <returns>индекс головы</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetHeadIndex(int headIndexOp)
        {
            var result = (headIndexOp >> 16) & ((1 << 16) - 1);
            if (result == NoElementHeadIndex)
                return -1;
            return result;
        }
        /// <summary>
        /// Получить номер операции в headIndexOp
        /// </summary>
        /// <param name="headIndexOp">Значение headIndexOp</param>
        /// <returns>Номер операции</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetOp(int headIndexOp)
        {
            return (headIndexOp & ((1 << 16) - 1));
        }
        /// <summary>
        /// Запаковать новый индекс, увеличив счётчик операций
        /// </summary>
        /// <param name="newHead">Новый индекс головы</param>
        /// <param name="curHeadIndexOp">Текущее значение headIndexOp</param>
        /// <returns>Перепакованный headIndexOp</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Repack(int newHead, int curHeadIndexOp)
        {
            if (newHead < 0)
                newHead = NoElementHeadIndex;
            int newOp = (GetOp(curHeadIndexOp) + 1) & ((1 << 16) - 1);
            return (newHead << 16) | newOp;
        }


        /// <summary>
        /// Индекс головы
        /// </summary>
        internal int HeadIndex { get { return GetHeadIndex(Volatile.Read(ref _headIndexOp)); } }
        /// <summary>
        /// Массив данных
        /// </summary>
        internal SparceArrayStorage<PoolElementWrapper<T>> DataArray { get { return _dataArray; } }



        private void AddCore(PoolElementWrapper<T> element)
        {
            SpinWait sw = new SpinWait();
            var headIndexOp = _headIndexOp;
            element.NextIndex = GetHeadIndex(headIndexOp);
            while (Interlocked.CompareExchange(ref _headIndexOp, Repack(element.ThisIndex, headIndexOp), headIndexOp) != headIndexOp)
            {
                sw.SpinOnce();
                headIndexOp = _headIndexOp;
                element.NextIndex = GetHeadIndex(headIndexOp);
            }
        }

        /// <summary>
        /// Добавить элемент в стек
        /// </summary>
        /// <param name="element">Элемент</param>
        public void Add(PoolElementWrapper<T> element)
        {
            TurboContract.Requires(element != null, conditionString: "element != null");
            TurboContract.Requires(element.ThisIndex >= 0, conditionString: "element.ThisIndex >= 0");
            TurboContract.Requires(element.ThisIndex < (1 << 16), conditionString: "element.ThisIndex < (1 << 16)");
            TurboContract.Requires(element.NextIndex < 0, conditionString: "element.NextIndex < 0");

            TurboContract.Assert(element.ThisIndex == DataArray.IndexOf(element), conditionString: "element.ThisIndex == DataArray.IndexOf(element)");

            var headIndexOp = _headIndexOp;
            element.NextIndex = GetHeadIndex(headIndexOp);
            if (Interlocked.CompareExchange(ref _headIndexOp, Repack(element.ThisIndex, headIndexOp), headIndexOp) == headIndexOp)
                return;


            AddCore(element);
        }



        private bool TryTakeCore(out PoolElementWrapper<T> element)
        {
            SpinWait sw = new SpinWait();
            var headIndexOp = _headIndexOp;

            while (GetHeadIndex(headIndexOp) >= 0)
            {
                var headElem = _dataArray.GetItemSafe(GetHeadIndex(headIndexOp));
                if (headElem != null && Interlocked.CompareExchange(ref _headIndexOp, Repack(headElem.NextIndex, headIndexOp), headIndexOp) == headIndexOp)
                {
                    element = headElem;
                    element.NextIndex = -1;
                    return true;
                }

                sw.SpinOnce();

                headIndexOp = _headIndexOp;
            }

            element = null;
            return false;
        }

        /// <summary>
        /// Попробовать забрать элемент из стека
        /// </summary>
        /// <param name="element">Вытянутый элемент</param>
        /// <returns>Успешность</returns>
        public bool TryTake(out PoolElementWrapper<T> element)
        {
            var headIndexOp = _headIndexOp;
            if (GetHeadIndex(headIndexOp) < 0)
            {
                element = null;
                return false;
            }

            var headElem = _dataArray.GetItemSafe(GetHeadIndex(headIndexOp));
            if (headElem != null && Interlocked.CompareExchange(ref _headIndexOp, Repack(headElem.NextIndex, headIndexOp), headIndexOp) == headIndexOp)
            {
                element = headElem;
                element.NextIndex = -1;
                return true;
            }

            return TryTakeCore(out element);
        }
    }


    /// <summary>
    /// Debug-view для IndexedStackElementStorage
    /// </summary>
    internal class IndexedStackElementStorageDebugView<T>
    {
        private IndexedStackElementStorage<T> _original;

        public IndexedStackElementStorageDebugView(IndexedStackElementStorage<T> original)
        {
            _original = original;
        }

        public int Count
        {
            get
            {
                int result = 0;
                for (var cur = _original.HeadIndex; cur >= 0; cur = _original.DataArray.GetItem(cur).NextIndex)
                    result++;

                return result;
            }
        }

        public PoolElementWrapper<T>[] Data
        {
            get
            {
                List<PoolElementWrapper<T>> result = new List<PoolElementWrapper<T>>();
                for (var cur = _original.HeadIndex; cur >= 0; cur = _original.DataArray.GetItem(cur).NextIndex)
                    result.Add(_original.DataArray.GetItem(cur));

                return result.ToArray();
            }
        }
    }
}
