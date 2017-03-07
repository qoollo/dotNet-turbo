using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.Contracts;
using System.Diagnostics;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Represents a weak multicast delegate
    /// </summary>
    /// <typeparam name="T">Type of the original delegate</typeparam>
    public class MulticastWeakDelegate<T> where T : class
    {
        /// <summary>
        /// Code contracts
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_locker != null);
            Contract.Invariant(_handlers != null);
        }
 
        private readonly object _locker;
        private readonly List<WeakDelegate> _handlers;


        /// <summary>
        /// MulitcastWeakDelegate constructor
        /// </summary>
        public MulticastWeakDelegate()
        {
            if (!typeof(T).IsSubclassOf(typeof(Delegate)))
                throw new InvalidTypeException("Can't create MulticastWeakDelegate for non delegate type: " + typeof(T).Name);

            _locker = new object();
            _handlers = new List<WeakDelegate>();
        }

        /// <summary>
        /// Add subscription to the current delegate
        /// </summary>
        /// <param name="reference">New subscriber</param>
        public void Add(T reference)
        {
            Contract.Requires(reference != null);
            Debug.Assert(reference is Delegate);

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
        /// Remove subscription from the current delegate
        /// </summary>
        /// <param name="reference">Subscriber that will be removed</param>
        public void Remove(T reference)
        {
            Contract.Requires(reference != null);

            if (reference is MulticastDelegate)
            {
                var invList = (reference as MulticastDelegate).GetInvocationList();

                lock (_locker)
                {
                    int index = _handlers.FindIndex(x => Array.IndexOf(invList, x.GetDelegate()) >= 0);
                    if (index >= 0)
                        _handlers.RemoveAt(index);
                    _handlers.RemoveAll(x => !x.IsActive);
                }
            }
            else
            {
                lock (_locker)
                {
                    int index = _handlers.FindIndex(x => reference.Equals(x.GetDelegate()));
                    if (index >= 0)
                        _handlers.RemoveAt(index);
                    _handlers.RemoveAll(x => !x.IsActive);
                }
            }
        }

        /// <summary>
        /// Builds the strongly referenced delegate from the current weak delegate
        /// </summary>
        /// <returns>Constructed delegate</returns>
        public T GetDelegate()
        {
            Delegate result = null;

            lock (_locker)
            {
                for (int i = 0; i < _handlers.Count; i++)
                {
                    Debug.Assert(_handlers[i] != null);

                    var newDeleg = _handlers[i].GetDelegate();
                    if (newDeleg != null)
                    {
                        result = Delegate.Combine(result, newDeleg);
                    }
                    else
                    {
                        _handlers.RemoveAt(i);
                        i--;
                    }
                }
            }

            return (result as T);
        }
    }
}