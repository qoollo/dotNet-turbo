using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Profiling
{
    /// <summary>
    /// Интерфейс профилировщика для сервисных классов
    /// </summary>
    public interface IProfilingProvider
    {
        /// <summary>
        /// Создан объектный пул
        /// </summary>
        /// <param name="poolName">Имя пула</param>
        void ObjectPoolCreated(string poolName);
        /// <summary>
        /// Уничтожен объектный пул
        /// </summary>
        /// <param name="poolName">Имя</param>
        /// <param name="fromFinalizer">Из финализатора</param>
        void ObjectPoolDisposed(string poolName, bool fromFinalizer);
        /// <summary>
        /// Оценка числа арендованных элементов (вызывается при аренде нового элемента)
        /// </summary>
        /// <param name="poolName">Имя пула</param>
        /// <param name="currentRentedCount">Текущее число арендованных элементов</param>
        void ObjectPoolElementRented(string poolName, int currentRentedCount);
        /// <summary>
        /// Оценка числа арендованных элементов (вызывается при освобождении арендованного элемента)
        /// </summary>
        /// <param name="poolName">Имя пула</param>
        /// <param name="currentRentedCount">Текущее число арендованных элементов</param>
        void ObjectPoolElementReleased(string poolName, int currentRentedCount);
        /// <summary>
        /// Уведомление о том, что элемент пула перешёл в непригодное для использования состояние
        /// </summary>
        /// <param name="poolName">Имя пула</param>
        /// <param name="currentElementCount">Текущее число элементов в пуле</param>
        void ObjectPoolElementFaulted(string poolName, int currentElementCount);
        /// <summary>
        /// Создан новый элемент в пуле
        /// </summary>
        /// <param name="poolName">Имя пула</param>
        /// <param name="currentElementCount">Текущее число элементов в пуле</param>
        void ObjectPoolElementCreated(string poolName, int currentElementCount);
        /// <summary>
        /// Элемент пула уничтожен
        /// </summary>
        /// <param name="poolName">Имя пула</param>
        /// <param name="currentElementCount">Текущее число элементов в пуле</param>
        void ObjectPoolElementDestroyed(string poolName, int currentElementCount);
        /// <summary>
        /// Оценка времени, в течение которого элемент пула был арендован
        /// </summary>
        /// <param name="poolName">Имя пула</param>
        /// <param name="time">Время удержания</param>
        void ObjectPoolElementRentedTime(string poolName, TimeSpan time);




        /// <summary>
        /// Создан асинхронный обработчик
        /// </summary>
        /// <param name="queueProcName">Имя обработчика</param>
        void QueueAsyncProcessorCreated(string queueProcName);
        /// <summary>
        /// Асинхронный обработчик остановлен
        /// </summary>
        /// <param name="queueProcName">Имя обработчика</param>
        /// <param name="fromFinalizer">Остановка из финализатора</param>
        void QueueAsyncProcessorDisposed(string queueProcName, bool fromFinalizer);
        /// <summary>
        /// Запустился поток в асинхронном обработчике
        /// </summary>
        /// <param name="queueProcName">Имя обработчика</param>
        /// <param name="curThreadCount">Текущее число потоков</param>
        /// <param name="expectedThreadCount">Ожидаемое число потоков</param>
        void QueueAsyncProcessorThreadStart(string queueProcName, int curThreadCount, int expectedThreadCount);
        /// <summary>
        /// Остановился поток в асинхронном обработчике (либо по завершению, либо из-за ошибки)
        /// </summary>
        /// <param name="queueProcName">Имя обработчика</param>
        /// <param name="curThreadCount">Текущее число потоков</param>
        /// <param name="expectedThreadCount">Ожидаемое число потоков</param>
        void QueueAsyncProcessorThreadStop(string queueProcName, int curThreadCount, int expectedThreadCount);
        /// <summary>
        /// Оценка времени обработки элемента в асинхронном обработчике
        /// </summary>
        /// <param name="queueProcName">Имя обработчика</param>
        /// <param name="time">Время обработки</param>
        void QueueAsyncProcessorElementProcessed(string queueProcName, TimeSpan time);
        /// <summary>
        /// Оценка числа элементов в очереди на обработку внутри асинхронного обработчика
        /// </summary>
        /// <param name="queueProcName">Имя обработчика</param>
        /// <param name="newElementCount">Число элементов в очереди</param>
        /// <param name="maxElementCount">Максимальное число элементов</param>
        void QueueAsyncProcessorElementCountIncreased(string queueProcName, int newElementCount, int maxElementCount);
        /// <summary>
        /// Оценка числа элементов в очереди на обработку внутри асинхронного обработчика
        /// </summary>
        /// <param name="queueProcName">Имя обработчика</param>
        /// <param name="newElementCount">Число элементов в очереди</param>
        /// <param name="maxElementCount">Максимальное число элементов</param>
        void QueueAsyncProcessorElementCountDecreased(string queueProcName, int newElementCount, int maxElementCount);
        /// <summary>
        /// Вызывается при отбрасывании элемента в TryAdd в случае переполненности очереди
        /// </summary>
        /// <param name="queueProcName">Имя обработчика</param>
        /// <param name="currentElementCount">Число элементов в очереди</param>
        void QueueAsyncProcessorElementRejectedInTryAdd(string queueProcName, int currentElementCount);



        /// <summary>
        /// Пул потоков создан
        /// </summary>
        /// <param name="threadPoolName">Имя пула потоков</param>
        void ThreadPoolCreated(string threadPoolName);
        /// <summary>
        /// Пул потоков остановлен
        /// </summary>
        /// <param name="threadPoolName">Имя пула потоков</param>
        /// <param name="fromFinalizer">Остановка из финализатора</param>
        void ThreadPoolDisposed(string threadPoolName, bool fromFinalizer);
        /// <summary>
        /// Изменилось число потоков в пуле
        /// </summary>
        /// <param name="threadPoolName">Имя пула потоков</param>
        /// <param name="curThreadCount">Текущее число потоков</param>
        void ThreadPoolThreadCountChange(string threadPoolName, int curThreadCount);
        /// <summary>
        /// Оценка времени ожидания выполнения задачи в пуле потоков (время нахождения в очереди)
        /// </summary>
        /// <param name="threadPoolName">Имя пула потоков</param>
        /// <param name="time">Время нахождения в очереди</param>
        void ThreadPoolWaitingInQueueTime(string threadPoolName, TimeSpan time);
        /// <summary>
        /// Оценка времени выполнения задачи в пуле потоков
        /// </summary>
        /// <param name="threadPoolName">Имя пула потоков</param>
        /// <param name="time">Время исполнения</param>
        void ThreadPoolWorkProcessed(string threadPoolName, TimeSpan time);
        /// <summary>
        /// Сигнал об отмене задачи в пуле
        /// </summary>
        /// <param name="threadPoolName">Имя пула потоков</param>
        void ThreadPoolWorkCancelled(string threadPoolName);
        /// <summary>
        /// Изменилось число задач в очереди на обработку в пуле потоков
        /// </summary>
        /// <param name="threadPoolName">Имя пула потоков</param>
        /// <param name="globalQueueItemCount">Текущее число задач в общей очереди</param>
        /// <param name="maxItemCount">Максимальное число задач в очереди</param>
        void ThreadPoolWorkItemsCountIncreased(string threadPoolName, int globalQueueItemCount, int maxItemCount);
        /// <summary>
        /// Изменилось число задач в очереди на обработку в пуле потоков
        /// </summary>
        /// <param name="threadPoolName">Имя пула потоков</param>
        /// <param name="globalQueueItemCount">Текущее число задач в общей очереди</param>
        /// <param name="maxItemCount">Максимальное число задач в очереди</param>
        void ThreadPoolWorkItemsCountDecreased(string threadPoolName, int globalQueueItemCount, int maxItemCount);
        /// <summary>
        /// Вызывается при отбрасывании задачи в TryAdd в случае переполненности очереди
        /// </summary>
        /// <param name="threadPoolName">Имя пула потоков</param>
        void ThreadPoolWorkItemRejectedInTryAdd(string threadPoolName);



        /// <summary>
        /// Создан менеджер группы потоков
        /// </summary>
        /// <param name="threadSetManagerName">Имя менеджера</param>
        /// <param name="initialThreadCount">Указанное число потоков</param>
        void ThreadSetManagerCreated(string threadSetManagerName, int initialThreadCount);
        /// <summary>
        /// Менеджер группы потоков остановлен
        /// </summary>
        /// <param name="threadSetManagerName">Имя менеджера</param>
        /// <param name="fromFinalizer">Остановка из финализатора</param>
        void ThreadSetManagerDisposed(string threadSetManagerName, bool fromFinalizer);
        /// <summary>
        /// Запустился поток в менеджере группы потоков
        /// </summary>
        /// <param name="threadSetManagerName">Имя менеджера</param>
        /// <param name="curThreadCount">Текущее число потоков</param>
        /// <param name="expectedThreadCount">Ожидаемое число потоков</param>
        void ThreadSetManagerThreadStart(string threadSetManagerName, int curThreadCount, int expectedThreadCount);
        /// <summary>
        /// Остановился поток в менеджере группы потоков (либо по завершению, либо из-за ошибки)
        /// </summary>
        /// <param name="threadSetManagerName">Имя менеджера</param>
        /// <param name="curThreadCount">Текущее число потоков</param>
        /// <param name="expectedThreadCount">Ожидаемое число потоков</param>
        void ThreadSetManagerThreadStop(string threadSetManagerName, int curThreadCount, int expectedThreadCount);
    }
}
