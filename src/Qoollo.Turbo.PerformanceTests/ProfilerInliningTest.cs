using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.PerformanceTests
{
    internal interface IProfilerTest
    {
        void Method(string val);
    }
    internal class ProfilerWrapper: IProfilerTest
    {
        private readonly IProfilerTest _wrp;
        public ProfilerWrapper(IProfilerTest wrp)
        {
            _wrp = wrp;
        }

        protected virtual void Handle(Exception ex)
        {
            throw new Exception();
        }

        public void Method(string val)
        {
            try { _wrp.Method(val); }
            catch (Exception ex) { Handle(ex); }
        }
    }

    internal class DefaultProfiler : IProfilerTest
    {
        public static DefaultProfiler Instance { get; } = new DefaultProfiler();
        public void Method(string val) { }
    }

    internal class ProfilerTest : IProfilerTest
    {
        public volatile string LastMsg = null;

        public void Method(string val)
        {
            LastMsg = val;
        }
    }


    internal static class ProfilerTst
    {
        private static readonly object _syncObject = new object();
        private static IProfilerTest _profiler = null;// DefaultProfiler.Instance;
        private static bool _isProfilingEnabled = false;

        public static bool IsProfilingEnabled { get { return _isProfilingEnabled; } }

        public static void SetProfiler(IProfilerTest profiler)
        {
            lock (_syncObject)
            {
                if (profiler == null)
                    profiler = null;//DefaultProfiler.Instance;
                else if (profiler.GetType() != typeof(ProfilerWrapper) && !profiler.GetType().IsSubclassOf(typeof(ProfilerWrapper)))
                    profiler = new ProfilerWrapper(profiler);

                System.Threading.Interlocked.Exchange(ref _profiler, profiler);
                _isProfilingEnabled = !object.ReferenceEquals(profiler, DefaultProfiler.Instance);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void InnerMethod(string val)
        {
            _profiler?.Method(val);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Method(string val)
        {
            if (_profiler != null)
                InnerMethod(val);

            //_profiler?.Method(val);
        }
    }

    public static class ProfilerInliningTest
    {
        static int _incVar;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void TestMethod()
        {
            _incVar++;
            ProfilerTst.Method("a");
            _incVar--;
        }


        public static void RunTest()
        {
            for (int i = 0; i < 10; i++)
                TestMethod();

            for (int m = 0; m < 3; m++)
            {
                ProfilerTst.SetProfiler(null);

                Stopwatch sw = Stopwatch.StartNew();
                for (int i = 0; i < 500000000; i++)
                    TestMethod();
                sw.Stop();

                ProfilerTst.SetProfiler(new ProfilerTest());
                //System.Diagnostics.Debugger.Launch();

                Stopwatch sw2 = Stopwatch.StartNew();
                for (int i = 0; i < 500000000; i++)
                    TestMethod();
                sw2.Stop();

                Console.WriteLine($"Loop 1: {sw.ElapsedMilliseconds}ms, Loop 2: {sw2.ElapsedMilliseconds}ms");
            }
        }
    }
}
