using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.Helpers;

namespace Qoollo.Turbo.IoC.ServiceStuff
{
    /// <summary>
    /// Вспомогательные методы для поддержки создания объектов
    /// </summary>
    public static class InstantiationService
    {
        /// <summary>
        /// Создать объект типа objType.
        /// Должен существовать конструктор без параметров
        /// </summary>
        /// <param name="objType">Тип объекта</param>
        /// <returns>Созданный объект</returns>
        public static object CreateObject(Type objType)
        {
            Contract.Requires(objType != null);

            return OnjectInstantiationHelper.CreateObject(objType);
        }

        /// <summary>
        /// Создать объект типа T
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="resolver">Источник инъекций</param>
        /// <returns>Созданный объект</returns>
        public static T CreateObject<T>(IInjectionResolver resolver)
        {
            Contract.Requires(resolver != null);

            return (T)OnjectInstantiationHelper.CreateObject(typeof(T), resolver, null);
        }

        /// <summary>
        /// Создать объект типа objType
        /// </summary>
        /// <param name="objType">Тип объекта</param>
        /// <param name="resolver">Источник инъекций</param>
        /// <returns>Созданный объект</returns>
        public static object CreateObject(Type objType, IInjectionResolver resolver)
        {
            Contract.Requires(objType != null);
            Contract.Requires(resolver != null);

            return OnjectInstantiationHelper.CreateObject(objType, resolver, null);
        }
    }
}
