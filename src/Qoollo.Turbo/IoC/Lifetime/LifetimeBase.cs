using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Turbo.IoC.ServiceStuff;

namespace Qoollo.Turbo.IoC.Lifetime
{
    /// <summary>
    /// Base class for the lifetime containers
    /// </summary>
    [ContractClass(typeof(LifetimeBaseCodeContractCheck))]
    public abstract class LifetimeBase : IDisposable
    {
        private readonly Type _outputType;

        /// <summary>
        /// Code contracts
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(OutputType != null);
        }

        /// <summary>
        /// LifetimeBase constructor
        /// </summary>
        /// <param name="outType">The type of the object to be stored in the current Lifetime container</param>
        public LifetimeBase(Type outType)
        {
            Contract.Requires<ArgumentNullException>(outType != null, nameof(outType));

            _outputType = outType;
        }

        /// <summary>
        /// Resolves the object held by the container
        /// </summary>
        /// <param name="resolver">Injection resolver to acquire parameters</param>
        /// <returns>Resolved instance of the object</returns>
        /// <exception cref="CommonIoCException">Can be raised when injections not found</exception>
        public abstract object GetInstance(IInjectionResolver resolver);

        /// <summary>
        /// Attempts to resolve the object held by the container.
        /// Resolution can fail when required injection objects not found
        /// </summary>
        /// <param name="resolver">Injection resolver to acquire parameters</param>
        /// <param name="val">Resolved instance of the object</param>
        /// <returns>True if the object was successfully resolved</returns>
        public bool TryGetInstance(IInjectionResolver resolver, out object val)
        {
            Contract.Requires(resolver != null);
            Contract.Ensures((Contract.Result<bool>() == true && 
                    ((Contract.ValueAtReturn<object>(out val) != null && Contract.ValueAtReturn<object>(out val).GetType() == this.OutputType) ||
                     (Contract.ValueAtReturn<object>(out val) == null && this.OutputType.IsAssignableFromNull())))           
                || (Contract.Result<bool>() == false && Contract.ValueAtReturn<object>(out val) == null));


            try
            {
                val = GetInstance(resolver);
                return true;
            }
            catch (CommonIoCException)
            {
            }
            catch (KeyNotFoundException)
            {
            }

            val = null;
            return false;
        }


        /// <summary>
        /// Gets the type of the object held by the Lifetime container
        /// </summary>
        public Type OutputType
        {
            get { return _outputType; }
        }

        /// <summary>
        /// Cleans-up all resources
        /// </summary>
        /// <param name="isUserCall">True when called explicitly by user from Dispose method</param>
        protected virtual void Dispose(bool isUserCall)
        {
        }

        /// <summary>
        /// Cleans-up all resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }




    /// <summary>
    /// Code contracts
    /// </summary>
    [ContractClassFor(typeof(LifetimeBase))]
    abstract class LifetimeBaseCodeContractCheck : LifetimeBase
    {
        /// <summary>Code contracts</summary>
        private LifetimeBaseCodeContractCheck() : base(typeof(int)) { }

        public override object GetInstance(IInjectionResolver resolver)
        {
            Contract.Requires(resolver != null);
            Contract.Ensures((Contract.Result<object>() != null && Contract.Result<object>().GetType() == this.OutputType) ||
                             (Contract.Result<object>() == null && this.OutputType.IsAssignableFromNull()));

            throw new NotImplementedException();
        }
    }
}
