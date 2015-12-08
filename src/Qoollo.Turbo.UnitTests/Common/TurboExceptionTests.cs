using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Common
{
    [TestClass]
    public class TurboExceptionTests
    {
        private void TestTypedExceptionWithMessage<TExc>() where TExc: Exception
        {
            try
            {
                TurboException.Throw<TExc>("test message");
                Assert.Fail("Should throw exception");
            }
            catch (Exception ex)
            {
                Assert.AreEqual("test message", ex.Message);
                Assert.IsTrue(ex.GetType() == typeof(TExc));
            }
        }

        private void TestTypedExceptionWithoutMessage<TExc>() where TExc : Exception
        {
            try
            {
                TurboException.Throw<TExc>();
                Assert.Fail("Should throw exception");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.GetType() == typeof(TExc));
            }
        }


        [TestMethod]
        public void TestThrowException()
        {
            TestTypedExceptionWithMessage<Exception>();
            TestTypedExceptionWithoutMessage<Exception>();
        }

        [TestMethod]
        public void TestThrowArgumentException()
        {
            TestTypedExceptionWithMessage<ArgumentException>();
            TestTypedExceptionWithoutMessage<ArgumentException>();
        }

        [TestMethod]
        public void TestThrowArgumentNullException()
        {
            TestTypedExceptionWithMessage<ArgumentNullException>();
            TestTypedExceptionWithoutMessage<ArgumentNullException>();
        }

        [TestMethod]
        public void TestThrowArgumentOutOfRangeException()
        {
            TestTypedExceptionWithMessage<ArgumentOutOfRangeException>();
            TestTypedExceptionWithoutMessage<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void TestThrowInvalidOperationException()
        {
            TestTypedExceptionWithMessage<InvalidOperationException>();
            TestTypedExceptionWithoutMessage<InvalidOperationException>();
        }

        [TestMethod]
        public void TestThrowApplicationException()
        {
            TestTypedExceptionWithMessage<ApplicationException>();
            TestTypedExceptionWithoutMessage<ApplicationException>();
        }

        [TestMethod]
        public void TestThrowSystemException()
        {
            TestTypedExceptionWithMessage<SystemException>();
            TestTypedExceptionWithoutMessage<SystemException>();
        }

        [TestMethod]
        public void TestThrowObjectDisposedException()
        {
            TestTypedExceptionWithMessage<ObjectDisposedException>();
            TestTypedExceptionWithoutMessage<ObjectDisposedException>();
        }

        [TestMethod]
        public void TestThrowKeyNotFoundException()
        {
            TestTypedExceptionWithMessage<KeyNotFoundException>();
            TestTypedExceptionWithoutMessage<KeyNotFoundException>();
        }


        [TestMethod]
        public void TestAssertNotTriggered()
        {
            TurboException.Assert<ArgumentException>(true, "message");
        }

        [TestMethod]
        public void TestAssertTriggered()
        {
            try
            {
                TurboException.Assert<ArgumentException>(false, "message");
                Assert.Fail("Should throw exception");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.GetType() == typeof(ArgumentException));
            }
        }
    }
}
