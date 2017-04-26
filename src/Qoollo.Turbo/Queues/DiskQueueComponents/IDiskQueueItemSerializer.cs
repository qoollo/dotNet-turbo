using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    /// <summary>
    /// Defines item serialization/deserialization logic for DiskQueue
    /// </summary>
    /// <typeparam name="T">Type of the item</typeparam>
    public interface IDiskQueueItemSerializer<T>
    {
        /// <summary>
        /// Expected size of serialized item in bytes. 
        /// Helps in buffer preallocation.
        /// Can be '-1' when size is not predictable
        /// </summary>
        int ExpectedSizeInBytes { get; }
        /// <summary>
        /// Serialize item into Stream wrapped by BinaryWriter
        /// </summary>
        /// <param name="writer">BinaryWriter to write data to stream</param>
        /// <param name="item">Item to serialize</param>
        void Serialize(BinaryWriter writer, T item);
        /// <summary>
        /// Deserialize item from Stream wrapped by BinaryReader
        /// </summary>
        /// <param name="reader">Reader to read data from stream</param>
        /// <returns>Item deserialized from the stream</returns>
        T Deserialize(BinaryReader reader);
    }
}
