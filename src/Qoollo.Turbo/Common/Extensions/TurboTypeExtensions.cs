using Qoollo.Turbo;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    /// <summary>
    /// Extension methods for the Type objects
    /// </summary>
    [Obsolete("Class was renamed to TurboTypeExtensions", true)]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class TypeExtensions
    {
    }

    /// <summary>
    /// Extension methods for the Type objects
    /// </summary>
    public static class TurboTypeExtensions
    {
        /// <summary>
        /// Gets a value indicating whether the 'null' is a valid value for the Type
        /// </summary>
        /// <param name="tp">Type object</param>
        /// <returns>True if the null is a valid value</returns>
        [Pure]
        public static bool IsAssignableFromNull(this Type tp)
        {
            TurboContract.Requires(tp != null);

            return (!tp.IsValueType) || (tp.IsGenericType && tp.GetGenericTypeDefinition() == typeof(Nullable<>));
        }


        /// <summary>
        /// Returns the name of the Type
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="genericPrefix">Prefix for generic part</param>
        /// <param name="genericSuffix">Suffix for generic part</param>
        /// <returns>Name of the type in C# format</returns>
        [Pure]
        private static string GetTypeName(Type type, string genericPrefix, string genericSuffix)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!type.IsGenericType)
                return type.Name;

            string mainPart = type.GetGenericTypeDefinition().Name;
            int pos = mainPart.IndexOf("`");
            if (pos < 0)
                return mainPart;

            mainPart = mainPart.Substring(0, pos);

            var genericArgs = type.GetGenericArguments();

            string genericPart = "";
            if (genericArgs.Length == 1)
            {
                genericPart = GetTypeName(genericArgs[0], genericPrefix, genericSuffix);
            }
            else
            {
                for (int i = 0; i < genericArgs.Length - 1; i++)
                    genericPart += GetTypeName(genericArgs[i], genericPrefix, genericSuffix) + ", ";

                if (genericArgs.Length > 0)
                    genericPart += GetTypeName(genericArgs[genericArgs.Length - 1], genericPrefix, genericSuffix);
            }


            return string.Concat(mainPart, genericPrefix, genericPart, genericSuffix);
        }


        /// <summary>
        /// Returns the full name of the Type (includes assembly)
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="genericPrefix">Prefix for generic part</param>
        /// <param name="genericSuffix">Suffix for generic part</param>
        /// <returns>Full name of the type in C# format</returns>
        [Pure]
        private static string GetFullTypeName(Type type, string genericPrefix, string genericSuffix)
        {
            TurboContract.Requires(type != null);

            if (!type.IsNested)
                return type.Namespace + "." + GetTypeName(type, genericPrefix, genericSuffix);

            Type curTp = type;
            string nestedPart = "";

            while (curTp.IsNested) 
            {
                curTp = curTp.DeclaringType;
                nestedPart = GetTypeName(curTp, genericPrefix, genericSuffix) + "." + nestedPart;
            }

            return type.Namespace + "." + nestedPart + GetTypeName(type, genericPrefix, genericSuffix);
        }



        /// <summary>
        /// Returns the name of the Type in C# format
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Name of the type in C# format</returns>
        [Pure]
        public static string GetCSName(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return GetTypeName(type, "<", ">");
        }
        /// <summary>
        /// Returns the full name of the Type in C# format (includes assembly)
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Full name of the type in C# format</returns>
        [Pure]
        public static string GetCSFullName(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return GetFullTypeName(type, "<", ">");
        }


        /// <summary>
        /// Returns the name of the Type in VisualBasic format
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Name of the type in VisualBasic format</returns>
        [Pure]
        public static string GetVBName(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return GetTypeName(type, "(Of ", ")");

        }
        /// <summary>
        /// Returns the full name of the Type in VisualBasic format (includes assembly)
        /// </summary>
        /// <param name="type">Type</param>
        /// <returns>Full name of the type in VisualBasic format</returns>
        [Pure]
        public static string GetVBFullName(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return GetFullTypeName(type, "(Of ", ")");
        }
    }
}
