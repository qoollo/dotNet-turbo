using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;

namespace Qoollo.Turbo.OldPool
{
    /// <summary>
    /// Обёртка над элементом пула с внешней проверкой валидности
    /// </summary>
    /// <typeparam name="T">Тип элемента пула</typeparam>
    internal class UnifiedPoolElement<T> : PoolElement<T>
        where T : class
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_validator != null);
        }

        private IPoolElementValidator<T> _validator;

        /// <summary>
        /// Конструктор UnifiedPoolElement
        /// </summary>
        /// <param name="pool">Менеджер пула, либо другой объект для освобождения элемента</param>
        /// <param name="validator">Объект, проверяющий валидность элемента</param>
        /// <param name="elem">Сам элемент</param>
        public UnifiedPoolElement(IPoolElementReleaser<T> pool, IPoolElementValidator<T> validator, T elem)
            : base(pool, elem)
        {
            Contract.Requires(pool != null);
            Contract.Requires(validator != null);

            _validator = validator;
        }

        /// <summary>
        /// Валиден ли элемент пула. Обязательно надо проверять.
        /// </summary>
        public override bool IsValid
        {
            get
            {
                return base.IsValid && _validator.IsValid(this.Element);
            }
        }
    }

    /// <summary>
    /// Интерфейс проверки валидности объекта (элемента пула)
    /// </summary>
    /// <typeparam name="T">Тип элемента пула</typeparam>
    internal interface IPoolElementValidator<T>
        where T: class
    {
        /// <summary>
        /// Можно ли использовать данный объект
        /// </summary>
        /// <param name="elem">Объект</param>
        /// <returns>Валиден ли объект</returns>
        bool IsValid(T elem);
    }
}
