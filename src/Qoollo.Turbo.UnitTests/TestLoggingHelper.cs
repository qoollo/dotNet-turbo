using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests
{
    internal class TestLoggingHelper
    {
        private static UnhandledExceptionEventHandler _unhandledHandler;
        private static EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> _firstChanceHandler;

        private static int _logFirstChanceEntry = 0;

        private static void LogUnhandled(object sender, UnhandledExceptionEventArgs e, TestContext testContext)
        {
            try
            {
                testContext.WriteLine("Unhandled exception: " + Environment.NewLine +
                    //testContext.FullyQualifiedTestClassName + "::" + testContext.TestName + Environment.NewLine +
                    e.ExceptionObject.ToString() + Environment.NewLine +
                    "=============" + Environment.NewLine);
            }
            catch
            {
                System.Diagnostics.Trace.WriteLine("Unhandled exception: " + Environment.NewLine +
                    //testContext.FullyQualifiedTestClassName + "::" + testContext.TestName + Environment.NewLine +
                    e.ExceptionObject.ToString() + Environment.NewLine +
                    "=============" + Environment.NewLine);
            }
        }
        private static void LogFirstChance(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e, TestContext testContext)
        {
            try
            {
                if (System.Threading.Interlocked.Increment(ref _logFirstChanceEntry) == 1)
                {
                    testContext.WriteLine("First chance exception: " + Environment.NewLine +
                        //testContext.FullyQualifiedTestClassName + "::" + testContext.TestName + Environment.NewLine +
                        e.Exception.ToString() + Environment.NewLine +
                        "=============" + Environment.NewLine);
                }
            }
            catch
            {
                System.Diagnostics.Trace.WriteLine("First chance exception: " + Environment.NewLine +
                    //testContext.FullyQualifiedTestClassName + "::" + testContext.TestName + Environment.NewLine +
                        e.Exception.ToString() + Environment.NewLine +
                        "=============" + Environment.NewLine);
            }
            finally
            {
                System.Threading.Interlocked.Decrement(ref _logFirstChanceEntry);
            }
        }

        public static void Subscribe(TestContext context, bool withFirstChance = true)
        {
            Unsubscribe();

            _unhandledHandler = (s, e) => LogUnhandled(s, e, context);
            AppDomain.CurrentDomain.UnhandledException += _unhandledHandler;
            if (withFirstChance)
            {
                _firstChanceHandler = (s, e) => LogFirstChance(s, e, context);
                AppDomain.CurrentDomain.FirstChanceException += _firstChanceHandler;
            }
        }
        public static void Unsubscribe()
        {
            if (_unhandledHandler != null)
            {
                AppDomain.CurrentDomain.UnhandledException -= _unhandledHandler;
                _unhandledHandler = null;
            }
            if (_firstChanceHandler != null)
            {
                AppDomain.CurrentDomain.FirstChanceException -= _firstChanceHandler;
                _firstChanceHandler = null;
            }
        }
    }
}
