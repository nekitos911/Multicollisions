using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using MoreLinq.Extensions;

namespace MoraHash
{
    public class HashFunction
    {
        private static readonly int BlockSize = 16;
        
        private byte[] _n = new byte[64];
        private byte[] _sigma = new byte[16];
        private byte[] _iv = new byte[16];
        
        private byte[] P(byte[] state) =>
            Enumerable.Range(0, state.Length).Select(i => state[Constants.Tau[i]]).ToArray();

        private byte[] S(byte[] state) => 
            Enumerable.Range(0, state.Length).Select(i => Constants.SBox[state[i]]).ToArray();
        
        private byte[] L(byte[] state) => BatchExtension.Batch(state, 4)
            .Select(bytes =>
                MoreEnumerable.Batch(Constants.L, 4)
                    .Zip(
                        BatchExtension.Batch(new BitArray(bytes.ToArray()).Cast<bool>(), 4)
                            .Where((x, index) => index % 2 == 0).SelectMany(b => b).ToArray(), (v, b) => (v, b))
                    .Where(tup => tup.b).Select(tup => tup.v).Aggregate(Utils.Xor)).SelectMany(res => res).ToArray();

        private byte[] GetHash(byte[] message)
        {
            var h = new byte[BlockSize];
            Array.Copy(_iv, h, BlockSize);

            byte[] n0 = new byte[16];

            IEnumerable<IEnumerable<byte>> blocks = MoreEnumerable.Batch(message, 64);
            
            // Если блок меньше 64, это делать не надо
            MoreEnumerable.ForEach(blocks.Skip(1), msg =>
            {
                h = G_n(_n, h, msg);
                _n = _n.RingSum(BitConverter.GetBytes(64).ToArray());
                _sigma = _sigma.RingSum(msg);
            });

            byte[] m = Enumerable.Append(MoreEnumerable.Pad(blocks.Last(), 63), (byte) 1).ToArray();
           
            h = G_n(_n, h, m);
           
            byte[] msgLen = BitConverter.GetBytes((uint)(m.Length * 8));

            _n = _n.RingSum(msgLen.ToArray());

            _sigma = _sigma.RingSum(m);

            h = G_n(n0, h, _n);
            h = G_n(n0, h, _sigma);

            return h;
        }
        
        private byte[] G_n(IEnumerable<byte> N, IEnumerable<byte> h, IEnumerable<byte> m) => E(L(P(S(h.Xor(N)))), m.ToArray()).Xor(h).Xor(m);

        private byte[] E(byte[] k, byte[] m)
        {
            byte[] state = k.Xor(m);
            for (int i = 0; i < Constants.C.Length; i++)
            {
                state = L(P(S(state))).Xor(KeySchedule(k, i));
            }
            return state;
        }

        private byte[] KeySchedule(byte[] k, int i) => L(P(S(k.Xor(Constants.C[i]))));
        
        public string ComputeHash(byte[] message)
        {
            byte[] res = GetHash(message.ToArray());
            return BitConverter.ToString(res.ToArray()).Replace("-", string.Empty);
        }
        
    }
}