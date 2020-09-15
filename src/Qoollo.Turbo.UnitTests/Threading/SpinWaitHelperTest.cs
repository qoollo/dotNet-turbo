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

        [TestMethod]
        public void FrameworkSupportSpinWaitNormalization()
        {
            Assert.IsTrue(SpinWaitHelper.IsFrameworkSupportSpinWaitNormalization());
        }
#endif

#if NET45 || NET46
        [TestMethod]
        public void FrameworkNotSupportSpinWaitNormalization()
        {
            Assert.IsFalse(SpinWaitHelper.IsFrameworkSupportSpinWaitNormalization());
        }
#endif

        [TestMethod]
        public void ProcessorDetectionIsNotFail()
        {
            TestContext.WriteLine(SpinWaitHelper.DetectProcessorKind().ToString());
        }


        [TestMethod]
        public void SpinWaitSmallFluctuation()
        {
            List<int> measureResults = new List<int>();
            for (int i = 0; i < 10; i++)
            {
                measureResults.Add(SpinWaitHelper.MeasureSpinWaitNormalizationCoef());
                Thread.Sleep(100);
            }

            int minMeasure = measureResults.Min();
            int maxMeasure = measureResults.Max();
            TestContext.WriteLine($"MinMeasure: {minMeasure}, MaxMeasure: {maxMeasure}");
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

            this.TestContext.WriteLine($"Normalization coef: {SpinWaitHelper.NormalizationCoef}");
        }


        private void SpinningNormalizedValidationCore()
        {
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 500; i++)
                SpinWaitHelper.SpinWait(1000 * 1000 / 37);
            sw.Stop();
            TestContext.WriteLine($"Measured time: {sw.ElapsedMilliseconds}ms");
            // Expect 500ms (can be large due to context switch)
            Assert.IsTrue(sw.ElapsedMilliseconds > 400 && sw.ElapsedMilliseconds < 600, "Measured time: " + sw.ElapsedMilliseconds.ToString());
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
