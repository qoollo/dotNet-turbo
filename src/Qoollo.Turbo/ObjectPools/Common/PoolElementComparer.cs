using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools.Common
{
    /// <summary>
    /// Базовый класс для сравнения элементов
    /// </summary>
    /// <typeparam name="T">Тип элемента</typeparam>
    [ContractClass(typeof(PoolElementComparerCodeContract<>))]
    public abstract class PoolElementComparer<T>
    {
        /// <summary>
        /// Обернуть интерфейс IComparer
        /// </summary>
        /// <param name="comparer">Компаратор IComparer</param>
        /// <returns>Компаратор</returns>
        public static PoolElementComparer<T> Wrap(IComparer<T> comparer)
        {
            if (object.ReferenceEquals(comparer, null) || object.ReferenceEquals(comparer, Comparer<T>.Default))
                return new DefaultPoolElementComparer<T>();

            return new WrappingPoolElementComparer<T>(comparer);
        }
        /// <summary>
        /// Создать компаратор по умолчанию
        /// </summary>
        /// <returns>Компаратор</returns>
        public static PoolElementComparer<T> CreateDefault()
        {
            return new DefaultPoolElementComparer<T>(); 
        }


        /// <summary>
        /// Сравнить. (a &gt; b =&gt; result &gt; 0; a == b =&gt; result == 0; a &lt; b =&gt; result &lt; 0)
        /// </summary>
        /// <param name="a">A</param>
        /// <param name="b">B</param>
        /// <param name="stopHere">Можно остановить поиск лучшего на текущем элементе</param>
        /// <returns>Результат сравнения</returns>
        public abstract int Compare(PoolElementWrapper<T> a, PoolElementWrapper<T> b, out bool stopHere);
    }



    [ContractClassFor(typeof(PoolElementComparer<>))]
    internal abstract class PoolElementComparerCodeContract<T> : PoolElementComparer<T>
    {
        public override int Compare(PoolElementWrapper<T> a, PoolElementWrapper<T> b, out bool stopHere)
        {
            Contract.Requires(a != null);
            Contract.Requires(b != null);

            throw new NotImplementedException();
        }
    }
}
