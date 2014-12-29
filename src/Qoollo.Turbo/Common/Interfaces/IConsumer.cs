using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Потребитель элементов
    /// </summary>
    /// <typeparam name="T">Тип элемента</typeparam>
    public interface IConsumer<in T>
    {
        /// <summary>
        /// Добавить элемент
        /// </summary>
        /// <param name="item">Элемент</param>
        void Add(T item);
        /// <summary>
        /// Попытаться добавить элемент
        /// </summary>
        /// <param name="item">Элемент</param>
        /// <returns>Успешность</returns>
        bool TryAdd(T item);
    }
}
