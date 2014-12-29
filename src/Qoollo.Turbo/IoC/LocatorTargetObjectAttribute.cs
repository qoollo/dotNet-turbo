using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC
{
    /// <summary>
    /// Базовый аттрибут, который используется при автоматическом поиске соответствий
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class LocatorTargetObjectAttribute: Attribute
    {
        /// <summary>
        /// Конструктор LocatorTargetObjectAttribute
        /// </summary>
        /// <param name="mode">Режим инстанцирования объекта</param>
        public LocatorTargetObjectAttribute(ObjectInstantiationMode mode)
        {
            Mode = mode;
        }
        /// <summary>
        /// Режим инстанцирования объекта
        /// </summary>
        public ObjectInstantiationMode Mode
        {
            get;
            private set;
        }
    }
}
