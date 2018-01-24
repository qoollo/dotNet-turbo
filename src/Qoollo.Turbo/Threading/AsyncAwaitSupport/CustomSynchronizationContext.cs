using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading
{
    /// <summary>
    /// Контекст синхронизации
    /// </summary>
    public class CustomSynchronizationContext: SynchronizationContext
    {
        /// <summary>
        /// Контракты
        /// </summary>
        [ContractInvariantMethod]
        private void Invariant()
        {
            TurboContract.Invariant(_supplier != null);
        }

        private ICustomSynchronizationContextSupplier _supplier;

        /// <summary>
        /// Конструктор контекста синхронизации
        /// </summary>
        /// <param name="supplier">Объект-исполнитель действий</param>
        public CustomSynchronizationContext(ICustomSynchronizationContextSupplier supplier)
        {
            TurboContract.Requires(supplier != null, conditionString: "supplier != null");

            _supplier = supplier;
        }

        /// <summary>
        /// Создание копии
        /// </summary>
        /// <returns>Копия</returns>
        public override SynchronizationContext CreateCopy()
        {
            return new CustomSynchronizationContext(_supplier);
        }
        /// <summary>
        /// Синхронный запуск задания
        /// </summary>
        /// <param name="d">Задание</param>
        /// <param name="state">Параметр</param>
        public override void Send(SendOrPostCallback d, object state)
        {
            _supplier.RunSync(d, state);
        }
        /// <summary>
        /// Асинхронный запуск задания
        /// </summary>
        /// <param name="d">Задание</param>
        /// <param name="state">Параметр</param>
        public override void Post(SendOrPostCallback d, object state)
        {
            _supplier.RunAsync(d, state);
        }
    }


    /// <summary>
    /// Поставщик синхронизации
    /// </summary>
    [ContractClass(typeof(ICustomSynchronizationContextSupplierCodeContractCheck))]
    public interface ICustomSynchronizationContextSupplier
    {
        /// <summary>
        /// Асинхронное выполнение задания
        /// </summary>
        /// <param name="act">Задание</param>
        /// <param name="state">Состояние</param>
        void RunAsync(SendOrPostCallback act, object state);
        /// <summary>
        /// Синхронное выполнение задание
        /// </summary>
        /// <param name="act">Задание</param>
        /// <param name="state">Состояние</param>
        void RunSync(SendOrPostCallback act, object state);
    }


    /// <summary>
    /// Контракты
    /// </summary>
    [ContractClassFor(typeof(ICustomSynchronizationContextSupplier))]
    abstract class ICustomSynchronizationContextSupplierCodeContractCheck : ICustomSynchronizationContextSupplier
    {
        /// <summary>Контракты</summary>
        private ICustomSynchronizationContextSupplierCodeContractCheck() { }

        /// <summary>Контракты</summary>
        public void RunAsync(SendOrPostCallback act, object state)
        {
            Contract.Requires(act != null);

            throw new NotImplementedException();
        }

        /// <summary>Контракты</summary>
        public void RunSync(SendOrPostCallback act, object state)
        {
            Contract.Requires(act != null);

            throw new NotImplementedException();
        }
    }
}
