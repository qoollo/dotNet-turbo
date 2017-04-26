using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ServiceStuff
{
    /// <summary>
    /// Platform information
    /// </summary>
    internal static class PlatformHelper
    {
        private static readonly int _processorCount = Environment.ProcessorCount;
        /// <summary>
        /// Cached number of processors
        /// </summary>
        public static int ProcessorCount { get { return _processorCount; } }
    }
}
