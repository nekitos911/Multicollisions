using System;
using System.Linq;
using Algorithm;
using NUnit.Framework;

namespace MoraHash.Tests
{
    public class TestMoraHash
    {
        [Test]
        public void TestG_n()
        {
            // h1 из контрольного примера
            var expected = new byte[] {0x1c, 0x9b, 0xea, 0x78, 0xab, 0x26, 0x32, 0x56};
            // дополненное сообщение из контрольного примера
            var m = new byte[] {0x01, 0xd4, 0x44, 0x90, 0x7e, 0xfb, 0x8c, 0xf7};
            var mora = new HashFunction();
            
            var result = mora.G_n(new byte[8], new byte[8], m);
            var resultMultiCol = MultiCollisions.G_n(0, 0, BitConverter.ToUInt64(m.Reverse().ToArray(), 0));
            
            Assert.True(expected.SequenceEqual(result));
            Assert.True(expected.SequenceEqual(BitConverter.GetBytes(resultMultiCol).Reverse()));
        }
        
        
        [Test]
        public void TestHash()
        {
            //h из контрольного примера
            var expected = new byte[] {0xb1, 0x7e, 0xb3, 0xf6, 0x0f, 0x29, 0x0e, 0xfd};
            // сообщение из контрольного примера
            var m = new byte[] { 0xd4, 0x44, 0x90, 0x7e, 0xfb, 0x8c, 0xf7};
            var mora = new HashFunction();
            var result = mora.ComputeHash(m);
            
            Assert.True(expected.SequenceEqual(result));
        }
    }
}