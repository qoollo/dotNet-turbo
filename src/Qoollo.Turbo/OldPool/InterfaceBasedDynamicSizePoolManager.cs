using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Threading;

namespace Qoollo.Turbo.OldPool
{
    /// <summary>
    /// Пул с изменением числа элементов при необходимости и выделенной в интрефейс логикой
    /// </summary>
    /// <typeparam name="T">Тип элемента пула</typeparam>
    internal class InterfaceBasedDynamicSizePoolManager<T> : UnifiedDynamicSizePoolManager<T>
        where T: class
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(_logic != null);
        }

        private IDynamicSizePoolLogic<T> _logic;

        /// <summary>
        /// Конструктор InterfaceBasedDynamicSizePoolManager
        /// </summary>
        /// <param name="logic">Объект, реализующий логику работы пула</param>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="getRetryTimeout">Время повтора между попытками получить новый элемент</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        /// <param name="name">Имя пула</param>
        public InterfaceBasedDynamicSizePoolManager(IDynamicSizePoolLogic<T> logic, int maxElemCount, int trimPeriod, int getRetryTimeout, string name)
            :base(maxElemCount, trimPeriod, getRetryTimeout, name)
        {
            Contract.Requires<ArgumentNullException>(logic != null, "logic");

            _logic = logic;
        }

        /// <summary>
        /// Конструктор InterfaceBasedDynamicSizePoolManager
        /// </summary>
        /// <param name="logic">Объект, реализующий логику работы пула</param>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="getRetryTimeout">Время повтора между попытками получить новый элемент</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        public InterfaceBasedDynamicSizePoolManager(IDynamicSizePoolLogic<T> logic, int maxElemCount, int trimPeriod, int getRetryTimeout)
            : base(maxElemCount, trimPeriod, getRetryTimeout)
        {
            Contract.Requires<ArgumentNullException>(logic != null, "logic");

            _logic = logic;
        }

        /// <summary>
        /// Конструктор InterfaceBasedDynamicSizePoolManager
        /// </summary>
        /// <param name="logic">Объект, реализующий логику работы пула</param>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        /// <param name="name">Имя пула</param>
        public InterfaceBasedDynamicSizePoolManager(IDynamicSizePoolLogic<T> logic, int maxElemCount, int trimPeriod, string name)
            : this(logic, maxElemCount, trimPeriod, 2000, name)
        {
        }

        /// <summary>
        /// Конструктор InterfaceBasedDynamicSizePoolManager
        /// </summary>
        /// <param name="logic">Объект, реализующий логику работы пула</param>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        public InterfaceBasedDynamicSizePoolManager(IDynamicSizePoolLogic<T> logic, int maxElemCount, int trimPeriod)
            : this(logic, maxElemCount, trimPeriod, 2000)
        {
        }

        /// <summary>
        /// Конструктор InterfaceBasedDynamicSizePoolManager
        /// </summary>
        /// <param name="logic">Объект, реализующий логику работы пула</param>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="name">Имя пула</param>
        public InterfaceBasedDynamicSizePoolManager(IDynamicSizePoolLogic<T> logic, int maxElemCount, string name)
            : this(logic, maxElemCount, -1, 2000, name)
        {
        }

        /// <summary>
        /// Конструктор InterfaceBasedDynamicSizePoolManager
        /// </summary>
        /// <param name="logic">Объект, реализующий логику работы пула</param>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        public InterfaceBasedDynamicSizePoolManager(IDynamicSizePoolLogic<T> logic, int maxElemCount)
            : this(logic, maxElemCount, -1, 2000)
        { 
        }

        /// <summary>
        /// Создание элемента. Ожидание крайне не желательно. 
        /// Не должен кидать исключения, если только не надо прибить всю систему.
        /// </summary>
        /// <param name="elem">Созданный элемент, если удалось создать</param>
        /// <param name="timeout">Таймаут создания</param>
        /// <param name="token">Токен отмены создания</param>
        /// <returns>Удалось ли создать элемент</returns>
        protected override bool CreateElement(out T elem, int timeout, CancellationToken token)
        {
            return _logic.CreateElement(out elem, timeout, token);
        }
        /// <summary>
        /// Проверка, пригоден ли элемент для дальнейшего использования
        /// </summary>
        /// <param name="elem">Элемент</param>
        /// <returns>Пригоден ли для дальнейшего использования</returns>
        protected override bool IsValidElement(T elem)
        {
            return _logic.IsValidElement(elem);
        }
        /// <summary>
        /// Уничтожить элемент
        /// </summary>
        /// <param name="elem">Элемент</param>
        protected override void DestroyElement(T elem)
        {
            _logic.ReleaseElement(elem);
        }
    }


    /// <summary>
    /// Интерфейс с логикой работы пула
    /// </summary>
    /// <typeparam name="T">Тип элемента пула</typeparam>
    [ContractClass(typeof(IDynamicSizePoolLogicCodeContractCheck<>))]
    internal interface IDynamicSizePoolLogic<T>
        where T : class
    {
        /// <summary>
        /// Создание элемента. Ожидание крайне не желательно. 
        /// Не должен кидать исключения, если только не надо прибить всю систему.
        /// </summary>
        /// <param name="elem">Созданный элемент, если удалось создать</param>
        /// <param name="timeout">Таймаут создания</param>
        /// <param name="token">Токен отмены создания</param>
        /// <returns>Удалось ли создать элемент</returns>
        bool CreateElement(out T elem, int timeout, CancellationToken token);
        /// <summary>
        /// Проверка, пригоден ли элемент для дальнейшего использования
        /// </summary>
        /// <param name="elem">Элемент</param>
        /// <returns>Пригоден ли для дальнейшего использования</returns>
        bool IsValidElement(T elem);
        /// <summary>
        /// Уничтожить элемент
        /// </summary>
        /// <param name="elem">Элемент</param>
        void ReleaseElement(T elem);
    }




    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(IDynamicSizePoolLogic<>))]
    abstract class IDynamicSizePoolLogicCodeContractCheck<T> : IDynamicSizePoolLogic<T>
        where T: class
    {
        /// <summary>Контракты</summary>
        private IDynamicSizePoolLogicCodeContractCheck() { }

        /// <summary>Контракты</summary>
        public bool CreateElement(out T elem, int timeout, CancellationToken token)
        {
            Contract.Ensures((Contract.Result<bool>() && Contract.ValueAtReturn<T>(out elem) != null) || !Contract.Result<bool>());

            throw new NotImplementedException();
        }
        /// <summary>Контракты</summary>
        public bool IsValidElement(T elem)
        {
            Contract.Ensures((Contract.Result<bool>() && elem != null) || !Contract.Result<bool>());

            throw new NotImplementedException();
        }
        /// <summary>Контракты</summary>
        public void ReleaseElement(T elem)
        {
            throw new NotImplementedException();
        }
    }
}
