using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Interface for data producer
    /// </summary>
    /// <typeparam name="T">Type of the produced elements</typeparam>
    public interface IProducer<T>
    {
        /// <summary>
        /// Takes new item from the producer
        /// </summary>
        /// <returns>Taken item</returns>
        T Take();
        /// <summary>
        /// Attempts to take new item from the producer
        /// </summary>
        /// <param name="item">Taken item</param>
        /// <returns>True if item was taken successfully</returns>
        bool TryTake(out T item);
    }
}
