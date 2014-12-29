using System;
using System.Runtime.CompilerServices;
using System.Diagnostics.Contracts;
using System.Reflection;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Данные о слабо связанном делегате
    /// </summary>
    public class WeakDelegate
    {
        /// <summary>
        /// Хранилище ссылки
        /// </summary>
        private IWeakEventReferenceStorage _valueStorage;

        /// <summary>
        /// Тип делегата
        /// </summary>
        public Type DelegateType { get; private set; }

        /// <summary>
        /// Собственно делегат. Может быть null
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
        /// Статический ли делегат
        /// </summary>
        public bool IsStatic
        {
            get
            {
                return _valueStorage == null;
            }
        }

        /// <summary>
        /// Активен ли делегат
        /// </summary>
        public bool IsActive
        {
            get
            {
                return _valueStorage == null || _valueStorage.Target != null;
            }
        }

        /// <summary>
        /// Вызываемый метод
        /// </summary>
        public MethodInfo Method { get; private set; }

        /// <summary>
        /// Формирование делегата
        /// </summary>
        /// <returns>Делегат</returns>
        public Delegate GetDelegate()
        {
            if (_valueStorage == null)
                return Delegate.CreateDelegate(DelegateType, Method, false);

            var target = _valueStorage.Target;
            if (target == null)
                return null;

            return Delegate.CreateDelegate(DelegateType, target, Method, false);
        }

        /// <summary>
        /// Конструктор WeakDelegate
        /// </summary>
        /// <param name="value">Делегат, из которого создаём</param>
        public WeakDelegate(Delegate value)
        {
            Contract.Requires(value != null);

            DelegateType = value.GetType();
            Method = value.Method;

            if (value.Target == null)
            {
                _valueStorage = null;
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
    }
}