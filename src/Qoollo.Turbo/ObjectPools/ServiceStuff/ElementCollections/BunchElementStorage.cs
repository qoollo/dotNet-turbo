using Qoollo.Turbo.ObjectPools.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.ObjectPools.ServiceStuff.ElementCollections
{
    /// <summary>
    /// Обёртка над группой IndexedStackElementStorage.
    /// Каждому потоку соотносится отдельный стек в зависимости от индекса этого потока
    /// </summary>
    /// <typeparam name="T">Тип элемента</typeparam>
    internal class BunchElementStorage<T>
    {
        private readonly IndexedStackElementStorage<T>[] _bunches;

        /// <summary>
        /// Конструктор BunchElementStorage
        /// </summary>
        /// <param name="arr">Массив данных</param>
        public BunchElementStorage(SparceArrayStorage<PoolElementWrapper<T>> arr)
        {
            TurboContract.Requires(arr != null, conditionString: "arr != null");

            _bunches = new IndexedStackElementStorage<T>[Math.Min(8, Environment.ProcessorCount)];
            for (int i = 0; i < _bunches.Length; i++)
                _bunches[i] = new IndexedStackElementStorage<T>(arr);
        }

        /// <summary>
        /// Добавить элемент
        /// </summary>
        /// <param name="element">Элемент</param>
        public void Add(PoolElementWrapper<T> element)
        {
            TurboContract.Requires(element != null, conditionString: "element != null");

            _bunches[Thread.CurrentThread.ManagedThreadId % _bunches.Length].Add(element);
        }

        /// <summary>
        /// Забрать элемент. 
        /// Наличие элемент должно контролироваться из вне!!!
        /// </summary>
        /// <param name="element">Выбранный элемент</param>
        public void Take(out PoolElementWrapper<T> element)
        {
            int curIndex = Thread.CurrentThread.ManagedThreadId % _bunches.Length;
            int iterationCount = 0;

            while (true)
            {
                if (_bunches[curIndex].TryTake(out element))
                    return;
                curIndex = (curIndex + 1) % _bunches.Length;

                TurboContract.Assert(iterationCount++ < 1024, conditionString: "iterationCount++ < 1024");
            }
        }
    }
}
