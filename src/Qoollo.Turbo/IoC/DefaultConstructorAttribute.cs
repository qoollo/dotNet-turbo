using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qoollo.Turbo.IoC
{
    /// <summary>
    /// Indicates that the constructor should be used as default by IoC container
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public class DefaultConstructorAttribute : Attribute
    {
    }
}
