using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreLinq.Extensions;

namespace MoraHash
{
    public static class ByteUtils
    {
        public static byte[] Xor(this IEnumerable<byte> l, IEnumerable<byte> r) => l.Zip(r, (bl, br) => (bl, br)).Select(b => (byte)(b.bl ^ b.br)).ToArray();
        
        public static byte[] RingSum(this IEnumerable<byte> a, IEnumerable<byte> b, int dim = 16) => a.Zip(b.Pad(a.Count()), (b1, b2) => (b1, b2))
            .Select(tup => (byte) ((uint) (tup.b1 + tup.b2) % dim)).ToArray();
        
        public static byte[] AddModulo64(this byte[] a, byte[] b)
        {
            byte[] temp = new byte[8];
            int i = 0, t = 0;
            byte[] tempA = new byte[8];
            byte[] tempB = new byte[8];
            Array.Copy(a, 0, tempA, 8 - a.Length, a.Length);
            Array.Copy(b, 0, tempB, 8 - b.Length, b.Length);
            for (i = 7; i >= 0; i--)
            {
                t = tempA[i] + tempB[i] + (t >> 8);
                temp[i] = (byte)(t & 0xFF);
            }
            return temp;
        }
        
        public static byte JoinBytes(byte hi, byte low) => (byte)((hi << 4) | (low & 0xffffffffL));

        public static (byte hi, byte low) SplitByte(byte val) => ((byte) (val >> 4), (byte) (val & 0xf));
        
        public static BitArray ToBitArray (this byte[] bytes, int bitCount) {
            BitArray ba = new BitArray (bitCount);
            for (int i = 0; i < bitCount; ++i) {
                ba.Set (i, ((bytes[i / 8] >> (i % 8)) & 0x01) > 0);
            }
            return ba;
        }
    }
    
    
}