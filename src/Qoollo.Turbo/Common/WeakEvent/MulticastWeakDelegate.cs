using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.Contracts;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Слабо связанное событие
    /// </summary>
    /// <typeparam name="T">Тип делегата</typeparam>
    public class MulticastWeakDelegate<T> where T : class
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_locker != null);
            Contract.Invariant(_handlers != null);
        }
 
        private object _locker;
        private List<WeakDelegate> _handlers;

        /// <summary>
        /// Статический конструктор для проверки валидности типа T
        /// </summary>
        static MulticastWeakDelegate()
        {
            Contract.Assume(typeof(T).IsSubclassOf(typeof(Delegate)));
        }

        /// <summary>
        /// Конструктор MulitcastWeakDelegate
        /// </summary>
        public MulticastWeakDelegate()
        {
            _locker = new object();
            _handlers = new List<WeakDelegate>();
        }

        /// <summary>
        /// Подписка на событие
        /// </summary>
        /// <param name="reference">Делегат</param>
        public void Add(T reference)
        {
            Contract.Requires(reference != null);
            Contract.Assume(reference is Delegate);

            if (reference is MulticastDelegate)
            {
                var invList = (reference as MulticastDelegate).GetInvocationList();
                lock (_locker)
                {
                    for (int i = 0; i < invList.Length; i++)
                    {
                        var weakEventReference = new WeakDelegate(invList[i]);
                        _handlers.Add(weakEventReference);
                    }
                }
            }
            else
            {
                lock (_locker)
                {
                    var weakEventReference = new WeakDelegate(reference as Delegate);
                    _handlers.Add(weakEventReference);
                }
            }
        }

        /// <summary>
        /// Отписка от события
        /// </summary>
        /// <param name="reference">Делегат</param>
        public void Remove(T reference)
        {
            Contract.Requires(reference != null);

            if (reference is MulticastDelegate)
            {
                var invList = (reference as MulticastDelegate).GetInvocationList();

                lock (_locker)
                {
                    _handlers.RemoveAll(x => !x.IsActive || invList.Contains(x.GetDelegate()));
                }
            }
            else
            {
                lock (_locker)
                {
                    _handlers.RemoveAll(x => !x.IsActive || reference.Equals(x.GetDelegate()));
                }
            }
        }

        /// <summary>
        /// Получить делегат, содержащий все делегаты для живых объектов
        /// </summary>
        /// <returns>Делегат для инициализации события</returns>
        public T GetDelegate()
        {
            List<Delegate> activeHandlers = new List<Delegate>(_handlers.Count);
            lock (_locker)
            {
                for (int i = 0; i < _handlers.Count; i++)
                {
                    Contract.Assume(_handlers[i] != null);

                    var newDeleg = _handlers[i].GetDelegate();
                    if (newDeleg != null)
                    {
                        activeHandlers.Add(newDeleg);
                    }
                    else
                    {
                        _handlers.RemoveAt(i);
                        i--;
                    }
                }
            }

            return Delegate.Combine(activeHandlers.ToArray()) as T;
        }
    }
}