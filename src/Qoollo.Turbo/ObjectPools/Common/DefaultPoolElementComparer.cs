using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools.Common
{
    /// <summary>
    /// Default implementation of the comparer for the specified generic type (uses <see cref="Comparer{T}.Default"/> under the hood)
    /// </summary>
    /// <typeparam name="T">The type of objects to compare</typeparam>
    public class DefaultPoolElementComparer<T> : PoolElementComparer<T>
    {
        private readonly Comparer<T> _comparer;

        /// <summary>
        /// DefaultPoolElementComparer constructor
        /// </summary>
        public DefaultPoolElementComparer()
        {
            _comparer = Comparer<T>.Default;
        }

        /// <summary>
        /// Compares two elements to choose the best one (a &gt; b =&gt; result &gt; 0; a == b =&gt; result == 0; a &lt; b =&gt; result &lt; 0)
        /// </summary>
        /// <param name="a">First element to compare</param>
        /// <param name="b">Second element to compare</param>
        /// <param name="stopHere">Flag that indicates that the element '<paramref name="a"/>' is sufficient and scanning can be stopped</param>
        /// <returns>Comparison result</returns>
        public override int Compare(PoolElementWrapper<T> a, PoolElementWrapper<T> b, out bool stopHere)
        {
            stopHere = false;
            return _comparer.Compare(a.Element, b.Element);
        }
    }


    /// <summary>
    /// <see cref="PoolElementComparer{T}"/> that wraps specified <see cref="IComparer{T}"/> implementation
    /// </summary>
    /// <typeparam name="T">The type of objects to compare</typeparam>
    public class WrappingPoolElementComparer<T> : PoolElementComparer<T>
    {
        private readonly IComparer<T> _comparer;

        /// <summary>
        /// WrappingPoolElementComparer constructor
        /// </summary>
        /// <param name="comparer">Inner comparer</param>
        public WrappingPoolElementComparer(IComparer<T> comparer)
        {
            _comparer = comparer ?? Comparer<T>.Default;
        }
        /// <summary>
        /// WrappingPoolElementComparer constructor
        /// </summary>
        public WrappingPoolElementComparer()
        {
            _comparer = Comparer<T>.Default;
        }

        /// <summary>
        /// Compares two elements to choose the best one (a &gt; b =&gt; result &gt; 0; a == b =&gt; result == 0; a &lt; b =&gt; result &lt; 0)
        /// </summary>
        /// <param name="a">First element to compare</param>
        /// <param name="b">Second element to compare</param>
        /// <param name="stopHere">Flag that indicates that the element '<paramref name="a"/>' is sufficient and scanning can be stopped</param>
        /// <returns>Comparison result</returns>
        public override int Compare(PoolElementWrapper<T> a, PoolElementWrapper<T> b, out bool stopHere)
        {
            stopHere = false;
            return _comparer.Compare(a.Element, b.Element);
        }
    }
}