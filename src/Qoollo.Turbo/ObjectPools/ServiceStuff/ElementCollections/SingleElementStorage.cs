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
    /// Атомарное хранилище одного элемента
    /// </summary>
    /// <typeparam name="T">Тип элемента</typeparam>
    [System.Diagnostics.DebuggerDisplay("HasElement = {HasElement}")]
    internal class SingleElementStorage<T> where T: class
    {
        private volatile Thread _ownerThread;
        private volatile SingleElementStorage<T> _next;
        private T _element;

        /// <summary>
        /// Конструктор SingleElementStorage
        /// </summary>
        /// <param name="ownerThread">Поток - владелец контейнера</param>
        public SingleElementStorage(Thread ownerThread)
        {
            _ownerThread = ownerThread;
            _next = null;
        }
        /// <summary>
        /// Конструктор SingleElementStorage
        /// </summary>
        public SingleElementStorage()
        {
            _ownerThread = null;
            _next = null;
        }

        /// <summary>
        /// Поток, владеющий хранилищем
        /// </summary>
        public Thread OwnerThread { get { return _ownerThread; } }
        /// <summary>
        /// Владеет ли хоть какой-нибудь поток хранилищем
        /// </summary>
        public bool IsUnowned { get { return _ownerThread == null || _ownerThread.ThreadState == ThreadState.Stopped; } }
        /// <summary>
        /// Есть ли элемент
        /// </summary>
        public bool HasElement { get { return Volatile.Read(ref _element) != null; } }

        /// <summary>
        /// Ссылка на следующий (для организации списка)
        /// </summary>
        public SingleElementStorage<T> Next
        {
            get { return _next; }
            set { _next = value; }
        }


        /// <summary>
        /// Установить владельца
        /// </summary>
        /// <param name="ownerThread">Поток-владелец</param>
        public void SetOwnerThread(Thread ownerThread)
        {
            Contract.Requires(ownerThread != null);

            Contract.Assert(this.IsUnowned);
            _ownerThread = ownerThread;
        }



        /// <summary>
        /// Попытаться получить элемент
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <returns>Успешность</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryTake(out T element)
        {
            element = Interlocked.Exchange(ref _element, null);
            return element != null;
        }

        /// <summary>
        /// Попытаться добавить элемент
        /// </summary>
        /// <param name="element">Элемент</param>
        /// <returns>Успешность</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(T element)
        {
            Contract.Requires(element != null);
            return Interlocked.CompareExchange(ref _element, element, null) == null;
        }
    }
}
