using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Base exception for Turbo library with a range of helper methods
    /// </summary>
    [Serializable]
    public class TurboException: Exception
    {
        /// <summary>
        /// TurboException constructor
        /// </summary>
        public TurboException() : base("TurboException") { }
        /// <summary>
        /// TurboException constructor with error message
        /// </summary>
        /// <param name="message">Error message</param>
        public TurboException(string message) : base(message) { }
        /// <summary>
        /// TurboException constructor with error message and innerException
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public TurboException(string message, Exception innerException) : base(message, innerException) { }

#if !HAS_NO_SERIALIZABLE_ATTRIBUTE
        /// <summary>
        /// TurboException constructor for deserialization
        /// </summary>
        /// <param name="info">SerializationInfo</param>
        /// <param name="context">StreamingContext</param>
        protected TurboException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
#endif

        // =============================

        /// <summary>
        /// Throws the exception of the specified type with the specified message
        /// </summary>
        /// <param name="exceptionType">Type of the exceptional object to throw</param>
        /// <param name="message">Message, that will be passed to Exception constructor (can be null)</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void Throw(Type exceptionType, string message)
        {
            if (exceptionType == null)
                throw new ArgumentNullException(nameof(exceptionType));
            if (exceptionType != typeof(Exception) && !exceptionType.IsSubclassOf(typeof(Exception)))
                throw new ArgumentException(nameof(exceptionType) + " should be of type 'Exception'");

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
        public static void Throw<TException>(string message) where TException : Exception
        {
            Throw(typeof(TException), message);
        }
        /// <summary>
        /// Throws the exception of the TException type
        /// </summary>
        /// <typeparam name="TException">Type of the exception to throw</typeparam>
        public static void Throw<TException>() where TException : Exception
        {
            Throw(typeof(TException), null);
        }



        /// <summary>
        /// Throws TurboAssertionException with specified message
        /// </summary>
        /// <param name="message">Message, that will be passed to TurboException constructor (can be null)</param>
        private static void ThrowAssertionException(string message)
        {
            if (message == null)
                throw new TurboAssertionException();

            throw new TurboAssertionException(message);
        }
        /// <summary>
        /// Throws TurboAssertionException
        /// </summary>
        private static void ThrowAssertionException()
        {
            throw new TurboAssertionException();
        }
        /// <summary>
        /// Checks for a condition. Throws Exception if the condition is false
        /// </summary>
        /// <typeparam name="TException">The exception to throw if the condition is false</typeparam>
        /// <param name="condition">The conditional expression to test</param>
        /// <param name="message">A message to display if the condition is not met</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Assert<TException>(bool condition, string message) where TException: Exception
        {
            if (!condition)
                Throw<TException>(message);
        }
        /// <summary>
        /// Checks for a condition. Throws Exception if the condition is false
        /// </summary>
        /// <typeparam name="TException">The exception to throw if the condition is false</typeparam>
        /// <param name="condition">The conditional expression to test</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Assert<TException>(bool condition) where TException : Exception
        {
            if (!condition)
                Throw<TException>();
        }

        /// <summary>
        /// Checks for a condition. Throws TurboAssertionException if the condition is false
        /// </summary>
        /// <param name="condition">The conditional expression to test</param>
        /// <param name="message">A message to display if the condition is not met</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool condition, string message)
        {
            if (!condition)
                ThrowAssertionException(message);
        }
        /// <summary>
        /// Checks for a condition. Throws TurboAssertionException if the condition is false
        /// </summary>
        /// <param name="condition">The conditional expression to test</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool condition)
        {
            if (!condition)
                ThrowAssertionException();
        }
    }
}
