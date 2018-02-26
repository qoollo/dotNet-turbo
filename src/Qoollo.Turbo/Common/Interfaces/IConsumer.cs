using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Interface for data consumer
    /// </summary>
    /// <typeparam name="T">Type of the consuming elements</typeparam>
    public interface IConsumer<in T>
    {
        /// <summary>
        /// Pushes new element to the consumer
        /// </summary>
        /// <param name="item">Element</param>
        void Add(T item);
        /// <summary>
        /// Attempts to push new element to the consumer
        /// </summary>
        /// <param name="item">Element</param>
        /// <returns>True if the element was consumed successfully</returns>
        bool TryAdd(T item);
    }
}
