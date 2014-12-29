using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    /// <summary>
    /// Расширения для Type
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Можно ли типу присвоить null
        /// </summary>
        /// <param name="tp">Тип</param>
        /// <returns>Можно ли</returns>
        [Pure]
        public static bool IsAssignableFromNull(this Type tp)
        {
            Contract.Requires(tp != null);

            return (!tp.IsValueType) || (tp.IsGenericType && tp.GetGenericTypeDefinition() == typeof(Nullable<>));
        }


        /// <summary>
        /// Получение имени типа в формате C#
        /// </summary>
        /// <param name="type">Тип</param>
        /// <returns>Имя</returns>
        [Pure]
        public static string GetCSName(this Type type)
        {
            Contract.Requires(type != null);

            if (!type.IsGenericType)
                return type.Name;

            string mainPart = type.GetGenericTypeDefinition().Name;
            int pos = mainPart.IndexOf("`");
            if (pos < 0)
                return mainPart;

            mainPart = mainPart.Substring(0, pos);

            string genericPart = "";

            var genericArgs = type.GetGenericArguments();

            for (int i = 0; i < genericArgs.Length - 1; i++)
                genericPart += GetCSName(genericArgs[i]) + ", ";

            if (genericArgs.Length > 0)
                genericPart += GetCSName(genericArgs[genericArgs.Length - 1]);


            return mainPart + "<" + genericPart + ">";
        }


        /// <summary>
        /// Получение полного имени типа в формате C#
        /// </summary>
        /// <param name="type">Тип</param>
        /// <returns>Полное имя</returns>
        [Pure]
        public static string GetCSFullName(this Type type)
        {
            Contract.Requires(type != null);

            if (!type.IsNested)
                return type.Namespace + "." + GetCSName(type);

            Type curTp = type;
            string nestedPart = "";

            while (curTp.IsNested) 
            {
                curTp = curTp.DeclaringType;
                nestedPart = GetCSName(curTp) + "." + nestedPart;
            }

            return type.Namespace + "." + nestedPart + GetCSName(type);
        }
    }
}
