using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Поставщик/потребитель элементов
    /// </summary>
    /// <typeparam name="T">Тип элемента</typeparam>
    public interface IProducerConsumer<T>: IProducer<T>, IConsumer<T>
    {
    }
}
