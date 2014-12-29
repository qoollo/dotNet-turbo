using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Поддержка заморозки. Замороженный объект должен стать неизменяемым.
    /// </summary>
    public interface IFreezable
    {
        /// <summary>
        /// Заморозить
        /// </summary>
        void Freeze();
        /// <summary>
        /// Заморожен ли объект
        /// </summary>
        bool IsFrozen { get; }
    }
}
