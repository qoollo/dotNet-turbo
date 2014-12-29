using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.OldPool
{
    /// <summary>
    /// Пул с балансировкой и изменением числа элементов при необходимости, а также унифицированной проверкой валидности элементов
    /// </summary>
    /// <typeparam name="T">Тип элемента пула</typeparam>
    internal abstract class UnifiedBalancingDynamicSizePoolManager<T> : BalancingDynamicSizePoolManager<T, UnifiedPoolElement<T>>, IPoolElementValidator<T>
        where T: class
    {
        /// <summary>
        /// Конструктор UnifiedBalancingDynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="getRetryTimeout">Время повтора между попытками получить новый элемент</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        /// <param name="elementComparer">Сравнение элементов с целью поиска наилучшего</param>
        /// <param name="name">Имя пула</param>
        public UnifiedBalancingDynamicSizePoolManager(int maxElemCount, int trimPeriod, int getRetryTimeout, IComparer<T> elementComparer, string name)
            : base(maxElemCount, trimPeriod, getRetryTimeout, elementComparer, name)
        {
        }

        /// <summary>
        /// Конструктор UnifiedBalancingDynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="getRetryTimeout">Время повтора между попытками получить новый элемент</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        /// <param name="elementComparer">Сравнение элементов с целью поиска наилучшего</param>
        public UnifiedBalancingDynamicSizePoolManager(int maxElemCount, int trimPeriod, int getRetryTimeout, IComparer<T> elementComparer)
            : base(maxElemCount, trimPeriod, getRetryTimeout, elementComparer)
        {
        }

        /// <summary>
        /// Конструктор UnifiedBalancingDynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        /// <param name="elementComparer">Сравнение элементов с целью поиска наилучшего</param>
        /// <param name="name">Имя пула</param>
        public UnifiedBalancingDynamicSizePoolManager(int maxElemCount, int trimPeriod, IComparer<T> elementComparer, string name)
            : base(maxElemCount, trimPeriod, elementComparer, name)
        {
        }

        /// <summary>
        /// Конструктор UnifiedBalancingDynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        /// <param name="elementComparer">Сравнение элементов с целью поиска наилучшего</param>
        public UnifiedBalancingDynamicSizePoolManager(int maxElemCount, int trimPeriod, IComparer<T> elementComparer)
            : base(maxElemCount, trimPeriod, elementComparer)
        {
        }

        /// <summary>
        /// Конструктор UnifiedBalancingDynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="trimPeriod">Время до уменьшения числа элементов (когда используются не все)</param>
        public UnifiedBalancingDynamicSizePoolManager(int maxElemCount, int trimPeriod)
            : base(maxElemCount, trimPeriod)
        {
        }

        /// <summary>
        /// Конструктор UnifiedBalancingDynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="elementComparer">Сравнение элементов с целью поиска наилучшего</param>
        /// <param name="name">Имя пула</param>
        public UnifiedBalancingDynamicSizePoolManager(int maxElemCount, IComparer<T> elementComparer, string name)
            : base(maxElemCount, elementComparer, name)
        {
        }

        /// <summary>
        /// Конструктор UnifiedBalancingDynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="name">Имя пула</param>
        public UnifiedBalancingDynamicSizePoolManager(int maxElemCount, string name)
            : base(maxElemCount, name)
        {
        }

        /// <summary>
        /// Конструктор UnifiedBalancingDynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        /// <param name="elementComparer">Сравнение элементов с целью поиска наилучшего</param>
        public UnifiedBalancingDynamicSizePoolManager(int maxElemCount, IComparer<T> elementComparer)
            : base(maxElemCount, elementComparer)
        {
        }

        /// <summary>
        /// Конструктор UnifiedBalancingDynamicSizePoolManager
        /// </summary>
        /// <param name="maxElemCount">Максимальное число элементов в пуле (-1 - неограничено)</param>
        public UnifiedBalancingDynamicSizePoolManager(int maxElemCount)
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
