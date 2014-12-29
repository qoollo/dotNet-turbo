using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools.Common
{
    /// <summary>
    /// Стандартный объект для поддержки операций в пуле
    /// </summary>
    /// <typeparam name="T">Тип элемента</typeparam>
    public class DefaultPoolElementOperationSource<T> : IPoolElementOperationSource<T>
    {
        /// <summary>
        /// Является ли элемент валидным
        /// </summary>
        /// <param name="container">Контейнер элемента</param>
        /// <returns>Является ли валидным</returns>
        bool IPoolElementOperationSource<T>.IsValid(PoolElementWrapper<T> container)
        {
            return true;
        }
    }
}
