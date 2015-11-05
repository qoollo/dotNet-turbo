using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Helper to throw exceptions of the specified type
    /// </summary>
    public static class ExceptionHelper
    {
        /// <summary>
        /// Throws the exception of the specified type with the specified message
        /// </summary>
        /// <param name="exceptionType">Type of the exceptional object to throw</param>
        /// <param name="message">Message, that will be passed to Exception constructor (can be null)</param>
        public static void ThrowException(Type exceptionType, string message)
        {
            Contract.Requires(exceptionType != null);

            QoolloExceptionExtensions.ThrowException(exceptionType, message);
        }
        /// <summary>
        /// Throws the exception of the TException type with specified message
        /// </summary>
        /// <typeparam name="TException">Type of the exception to throw</typeparam>
        /// <param name="message">Message, that will be passed to Exception constructor (can be null)</param>
        public static void ThrowException<TException>(string message) where TException : Exception
        {
            QoolloExceptionExtensions.ThrowException(typeof(TException), message);
        }
        /// <summary>
        /// Throws the exception of the TException type
        /// </summary>
        /// <typeparam name="TException">Type of the exception to throw</typeparam>
        public static void ThrowException<TException>() where TException : Exception
        {
            QoolloExceptionExtensions.ThrowException(typeof(TException), null);
        }
    }
}
