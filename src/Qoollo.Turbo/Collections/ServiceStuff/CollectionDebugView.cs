using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections.ServiceStuff
{
    /// <summary>
    /// Представление коллекции для отображения в отладчике
    /// </summary>
    /// <typeparam name="T">Тип элементов коллекции</typeparam>
    internal sealed class CollectionDebugView<T>
    {
        private readonly IEnumerable<T> collection;

        /// <summary>
        /// Конструктор CollectionDebugView
        /// </summary>
        /// <param name="collection">Коллекция</param>
        public CollectionDebugView(IEnumerable<T> collection)
        {
            this.collection = collection;
        }
        
        /// <summary>
        /// Элементы коллекции
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public T[] Items
		{
			get
			{
				return collection.ToArray();
			}
		}
    }
}
