using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    /// <summary>
    /// Implements IEqualityComparer&lt;T&gt; to compare objects by its references
    /// </summary>
    /// <typeparam name="T">The type of objects to compare</typeparam>
    public class ByReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        private static ByReferenceEqualityComparer<T> _default;
        /// <summary>
        /// Returns a default by reference equality comparer for the type
        /// </summary>
        public static ByReferenceEqualityComparer<T> Default
        {
            get
            {
                return _default ?? (_default = new ByReferenceEqualityComparer<T>());
            }
        }

        /// <summary>
        /// Determines whether two objects of type T are the same
        /// </summary>
        /// <param name="x">The first object to compare</param>
        /// <param name="y">The second object to compare</param>
        /// <returns>true if the specified objects are equal; otherwise, false</returns>
        public bool Equals(T x, T y)
        {
            return object.ReferenceEquals(x, y);
        }

        /// <summary>
        /// Calculates the hash for the specified object
        /// </summary>
        /// <param name="obj">The object for which to get a hash code</param>
        /// <returns>A hash code for the specified object</returns>
        public int GetHashCode(T obj)
        {
            if (object.ReferenceEquals(obj, null))
                return 0;
            return obj.GetHashCode();
        }
    }
}
