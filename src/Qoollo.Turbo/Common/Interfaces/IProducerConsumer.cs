using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Consolidated interface for data producer and data consumer
    /// </summary>
    /// <typeparam name="T">Type of the produced/consumed elements</typeparam>
    public interface IProducerConsumer<T>: IProducer<T>, IConsumer<T>
    {
    }
}
