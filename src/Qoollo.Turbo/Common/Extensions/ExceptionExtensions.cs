using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;

namespace System
{
    /// <summary>
    /// Расширение для исключений
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Получение подробного описания исключения
        /// </summary>
        /// <param name="ex">Исключение</param>
        /// <returns>Описание</returns>
        public static string GetFullDescription(this Exception ex)
        {
            Contract.Requires(ex != null);
            Contract.Ensures(Contract.Result<string>() != null);

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
        /// Получение краткого описания исключения (без StackTrace)
        /// </summary>
        /// <param name="ex">Исключение</param>
        /// <returns>Описание</returns>
        public static string GetShortDescription(this Exception ex)
        {
            Contract.Requires(ex != null);
            Contract.Ensures(Contract.Result<string>() != null);

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
        /// Является ли исключение - исключением из библиотеки контрактов
        /// </summary>
        /// <param name="ex">Исключение</param>
        /// <returns>Является ли</returns>
        public static bool IsCodeContractException(this Exception ex)
        {
            Contract.Requires(ex != null);

            return ex.GetType().FullName.StartsWith(CodeContractAssemblyName);
        }



        /// <summary>
        /// Выбросить исключение указанного типа
        /// </summary>
        /// <param name="exceptionType">Тип исключения</param>
        /// <param name="message">Сообщение (может отсутствовать)</param>
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
        /// Выбросить исключение типа TException
        /// </summary>
        /// <typeparam name="TException">Тип исключения</typeparam>
        /// <param name="message">Сообщение</param>
        public static void ThrowException<TException>(string message) where TException: Exception
        {
            ThrowException(typeof(TException), message);
        }
        /// <summary>
        /// Выбросить исключение типа TException
        /// </summary>
        /// <typeparam name="TException">Тип исключения</typeparam>
        public static void ThrowException<TException>() where TException : Exception
        {
            ThrowException(typeof(TException), null);
        }
    }
}
