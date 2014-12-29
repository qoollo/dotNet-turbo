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
    /// Контейнер объекта, управляющий временем его жизни
    /// </summary>
    [ContractClass(typeof(LifetimeBaseCodeContractCheck))]
    public abstract class LifetimeBase : IDisposable
    {
        private Type _outputType;

        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(OutputType != null);
        }

        /// <summary>
        /// Конструктор LifetimeBase
        /// </summary>
        /// <param name="outType">Тип объекта, возвращаемый данным контейнером</param>
        public LifetimeBase(Type outType)
        {
            Contract.Requires<ArgumentNullException>(outType != null, "outType");

            _outputType = outType;
        }

        /// <summary>
        /// Возвращает объект, которым управляет данный контейнер
        /// </summary>
        /// <param name="resolver">Резолвер инъекций</param>
        /// <returns>Полученный объект</returns>
        public abstract object GetInstance(IInjectionResolver resolver);

        /// <summary>
        /// Пытается вернуть объект, которым управляет данный контейнер.
        /// Ошибки могут происходить в случае, если нет требуемых инъекций для создания
        /// </summary>
        /// <param name="resolver">Резолвер инъекций</param>
        /// <param name="val">Полученный объект (в случае успеха)</param>
        /// <returns>Удалось ли получить объект</returns>
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
        /// Тип возвращаемого объекта
        /// </summary>
        public Type OutputType
        {
            get { return _outputType; }
        }

        /// <summary>
        /// Внутренний метод освобождения ресурсов
        /// </summary>
        /// <param name="isUserCall">Был ли вызван пользователем (false - вызван деструктором)</param>
        protected virtual void Dispose(bool isUserCall)
        {
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }




    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(LifetimeBase))]
    abstract class LifetimeBaseCodeContractCheck : LifetimeBase
    {
        /// <summary>Контракты</summary>
        private LifetimeBaseCodeContractCheck() : base(typeof(int)) { }

        /// <summary>Контракты</summary>
        public override object GetInstance(IInjectionResolver resolver)
        {
            Contract.Requires(resolver != null);
            Contract.Ensures((Contract.Result<object>() != null && Contract.Result<object>().GetType() == this.OutputType) ||
                             (Contract.Result<object>() == null && this.OutputType.IsAssignableFromNull()));

            throw new NotImplementedException();
        }
    }
}
