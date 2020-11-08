using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq.Extensions;

namespace MoraHash
{
    internal static class Utils
    {
        public static byte[] Xor(this IEnumerable<byte> l, IEnumerable<byte> r) => l.Zip(r, (bl, br) => (bl, br)).Select(b => (byte)(b.bl ^ b.br)).ToArray();
        
        public static byte[] RingSum(this IEnumerable<byte> a, IEnumerable<byte> b, int dim = 16) => a.Zip(b.Pad(a.Count()), (b1, b2) => (b1, b2))
            .Select(tup => (byte) ((uint) (tup.b1 + tup.b2) % dim)).ToArray();
    }
    
    
}