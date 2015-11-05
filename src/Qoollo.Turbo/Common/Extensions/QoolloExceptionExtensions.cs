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
    public static class QoolloExceptionExtensions
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
            Contract.Requires<ArgumentNullException>(exceptionType != null);
            Contract.Requires<ArgumentException>(exceptionType == typeof(Exception) || exceptionType.IsSubclassOf(typeof(Exception)));


            Exception ex = null;

            if (message == null)
            {
                var emptyArgConstructor = exceptionType.GetConstructor(Type.EmptyTypes);
                if (emptyArgConstructor != null)
                    ex = emptyArgConstructor.Invoke(null) as Exception;
            }

            if (ex == null)
            {
                var singleArgConstructor = exceptionType.GetConstructor(new Type[] { typeof(string) });
                if (singleArgConstructor != null && singleArgConstructor.GetParameters()[0].Name == "message")
                    ex = singleArgConstructor.Invoke(new object[] { message }) as Exception;
            }

            if (ex == null)
            {
                var messageExcConstructor = exceptionType.GetConstructor(new Type[] { typeof(string), typeof(Exception) });
                var messageExcConstructorParams = messageExcConstructor != null ? messageExcConstructor.GetParameters() : null;
                if (messageExcConstructor != null && messageExcConstructorParams[0].Name == "message" && messageExcConstructorParams[1].Name == "innerException")
                    ex = messageExcConstructor.Invoke(new object[] { message, null }) as Exception;
            }

            if (ex == null)
            {
                var doubleArgConstructor = exceptionType.GetConstructor(new Type[] { typeof(string), typeof(string) });
                if (doubleArgConstructor != null)
                {
                    var constructorParams = doubleArgConstructor.GetParameters();
                    if ((constructorParams[0].Name == "paramName" || constructorParams[0].Name == "objectName") && constructorParams[1].Name == "message")
                        ex = doubleArgConstructor.Invoke(new object[] { null, message }) as Exception;
                    else if (constructorParams[0].Name == "message")
                        ex = doubleArgConstructor.Invoke(new object[] { message, null }) as Exception;
                }
            }

            if (ex == null)
            {
                var singleArgConstructor = exceptionType.GetConstructor(new Type[] { typeof(string) });
                if (singleArgConstructor != null)
                    ex = singleArgConstructor.Invoke(new object[] { message ?? "" }) as Exception;
            }

            if (ex == null && message != null)
            {
                var emptyArgConstructor = exceptionType.GetConstructor(Type.EmptyTypes);
                if (emptyArgConstructor != null)
                    ex = emptyArgConstructor.Invoke(null) as Exception;
            }

            if (ex == null)
                throw new ArgumentException("Can't throw Exception of type '" + exceptionType.Name + "' with message '" + (message ?? "") + "'");

            throw ex;
        }

        /// <summary>
        /// Throws the exception of the TException type with specified message
        /// </summary>
        /// <typeparam name="TException">Type of the exception to throw</typeparam>
        /// <param name="message">Message, that will be passed to Exception constructor (can be null)</param>
        public static void ThrowException<TException>(string message) where TException: Exception
        {
            ThrowException(typeof(TException), message);
        }
        /// <summary>
        /// Throws the exception of the TException type
        /// </summary>
        /// <typeparam name="TException">Type of the exception to throw</typeparam>
        public static void ThrowException<TException>() where TException : Exception
        {
            ThrowException(typeof(TException), null);
        }
    }
}
