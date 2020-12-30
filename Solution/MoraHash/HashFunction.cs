using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MoreLinq;

namespace MoraHash
{
    public class HashFunction
    {
        private static readonly int BlockSize = 8;
        
        private byte[] _n = new byte[BlockSize];
        private byte[] _sigma = new byte[BlockSize];
        private byte[] _iv = new byte[BlockSize];

        public byte[] P(byte[] state)
        {
            var tmp = new byte[16];
            var ret = new byte[8];
            for (int i = 0; i < state.Length; i++)
            {
                var splitted = ByteUtils.SplitByte(state[i]);
                tmp[i * 2] = splitted.hi;
                tmp[i * 2 + 1] = splitted.low;
            }

            var res = Enumerable.Range(0, tmp.Length).Select(i => tmp[Constants.Tau[i]]).ToArray();
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = ByteUtils.JoinBytes(res[i * 2], res[i * 2 + 1]);
            }

            return ret;
        }

        public byte[] S(byte[] state)
        {
            var tmp = new byte[16];
            var ret = new byte[8];
            
            for (int i = 0; i < state.Length; i++)
            {
                var splitted = ByteUtils.SplitByte(state[i]);
                tmp[i * 2] = splitted.hi;
                tmp[i * 2 + 1] = splitted.low;
            }
            
            var res = Enumerable.Range(0, tmp.Length).Select(i => (byte)Constants.SBox[tmp[i]]).ToArray();
            
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = ByteUtils.JoinBytes(res[i * 2], res[i * 2 + 1]);
            }

            return ret;
        }
        
        public byte[] L(byte[] state)
        {
            byte[] result = new byte[BlockSize];
//            Parallel.For(0, 4, i =>
            for (int i = 0; i < 4; i++)
            {
                int t = 0;
                byte[] tempArray = new byte[2];
                Array.Copy(state, i * 2, tempArray, 0, 2);
                tempArray = tempArray.Reverse().ToArray();
                var tempBits1 = tempArray.ToBitArray(16);
                bool[] tempBits = new bool[BlockSize * 2];
                tempBits1.CopyTo(tempBits, 0);
                tempBits = tempBits.Reverse().ToArray();

                for (int j = 0; j < tempBits.Length; j++)
                {
                    if (tempBits[j])
                    {
                        t ^= Constants.L[j];
                    }
                }
                Array.Copy(BitConverter.GetBytes(t).Reverse().ToArray(), 2, result, i * 2, 2);
            }

            return result;
        }

        private byte[] GetHash(byte[] message)
        {
            var h = new byte[BlockSize];
            _n = new byte[BlockSize];
            _sigma = new byte[BlockSize];
            Array.Copy(_iv, h, BlockSize);

            byte[] n0 = new byte[BlockSize];

            IEnumerable<IEnumerable<byte>> blocks = message.Batch(BlockSize).ToArray();

            blocks.Where(block => block.Count() >= BlockSize).Reverse().ForEach(block =>
            {
                h = G_n(_n, h, block);
                _n = _n.AddModulo64(BitConverter.GetBytes((long)64).Reverse().ToArray());
                _sigma = _sigma.AddModulo64(block.ToArray());
            });

            var lastBlockSize = blocks.Last().Count();

            byte[] pad = MoreEnumerable
                .Append(new byte[lastBlockSize < BlockSize ? BlockSize - 1 - lastBlockSize : BlockSize - 1], (byte) 1).ToArray();

            byte[] m = pad
                .Concat(blocks.Where(block => block.Count() < BlockSize).DefaultIfEmpty(new byte[0]).First()).ToArray();
            
            h = G_n(_n, h, m);
           
            var msgLen = BitConverter.GetBytes((long)(message.Length * 8)).Reverse();

            _n = _n.AddModulo64(msgLen.ToArray());

            _sigma = _sigma.AddModulo64(m);

            h = G_n(n0, h, _n);
            h = G_n(n0, h, _sigma);

            return h;
        }
        
        public byte[] G_n(IEnumerable<byte> N, IEnumerable<byte> h, IEnumerable<byte> m)
        {
            return E(L(P(S(h.Xor(N)))), m.ToArray()).Xor(h).Xor(m);
        }

        public byte[] E(byte[] k, byte[] m)
        {
            byte[] state = k.Xor(m);
            for (int i = 0; i < Constants.C.Length - 1; i++)
            {
                k = KeySchedule(k, i);
                state = L(P(S(state))).Xor(k);
            }
            return state;
        }

        private byte[] KeySchedule(byte[] k, int i) => L(P(S(k.Xor(BitConverter.GetBytes(Constants.C[i]).Reverse()))));
        
        public byte[] ComputeHash(byte[] message)
        {
            return GetHash(message.ToArray());
        }

        public static string StringRepresentation(byte[] input)
        {
            return BitConverter.ToString(input.ToArray()).Replace("-", string.Empty);
        }
        
    }
}