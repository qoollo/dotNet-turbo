using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools.Common
{
    /// <summary>
    /// Враппер над элементами объектного пула
    /// </summary>
    [DebuggerDisplay("IsBusy = {IsBusy}, IsDestroyed = {IsElementDestroyed}")]
    public class PoolElementWrapper
    {
        private readonly object _owner;

        private int _isBusy;
        private volatile bool _isElementdDestroyed;
        private volatile bool _isRemoved;

        /// <summary>
        /// Конструктор PoolElementWrapper
        /// </summary>
        /// <param name="owner">Объект-владелец</param>
        public PoolElementWrapper(object owner)
        {
            _owner = owner;
            _isBusy = 0;
            _isRemoved = false;
            _isElementdDestroyed = false;
            ThisIndex = -1;
            NextIndex = -1;
        }
        /// <summary>
        /// Конструктор PoolElementWrapper
        /// </summary>
        public PoolElementWrapper()
            : this(null)
        {
        }


        /// <summary>
        /// Мой индекс в SparceArray
        /// </summary>
        internal volatile int ThisIndex;
        /// <summary>
        /// Индекс следующего за мной в SparceArray
        /// </summary>
        internal volatile int NextIndex;



        /// <summary>
        /// Занят ли элемент
        /// </summary>
        public bool IsBusy { get { return Volatile.Read(ref _isBusy) != 0; } }
        /// <summary>
        /// Удалён ли враппер
        /// </summary>
        public bool IsRemoved { get { return _isRemoved; } }
        /// <summary>
        /// Уничтожен ли оборачиваемый элемент
        /// </summary>
        public bool IsElementDestroyed { get { return _isElementdDestroyed; } }
        /// <summary>
        /// Владелец элемента
        /// </summary>
        public object Owner { get { return _owner; } }



        /// <summary>
        /// Пометить, что элемент уничтожен
        /// </summary>
        public void MarkElementDestroyed()
        {
            Contract.Assert(!this.IsElementDestroyed, "Can't destroy element 2 times");
            _isElementdDestroyed = true;
        }
        /// <summary>
        /// Пометить, что элемент окончательно удалён
        /// </summary>
        public void MarkRemoved()
        {
            Contract.Assert(this.IsElementDestroyed, "Trying to remove Pool Element that was not destroyed");
            Contract.Assert(!this.IsRemoved, "Can't remove element 2 times");
            _isRemoved = true;
        }



        /// <summary>
        /// Пометить, что элемент занят
        /// </summary>
        protected internal void MakeBusy()
        {
#if DEBUG
            MakeBusyAtomic();
#else

            Contract.Assert(Volatile.Read(ref _isBusy) == 0, "Pool Element is already busy");
            Volatile.Write(ref _isBusy, 1);
#endif
        }
        /// <summary>
        /// Освободить элемент
        /// </summary>
        protected internal void MakeAvailable()
        {
#if DEBUG
            MakeAvailableAtomic();
#else
            Contract.Assert(Volatile.Read(ref _isBusy) == 1, "Pool Element is already available");
            Volatile.Write(ref _isBusy, 0);
#endif
        }

        /// <summary>
        /// Попробовать сделать элемент занятым
        /// </summary>
        /// <returns>Удалось ли</returns>
        protected internal bool TryMakeBusyAtomic()
        {
            return Interlocked.CompareExchange(ref _isBusy, 1, 0) == 0;
        }
        /// <summary>
        /// Сделать элемент занятым (атомарно)
        /// </summary>
        protected internal void MakeBusyAtomic()
        {
            int prevIsBusy = Interlocked.Exchange(ref _isBusy, 1);
            Contract.Assert(prevIsBusy == 0, "Pool Element is already busy");
        }
        /// <summary>
        /// Сделать элемент доступным (атомарно)
        /// </summary>
        protected internal void MakeAvailableAtomic()
        {
            int prevIsBusy = Interlocked.Exchange(ref _isBusy, 0);
            Contract.Assert(prevIsBusy == 1, "Pool Element is already available");
        }

#if DEBUG

        private bool _finalizerReRegistered;

        /// <summary>
        /// Get string with diagnostic info for this wrapper
        /// </summary>
        /// <returns>Diagnostic</returns>
        protected virtual string CollectDiagnosticInfo()
        {
            return string.Empty;
        }

        /// <summary>
        /// Финализатор
        /// </summary>
        ~PoolElementWrapper()
        {
            if (!this.IsElementDestroyed && !_finalizerReRegistered)
            {
                GC.ReRegisterForFinalize(this);
                if (GC.GetGeneration(this) >= GC.MaxGeneration)
                    _finalizerReRegistered = true;
            }
            else
            {
                Contract.Assert(this.IsElementDestroyed, "Element should be destroyed before removing the PoolElementWrapper. Probably you forget to call Dispose on the owner pool. Info: " + this.CollectDiagnosticInfo());
            }
        }
#endif
    }


    /// <summary>
    /// Типизированный враппер над элементами объектного пула
    /// </summary>
    /// <typeparam name="T">Тип элемента</typeparam>
    [DebuggerDisplay("IsValid = {IsValid}, IsDestroyed = {IsElementDestroyed}, IsBusy = {IsBusy}")]
    public class PoolElementWrapper<T> : PoolElementWrapper
    {
        private string _sourcePoolName;
        private readonly T _element;
        private readonly IPoolElementOperationSource<T> _operations;

        private string _lastTimeUsedAtMemberName;
        private string _lastTimeUsedAtFilePath;
        private int _lastTimeUsedAtLineNumber;
        private DateTime _lastTimeUsedAtTime;
        private DateTime _createTime;
        private int _numberOfTimesWasRented;
        private int _numberOfTimesWasReleased;

        private bool _sourcePoolDisposed;

        /// <summary>
        /// Конструктор PoolElementWrapper
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <param name="operations">Помошник с операциями для элемента</param>
        /// <param name="owner">Владелец</param>
        public PoolElementWrapper(T element, IPoolElementOperationSource<T> operations, object owner)
            : base(owner)
        {
            Contract.Requires<ArgumentNullException>(operations != null);

            _element = element;
            _operations = operations;
            _createTime = DateTime.Now;
        }

        /// <summary>
        /// Оборачиваемый элемент
        /// </summary>
        public T Element { get { return _element; } }
        /// <summary>
        /// Валиден ли элемент
        /// </summary>
        public bool IsValid { get { return !IsElementDestroyed && _operations.IsValid(this); } }
        /// <summary>
        /// Имя пула-владельца
        /// </summary>
        internal string SourcePoolName { get { return _sourcePoolName; } }

        /// <summary>
        /// Уничтожен ли родительский пул
        /// </summary>
        internal bool SourcePoolDisposed { get { return _sourcePoolDisposed; } }

        /// <summary>
        /// Иия метода, в котором произошло последнее получение элемента пула
        /// </summary>
        internal string LastTimeUsedAtMemberName { get { return _lastTimeUsedAtMemberName; } }
        /// <summary>
        /// Путь до файла, в котором произошло последнее получение элемента пула
        /// </summary>
        internal string LastTimeUsedAtFilePath { get { return _lastTimeUsedAtFilePath; } }
        /// <summary>
        /// Строка файла, в которой произошло последнее получение элемента пула
        /// </summary>
        internal int LastTimeUsedAtLineNumber { get { return _lastTimeUsedAtLineNumber; } }
        /// <summary>
        /// Время, когда элемент был арендован в последний раз
        /// </summary>
        internal DateTime LastTimeUsedAtTime { get { return _lastTimeUsedAtTime; } }
        /// <summary>
        /// Время создания элемента
        /// </summary>
        internal DateTime CreateTime { get { return _createTime; } }
        /// <summary>
        /// Сколько раз арендовали этот элемент
        /// </summary>
        internal int NumberOfTimesWasRented { get { return _numberOfTimesWasRented; } }
        /// <summary>
        /// Сколько раз элемент был освобождён
        /// </summary>
        internal int NumberOfTimesWasReleased { get { return _numberOfTimesWasReleased; } }

        /// <summary>
        /// Задать имя пула-владельца
        /// </summary>
        /// <param name="name">Имя</param>
        internal void SetPoolName(string name)
        {
            Contract.Requires(name != null);
            _sourcePoolName = name;
        }
        /// <summary>
        /// Помечает, что родительский пул уничтожен
        /// </summary>
        internal void SetPoolDisposed()
        {
            _sourcePoolDisposed = true;
        }

        /// <summary>
        /// Обновить статистику при аренде
        /// </summary>
        /// <param name="memberName">Имя метода</param>
        /// <param name="filePath">Путь до файла</param>
        /// <param name="lineNumber">Номер строки в файле</param>
        /// <param name="rentTime">Время аренды</param>
        internal void UpdateStatOnRent(string memberName, string filePath, int lineNumber, DateTime rentTime)
        {
            _lastTimeUsedAtMemberName = memberName;
            _lastTimeUsedAtFilePath = filePath;
            _lastTimeUsedAtLineNumber = lineNumber;
            _lastTimeUsedAtTime = rentTime;
            Interlocked.Increment(ref _numberOfTimesWasRented);
        }
        /// <summary>
        /// Обновить статистику при освобождении
        /// </summary>
        internal void UpdateStatOnRelease()
        {
            Interlocked.Increment(ref _numberOfTimesWasReleased);
        }


#if DEBUG
        /// <summary>
        /// Get string with diagnostic info for this wrapper
        /// </summary>
        /// <returns>Diagnostic</returns>
        protected override string CollectDiagnosticInfo()
        {
            StringBuilder res = new StringBuilder();
            res.AppendFormat("PoolName = '{0}', ", _sourcePoolName ?? "");
            res.AppendFormat("SourcePoolDisposed = {0}, ", SourcePoolDisposed);
            res.AppendFormat("IsBusy = {0}, ", IsBusy);
            res.AppendFormat("IsRemoved = {0}, ", IsRemoved);
            res.AppendFormat("IsDestroyed = {0}, ", IsElementDestroyed);
            res.AppendFormat("NumberOfTimesRented = {0}, ", _numberOfTimesWasRented);
            res.AppendFormat("NumberOfTimesReleased = {0}", _numberOfTimesWasReleased);
            return res.ToString();
        }
#endif
    }
}
