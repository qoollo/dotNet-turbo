using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.IoC;
using Qoollo.Turbo.IoC.Lifetime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.IoC
{
    [TestClass]
    public class TurboContainerTests
    {
        public interface ITestInterface { }
        public class TestImplementation : ITestInterface { }
        public class TestClass { }
        public class TestInjectionToConstructor
        {
            public readonly double Value;
            public readonly string StrValue;
            public TestInjectionToConstructor(double val, string str)
            {
                Value = val;
                StrValue = str;
            }
        }

        // =============================


        [TestMethod]
        public void SimpleResolveTest()
        {
            using (TurboContainer container = new TurboContainer())
            {
                container.AddSingleton<int>(10);
                var result = container.Resolve<int>();

                Assert.AreEqual(10, result);
            }
        }


        [TestMethod]
        public void SimpleResolveTest2()
        {
            using (var container = new TurboContainer())
            {
                container.AddSingleton<int>(10);
                container.AddSingleton<string>("value");
                container.AddPerCall<ITestInterface, TestImplementation>();
                container.AddAssociation<TestClass, TestClass>(LifetimeFactories.DeferedSingleton);
                container.AddAssociation<double>(new PerCallLifetime(typeof(double), r => 15.0));

                Assert.AreEqual("value", container.Resolve<string>());
                Assert.IsInstanceOfType(container.Resolve<ITestInterface>(), typeof(TestImplementation));

                var obj = container.CreateObject<TestInjectionToConstructor>();
                Assert.IsNotNull(obj);
                Assert.AreEqual(15.0, obj.Value);
                Assert.AreEqual("value", obj.StrValue);
                Assert.IsNotNull(container.Resolve<TestClass>());
            }
        }
    }
}
