using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections
{
    /// <summary>
    /// Два уровня приоритета
    /// </summary>
    public enum HighLowPriority
    {
        /// <summary>
        /// Высокий приоритет
        /// </summary>
        High = 0,
        /// <summary>
        /// Обычный приоритет (низкий)
        /// </summary>
        Low = 1
    }

    /// <summary>
    /// Очередь с 2-мя уровнями приоритета
    /// </summary>
    /// <typeparam name="T">Тип элементов</typeparam>
    public class HighLowPriorityQueue<T> : LimitedPriorityQueueBase<T, HighLowPriority>
    {
        private const int PriorityLevels = 2;

        /// <summary>
        /// Конструктор HighLowPriorityQueue
        /// </summary>
        public HighLowPriorityQueue()
            : base(PriorityLevels)
        {
        }

        /// <summary>
        /// Конструктор HighLowPriorityQueue
        /// </summary>
        /// <param name="collection">Начальная коллекция</param>
        public HighLowPriorityQueue(IEnumerable<T> collection)
            : base(collection, PriorityLevels)
        {

        }

        /// <summary>
        /// Отображение типа приоритета на его номер
        /// </summary>
        /// <param name="prior">Приоритет</param>
        /// <returns>Номер приоритета</returns>
        protected override int MapPriority(HighLowPriority prior)
        {
            return (int)prior;
        }

        /// <summary>
        /// Добавить элемен в конец очереди с низким приоритетом
        /// </summary>
        /// <param name="item">Элемент</param>
        public void Enqueue(T item)
        {
            base.Enqueue(item, HighLowPriority.Low);
        }

        /// <summary>
        /// Добавить элемен в конец очереди с высоким приоритетом
        /// </summary>
        /// <param name="item">Элемент</param>
        public void EnqueueHigh(T item)
        {
            base.Enqueue(item, HighLowPriority.High);
        }
    }
}
