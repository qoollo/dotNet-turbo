using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;

namespace System
{
    /// <summary>
    /// Extension methods for Exception objects
    /// </summary>
    [Obsolete("Class was renamed to TurboExceptionExtensions", true)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ExceptionExtensions
    {
    }

    /// <summary>
    /// Extension methods for Exception objects
    /// </summary>
    public static class TurboExceptionExtensions
    {
        /// <summary>
        /// Produces full description for the Exception (almost equivalent to ToString results)
        /// </summary>
        /// <param name="ex">Source exception</param>
        /// <returns>Full description for the exception</returns>
        public static string GetFullDescription(this Exception ex)
        {
            Contract.Requires(ex != null);
            Contract.Ensures(Contract.Result<string>() != null);

            if (ex == null)
                throw new ArgumentNullException("ex");

            StringBuilder builder = new StringBuilder(1000);
            builder.Append(ex.GetType().Name).Append(": ").Append(ex.Message).AppendLine();
            builder.Append("Source: ").Append(ex.Source).AppendLine();
            builder.Append("StackTrace: ").Append(ex.StackTrace).AppendLine();

            Exception cur = ex.InnerException;
            while (cur != null)
            {
                builder.Append("--> ").Append(cur.GetType().Name).Append(": ").Append(cur.Message).AppendLine();
                builder.Append("    Source: ").Append(cur.Source).AppendLine();
                builder.Append("    StackTrace: ").Append(cur.StackTrace).AppendLine();
                cur = cur.InnerException;
            }

            return builder.ToString();
        }

        /// <summary>
        /// Produces full description for the Exception (without StackTrace)
        /// </summary>
        /// <param name="ex">Source exception</param>
        /// <returns>Full description for the exception</returns>
        public static string GetShortDescription(this Exception ex)
        {
            Contract.Requires(ex != null);
            Contract.Ensures(Contract.Result<string>() != null);

            if (ex == null)
                throw new ArgumentNullException("ex");

            StringBuilder builder = new StringBuilder(256);
            builder.Append(ex.GetType().Name).Append(": ").Append(ex.Message).AppendLine();

            Exception cur = ex.InnerException;
            while (cur != null)
            {
                builder.Append("--> ").Append(cur.GetType().Name).Append(": ").Append(cur.Message).AppendLine();
                cur = cur.InnerException;
            }

            return builder.ToString();
        }


        private const string CodeContractAssemblyName = "System.Diagnostics.Contracts";

        /// <summary>
        /// Gets a value indicating whether the Excpetion is an instance of CodeContractException type
        /// </summary>
        /// <param name="ex">Source exception</param>
        /// <returns>True if the CodeContract exception</returns>
        public static bool IsCodeContractException(this Exception ex)
        {
            Contract.Requires(ex != null);

            return ex.GetType().FullName.StartsWith(CodeContractAssemblyName);
        }



        /// <summary>
        /// Throws the exception of the specified type with the specified message
        /// </summary>
        /// <param name="exceptionType">Type of the exceptional object to throw</param>
        /// <param name="message">Message, that will be passed to Exception constructor (can be null)</param>
        public static void ThrowException(Type exceptionType, string message)
        {
            Contract.Requires(exceptionType != null);
            Contract.Requires(exceptionType == typeof(Exception) || exceptionType.IsSubclassOf(typeof(Exception)));

            Qoollo.Turbo.TurboException.Throw(exceptionType, message);
        }
        /// <summary>
        /// Throws the exception of the TException type with specified message
        /// </summary>
        /// <typeparam name="TException">Type of the exception to throw</typeparam>
        /// <param name="message">Message, that will be passed to Exception constructor (can be null)</param>
        public static void ThrowException<TException>(string message) where TException: Exception
        {
            Qoollo.Turbo.TurboException.Throw<TException>(message);
        }
        /// <summary>
        /// Throws the exception of the TException type
        /// </summary>
        /// <typeparam name="TException">Type of the exception to throw</typeparam>
        public static void ThrowException<TException>() where TException : Exception
        {
            Qoollo.Turbo.TurboException.Throw<TException>();
        }

        /// <summary>
        /// Checks for a condition. Throws Exception if the condition is false
        /// </summary>
        /// <typeparam name="TException">The exception to throw if the condition is false</typeparam>
        /// <param name="condition">The conditional expression to test</param>
        /// <param name="message">A message to display if the condition is not met</param>
        public static void Assert<TException>(bool condition, string message) where TException : Exception
        {
            if (!condition)
                Qoollo.Turbo.TurboException.Throw<TException>(message);
        }
        /// <summary>
        /// Checks for a condition. Throws Exception if the condition is false
        /// </summary>
        /// <typeparam name="TException">The exception to throw if the condition is false</typeparam>
        /// <param name="condition">The conditional expression to test</param>
        public static void Assert<TException>(bool condition) where TException : Exception
        {
            if (!condition)
                Qoollo.Turbo.TurboException.Throw<TException>();
        }
    }
}
