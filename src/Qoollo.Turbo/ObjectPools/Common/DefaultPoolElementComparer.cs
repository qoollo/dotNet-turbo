using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools.Common
{
    /// <summary>
    /// Стандартный сравниватель элементов пула
    /// </summary>
    /// <typeparam name="T">Тип элементов</typeparam>
    public class DefaultPoolElementComparer<T> : PoolElementComparer<T>
    {
        private readonly Comparer<T> _comparer;

        /// <summary>
        /// Конструктор DefaultPoolElementComparer
        /// </summary>
        public DefaultPoolElementComparer()
        {
            _comparer = Comparer<T>.Default;
        }

        /// <summary>
        /// Сравнить. (a &gt; b =&gt; result &gt; 0; a == b =&gt; result == 0; a &lt; b =&gt; result &lt; 0)
        /// </summary>
        /// <param name="a">A</param>
        /// <param name="b">B</param>
        /// <param name="stopHere">Можно остановить поиск лучшего на текущем элементе</param>
        /// <returns>Результат сравнения</returns>
        public override int Compare(PoolElementWrapper<T> a, PoolElementWrapper<T> b, out bool stopHere)
        {
            stopHere = false;
            return _comparer.Compare(a.Element, b.Element);
        }
    }


    /// <summary>
    /// Обёртывающи компаратор
    /// </summary>
    /// <typeparam name="T">Тип элементов</typeparam>
    public class WrappingPoolElementComparer<T> : PoolElementComparer<T>
    {
        private readonly IComparer<T> _comparer;

        /// <summary>
        /// Конструктор WrappingPoolElementComparer
        /// </summary>
        /// <param name="comparer">Внутренний компаратор</param>
        public WrappingPoolElementComparer(IComparer<T> comparer)
        {
            _comparer = comparer ?? Comparer<T>.Default;
        }
        /// <summary>
        /// Конструктор WrappingPoolElementComparer
        /// </summary>
        public WrappingPoolElementComparer()
        {
            _comparer = Comparer<T>.Default;
        }

        /// <summary>
        /// Сравнить. (a &gt; b =&gt; result &gt; 0; a == b =&gt; result == 0; a &lt; b =&gt; result &lt; 0)
        /// </summary>
        /// <param name="a">A</param>
        /// <param name="b">B</param>
        /// <param name="stopHere">Можно остановить поиск лучшего на текущем элементе</param>
        /// <returns>Результат сравнения</returns>
        public override int Compare(PoolElementWrapper<T> a, PoolElementWrapper<T> b, out bool stopHere)
        {
            stopHere = false;
            return _comparer.Compare(a.Element, b.Element);
        }
    }
}