using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections
{
    /// <summary>
    /// Словарь в режиме только для чтения
    /// </summary>
    /// <typeparam name="TKey">Тип ключа</typeparam>
    /// <typeparam name="TValue">Тип значений</typeparam>
    [Serializable]
    public class ReadOnlyDictionaryWrapper<TKey, TValue> : System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>
    {
        private static readonly ReadOnlyDictionaryWrapper<TKey, TValue> _empty = new ReadOnlyDictionaryWrapper<TKey, TValue>(new Dictionary<TKey, TValue>());
        /// <summary>
        /// Пустой словарь
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
        /// Конструктор ReadOnlyDictionaryWrapper
        /// </summary>
        /// <param name="dict">Обёртываемый словарь</param>
        public ReadOnlyDictionaryWrapper(IDictionary<TKey, TValue> dict)
            : base(dict)
        {
            Contract.Requires<ArgumentNullException>(dict != null);
        }
    }
}
