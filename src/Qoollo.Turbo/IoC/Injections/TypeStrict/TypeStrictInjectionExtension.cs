using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC.Injections
{

    /// <summary>
    /// Расширения для контейнеров инъекций с ключём Type
    /// </summary>
    public static class TypeStrictInjectionExtension
    {
        /// <summary>
        /// Получение инъекции по её типу
        /// </summary>
        /// <typeparam name="T">Тип запрашиваемой инъекции</typeparam>
        /// <param name="obj">Контейнер</param>
        /// <returns>Инъекция</returns>
        public static T GetInjection<T>(this GenericInjectionContainerBase<Type> obj)
        {
            return (T)obj.GetInjection(typeof(T));
        }

        /// <summary>
        /// Попытка получить инъекцию
        /// </summary>
        /// <typeparam name="T">Тип запрашиваемой инъекции</typeparam>
        /// <param name="obj">Контейнер</param>
        /// <param name="val">Значение инъекции в случае успеха</param>
        /// <returns>Успешность</returns>
        public static bool TryGetInjection<T>(this GenericInjectionContainerBase<Type> obj, out T val)
        {
            object tmpVal = null;
            if (obj.TryGetInjection(typeof(T), out tmpVal))
            {
                val = (T)tmpVal;
                return true;
            }
            val = default(T);
            return false;
        }

        /// <summary>
        /// Содержит ли контейнер инъекцию с типом T
        /// </summary>
        /// <typeparam name="T">Тип инъекции</typeparam>
        /// <param name="obj">Контейнер</param>
        /// <returns>Содержит ли</returns>
        public static bool Contains<T>(this GenericInjectionContainerBase<Type> obj)
        {
            return obj.Contains(typeof(T));
        }


        /// <summary>
        /// Добавить инъекцию в контейнер
        /// </summary>
        /// <typeparam name="T">Тип инъекции</typeparam>
        /// <param name="obj">Контейнер</param>
        /// <param name="val">Значение инъекции</param>
        public static void AddInjection<T>(this GenericInjectionContainerBase<Type> obj, T val)
        {
            obj.AddInjection(typeof(T), val);
        }


        /// <summary>
        /// Попытаться добавить инъекцию в контейнер
        /// </summary>
        /// <typeparam name="T">Тип инъекции</typeparam>
        /// <param name="obj">Контейнер</param>
        /// <param name="val">Значение</param>
        /// <returns>Успешность</returns>
        public static bool TryAddInjection<T>(this GenericInjectionContainerBase<Type> obj, T val)
        {
            return obj.TryAddInjection(typeof(T), val);
        }


        /// <summary>
        /// Удалить инъекцию из контейнера
        /// </summary>
        /// <typeparam name="T">Тип удаляемой инъекции</typeparam>
        /// <param name="obj">Контейнер</param>
        /// <returns>Была ли она там</returns>
        public static bool RemoveInjection<T>(this GenericInjectionContainerBase<Type> obj)
        {
            return obj.RemoveInjection(typeof(T));
        }
    }
}
