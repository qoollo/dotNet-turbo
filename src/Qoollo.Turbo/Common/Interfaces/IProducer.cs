using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Поставщик элементов
    /// </summary>
    /// <typeparam name="T">Тип элемента</typeparam>
    public interface IProducer<T>
    {
        /// <summary>
        /// Взять элемент
        /// </summary>
        /// <returns>Выбранный элемент</returns>
        T Take();
        /// <summary>
        /// Попытаться взять элемент
        /// </summary>
        /// <param name="item">Выбранный элемент</param>
        /// <returns>Успешность</returns>
        bool TryTake(out T item);
    }
}
