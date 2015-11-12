using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Common
{
    [TestClass]
    public class TypeUIDResolverTests
    {
        [TestMethod]
        public void GenericTypeIdGeneration()
        {
            var intId = TypeUIDResolver<int>.GetMyId();
            var stringId = TypeUIDResolver<string>.GetMyId();

            Assert.AreNotEqual(0, intId);
            Assert.AreNotEqual(0, stringId);
            Assert.AreEqual(intId, TypeUIDResolver<int>.GetMyId());
            Assert.AreEqual(stringId, TypeUIDResolver<string>.GetMyId());
        }


        [TestMethod]
        public void VerfificationInTypeMapper()
        {
            var intId = TypeUIDResolver<int>.GetMyId();
            var stringId = TypeUIDResolver<string>.GetMyId();

            Assert.AreNotEqual(0, intId);
            Assert.AreNotEqual(0, stringId);
            Assert.AreEqual(intId, TypeUIDResolver.GetTypeId(typeof(int)));
            Assert.AreEqual(stringId, TypeUIDResolver.GetTypeId(typeof(string)));
        }


        [TestMethod]
        public void NonGenericTypeIdGeneration()
        {
            var intId = TypeUIDResolver.GetOrGenerateTypeId(typeof(int));
            var stringId = TypeUIDResolver.GetOrGenerateTypeId(typeof(string));

            Assert.AreNotEqual(0, intId);
            Assert.AreNotEqual(0, stringId);
            Assert.AreEqual(intId, TypeUIDResolver<int>.GetMyId());
            Assert.AreEqual(stringId, TypeUIDResolver<string>.GetMyId());
        }


        //[TestMethod]
        public void GenerationPerformanceTest()
        {
            var intId = TypeUIDResolver<int>.GetMyId();
            var stringId = TypeUIDResolver<string>.GetMyId();

            int idealId = 0;
            var swIdeal = Stopwatch.StartNew();
            for (int i = 0; i < 1000000; i++)
            {
                idealId += 1;
                System.Threading.Thread.MemoryBarrier();
            }
            swIdeal.Stop();

            int realId = 0;
            var swReal = Stopwatch.StartNew();
            for (int i = 0; i < 1000000; i++)
            {
                realId += TypeUIDResolver<string>.GetMyId();
                System.Threading.Thread.MemoryBarrier();
            }
            swReal.Stop();

#if DEBUG
            Assert.IsTrue(((double)swReal.ElapsedTicks / swIdeal.ElapsedTicks) < 4, "Generation slower in compare with increment");
#else
            Assert.IsTrue(((double)swReal.ElapsedTicks / swIdeal.ElapsedTicks) < 3, "Generation slower in compare with increment");
#endif
        }
    }
}
