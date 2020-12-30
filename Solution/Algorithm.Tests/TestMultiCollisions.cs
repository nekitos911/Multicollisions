using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoraHash;
using NUnit.Framework;

namespace Algorithm.Tests
{
    public class TestMultiCollisions
    {

        private class ByteArrayComparer : IEqualityComparer<byte[]>
        {
            private static ByteArrayComparer _default;

            public static ByteArrayComparer Default => _default ?? (_default = new ByteArrayComparer());

            public bool Equals(byte[] x, byte[] y)
            {
                return StructuralComparisons.StructuralEqualityComparer.Equals(x, y);
            }

            public int GetHashCode(byte[] obj)
            {
                return StructuralComparisons.StructuralEqualityComparer.GetHashCode(obj);
            }
        }

        [Test]
        public void TestCollisions()
        {
            var mora = new HashFunction();

            var lst = new List<List<ulong>>();
            var solutions = new List<ulong[]>();
            
            // Коллизии найдены заранее
            // 15191912733080746243 : 2816499036862610626
            // 12602365574662108235 : 15317591334086282728
//            2541090681042594495 : 12112829685509080139
//            8325695980190155696 : 10246617843613846309
//            4364755641628337682 : 7731428877087533129
            lst.Add(new List<ulong>() {4364755641628337682, 7731428877087533129});
            lst.Add(new List<ulong>() {8325695980190155696, 10246617843613846309});
            lst.Add(new List<ulong>() {2541090681042594495, 12112829685509080139});
            lst.Add(new List<ulong>() {12602365574662108235, 15317591334086282728});
            lst.Add(new List<ulong>() {15191912733080746243, 2816499036862610626});
            
            Utils.Solve(lst, solutions, new ulong[lst.Count]);

            var messages = solutions.Select(seq =>
            {
                IEnumerable<byte> ret = new byte[0];

                ret = seq.Aggregate(ret, (current, value) => 
                    current.Concat(BitConverter.GetBytes(ulong.MaxValue - value + 1).Reverse())
                    .Concat(BitConverter.GetBytes(value).Reverse()));

                return ret.ToArray();
            }).ToArray();
            
            Assert.True(solutions.Count() == 1 << lst.Count());
            
            bool allMessagesAreDifferent = messages.Distinct(ByteArrayComparer.Default).Count() == messages.Length;
            
            Assert.True(allMessagesAreDifferent);
            
            // 2 ^ lst.Count хеши
            var hashes = messages.Select(msg => mora.ComputeHash(msg)).ToArray();

            // Все хеши должны иметь одинаковое значение
            bool allHashesAreEqual = hashes.All(hash => hash.SequenceEqual(hashes.First()));
            
            Assert.True(allHashesAreEqual);
        }
    }
}