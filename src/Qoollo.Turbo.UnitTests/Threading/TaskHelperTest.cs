using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Threading.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Threading
{
    [TestClass]
    public class TaskHelperTest
    {
        class TestTaskScheduler : TaskScheduler
        {
            public readonly List<Task> Tasks = new List<Task>();

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                lock (Tasks)
                {
                    return Tasks.ToArray();
                }
            }

            protected override void QueueTask(Task task)
            {
                lock (Tasks)
                {
                    Tasks.Add(task);
                    ThreadPool.QueueUserWorkItem((st) =>
                    {
                        TryExecuteTask(task);
                        TryDequeue(task);
                    });
                }
            }

            protected override bool TryDequeue(Task task)
            {
                lock (Tasks)
                {
                    return Tasks.Remove(task);
                }
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return TryExecuteTask(task);
            }
        }

        // ===============

        [TestMethod]
        public void SetTaskSchedulerAndExecuteEntryTest()
        {
            var sched = new TestTaskScheduler();

            int val = 0;
            AtomicBool taskSchedullerSetted = new AtomicBool(false);

            Task tsk = null;
            tsk = new Task(() =>
            {
                taskSchedullerSetted.Value = TaskScheduler.Current == sched;
                Interlocked.Increment(ref val);
            });

            TaskHelper.SetTaskScheduler(tsk, sched);
            TaskHelper.ExecuteTaskEntry(tsk);

            taskSchedullerSetted.AssertIsTrue(10000, "taskSchedullerSetted");
        }

        [TestMethod]
        public void CancelTaskTest()
        {
            int val = 0;
            Task tsk = new Task(() =>
            {
                Interlocked.Increment(ref val);
            });

            Assert.IsFalse(tsk.IsCanceled, "Should not be cancelled");
            TaskHelper.CancelTask(tsk, false);
            Assert.IsTrue(tsk.IsCanceled, "Should be cancelled");
        }
    }
}
