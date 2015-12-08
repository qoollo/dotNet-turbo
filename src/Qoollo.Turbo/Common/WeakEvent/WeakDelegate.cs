using System;
using System.Runtime.CompilerServices;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Represents weak delegate, which not prevent the callee object from being reclaimed by GC
    /// </summary>
    public class WeakDelegate
    {
        /// <summary>
        /// Reference storage
        /// </summary>
        private readonly WeakEventReferenceStorageBase _valueStorage;
        /// <summary>
        /// Stores source delegate when it references static method
        /// </summary>
        private readonly Delegate _staticDelegateStorage;
        /// <summary>
        /// The type of the delegate
        /// </summary>
        private readonly Type _delegateType;
        /// <summary>
        /// The method represented by the delegate
        /// </summary>
        private readonly MethodInfo _methodInfo;

        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_valueStorage != null || _staticDelegateStorage != null);
            Contract.Invariant(_delegateType != null);
            Contract.Invariant(_methodInfo != null);
        }

        /// <summary>
        /// WeakDelegate constructor
        /// </summary>
        /// <param name="value">Source delegate</param>
        public WeakDelegate(Delegate value)
        {
            Contract.Requires<ArgumentNullException>(value != null);

            _delegateType = value.GetType();
            _methodInfo = value.Method;

            if (value.Target == null)
            {
                _valueStorage = null;
                _staticDelegateStorage = value;
            }
            else if (Attribute.IsDefined(value.Method.DeclaringType, typeof(CompilerGeneratedAttribute)))
            {
                _valueStorage = new StrongReferenceStorage(value.Target);
            }
            else
            {
                _valueStorage = new WeakReferenceStorage(value.Target);
            }
        }


        /// <summary>
        /// Gets the type of the delegate
        /// </summary>
        public Type DelegateType { get { return _delegateType; } }

        /// <summary>
        /// Gets the callee object (can be null if the object was collected by GC)
        /// </summary>
        public object Target
        {
            get
            {
                if (_valueStorage == null)
                    return null;
                return _valueStorage.Target;
            }
        }

        /// <summary>
        /// Gets the value indicating that delegate created for the static method
        /// </summary>
        public bool IsStatic { get { return _valueStorage == null; } }

        /// <summary>
        /// Gets the value indicating that the callee object still alive (not reclaimed by GC)
        /// </summary>
        public bool IsActive { get { return _valueStorage == null || _valueStorage.Target != null; } }

        /// <summary>
        /// Gets the method represented by the delegate
        /// </summary>
        public MethodInfo Method { get { return _methodInfo; } }



        /// <summary>
        /// Returns the delegate object constructed from the current WeakDelegate. 
        /// Can return null when the callee object was collected by GC.
        /// </summary>
        /// <returns>Constructed strong delegate (can be null when the callee object was collected by GC)</returns>
        public Delegate GetDelegate()
        {
            if (_valueStorage == null)
                return _staticDelegateStorage;

            var target = _valueStorage.Target;
            if (target == null)
                return null;

            return Delegate.CreateDelegate(DelegateType, target, Method, false);
        }
    }
}