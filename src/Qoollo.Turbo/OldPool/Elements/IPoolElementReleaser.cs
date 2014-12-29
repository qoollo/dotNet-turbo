using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qoollo.Turbo.OldPool
{
    /// <summary>
    /// Интерфейс для выполнения освобождения занятого элемента пула
    /// </summary>
    /// <typeparam name="T">Тип элемента</typeparam>
    internal interface IPoolElementReleaser<T>
    {
        /// <summary>
        /// Выполнить освобождение
        /// </summary>
        /// <param name="elem">Элемент пула</param>
        void Release(T elem);
    }
}
