#if !HAS_SERIALIZABLE

using System;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Indicates that a class can be serialized. This class cannot be inherited
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
    internal sealed class SerializableAttribute : Attribute { }

    /// <summary>
    /// Indicates that a field of a serializable class should not be serialized. This class cannot be inherited
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    internal sealed class NonSerializedAttribute : Attribute { }
}

#endif
