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
        /// Specifies a precondition contract
        /// </summary>
        /// <param name="condition">Condition</param>
        /// <param name="userMessage">Message</param>
        /// <param name="conditionString">String representation of 'condition' argument</param>
        [System.Diagnostics.Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Requires(bool condition, string userMessage = null, string conditionString = null)
        {
            System.Diagnostics.Debug.Assert(condition, conditionString, userMessage);
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
        /// <param name="userMessage">Message</param>
        /// <param name="conditionString">String representation of 'condition' argument</param>
        [System.Diagnostics.Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Assert(bool condition, string userMessage = null, string conditionString = null)
        {
            System.Diagnostics.Debug.Assert(condition, conditionString, userMessage);
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
        /// Checks for a condition; if the condition is false, outputs a specified message and displays a message box that shows the call stack
        /// </summary>
        /// <param name="condition">Condition</param>
        /// <param name="userMessage">Message</param>
        /// <param name="conditionString">String representation of 'condition' argument</param>
        [System.Diagnostics.Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Assume(bool condition, string userMessage = null, string conditionString = null)
        {
            System.Diagnostics.Debug.Assert(condition, conditionString, userMessage);
        }

        /// <summary>
        /// Specifies an invariant contract for the enclosing method or property
        /// </summary>
        /// <param name="condition">Condition</param>
        [System.Diagnostics.Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Invariant(bool condition)
        {
            System.Diagnostics.Debug.Assert(condition);
        }
        /// <summary>
        /// Specifies an invariant contract for the enclosing method or property
        /// </summary>
        /// <param name="condition">Condition</param>
        /// <param name="userMessage">Message</param>
        [System.Diagnostics.Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Invariant(bool condition, string userMessage)
        {
            System.Diagnostics.Debug.Assert(condition, userMessage);
        }


        /// <summary>
        /// Specifies a postcondition contract for the enclosing method or property
        /// </summary>
        /// <param name="condition">Condition</param>
        [System.Diagnostics.Conditional("__NEVER__")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Ensures(bool condition)
        {
            throw new NotSupportedException("'Ensures' is not supported");
        }
        /// <summary>
        /// Specifies a postcondition contract for the enclosing method or property
        /// </summary>
        /// <param name="condition">Condition</param>
        /// <param name="userMessage">Message</param>
        [System.Diagnostics.Conditional("__NEVER__")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Ensures(bool condition, string userMessage)
        {
            throw new NotSupportedException("'Ensures' is not supported");
        }

        /// <summary>
        /// Specifies a postcondition contract for the enclosing method or property, based on the provided exception and condition
        /// </summary>
        /// <typeparam name="TException">The type of exception that invokes the postcondition check</typeparam>
        /// <param name="condition">Condition</param>
        [System.Diagnostics.Conditional("__NEVER__")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsuresOnThrow<TException>(bool condition) where TException: Exception
        {
            throw new NotSupportedException("'Ensures' is not supported");
        }
        /// <summary>
        /// Specifies a postcondition contract for the enclosing method or property, based on the provided exception and condition
        /// </summary>
        /// <typeparam name="TException">The type of exception that invokes the postcondition check</typeparam>
        /// <param name="condition">Condition</param>
        /// <param name="userMessage">Message</param>
        [System.Diagnostics.Conditional("__NEVER__")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsuresOnThrow<TException>(bool condition, string userMessage) where TException : Exception
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


    /// <summary>
    /// Marks construction without visible side-effects
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.Delegate, AllowMultiple = false, Inherited = true)]
    internal sealed class PureAttribute : Attribute { }

    /// <summary>
    /// Marks a method as being the invariant method for a class
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class ContractInvariantMethodAttribute : Attribute { }

    /// <summary>
    /// Specifies that a separate type contains the code contracts for this type
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
    [System.Diagnostics.Conditional("DEBUG")]
    internal sealed class ContractClassAttribute : Attribute
    {
        /// <summary>
        /// ContractClassAttribute constructor
        /// </summary>
        /// <param name="typeContainingContracts">The type that contains the code contracts for this type</param>
        public ContractClassAttribute(Type typeContainingContracts)
        {
            TypeContainingContracts = typeContainingContracts;
        }
        /// <summary>
        /// Gets the type that contains the code contracts for this type
        /// </summary>
        public Type TypeContainingContracts { get; }
    }

    /// <summary>
    /// Specifies the type with contracts
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    [System.Diagnostics.Conditional("DEBUG")]
    internal sealed class ContractClassForAttribute : Attribute
    {
        /// <summary>
        /// ContractClassForAttribute constructor
        /// </summary>
        /// <param name="typeContractsAreFor">The type that this code contract applies to</param>
        public ContractClassForAttribute(Type typeContractsAreFor)
        {
            TypeContractsAreFor = typeContractsAreFor;
        }
        /// <summary>
        /// Gets the type that this code contract applies to
        /// </summary>
        public Type TypeContractsAreFor { get; }
    }
}
