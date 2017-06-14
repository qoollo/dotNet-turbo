using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools.Common
{
    /// <summary>
    /// Base class for all pool element comparers
    /// </summary>
    /// <typeparam name="T">The type of elements in ObjectPool</typeparam>
    [ContractClass(typeof(PoolElementComparerCodeContract<>))]
    public abstract class PoolElementComparer<T>
    {
        /// <summary>
        /// Wraps the specified <see cref="IComparer{T}"/>
        /// </summary>
        /// <param name="comparer">Comparison logic implementation</param>
        /// <returns>Created comparer</returns>
        public static PoolElementComparer<T> Wrap(IComparer<T> comparer)
        {
            if (object.ReferenceEquals(comparer, null) || object.ReferenceEquals(comparer, Comparer<T>.Default))
                return new DefaultPoolElementComparer<T>();

            return new WrappingPoolElementComparer<T>(comparer);
        }
        /// <summary>
        /// Creates a default comparer (based on <see cref="Comparer{T}.Default"/>)
        /// </summary>
        /// <returns>Created comparer</returns>
        public static PoolElementComparer<T> CreateDefault()
        {
            return new DefaultPoolElementComparer<T>(); 
        }


        /// <summary>
        /// Compares two elements to choose the best one (a &gt; b =&gt; result &gt; 0; a == b =&gt; result == 0; a &lt; b =&gt; result &lt; 0)
        /// </summary>
        /// <param name="a">First element to compare</param>
        /// <param name="b">Second element to compare</param>
        /// <param name="stopHere">Flag that indicates that the element '<paramref name="a"/>' is sufficient and scanning can be stopped</param>
        /// <returns>Comparison result</returns>
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
