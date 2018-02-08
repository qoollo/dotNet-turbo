using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.IoC;
using Qoollo.Turbo.IoC.Lifetime;
using Qoollo.Turbo.IoC.Lifetime.Factories;
using Qoollo.Turbo.IoC.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.IoC
{
    [TestClass]
    public class LifetimeFactoriesTests : TestClassBase
    {
        public class TestBaseClass { }
        public class TestClassWithConstructor : TestBaseClass
        {
            public readonly int Val;

            public TestClassWithConstructor(int val)
            {
                Val = val;
            }
        }

        public class TestInjectionResolver : IInjectionResolver
        {
            public object Resolve(Type reqObjectType, string paramName, Type forType, object extData)
            {
                if (reqObjectType == typeof(int))
                    return 10;

                throw new ArgumentException("unknown type");
            }

            public T Resolve<T>(Type forType)
            {
                if (typeof(T) == typeof(int))
                    return (T)(object)10;

                throw new ArgumentException("unknown type");
            }
        }

        // ===================


        private LifetimeBase TestFactory<T>(LifetimeFactory factory)
        {
            TestInjectionResolver resolver = new TestInjectionResolver();

            var container = factory.Create(typeof(T), resolver, null);
            Assert.AreEqual(typeof(T), container.OutputType);

            var instance = container.GetInstance(resolver);
            Assert.IsNotNull(instance);
            Assert.IsInstanceOfType(instance, typeof(T));

            return container;
        }

        private T TestContainer<T>(LifetimeBase container)
        {
            TestInjectionResolver resolver = new TestInjectionResolver();

            Assert.AreEqual(typeof(T), container.OutputType);

            var instance = container.GetInstance(resolver);
            Assert.IsNotNull(instance);
            Assert.IsInstanceOfType(instance, typeof(T));

            return (T)instance;
        }


        [TestMethod]
        public void TestSingletonFactory()
        {
            var c = TestFactory<TestClassWithConstructor>(LifetimeFactories.Singleton);
            var inst1 = TestContainer<TestClassWithConstructor>(c);
            var inst2 = TestContainer<TestClassWithConstructor>(c);

            Assert.AreSame(inst1, inst2);
        }




        [TestMethod]
        public void TestDeferedSingletonFactory()
        {
            var c = TestFactory<TestClassWithConstructor>(LifetimeFactories.DeferedSingleton);
            var inst1 = TestContainer<TestClassWithConstructor>(c);
            var inst2 = TestContainer<TestClassWithConstructor>(c);

            Assert.AreSame(inst1, inst2);
        }




        [TestMethod]
        public void TestPerThreadFactory()
        {
            var c = TestFactory<TestClassWithConstructor>(LifetimeFactories.PerThread);
            var inst1 = TestContainer<TestClassWithConstructor>(c);
            var inst2 = TestContainer<TestClassWithConstructor>(c);
            var inst3 = Task.Run(() => TestContainer<TestClassWithConstructor>(c)).Result;

            Assert.AreSame(inst1, inst2);
            Assert.AreNotSame(inst1, inst3);
        }




        [TestMethod]
        public void TestPerCallFactory()
        {
            var c = TestFactory<TestClassWithConstructor>(LifetimeFactories.PerCall);
            var inst1 = TestContainer<TestClassWithConstructor>(c);
            var inst2 = TestContainer<TestClassWithConstructor>(c);

            Assert.AreNotSame(inst1, inst2);
        }



        [TestMethod]
        public void TestPerCallInlinedParamsFactory()
        {
            var c = TestFactory<TestClassWithConstructor>(LifetimeFactories.PerCallInlinedParams);
            var inst1 = TestContainer<TestClassWithConstructor>(c);
            var inst2 = TestContainer<TestClassWithConstructor>(c);

            Assert.AreNotSame(inst1, inst2);
        }
    }
}
