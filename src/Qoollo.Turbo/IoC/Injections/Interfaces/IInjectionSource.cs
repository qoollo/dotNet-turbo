using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.IoC.Injections
{
    /// <summary>
    /// Represents the source of injections
    /// </summary>
    /// <typeparam name="TKey">The type of the key in injection container</typeparam>
    public interface IInjectionSource<TKey>
    {
        /// <summary>
        /// Gets the injection object for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Resolved object to be injected</returns>
        object GetInjection(TKey key);

        /// <summary>
        /// Attempts to get the injection object for the specified key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="val">Resolved object to be injected if found</param>
        /// <returns>True if the injection object is registered for the specified key; overwise false</returns>
        bool TryGetInjection(TKey key, out object val);

        /// <summary>
        /// Determines whether the InjectionSource contains the key
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>True if the InjectionSource contains the key</returns>
        bool Contains(TKey key);
    }
}
