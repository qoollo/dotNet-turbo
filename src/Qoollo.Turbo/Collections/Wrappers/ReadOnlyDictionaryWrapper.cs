using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections
{
    /// <summary>
    /// Read-only wrapper around IDictionary interface
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
    [Serializable]
    public class ReadOnlyDictionaryWrapper<TKey, TValue> : System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>
    {
        private static readonly ReadOnlyDictionaryWrapper<TKey, TValue> _empty = new ReadOnlyDictionaryWrapper<TKey, TValue>(new Dictionary<TKey, TValue>());
        /// <summary>
        /// Empty ReadOnlyDictionaryWrapper
        /// </summary>
        public static ReadOnlyDictionaryWrapper<TKey, TValue> Empty
        {
            get
            {
                return _empty;
            }
        }

        // ===========

        /// <summary>
        /// ReadOnlyDictionaryWrapper constructor
        /// </summary>
        /// <param name="dictionary">Wrapped dictionary</param>
        public ReadOnlyDictionaryWrapper(IDictionary<TKey, TValue> dictionary)
            : base(dictionary)
        {
            TurboContract.Requires(dictionary != null);
        }
    }
}
