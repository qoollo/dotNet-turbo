using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools.ServiceStuff.ElementCollections
{
    /// <summary>
    /// Разреженый массив
    /// </summary>
    /// <typeparam name="T">Тип элементов массива</typeparam>
    [System.Diagnostics.DebuggerDisplay("Count = {Count}")]
    [System.Diagnostics.DebuggerTypeProxy(typeof(SparceArrayStorageDebugView<>))]
    internal class SparceArrayStorage<T> where T: class
    {
        private const int BlockSize = 16;
        private const int CompactionOverhead = BlockSize + BlockSize / 2;

        private readonly object _syncObj = new object();
        private readonly bool _allowCompactionOnRemove;

        private volatile T[] _data;
        private volatile int _count;

        /// <summary>
        /// Конструктор SparceArrayStorage
        /// </summary>
        /// <param name="allowCompactionOnRemove">Можно ли автоматически перетасовывать элементы (отключить, если есть зависимость от индексов)</param>
        public SparceArrayStorage(bool allowCompactionOnRemove)
        {
            _allowCompactionOnRemove = allowCompactionOnRemove;
            _data = new T[8];
            _count = 0;
        }
        /// <summary>
        /// Конструктор SparceArrayStorage
        /// </summary>
        public SparceArrayStorage()
            : this(false)
        {
        }

        /// <summary>
        /// Сырые данные
        /// </summary>
        public T[] RawData { get { return _data; } }
        /// <summary>
        /// Количество элементов
        /// </summary>
        public int Count { get { return _count; } }
        /// <summary>
        /// Вместимость массива
        /// </summary>
        public int Capacity { get { return _data.Length; } }


        /// <summary>
        /// Получить элемент по индексу
        /// </summary>
        /// <param name="index">Индекс</param>
        /// <returns>Элемент</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetItem(int index)
        {
            return Volatile.Read(ref _data[index]);
        }
        /// <summary>
        /// Получить элемент по индексу
        /// </summary>
        /// <param name="index">Индекс</param>
        /// <returns>Элемент</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetItemSafe(int index)
        {
            var data = _data;
            if (index >= data.Length)
                return null;

            return Volatile.Read(ref data[index]);
        }


        /// <summary>
        /// Найти индекс элемента (-1 - если не найден)
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <returns>Индекс (-1 - если не найден)</returns>
        [Pure]
        public int IndexOf(T element)
        {
            if (element == null)
                return -1;

            T[] data = _data;
            for (int i = 0; i < data.Length; i++)
            {
                var curElem = Volatile.Read(ref data[i]);
                if (curElem != null && object.ReferenceEquals(curElem, element))
                    return i;
            }

            return -1;
        }


        /// <summary>
        /// Расширить массив, если недостаточно места
        /// </summary>
        /// <returns>Новый массив после расширения</returns>
        private T[] IncreaseLength()
        {
            var data = _data;
            int newSize = data.Length < BlockSize ? BlockSize : data.Length + BlockSize;
            Array.Resize(ref data, newSize);
            _data = data;
            return data;
        }


        /// <summary>
        /// Добавить элемент в массив
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <returns>Индекс в массиве</returns>
        public int Add(T element)
        {
            Contract.Requires(element != null);

            lock (_syncObj)
            {
                T[] data = _data;
                int index = -1;
                if (data.Length > _count)
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (Volatile.Read(ref data[i]) == null)
                        {
                            index = i;
                            break;
                        }
                    }
                }
                else
                {
                    index = data.Length;
                    data = IncreaseLength();
                }

                Contract.Assert(index >= 0);
                Contract.Assert(index < data.Length);
                Contract.Assert(data[index] == null);

                try { }
                finally
                {
                    Volatile.Write(ref data[index], element);
                    _count++;
                }

                return index;
            }
        }


        /// <summary>
        /// Основной код удаления элемента
        /// </summary>
        /// <param name="index">Индекс элемента</param>
        /// <returns>Успешность</returns>
        private bool RemoveCore(int index)
        {
            Contract.Requires(index >= 0);
            Contract.Requires(index < _data.Length);

            T[] data = _data;
            if (Volatile.Read(ref data[index]) == null)
                return false;

            try { }
            finally
            {
                Volatile.Write(ref data[index], null);
                _count--;
            }

            if (_allowCompactionOnRemove)
                CompactWithElementMovingCore();

            return true;
        }
        /// <summary>
        /// Удалить элемент по индексу
        /// </summary>
        /// <param name="index">Индекс</param>
        /// <returns>Был ли удалён элемент</returns>
        public bool RemoveAt(int index)
        {
            Contract.Requires(index >= 0);

            lock (_syncObj)
            {
                T[] data = _data;
                if (index >= data.Length)
                    return false;

                return RemoveCore(index);
            }
        }
        /// <summary>
        /// Удалить элемент по значению
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <returns>Был ли удалён элемент</returns>
        public bool Remove(T element)
        {
            Contract.Requires(element != null);

            lock (_syncObj)
            {
                T[] data = _data;
                for (int i = 0; i < data.Length; i++)
                {
                    if (object.ReferenceEquals(Volatile.Read(ref data[i]), element))
                        return RemoveCore(i);
                }

                return false;
            }
        }


        /// <summary>
        /// Сжать массив. Разрешено перемещать элементы.
        /// </summary>
        private void CompactWithElementMovingCore()
        {
            T[] data = _data;
            int count = _count;

            if (data.Length - count < CompactionOverhead || data.Length <= BlockSize)
                return;

            int newSize = count + (BlockSize - (count % BlockSize));
            Contract.Assert(newSize < data.Length);

            T[] newData = new T[newSize];
            int targetIndex = 0;
            for (int i = 0; i < data.Length; i++)
            {
                var curElem = Volatile.Read(ref data[i]);
                if (curElem == null)
                    continue;

                newData[targetIndex] = curElem;
                targetIndex++;
            }


            _data = newData;
        }


        /// <summary>
        /// Сжать массив. Элементы двигать запрещено.
        /// </summary>
        private void CompactCore()
        {
            T[] data = _data;
            int count = _count;

            if (data.Length - count < CompactionOverhead || data.Length <= BlockSize)
                return;

            int emptyBackCount = 0;
            for (int i = data.Length - 1; i >= 0; i--)
            {
                if (Volatile.Read(ref data[i]) != null)
                    break;
                emptyBackCount++;
            }

            if (emptyBackCount < CompactionOverhead)
                return;

            int copyCount = data.Length - emptyBackCount;
            int newSize = copyCount + (BlockSize - (copyCount % BlockSize));

            Contract.Assert(newSize < data.Length);

            T[] newData = new T[newSize];
            for (int i = 0; i < newData.Length; i++)
                newData[i] = Volatile.Read(ref data[i]);

            _data = newData;
        }

        /// <summary>
        /// Функция сжатия
        /// </summary>
        /// <param name="allowElementMoving">Разрешено ли двигать элементы</param>
        private void CompactSync(bool allowElementMoving)
        {
            lock (_syncObj)
            {
                if (allowElementMoving)
                    CompactWithElementMovingCore();
                else
                    CompactCore();
            }
        }

        /// <summary>
        /// Сжать массив
        /// </summary>
        /// <param name="allowElementMoving">Разрешено ли двигать элементы</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Compact(bool allowElementMoving)
        {
            int count = _count;
            T[] data = _data;
            if (data.Length - count < CompactionOverhead || data.Length <= BlockSize)
                return;

            CompactSync(allowElementMoving);
        }


        /// <summary>
        /// Сжать массив (разрешено передвинуть только элемент с указанным индексом)
        /// </summary>
        /// <param name="index">Индекс элемента, который можно переместить</param>
        private void CompactElementAtCore(ref int index)
        {
            Contract.Requires(index >= 0);
            Contract.Requires(index < _data.Length);

            T[] data = _data;
            int count = _count;

            if (data.Length - count < CompactionOverhead || data.Length <= BlockSize)
                return;

            int expectedSizeAfterComaction = count + (BlockSize - (count % BlockSize));

            if (Volatile.Read(ref data[index]) != null && index >= expectedSizeAfterComaction)
            {
                for (int i = 0; i < index; i++)
                {
                    if (Volatile.Read(ref data[i]) == null)
                    {
                        try { }
                        finally
                        {
                            var tmp = Volatile.Read(ref data[index]);
                            Volatile.Write(ref data[index], null);
                            Volatile.Write(ref data[i], tmp);
                            Volatile.Write(ref index, i);
                        }
                        break;
                    }
                }
            }


            CompactCore();
        }


        /// <summary>
        /// Функция сжатия массива с передвижением указанного элемента
        /// </summary>
        /// <param name="index">Индекс элемента, который можно переместить</param>
        private void CompactElementAtSync(ref int index)
        {
            lock (_syncObj)
            {
                CompactElementAtCore(ref index);
            }
        }

        /// <summary>
        /// Сжать массив (разрешено передвинуть только элемент с указанным индексом)
        /// </summary>
        /// <param name="index">Индекс элемента, который можно переместить. Результурующий индекс будет записан туда же.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CompactElementAt(ref int index)
        {
            Contract.Requires(index >= 0);

            int count = _count;
            T[] data = _data;
            if (data.Length - count < CompactionOverhead || data.Length <= BlockSize)
                return;

            int expectedSizeAfterComaction = count + (BlockSize - (count % BlockSize));
            if (index < expectedSizeAfterComaction)
                return;

            CompactElementAtSync(ref index);
        }
    }


    /// <summary>
    /// Debug-view для SparceArrayStorage
    /// </summary>
    internal sealed class SparceArrayStorageDebugView<T> where T: class
    {
        private SparceArrayStorage<T> _original;

        public SparceArrayStorageDebugView(SparceArrayStorage<T> original)
        {
            _original = original;
        }

        public int Count { get { return _original.Count; } }
        public T[] RawData { get { return _original.RawData; } }
        public T[] DataOnly { get { return _original.RawData.Where(o => o != null).ToArray(); } }
    }
}
