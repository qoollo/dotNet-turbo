using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Queues.DiskQueueComponents
{
    public interface IDiskQueueItemSerializer<T>
    {
        int ExpectedSizeInBytes { get; }
        void Serialize(BinaryWriter writer, T item);
        T Deserialize(BinaryReader reader);
    }
}
