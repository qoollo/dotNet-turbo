using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Исключение в IoC контейнере
    /// </summary>
    [Serializable]
    public class CommonIoCException : Exception
    {
        /// <summary>
        /// Конструктор CommonIoCException без параметров
        /// </summary>
        public CommonIoCException() : base("Exception with IoC container") { }
        /// <summary>
        /// Конструктор CommonIoCException с сообщением об ошибке
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        public CommonIoCException(string message) : base(message) { }
        /// <summary>
        /// Конструктор CommonIoCException с сообщением об ошибке и внутренним исключением
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="innerException">Внутреннее исключение</param>
        public CommonIoCException(string message, Exception innerException) : base(message, innerException) { }
    }


    /// <summary>
    /// Исключение в IoC контейнере при работе с ассоциациями
    /// </summary>
    [Serializable]
    public class AssociationIoCException : CommonIoCException
    {
        /// <summary>
        /// Конструктор AssociationIoCException без параметров
        /// </summary>
        public AssociationIoCException() : base("Exception inside IoC association container") { }
        /// <summary>
        /// Конструктор AssociationIoCException с сообщением об ошибке
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        public AssociationIoCException(string message) : base(message) { }
        /// <summary>
        /// Конструктор AssociationIoCException с сообщением об ошибке и внутренним исключением
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="innerException">Внутреннее исключение</param>
        public AssociationIoCException(string message, Exception innerException) : base(message, innerException) { }
    }


    /// <summary>
    /// Исключение в IoC контейнере при работе с ассоциациями
    /// </summary>
    [Serializable]
    public class AssociationBadKeyForTypeException : ArgumentException
    {
        /// <summary>
        /// Конструктор AssociationBadKeyForTypeException без параметров
        /// </summary>
        public AssociationBadKeyForTypeException() : base("Exception inside IoC injection container") { }
        /// <summary>
        /// Конструктор AssociationBadKeyForTypeException с сообщением об ошибке
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        public AssociationBadKeyForTypeException(string message) : base(message) { }
        /// <summary>
        /// Конструктор AssociationBadKeyForTypeException с сообщением об ошибке и внутренним исключением
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="innerException">Внутреннее исключение</param>
        public AssociationBadKeyForTypeException(string message, Exception innerException) : base(message, innerException) { }
    }


    /// <summary>
    /// Исключение в IoC контейнере при работе с инъекциями
    /// </summary>
    [Serializable]
    public class InjectionIoCException : CommonIoCException
    {
        /// <summary>
        /// Конструктор InjectionIoCException без параметров
        /// </summary>
        public InjectionIoCException() { }
        /// <summary>
        /// Конструктор InjectionIoCException с сообщением об ошибке
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        public InjectionIoCException(string message) : base(message) { }
        /// <summary>
        /// Конструктор InjectionIoCException с сообщением об ошибке и внутренним исключением
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="innerException">Внутреннее исключение</param>
        public InjectionIoCException(string message, Exception innerException) : base(message, innerException) { }
    }
}
