using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Threading.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Qoollo.Turbo.UnitTests.Threading
{
    [TestClass]
    public class SpinWaitHelperTest: TestClassBase
    {
        [TestMethod]
        public void SpinWaitTimeValidationTest()
        {
            for (int i = 0; i < 30000; i++)
                Thread.SpinWait(1000);
        }

#if NETCOREAPP3_1
        [TestMethod]
        public void SpinWaitOnNetCore31IsAlwaysNormalized()
        {
            for (int i = 0; i < 10; i++)
            {
                int normValue = SpinWaitHelper.MeasureSpinWaitNormalizationCoef();
                Assert.AreEqual(1, normValue);
                Thread.Sleep(1);
            }
        }
#endif

        [TestMethod]
        public void SpinWaitSmallFluctuation()
        {
            List<int> measureResults = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                measureResults.Add(SpinWaitHelper.MeasureSpinWaitNormalizationCoef());
                Thread.Sleep(1);
            }

            int minMeasure = measureResults.Min();
            int maxMeasure = measureResults.Max();
            Assert.IsTrue(maxMeasure - minMeasure <= 1);
        }


        [TestMethod]
        [Timeout(5000)]
        public void SpinWaitPerformNormalizationInBackground()
        {
            while (!SpinWaitHelper.NormalizationCoefCalculated)
            {
                SpinWaitHelper.SpinWait(100);
            }
        }


        private void SpinningNormalizedValidationCore()
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 500; i++)
                SpinWaitHelper.SpinWait(37 * 1000);
            sw.Stop();
            TestContext.WriteLine($"Measured time: {sw.ElapsedMilliseconds}ms");
            Assert.IsTrue(sw.ElapsedMilliseconds > 400 && sw.ElapsedMilliseconds < 800, "Measured time: " + sw.ElapsedMilliseconds.ToString());
        }

        [TestMethod]
        [Timeout(5000)]
        public void SpinningNormalizedValidation()
        {
            while (!SpinWaitHelper.NormalizationCoefCalculated)
            {
                SpinWaitHelper.SpinWait(100);
            }

            SpinningNormalizedValidationCore();
            SpinningNormalizedValidationCore();
            SpinningNormalizedValidationCore();
        }
    }
}
