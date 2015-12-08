using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.IoC;
using Qoollo.Turbo.IoC.Helpers;
using Qoollo.Turbo.IoC.ServiceStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.IoC
{
    [TestClass]
    public class ObjectInstantiationHelperTests
    {
        public class TestSimpleClass { }

        public class TestClassWithConstructor
        {
            public readonly int Val;

            public TestClassWithConstructor(int val)
            {
                Val = val;
            }
        }

        public class TestClassWithMarkedConstructor
        {
            public readonly int Val;

            public TestClassWithMarkedConstructor()
            {
                Val = -1;
            }
            [DefaultConstructor]
            public TestClassWithMarkedConstructor(int val)
            {
                Val = val;
            }
        }

        public class TestInjectionResolver: IInjectionResolver
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


        // ==================


        private T ValidateFunc<T>(Func<IInjectionResolver, object> func)
        {
            var obj = func(new TestInjectionResolver());

            Assert.IsNotNull(obj);
            Assert.IsTrue(obj is T);

            return (T)obj;
        }


        private T ValidateFunc<T>(Func<object> func)
        {
            var obj = func();

            Assert.IsNotNull(obj);
            Assert.IsTrue(obj is T);

            return (T)obj;
        }

        private T ValidateInstCreator<T>(IInstanceCreator instCreator)
        {
            var obj = instCreator.CreateInstance(new TestInjectionResolver());

            Assert.IsNotNull(obj);
            Assert.IsTrue(obj is T);

            return (T)obj;
        }

        private T ValidateInstCreator<T>(IInstanceCreatorNoParam instCreator)
        {
            var obj = instCreator.CreateInstance();

            Assert.IsNotNull(obj);
            Assert.IsTrue(obj is T);

            return (T)obj;
        }

        // ====================



        [TestMethod]
        public void TestGetReflectionBasedCreationFunction()
        {
            ValidateFunc<TestSimpleClass>(ObjectInstantiationHelper.GetReflectionBasedCreationFunction(typeof(TestSimpleClass), null));
            ValidateFunc<TestClassWithConstructor>(ObjectInstantiationHelper.GetReflectionBasedCreationFunction(typeof(TestClassWithConstructor), null));
            var v3 = ValidateFunc<TestClassWithMarkedConstructor>(ObjectInstantiationHelper.GetReflectionBasedCreationFunction(typeof(TestClassWithMarkedConstructor), null));
            Assert.AreEqual(10, v3.Val);
        }



        [TestMethod]
        public void TestGetCompiledCreationFunction()
        {
            ValidateFunc<TestSimpleClass>(ObjectInstantiationHelper.GetCompiledCreationFunction(typeof(TestSimpleClass), null));
            ValidateFunc<TestClassWithConstructor>(ObjectInstantiationHelper.GetCompiledCreationFunction(typeof(TestClassWithConstructor), null));
            var v3 = ValidateFunc<TestClassWithMarkedConstructor>(ObjectInstantiationHelper.GetCompiledCreationFunction(typeof(TestClassWithMarkedConstructor), null));
            Assert.AreEqual(10, v3.Val);
        }


        [TestMethod]
        public void TestBuildInstanceCreatorFuncInDynAssembly()
        {
            ValidateFunc<TestSimpleClass>(ObjectInstantiationHelper.BuildCreatorFuncInDynAssembly(typeof(TestSimpleClass), null));
            ValidateFunc<TestClassWithConstructor>(ObjectInstantiationHelper.BuildCreatorFuncInDynAssembly(typeof(TestClassWithConstructor), null));
            var v3 = ValidateFunc<TestClassWithMarkedConstructor>(ObjectInstantiationHelper.BuildCreatorFuncInDynAssembly(typeof(TestClassWithMarkedConstructor), null));
            Assert.AreEqual(10, v3.Val);
        }


        [TestMethod]
        public void TestBuildInstanceCreatorInDynAssembly()
        {
            ValidateInstCreator<TestSimpleClass>(ObjectInstantiationHelper.BuildInstanceCreatorInDynAssembly(typeof(TestSimpleClass), null));
            ValidateInstCreator<TestClassWithConstructor>(ObjectInstantiationHelper.BuildInstanceCreatorInDynAssembly(typeof(TestClassWithConstructor), null));
            var v3 = ValidateInstCreator<TestClassWithMarkedConstructor>(ObjectInstantiationHelper.BuildInstanceCreatorInDynAssembly(typeof(TestClassWithMarkedConstructor), null));
            Assert.AreEqual(10, v3.Val);
        }


        [TestMethod]
        public void TestBuildInstanceCreatorFuncNoParamInDynAssembly()
        {
            ValidateFunc<TestSimpleClass>(ObjectInstantiationHelper.BuildCreatorFuncNoParamInDynAssembly(typeof(TestSimpleClass), new TestInjectionResolver(), null));
            ValidateFunc<TestClassWithConstructor>(ObjectInstantiationHelper.BuildCreatorFuncNoParamInDynAssembly(typeof(TestClassWithConstructor), new TestInjectionResolver(), null));
            var v3 = ValidateFunc<TestClassWithMarkedConstructor>(ObjectInstantiationHelper.BuildCreatorFuncNoParamInDynAssembly(typeof(TestClassWithMarkedConstructor), new TestInjectionResolver(), null));
            Assert.AreEqual(10, v3.Val);
        }

        [TestMethod]
        public void TestBuildInstanceCreatorNoParamInDynAssembly()
        {
            ValidateInstCreator<TestSimpleClass>(ObjectInstantiationHelper.BuildInstanceCreatorNoParamInDynAssembly(typeof(TestSimpleClass), new TestInjectionResolver(), null));
            ValidateInstCreator<TestClassWithConstructor>(ObjectInstantiationHelper.BuildInstanceCreatorNoParamInDynAssembly(typeof(TestClassWithConstructor), new TestInjectionResolver(), null));
            var v3 = ValidateInstCreator<TestClassWithMarkedConstructor>(ObjectInstantiationHelper.BuildInstanceCreatorNoParamInDynAssembly(typeof(TestClassWithMarkedConstructor), new TestInjectionResolver(), null));
            Assert.AreEqual(10, v3.Val);
        }
    }
}
