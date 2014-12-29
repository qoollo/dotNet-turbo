using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Работа с хранилищем данных в контексте исполнения
    /// </summary>
    public static class ExecutionContextStorage
    {
        /// <summary>
        /// Установить данные
        /// </summary>
        /// <param name="name">Имя</param>
        /// <param name="data">Данные</param>
        public static void SetData(string name, object data)
        {
            CallContext.LogicalSetData(name, data);
        }
        /// <summary>
        /// Считать данные по имени
        /// </summary>
        /// <param name="name">Имя</param>
        /// <returns>Данные</returns>
        public static object GetData(string name)
        {
            return CallContext.LogicalGetData(name);
        }
        /// <summary>
        /// Установить данные
        /// </summary>
        /// <typeparam name="T">Тип данных</typeparam>
        /// <param name="name">Имя</param>
        /// <param name="data">Данные</param>
        public static void SetData<T>(string name, T data)
        {
            CallContext.LogicalSetData(name, data);
        }
        /// <summary>
        /// Считать данные по имени
        /// </summary>
        /// <typeparam name="T">Тип данных</typeparam>
        /// <param name="name">Имя</param>
        /// <returns>Данные</returns>
        public static T GetData<T>(string name)
        {
            return (T)CallContext.LogicalGetData(name);
        }

        /// <summary>
        /// Проверить, установленны ли данные по имени
        /// </summary>
        /// <param name="name">Имя</param>
        /// <returns>Да или Нет</returns>
        public static bool HasData(string name)
        {
            return CallContext.LogicalGetData(name) != null;
        }

        /// <summary>
        /// Удалить данные по имени
        /// </summary>
        /// <param name="name">Имя</param>
        public static void RemoveData(string name)
        {
            CallContext.FreeNamedDataSlot(name);
        }
    }
}
