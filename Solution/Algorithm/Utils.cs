using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Alea;
using MoraHash;

namespace Algorithm
{
    public class Utils
    {
        public static readonly GlobalArraySymbol<ulong> ConstC = Gpu.DefineConstantArraySymbol<ulong>(Constants.C.Length);
        public static readonly GlobalArraySymbol<int> ConstSbox = Gpu.DefineConstantArraySymbol<int>(Constants.SBox.Length);
        public static readonly GlobalArraySymbol<int> ConstTau = Gpu.DefineConstantArraySymbol<int>(Constants.Tau.Length);
        public static readonly GlobalArraySymbol<int> ConstL = Gpu.DefineConstantArraySymbol<int>(Constants.L.Length);
        private static RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();
        
        /// <summary>
        /// Возвращает в solutions 2^t сообщений из t пар
        /// </summary>
        /// <param name="list"></param>
        /// <param name="solutions"></param>
        /// <param name="solution"></param>
        public static void Solve(List<List<ulong>> list, List<ulong[]> solutions, ulong[] solution)
        {
            if (solution.All(i => i != 0) && !solutions.Any(s => s.SequenceEqual(solution)))
                solutions.Add(solution);
            for (int i = 0; i < list.Count; i++)
            {
                if (solution[i] != 0)
                    continue; // a caller up the hierarchy set this index to be a number
                for (int j = 0; j < list[i].Count; j++)
                {
                    if (solution.Contains(list[i][j]))
                        continue;
                    var solutionCopy = solution.ToArray();
                    solutionCopy[i] = list[i][j];
                    Solve(list, solutions, solutionCopy);
                }
            }
        }
        
        public static int SplitLeftByte(ulong data, int byteNum)
        {
            int shift = (8 * byteNum);
            var b = (data >> shift) & 0xff;
            return (int)(b >> 4);
        }
        
        public static int SplitRightByte(ulong data, int byteNum)
        {
            int shift = (8 * byteNum);
            var b = (data >> shift) & 0xff;
            return (int)(b & 0xf);
        }
        
        public static ulong JoinBytes(int hi, int low)
        {
            return (ulong)((hi << 4) | (low & 0xffffffffL));
        }
        
         #region G_n

        public static ulong P(ulong state)
        {
            ulong ret = 0;
            for (int i = 0; i < 8; i++)
            {
                int shift = (64 - 8 - 8 * i);
                var l = Constants.Tau[i * 2] % 2 == 0 ? SplitLeftByte(state, 7 - Constants.Tau[i * 2] / 2) : SplitRightByte(state, 7 - Constants.Tau[i * 2] / 2);
                var r = Constants.Tau[i * 2 + 1] % 2 == 0 ? SplitLeftByte(state, 7 - Constants.Tau[i * 2 + 1] / 2) : SplitRightByte(state, 7 - Constants.Tau[i * 2 + 1] / 2);
                ret |= JoinBytes(l, r) << shift;
            }

            return ret;
        }
        
        public static ulong S(ulong state)
        {
            ulong ret = 0;
            for (int i = 0; i < 8; i++)
            {
                int shift = (8 * i);
                ret |= JoinBytes(Constants.SBox[SplitLeftByte(state, i)], Constants.SBox[SplitRightByte(state, i)]) << shift;
            }

            return ret;
        }
        
        public static ulong L(ulong state)
        {
            ulong result = 0;

            for (int i = 0; i < 4; i++)
            {
                int t = 0;
                for(int k = 0; k < 16; k++) {
                    if ((state & (1UL << (k + i * 16))) != 0)
                    {
                        t ^= Constants.L[16 - k - 1];
                    }
                }

                var data = (ulong) t;
                result |= data << (i * 16);
            }

            return result;
        }
        
        public static ulong E(ulong k, ulong m)
        {
            ulong state = k ^ m;
            for (int i = 0; i < Constants.C.Length - 1; i++)
            {
                k = KeySchedule(k, Constants.C[i]);
                state = L(P(S(state))) ^ k;
            }
            return state;
        }
        
        public static ulong KeySchedule(ulong k, ulong c)
        {
            return L(P(S(k ^ c)));
        }
        
        public static ulong G_n(ulong N, ulong h, ulong m)
        {
            return E(L(P(S(h ^ N))), m) ^ h ^ m;
        }

        public static ulong Function(ulong N, ulong h, ulong m)
        {
            var n1 = N + 64UL;
            return G_n(n1, G_n(N, h, m), ulong.MaxValue - m + 1);
        }

        #endregion
        
        #region GPU G_n
        public static ulong G_nGPU(ulong N, ulong h, ulong m)
        {
            return EGPU(LGPU(PGPU(SGPU(h ^ N))), m) ^ h ^ m;
        }
        
        public static ulong PGPU(ulong state)
        {
            ulong ret = 0;
            for (int i = 0; i < 8; i++)
            {
                int shift = (64 - 8 - 8 * i);
                var l = ConstTau[i * 2] % 2 == 0 ? SplitLeftByte(state, 7 - ConstTau[i * 2] / 2) : SplitRightByte(state, 7 - ConstTau[i * 2] / 2);
                var r = ConstTau[i * 2 + 1] % 2 == 0 ? SplitLeftByte(state, 7 - ConstTau[i * 2 + 1] / 2) : SplitRightByte(state, 7 - ConstTau[i * 2 + 1] / 2);
                ret |= JoinBytes(l, r) << shift;
            }

            return ret;
        }
        
        public static ulong SGPU(ulong state)
        {
            ulong ret = 0;
            for (int i = 0; i < 8; i++)
            {
                int shift = (8 * i);
                ret |= JoinBytes(ConstSbox[SplitLeftByte(state, i)], ConstSbox[SplitRightByte(state, i)]) << shift;
            }

            return ret;
        }
        
        public static ulong LGPU(ulong state)
        {
            ulong result = 0;

            for (int i = 0; i < 4; i++)
            {
                int t = 0;
                for(int k = 0; k < 16; k++) {
                    if ((state & (1UL << (k + i * 16))) != 0)
                    {
                        t ^= ConstL[16 - k - 1];
                    }
                }

                var data = (ulong) t;
                result |= data << (i * 16);
            }

            return result;
        }
        
        public static ulong EGPU(ulong k, ulong m)
        {
            ulong state = k ^ m;
            for (int i = 0; i < ConstC.Length - 1; i++)
            {
                k = KeyScheduleGPU(k, ConstC[i]);
                state = LGPU(PGPU(SGPU(state))) ^ k;
            }
            return state;
        }
        
        public static ulong KeyScheduleGPU(ulong k, ulong c)
        {
            return LGPU(PGPU(SGPU((k ^ c))));
        }
        

        #endregion
        
        public static IEnumerable<byte> GetRandomByteArray(int arraySize)
        {
            byte[] buffer = new byte[arraySize];
            _rng.GetBytes(buffer);
            return buffer;
        }
    }
}