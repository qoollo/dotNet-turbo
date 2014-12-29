using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qoollo.Turbo.OldPool
{
    /// <summary>
    /// Пул с изменением числа элементов при необходимости, а также унифицированной проверкой валидности элементов
    /// </summary>
    /// <typeparam name="T">Тип элемента пула</typeparam>
    internal abstract class UnifiedDynamicSizePoolManager<T> : DynamicSizePoolManager<T, UnifiedPoolElement<T>>, IPoolElementValidator<T>
        where T: class
    {
        /// <summary>
        /// Конструктор UnifiedDynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="getRetryTimeout">Время повтора между попытками получить новый элемент</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        /// <param name="name">Имя пула</param>
        public UnifiedDynamicSizePoolManager(int maxElemCount, int trimPeriod, int getRetryTimeout, string name)
            :base(maxElemCount, trimPeriod, getRetryTimeout, name)
        {
        }

        /// <summary>
        /// Конструктор UnifiedDynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="getRetryTimeout">Время повтора между попытками получить новый элемент</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        public UnifiedDynamicSizePoolManager(int maxElemCount, int trimPeriod, int getRetryTimeout)
            : base(maxElemCount, trimPeriod, getRetryTimeout)
        {
        }

        /// <summary>
        /// Конструктор UnifiedDynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        /// <param name="name">Имя пула</param>
        public UnifiedDynamicSizePoolManager(int maxElemCount, int trimPeriod, string name)
            : base(maxElemCount, trimPeriod, name)
        {
        }

        /// <summary>
        /// Конструктор UnifiedDynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        public UnifiedDynamicSizePoolManager(int maxElemCount, int trimPeriod)
            : base(maxElemCount, trimPeriod)
        {
        }

        /// <summary>
        /// Конструктор UnifiedDynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="name">Имя пула</param>
        public UnifiedDynamicSizePoolManager(int maxElemCount, string name)
            : base(maxElemCount, name)
        {
        }

        /// <summary>
        /// Конструктор UnifiedDynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        public UnifiedDynamicSizePoolManager(int maxElemCount)
            : base(maxElemCount)
        { 
        }


        /// <summary>
        /// Создание обёртки над элементом.
        /// </summary>
        /// <param name="elem">Элемент</param>
        /// <returns>Обёртка</returns>
        protected override UnifiedPoolElement<T> CreatePoolElement(T elem)
        {
            return new UnifiedPoolElement<T>(this, this, elem);
        }

        /// <summary>
        /// Можно ли использовать данный объект (Реализация интерфейса IPoolElementValidator)
        /// </summary>
        /// <param name="elem">Объект</param>
        /// <returns>Валиден ли объект</returns>
        bool IPoolElementValidator<T>.IsValid(T elem)
        {
            return IsValidElement(elem);
        }
    }
}
