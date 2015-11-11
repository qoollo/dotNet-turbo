using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// The exception that is thrown when some error with IoC container occured
    /// </summary>
    [Serializable]
    public class CommonIoCException : TurboException
    {
        /// <summary>
        /// CommonIoCException constructor
        /// </summary>
        public CommonIoCException() : base("Exception with IoC container") { }
        /// <summary>
        /// CommonIoCException constructor with error message
        /// </summary>
        /// <param name="message">Error message</param>
        public CommonIoCException(string message) : base(message) { }
        /// <summary>
        /// CommonIoCException constructor with error message and innerException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public CommonIoCException(string message, Exception innerException) : base(message, innerException) { }
        /// <summary>
        /// CommonIoCException constructor for deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected CommonIoCException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    /// <summary>
    /// The exception that is thrown when some error with IoC association container occured
    /// </summary>
    [Serializable]
    public class AssociationIoCException : CommonIoCException
    {
        /// <summary>
        /// AssociationIoCException constructor
        /// </summary>
        public AssociationIoCException() : base("Exception inside IoC association container") { }
        /// <summary>
        /// AssociationIoCException constructor with error message
        /// </summary>
        /// <param name="message">Error message</param>
        public AssociationIoCException(string message) : base(message) { }
        /// <summary>
        /// AssociationIoCException constructor with error message and innerException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public AssociationIoCException(string message, Exception innerException) : base(message, innerException) { }
        /// <summary>
        /// AssociationIoCException constructor for deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected AssociationIoCException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    /// <summary>
    /// The exception that is thrown when some error with IoC injection container occured
    /// </summary>
    [Serializable]
    public class InjectionIoCException : CommonIoCException
    {
        /// <summary>
        /// InjectionIoCException constructor
        /// </summary>
        public InjectionIoCException() : base("Exception inside IoC injection container") { }
        /// <summary>
        /// InjectionIoCException constructor with error message
        /// </summary>
        /// <param name="message">Error message</param>
        public InjectionIoCException(string message) : base(message) { }
        /// <summary>
        /// InjectionIoCException constructor with error message and innerException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public InjectionIoCException(string message, Exception innerException) : base(message, innerException) { }
        /// <summary>
        /// InjectionIoCException constructor for deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected InjectionIoCException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }



    /// <summary>
    /// The exception that is thrown when trying to register a new association with inappropriate key
    /// </summary>
    [Serializable]
    public class AssociationBadKeyForTypeException : ArgumentException
    {
        /// <summary>
        /// AssociationBadKeyForTypeException constructor
        /// </summary>
        public AssociationBadKeyForTypeException() : base("Incorrect key for the specified value in IoC association container") { }
        /// <summary>
        /// AssociationBadKeyForTypeException constructor with error message
        /// </summary>
        /// <param name="message">Error message</param>
        public AssociationBadKeyForTypeException(string message) : base(message) { }
        /// <summary>
        /// AssociationBadKeyForTypeException constructor with error message and innerException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public AssociationBadKeyForTypeException(string message, Exception innerException) : base(message, innerException) { }
        /// <summary>
        /// AssociationBadKeyForTypeException constructor for deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected AssociationBadKeyForTypeException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
