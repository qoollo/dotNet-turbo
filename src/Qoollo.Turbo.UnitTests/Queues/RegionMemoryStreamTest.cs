using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Turbo.Queues.DiskQueueComponents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.UnitTests.Queues
{
    [TestClass]
    public class RegionMemoryStreamTest
    {
        [TestMethod]
        public void TestReadWrite()
        {
            var inst = new RegionMemoryStream();
            Assert.IsTrue(inst.CanRead);
            Assert.IsTrue(inst.CanWrite);
            Assert.IsTrue(inst.CanSeek);

            var writer = new BinaryWriter(inst);
            for (int i = 0; i < 10; i++)
                writer.Write(i);

            inst.Position = 0;
            var reader = new BinaryReader(inst);
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i, reader.ReadInt32());
        }

        [TestMethod]
        public void TestOriginChange()
        {
            var inst = new RegionMemoryStream();

            var writer = new BinaryWriter(inst);
            for (int i = 0; i < 100; i++)
            {
                int curOrigin = inst.Origin;
                inst.SetOriginLength(curOrigin + 4, -1);
                
                Assert.AreEqual(0, inst.Position);
                Assert.AreEqual(0, inst.Length);
                Assert.IsTrue(inst.CanWrite);
                for (int j = 1; j <= 10; j++)
                    writer.Write(j);
                inst.Seek(4, SeekOrigin.Begin);
                Assert.AreEqual(40, inst.Length);

                int length = (int)inst.Length;
                inst.SetOrigin(curOrigin);
                writer.Write(length);
                inst.Seek(0, SeekOrigin.End);
                inst.SetCurrentPositionAsOrigin();
            }

            Assert.AreEqual(100 * (4 * 10 + 4), inst.InnerStream.Length);

            var reader = new BinaryReader(inst);
            inst.SetOrigin(0);
            for (int i = 0; i < 100; i++)
            {
                inst.SetCurrentPositionAsOrigin(4);
                int length = reader.ReadInt32();
                inst.SetCurrentPositionAsOrigin(length);

                Assert.AreEqual(0, inst.Position);
                Assert.AreEqual(length, inst.Length);
                Assert.IsFalse(inst.CanWrite);
                for (int j = 1; j <= 10; j++)
                    Assert.AreEqual(j, reader.ReadInt32());
                inst.Seek(4, SeekOrigin.Begin);

                inst.Seek(0, SeekOrigin.End);             
            }
        }


        [TestMethod]
        public void TestSeekPosition()
        {
            var inst = new RegionMemoryStream();
            var writer = new BinaryWriter(inst);

            for (int j = 1; j <= 10; j++)
                writer.Write(j);

            inst.SetOriginLength(4, 12);

            Assert.AreEqual(4, inst.Origin);
            Assert.AreEqual(0, inst.Position);
            Assert.AreEqual(12, inst.Length);

            inst.Seek(4, SeekOrigin.Begin);
            Assert.AreEqual(4, inst.Origin);
            Assert.AreEqual(4, inst.Position);
            Assert.AreEqual(12, inst.Length);

            inst.Seek(4, SeekOrigin.Current);
            Assert.AreEqual(4, inst.Origin);
            Assert.AreEqual(8, inst.Position);
            Assert.AreEqual(12, inst.Length);

            inst.Seek(-8, SeekOrigin.End);
            Assert.AreEqual(4, inst.Origin);
            Assert.AreEqual(4, inst.Position);
            Assert.AreEqual(12, inst.Length);


            inst.Position = 6;
            Assert.AreEqual(4, inst.Origin);
            Assert.AreEqual(6, inst.Position);
            Assert.AreEqual(12, inst.Length);


            try
            {
                inst.Seek(-20, SeekOrigin.End);
                Assert.Fail("exception expected");
            }
            catch (IOException) { }

            try
            {
                inst.Seek(20, SeekOrigin.Begin);
                inst.WriteByte(1);
                Assert.Fail("exception expected");
            }
            catch (NotSupportedException) { }

            try
            {
                inst.Seek(20, SeekOrigin.Current);
                inst.WriteByte(1);
                Assert.Fail("exception expected");
            }
            catch (NotSupportedException) { }
        }
    }
}
