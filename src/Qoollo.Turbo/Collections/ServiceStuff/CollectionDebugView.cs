using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Collections.ServiceStuff
{
    /// <summary>
    /// DebugView for the collection (show collection content in debugger)
    /// </summary>
    /// <typeparam name="T">The type of elements in collection</typeparam>
    internal sealed class CollectionDebugView<T>
    {
        private readonly IEnumerable<T> collection;

        /// <summary>
        /// CollectionDebugView constructor
        /// </summary>
        /// <param name="collection">Source collection</param>
        public CollectionDebugView(IEnumerable<T> collection)
        {
            this.collection = collection;
        }
        
        /// <summary>
        /// Items of the collection copied to array
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
