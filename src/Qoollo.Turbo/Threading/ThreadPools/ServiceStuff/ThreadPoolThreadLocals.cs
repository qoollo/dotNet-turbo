using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading.ThreadPools.ServiceStuff
{
    /// <summary>
    /// Локальная данные для потока из пула
    /// </summary>
    internal class ThreadPoolThreadLocals: IDisposable
    {
        /// <summary>
        /// Локальная очередь потока
        /// </summary>
        public readonly ThreadPoolLocalQueue LocalQueue;
        /// <summary>
        /// Глобальные данные пула
        /// </summary>
        public readonly ThreadPoolGlobals Globals;
        private volatile bool _isDisposed;

        /// <summary>
        /// Конструктор ThreadPoolThreadLocals
        /// </summary>
        /// <param name="globals">Глобальные данные пула</param>
        /// <param name="createLocalQueue">Создавать ли локальную очередь</param>
        public ThreadPoolThreadLocals(ThreadPoolGlobals globals, bool createLocalQueue)
        {
            Contract.Requires(globals != null);

            Globals = globals;
            if (createLocalQueue)
                LocalQueue = new ThreadPoolLocalQueue();
            _isDisposed = false;
        }


        private void Dispose(bool isUserCall)
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                Contract.Assert(isUserCall, "ThreadPoolThreadLocals should be disposed explicitly by calling Dispose on ThreadPool. ThreadPoolName: " + (Globals != null ? Globals.OwnerPoolName : "unknown"));
                if (!isUserCall)
                    throw new InvalidOperationException("ThreadPoolThreadLocals should be disposed explicitly");
            }
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Финализатор
        /// </summary>
        ~ThreadPoolThreadLocals()
        {
            Dispose(false);
        }
    }
}
