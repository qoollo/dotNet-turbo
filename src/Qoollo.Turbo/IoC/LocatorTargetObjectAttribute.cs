using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC
{
    /// <summary>
    /// Marks the class to make it available for automatic discovery by IoC container
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class LocatorTargetObjectAttribute: Attribute
    {
        /// <summary>
        /// LocatorTargetObjectAttribute constructor
        /// </summary>
        /// <param name="mode">Instantiation mode for the objects of marked class</param>
        public LocatorTargetObjectAttribute(ObjectInstantiationMode mode)
        {
            Mode = mode;
        }
        /// <summary>
        /// Instantiation mode for the objects of marked class
        /// </summary>
        public ObjectInstantiationMode Mode
        {
            get;
            private set;
        }
    }
}
