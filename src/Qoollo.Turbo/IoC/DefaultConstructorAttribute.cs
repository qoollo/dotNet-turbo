using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qoollo.Turbo.IoC
{
    /// <summary>
    /// Атрибут, помечающий конструктор, который будет использован для автоматического создания объекта
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    public class DefaultConstructorAttribute : Attribute
    {
    }
}
