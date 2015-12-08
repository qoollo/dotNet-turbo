using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo
{
    /// <summary>
    /// Helpers to store custom data inside ExecutionContext
    /// </summary>
    internal static class ExecutionContextStorage
    {
        /// <summary>
        /// Sets data by name
        /// </summary>
        /// <param name="name">Name</param>
        /// <param name="data">Data</param>
        public static void SetData(string name, object data)
        {
            CallContext.LogicalSetData(name, data);
        }
        /// <summary>
        /// Gets data by name
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>Data</returns>
        public static object GetData(string name)
        {
            return CallContext.LogicalGetData(name);
        }
        /// <summary>
        /// Sets data by name
        /// </summary>
        /// <typeparam name="T">Type of the data object</typeparam>
        /// <param name="name">Name</param>
        /// <param name="data">Data</param>
        public static void SetData<T>(string name, T data)
        {
            CallContext.LogicalSetData(name, data);
        }
        /// <summary>
        /// Gets data by name
        /// </summary>
        /// <typeparam name="T">Type of the data object</typeparam>
        /// <param name="name">Name</param>
        /// <returns>Data</returns>
        public static T GetData<T>(string name)
        {
            return (T)CallContext.LogicalGetData(name);
        }

        /// <summary>
        /// Checks whether the data setted for the specified name
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>True when setted</returns>
        public static bool HasData(string name)
        {
            return CallContext.LogicalGetData(name) != null;
        }

        /// <summary>
        /// Removes data by name
        /// </summary>
        /// <param name="name">Name</param>
        public static void RemoveData(string name)
        {
            CallContext.FreeNamedDataSlot(name);
        }
    }
}
