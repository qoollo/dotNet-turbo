using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Profiling
{
    /// <summary>
    /// Обёртка над внешним IProfilingProvider, перехватывающая исключения и закрывающий процесс
    /// </summary>
    public class ProfilingProviderWrapper : IProfilingProvider
    {
        private readonly IProfilingProvider _wrappedProvider;

        /// <summary>
        /// Конструктор ProfilingProviderWrapper
        /// </summary>
        /// <param name="wrappedProvider">Оборачиваемый профайлер</param>
        public ProfilingProviderWrapper(IProfilingProvider wrappedProvider)
        {
            if (wrappedProvider == null)
                throw new ArgumentNullException("wrappedProvider");

            _wrappedProvider = wrappedProvider;
        }
        /// <summary>
        /// Обработчик исключения
        /// </summary>
        /// <param name="ex">Исключение (может быть null)</param>
        protected virtual void ProcessException(Exception ex)
        {
            if (ex != null)
                Environment.FailFast("Qoollo.Turbo profiler throws unexpected exception." + Environment.NewLine + "Exception details: " + ex.ToString(), ex);
            else
                Environment.FailFast("Qoollo.Turbo profiler throws unexpected exception");
        }



        /// <summary>
        /// Создан объектный пул
        /// </summary>
        /// <param name="poolName">Имя пула</param>
        public void ObjectPoolCreated(string poolName)
        {
            try { _wrappedProvider.ObjectPoolCreated(poolName); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Уничтожен объектный пул
        /// </summary>
        /// <param name="poolName">Имя</param>
        /// <param name="fromFinalizer">Из финализатора</param>
        public void ObjectPoolDisposed(string poolName, bool fromFinalizer)
        {
            try { _wrappedProvider.ObjectPoolDisposed(poolName, fromFinalizer); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Оценка числа арендованных элементов (вызывается при аренде нового элемента)
        /// </summary>
        /// <param name="poolName">Имя пула</param>
        /// <param name="currentRentedCount">Текущее число арендованных элементов</param>
        public void ObjectPoolElementRented(string poolName, int currentRentedCount)
        {
            try { _wrappedProvider.ObjectPoolElementRented(poolName, currentRentedCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Оценка числа арендованных элементов (вызывается при освобождении арендованного элемента)
        /// </summary>
        /// <param name="poolName">Имя пула</param>
        /// <param name="currentRentedCount">Текущее число арендованных элементов</param>
        public void ObjectPoolElementReleased(string poolName, int currentRentedCount)
        {
            try { _wrappedProvider.ObjectPoolElementReleased(poolName, currentRentedCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Уведомление о том, что элемент пула перешёл в непригодное для использования состояние
        /// </summary>
        /// <param name="poolName">Имя пула</param>
        /// <param name="currentElementCount">Текущее число элементов в пуле</param>
        public void ObjectPoolElementFaulted(string poolName, int currentElementCount)
        {
            try { _wrappedProvider.ObjectPoolElementFaulted(poolName, currentElementCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Создан новый элемент в пуле
        /// </summary>
        /// <param name="poolName">Имя пула</param>
        /// <param name="currentElementCount">Текущее число элементов в пуле</param>
        public void ObjectPoolElementCreated(string poolName, int currentElementCount)
        {
            try { _wrappedProvider.ObjectPoolElementCreated(poolName, currentElementCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Элемент пула уничтожен
        /// </summary>
        /// <param name="poolName">Имя пула</param>
        /// <param name="currentElementCount">Текущее число элементов в пуле</param>
        public void ObjectPoolElementDestroyed(string poolName, int currentElementCount)
        {
            try { _wrappedProvider.ObjectPoolElementDestroyed(poolName, currentElementCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Оценка времени, в течение которого элемент пула был арендован
        /// </summary>
        /// <param name="poolName">Имя пула</param>
        /// <param name="time">Время удержания</param>
        public void ObjectPoolElementRentedTime(string poolName, TimeSpan time)
        {
            try { _wrappedProvider.ObjectPoolElementRentedTime(poolName, time); }
            catch (Exception ex) { ProcessException(ex); }
        }



        /// <summary>
        /// Создан асинхронный обработчик
        /// </summary>
        /// <param name="queueProcName">Имя обработчика</param>
        public void QueueAsyncProcessorCreated(string queueProcName)
        {
            try { _wrappedProvider.QueueAsyncProcessorCreated(queueProcName); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Асинхронный обработчик остановлен
        /// </summary>
        /// <param name="queueProcName">Имя обработчика</param>
        /// <param name="fromFinalizer">Остановка из финализатора</param>
        public void QueueAsyncProcessorDisposed(string queueProcName, bool fromFinalizer)
        {
            try { _wrappedProvider.QueueAsyncProcessorDisposed(queueProcName, fromFinalizer); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Запустился поток в асинхронном обработчике
        /// </summary>
        /// <param name="queueProcName">Имя обработчика</param>
        /// <param name="curThreadCount">Текущее число потоков</param>
        /// <param name="expectedThreadCount">Ожидаемое число потоков</param>
        public void QueueAsyncProcessorThreadStart(string queueProcName, int curThreadCount, int expectedThreadCount)
        {
            try { _wrappedProvider.QueueAsyncProcessorThreadStart(queueProcName, curThreadCount, expectedThreadCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Остановился поток в асинхронном обработчике (либо по завершению, либо из-за ошибки)
        /// </summary>
        /// <param name="queueProcName">Имя обработчика</param>
        /// <param name="curThreadCount">Текущее число потоков</param>
        /// <param name="expectedThreadCount">Ожидаемое число потоков</param>
        public void QueueAsyncProcessorThreadStop(string queueProcName, int curThreadCount, int expectedThreadCount)
        {
            try { _wrappedProvider.QueueAsyncProcessorThreadStop(queueProcName, curThreadCount, expectedThreadCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Оценка времени обработки элемента в асинхронном обработчике
        /// </summary>
        /// <param name="queueProcName">Имя обработчика</param>
        /// <param name="time">Время обработки</param>
        public void QueueAsyncProcessorElementProcessed(string queueProcName, TimeSpan time)
        {
            try { _wrappedProvider.QueueAsyncProcessorElementProcessed(queueProcName, time); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Оценка числа элементов в очереди на обработку внутри асинхронного обработчика
        /// </summary>
        /// <param name="queueProcName">Имя обработчика</param>
        /// <param name="newElementCount">Число элементов в очереди</param>
        /// <param name="maxElementCount">Максимальное число элементов</param>
        public void QueueAsyncProcessorElementCountIncreased(string queueProcName, int newElementCount, int maxElementCount)
        {
            try { _wrappedProvider.QueueAsyncProcessorElementCountIncreased(queueProcName, newElementCount, maxElementCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Оценка числа элементов в очереди на обработку внутри асинхронного обработчика
        /// </summary>
        /// <param name="queueProcName">Имя обработчика</param>
        /// <param name="newElementCount">Число элементов в очереди</param>
        /// <param name="maxElementCount">Максимальное число элементов</param>
        public void QueueAsyncProcessorElementCountDecreased(string queueProcName, int newElementCount, int maxElementCount)
        {
            try { _wrappedProvider.QueueAsyncProcessorElementCountDecreased(queueProcName, newElementCount, maxElementCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Вызывается при отбрасывании элемента в TryAdd в случае переполненности очереди
        /// </summary>
        /// <param name="queueProcName">Имя обработчика</param>
        /// <param name="currentElementCount">Число элементов в очереди</param>
        public void QueueAsyncProcessorElementRejectedInTryAdd(string queueProcName, int currentElementCount)
        {
            try { _wrappedProvider.QueueAsyncProcessorElementRejectedInTryAdd(queueProcName, currentElementCount); }
            catch (Exception ex) { ProcessException(ex); }
        }



        /// <summary>
        /// Пул потоков создан
        /// </summary>
        /// <param name="threadPoolName">Имя пула потоков</param>
        public void ThreadPoolCreated(string threadPoolName)
        {
            try { _wrappedProvider.ThreadPoolCreated(threadPoolName); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Пул потоков остановлен
        /// </summary>
        /// <param name="threadPoolName">Имя пула потоков</param>
        /// <param name="fromFinalizer">Остановка из финализатора</param>
        public void ThreadPoolDisposed(string threadPoolName, bool fromFinalizer)
        {
            try { _wrappedProvider.ThreadPoolDisposed(threadPoolName, fromFinalizer); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Изменилось число потоков в пуле
        /// </summary>
        /// <param name="threadPoolName">Имя пула потоков</param>
        /// <param name="curThreadCount">Текущее число потоков</param>
        public void ThreadPoolThreadCountChange(string threadPoolName, int curThreadCount)
        {
            try { _wrappedProvider.ThreadPoolThreadCountChange(threadPoolName, curThreadCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Оценка времени ожидания выполнения задачи в пуле потоков (время нахождения в очереди)
        /// </summary>
        /// <param name="threadPoolName">Имя пула потоков</param>
        /// <param name="time">Время нахождения в очереди</param>
        public void ThreadPoolWaitingInQueueTime(string threadPoolName, TimeSpan time)
        {
            try { _wrappedProvider.ThreadPoolWaitingInQueueTime(threadPoolName, time); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Оценка времени выполнения задачи в пуле потоков
        /// </summary>
        /// <param name="threadPoolName">Имя пула потоков</param>
        /// <param name="time">Время исполнения</param>
        public void ThreadPoolWorkProcessed(string threadPoolName, TimeSpan time)
        {
            try { _wrappedProvider.ThreadPoolWorkProcessed(threadPoolName, time); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Сигнал об отмене задачи в пуле
        /// </summary>
        /// <param name="threadPoolName">Имя пула потоков</param>
        public void ThreadPoolWorkCancelled(string threadPoolName)
        {
            try { _wrappedProvider.ThreadPoolWorkCancelled(threadPoolName); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Изменилось число задач в очереди на обработку в пуле потоков
        /// </summary>
        /// <param name="threadPoolName">Имя пула потоков</param>
        /// <param name="globalQueueItemCount">Текущее число задач в общей очереди</param>
        /// <param name="maxItemCount">Максимальное число задач в очереди</param>
        public void ThreadPoolWorkItemsCountIncreased(string threadPoolName, int globalQueueItemCount, int maxItemCount)
        {
            try { _wrappedProvider.ThreadPoolWorkItemsCountIncreased(threadPoolName, globalQueueItemCount, maxItemCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Изменилось число задач в очереди на обработку в пуле потоков
        /// </summary>
        /// <param name="threadPoolName">Имя пула потоков</param>
        /// <param name="globalQueueItemCount">Текущее число задач в общей очереди</param>
        /// <param name="maxItemCount">Максимальное число задач в очереди</param>
        public void ThreadPoolWorkItemsCountDecreased(string threadPoolName, int globalQueueItemCount, int maxItemCount)
        {
            try { _wrappedProvider.ThreadPoolWorkItemsCountDecreased(threadPoolName, globalQueueItemCount, maxItemCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Вызывается при отбрасывании задачи в TryAdd в случае переполненности очереди
        /// </summary>
        /// <param name="threadPoolName">Имя пула потоков</param>
        public void ThreadPoolWorkItemRejectedInTryAdd(string threadPoolName)
        {
            try { _wrappedProvider.ThreadPoolWorkItemRejectedInTryAdd(threadPoolName); }
            catch (Exception ex) { ProcessException(ex); }
        }




        /// <summary>
        /// Создан менеджер группы потоков
        /// </summary>
        /// <param name="threadSetManagerName">Имя менеджера</param>
        /// <param name="initialThreadCount">Указанное число потоков</param>
        public void ThreadSetManagerCreated(string threadSetManagerName, int initialThreadCount)
        {
            try { _wrappedProvider.ThreadSetManagerCreated(threadSetManagerName, initialThreadCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Менеджер группы потоков остановлен
        /// </summary>
        /// <param name="threadSetManagerName">Имя менеджера</param>
        /// <param name="fromFinalizer">Остановка из финализатора</param>
        public void ThreadSetManagerDisposed(string threadSetManagerName, bool fromFinalizer)
        {
            try { _wrappedProvider.ThreadSetManagerDisposed(threadSetManagerName, fromFinalizer); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Запустился поток в менеджере группы потоков
        /// </summary>
        /// <param name="threadSetManagerName">Имя менеджера</param>
        /// <param name="curThreadCount">Текущее число потоков</param>
        /// <param name="expectedThreadCount">Ожидаемое число потоков</param>
        public void ThreadSetManagerThreadStart(string threadSetManagerName, int curThreadCount, int expectedThreadCount)
        {
            try { _wrappedProvider.ThreadSetManagerThreadStart(threadSetManagerName, curThreadCount, expectedThreadCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
        /// <summary>
        /// Остановился поток в менеджере группы потоков (либо по завершению, либо из-за ошибки)
        /// </summary>
        /// <param name="threadSetManagerName">Имя менеджера</param>
        /// <param name="curThreadCount">Текущее число потоков</param>
        /// <param name="expectedThreadCount">Ожидаемое число потоков</param>
        public void ThreadSetManagerThreadStop(string threadSetManagerName, int curThreadCount, int expectedThreadCount)
        {
            try { _wrappedProvider.ThreadSetManagerThreadStop(threadSetManagerName, curThreadCount, expectedThreadCount); }
            catch (Exception ex) { ProcessException(ex); }
        }
    }
}
