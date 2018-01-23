using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools.ServiceStuff
{
    /// <summary>
    /// Трекер числа используемых элементов в DynamicPool
    /// </summary>
    internal class UsedElementTracker
    {
        /// <summary>
        /// Получить временной маркер в миллисекундах
        /// </summary>
        /// <returns>Временной маркер</returns>
        private static int GetTimestamp()
        {
            return Environment.TickCount;
        }

        // ========================

        private readonly int _checkPeriod;

        private int _lastTestTime;
        private int _minFreeElementsCount;

        private int _elementToDestroy;

        /// <summary>
        /// Конструктор UsedElementTracker
        /// </summary>
        /// <param name="checkPeriod">Период оценки минимального числа свободных элементов</param>
        public UsedElementTracker(int checkPeriod)
        {
            TurboContract.Requires(checkPeriod > 0, "checkPeriod > 0");

            _checkPeriod = checkPeriod;
            _lastTestTime = GetTimestamp();
            _minFreeElementsCount = int.MaxValue;
            _elementToDestroy = 0;
        }

        /// <summary>
        /// Текущее минимальное число свободных элементов
        /// </summary>
        public int MinFreeElementsCount { get { return Volatile.Read(ref _minFreeElementsCount); } }
        /// <summary>
        /// Текущее число элементов для уничтожения
        /// </summary>
        public int ElementToDestroy { get { return Math.Max(0, Volatile.Read(ref _elementToDestroy)); } }


        /// <summary>
        /// Сбросить измерения для текущего периода
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            Interlocked.Exchange(ref _elementToDestroy, 0);
            Interlocked.Exchange(ref _lastTestTime, GetTimestamp());
            Interlocked.Exchange(ref _minFreeElementsCount, int.MaxValue);
        }

        /// <summary>
        /// Обновить минимальное число свободных элементов
        /// </summary>
        /// <param name="curElementCount">Текущее число свободных элементов</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateMinFreeElementCount(int curElementCount)
        {
            TurboContract.Requires(curElementCount >= 0, "curElementCount >= 0");

            int minFreeElementsCount = Volatile.Read(ref _minFreeElementsCount);
            while (curElementCount < minFreeElementsCount)
            {
                if (Interlocked.CompareExchange(ref _minFreeElementsCount, curElementCount, minFreeElementsCount) == minFreeElementsCount)
                    break;

                minFreeElementsCount = Volatile.Read(ref _minFreeElementsCount);
            }
        }

        /// <summary>
        /// Запросить элемент на уничтожение. 
        /// Сообщает, можно ли уничтожить ещё элемент и уменьшает внутренний счётчик
        /// </summary>
        /// <returns>Можно ли уничтожить элемент</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RequestElementToDestroy()
        {
            if (Volatile.Read(ref _elementToDestroy) <= 0)
                return false;

            return Interlocked.Decrement(ref _elementToDestroy) >= 0;
        }

        /// <summary>
        /// Обновление состояния
        /// </summary>
        private void UpdateStateCore()
        {
            int timestamp = GetTimestamp();
            int lastTestTime = Volatile.Read(ref _lastTestTime);

            if (Interlocked.CompareExchange(ref _lastTestTime, timestamp, lastTestTime) != lastTestTime)
                return;

            int minFreeElementsCount = Interlocked.Exchange(ref _minFreeElementsCount, int.MaxValue);
            TurboContract.Assert(minFreeElementsCount >= 0, "minFreeElementsCount >= 0");

            if (minFreeElementsCount == int.MaxValue)
                Interlocked.Exchange(ref _elementToDestroy, 0);
            else
                Interlocked.Exchange(ref _elementToDestroy, minFreeElementsCount);
        }

        /// <summary>
        /// Обновить оценку числа элементов для уничтожения.
        /// Проверяет, что интервал замера прошёл и выполняет обновление.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateState()
        {
            if (_checkPeriod == int.MaxValue)
                return;

            if (GetTimestamp() - Volatile.Read(ref _lastTestTime) < _checkPeriod)
                return;

            UpdateStateCore();
        }
    }
}
