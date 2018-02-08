using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Qoollo.Turbo.UnitTests
{
    internal static class ErrorModeInterop
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern ErrorModes SetErrorMode(ErrorModes uMode);

        [Flags]
        enum ErrorModes : uint
        {
            SYSTEM_DEFAULT = 0x0,
            SEM_FAILCRITICALERRORS = 0x0001,
            SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
            SEM_NOGPFAULTERRORBOX = 0x0002,
            SEM_NOOPENFILEERRORBOX = 0x8000
        }

        public static void DisableCrashWindow()
        {
            var val = SetErrorMode(ErrorModes.SEM_NOGPFAULTERRORBOX);
            SetErrorMode(val | ErrorModes.SEM_NOGPFAULTERRORBOX);
        }
    }

    public class TestClassBase: IDisposable
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

        protected static void SubscribeToUnhandledExceptions(TestContext context, bool withFirstChance = true)
        {
            UnsubscribeFromUnhandledExceptions();

            _unhandledHandler = (s, e) => LogUnhandled(s, e, context);
            AppDomain.CurrentDomain.UnhandledException += _unhandledHandler;
            if (withFirstChance)
            {
                _firstChanceHandler = (s, e) => LogFirstChance(s, e, context);
                AppDomain.CurrentDomain.FirstChanceException += _firstChanceHandler;
            }
        }
        protected static void UnsubscribeFromUnhandledExceptions()
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



        // ===========================

        protected static void DisableCrashWindow()
        {
#if NETCOREAPP2_0
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                ErrorModeInterop.DisableCrashWindow();
#elif NET45 || NET46
            ErrorModeInterop.DisableCrashWindow();
#endif
        }

        // =======================


        protected TestClassBase()
        {
            DisableCrashWindow();
        }


        public TestContext TestContext { get; set; }


        protected virtual void Dispose(bool isUserCall)
        {

        }
        public void Dispose()
        {
            this.Dispose(true);
        }
    }
}
