using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Indicates freezing capabilities. Frozen instance can't be modified.
    /// </summary>
    public interface IFreezable
    {
        /// <summary>
        /// Freezes current instance
        /// </summary>
        void Freeze();
        /// <summary>
        /// Gets the value indicating whether current instance is frozen
        /// </summary>
        bool IsFrozen { get; }
    }
}
