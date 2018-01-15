using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Local replacement for Code Contracts
    /// </summary>
    internal static class TurboContract
    {
        /// <summary>
        /// Specifies a precondition contract
        /// </summary>
        /// <param name="condition">Condition</param>
        [System.Diagnostics.Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Requires(bool condition)
        {
            System.Diagnostics.Debug.Assert(condition);
        }
        /// <summary>
        /// Specifies a precondition contract
        /// </summary>
        /// <param name="condition">Condition</param>
        /// <param name="userMessage">Message</param>
        [System.Diagnostics.Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Requires(bool condition, string userMessage)
        {
            System.Diagnostics.Debug.Assert(condition, userMessage);
        }


        /// <summary>
        /// Checks for a condition; if the condition is false, outputs a specified message and displays a message box that shows the call stack
        /// </summary>
        /// <param name="condition">Condition</param>
        [System.Diagnostics.Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool condition)
        {
            System.Diagnostics.Debug.Assert(condition);
        }
        /// <summary>
        /// Checks for a condition; if the condition is false, outputs a specified message and displays a message box that shows the call stack
        /// </summary>
        /// <param name="condition">Condition</param>
        /// <param name="userMessage">Message</param>
        [System.Diagnostics.Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool condition, string userMessage)
        {
            System.Diagnostics.Debug.Assert(condition, userMessage);
        }


        /// <summary>
        /// Checks for a condition; if the condition is false, outputs a specified message and displays a message box that shows the call stack
        /// </summary>
        /// <param name="condition">Condition</param>
        [System.Diagnostics.Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assume(bool condition)
        {
            System.Diagnostics.Debug.Assert(condition);
        }
        /// <summary>
        /// Checks for a condition; if the condition is false, outputs a specified message and displays a message box that shows the call stack
        /// </summary>
        /// <param name="condition">Condition</param>
        /// <param name="userMessage">Message</param>
        [System.Diagnostics.Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assume(bool condition, string userMessage)
        {
            System.Diagnostics.Debug.Assert(condition, userMessage);
        }

        /// <summary>
        /// Specifies an invariant contract for the enclosing method or property
        /// </summary>
        /// <param name="condition">Condition</param>
        [System.Diagnostics.Conditional("__NEVER__")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Invariant(bool condition)
        {
            throw new NotSupportedException("'Invariant' is not supported");
        }
        /// <summary>
        /// Specifies an invariant contract for the enclosing method or property
        /// </summary>
        /// <param name="condition">Condition</param>
        /// <param name="userMessage">Message</param>
        [System.Diagnostics.Conditional("__NEVER__")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Invariant(bool condition, string userMessage)
        {
            throw new NotSupportedException("'Invariant' is not supported");
        }


        /// <summary>
        /// Specifies a postcondition contract for the enclosing method or property
        /// </summary>
        [System.Diagnostics.Conditional("__NEVER__")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Ensures()
        {
            throw new NotSupportedException("'Ensures' is not supported");
        }


        /// <summary>
        /// Represents values as they were at the start of a method or property
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="value">Value</param>
        /// <returns>Original value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T OldValue<T>(T value)
        {
            throw new NotSupportedException("'OldValue' is not supported");
        }

        /// <summary>
        /// Represents the return value of a method or property
        /// </summary>
        /// <typeparam name="T">Type of the returned value</typeparam>
        /// <returns>Returned value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Result<T>()
        {
            throw new NotSupportedException("'Result' is not supported");
        }

        /// <summary>
        /// Represents the final (output) value of an out parameter when returning from a method
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="value">Value</param>
        /// <returns>Value at return</returns>
        public static T ValueAtReturn<T>(out T value)
        {
            throw new NotSupportedException("'ValueAtReturn' is not supported");
        }
    }
}
