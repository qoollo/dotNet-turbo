using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Generic
{
    /// <summary>
    /// Сравнение объектов по ссылке
    /// </summary>
    /// <typeparam name="T">Тип объекта</typeparam>
    public class ByReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        private static ByReferenceEqualityComparer<T> _default;
        /// <summary>
        /// Стандартный сравниватель
        /// </summary>
        public static ByReferenceEqualityComparer<T> Default
        {
            get
            {
                if (_default == null)
                    _default = new ByReferenceEqualityComparer<T>();
                return _default;
            }
        }

        /// <summary>
        /// Сравнение
        /// </summary>
        /// <param name="x">X</param>
        /// <param name="y">Y</param>
        /// <returns>Равны ли ссылки</returns>
        public bool Equals(T x, T y)
        {
            return object.ReferenceEquals(x, y);
        }

        /// <summary>
        /// Получение hash
        /// </summary>
        /// <param name="obj">Объект</param>
        /// <returns>Hash</returns>
        public int GetHashCode(T obj)
        {
            if (object.ReferenceEquals(obj, null))
                return 0;
            return obj.GetHashCode();
        }
    }
}
