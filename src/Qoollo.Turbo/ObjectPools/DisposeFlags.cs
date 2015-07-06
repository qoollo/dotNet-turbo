using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools
{
    /// <summary>
    /// Dispose flags controlling ObjectPool disposing behaviour
    /// </summary>
    [Flags]
    public enum DisposeFlags
    {
        /// <summary>
        /// Default behaviour
        /// </summary>
        None = 0,
        /// <summary>
        /// Wait for all PoolElements to be released
        /// </summary>
        WaitForElementsReleased = 1
    }
}
