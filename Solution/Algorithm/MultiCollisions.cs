using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Alea;
using Alea.CSharp;
using Alea.Parallel;
using MoraHash;
using ServiceStack;

namespace Algorithm
{
    public class MultiCollisions
    {
        private static RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();
        
        private static readonly GlobalArraySymbol<ulong> ConstC = Gpu.DefineConstantArraySymbol<ulong>(Constants.C.Length);
        private static readonly GlobalArraySymbol<int> ConstSbox = Gpu.DefineConstantArraySymbol<int>(Constants.SBox.Length);
        private static readonly GlobalArraySymbol<int> ConstTau = Gpu.DefineConstantArraySymbol<int>(Constants.Tau.Length);
        private static readonly GlobalArraySymbol<int> ConstL = Gpu.DefineConstantArraySymbol<int>(Constants.L.Length);

        private async Task<ulong> AsyncG_n(ulong N, ulong h, ulong m)
        {
            return await Task.Run(() => G_n(N, h, m));
        }
        
        private MagicInput GetMagic(MagicInput input, ulong n, ulong h, int size)
        {
            var x = input.X0;
            var x0 = input.X0;
            var counter = 0;
            //if first 32 - size bits are zero, x - magic num
            while (x >> (32 + size) != 0)
            {
                x = Function(n, h, x);
                counter++;
            }

            return new MagicInput() {X = x, X0 = x0, Counter = counter};
        }
        
        private MagicInput GetMagicGPU(MagicInput input, ulong n, ulong h, int size)
        {
            var x = input.X0;
            var x0 = input.X0;
            var counter = 0;
            //if first 32 - size bits are zero, x - magic num
            while (x >> (32 + size) != 0)
            {
                x = FunctionGPU(n, h, x);
                counter++;
            }

            return new MagicInput() {X = x, X0 = x0, Counter = counter};
        }

        private MagicInput[] MagicCycle(MagicInput[] input, int maxSize, ulong n, ulong h)
        {
            var t = (int)Math.Log(maxSize, 2);
            var result = new MagicInput[input.Length];

            Parallel.For(0, result.Length, (i) =>
            {
                result[i] = GetMagic(input[i], n, h, t);
            });
            return result;
        }

        [GpuManaged]
        private MagicInput[] MagicCycleGPU(MagicInput[] input, int maxSize, ulong n, ulong h)
        {
            var t = (int)Math.Log(maxSize, 2);
            var result = new MagicInput[input.Length];
             
            Gpu.Default.For(0, result.Length, i =>
            {
                result[i] = GetMagicGPU(input[i], n, h, t);
            });
            
            return result;
        }

        private (ulong, ulong) MagicPoints(ulong n, ulong h)
        {
            (ulong, ulong) retVal = (0, 0);
            var dict = new ConcurrentDictionary<ulong, (ulong, long)>();
            var maxSize = 1_050_000;
            var step = 10_000;

            while (true)
            {
                Console.WriteLine("Begin");
                var input = new ulong[maxSize].AsParallel().Select(data => BitConverter.ToUInt64(GetRandomByteArray(8).ToArray(), 0)).Select(data => new MagicInput() {X0 = data}).ToArray();
                for (int i = 0; i < maxSize; i+= step)
                {
                    var st = new Stopwatch();
                    st.Start();
                    var result = MagicCycleGPU(input.Skip(i).Take(step).ToArray(), maxSize, n, h);
                    st.Stop();
                    var time = st.ElapsedMilliseconds;
                    Console.WriteLine(time);
                    
                    Parallel.ForEach(result, (res, state) =>
                    {
                        var x = res.X;
                        var x0 = res.X0;
                        var count = res.Counter;

                        if (!dict.ContainsKey(x))
                        {
                            dict[x] = (x0, count);
                        }
                        else
                        {
                            var x1 = dict[x].Item1;
                            var count1 = dict[x].Item2;
                        
                            while (count1 > count)
                            {
                                x1 = Function(n, h, x1);
                                count1--;
                            }

                            while (count > count1)
                            {
                                x0 = Function(n, h, x0);
                                count--;
                            }

                            while (true)
                            {
                                var next = Function(n, h, x1);
                                var next2 = Function(n, h, x0);

                                if (next == next2) break;

                                x1 = next;
                                x0 = next2;
                            }

                            if (x1 != 0 && x0 != 0)
                            {
                                retVal.Item1 = x1;
                                retVal.Item2 = x0;
                                state.Break();
                            }
                        }
                    });

                    if (retVal != (0, 0)) break;
                }
                if (retVal != (0, 0)) break;
            }
            
            return retVal;
        }
        
        private (ulong, ulong) Brent(int[] l, int[] sBox, int[] tau, ulong[] c)
        {
            var power = 1;
            var lam = 1;
            var x0 = G_n(0, 0, BitConverter.ToUInt64(GetRandomByteArray(8).ToArray(), 0));
  
            var tortoise = x0;
            var hare = G_n(0, 0, x0);

            while (tortoise != hare)
            {
                if (power == lam)
                {
                    tortoise = hare;
                    power *= 2;
                    lam = 0;
                }
                hare = G_n(0, 0, hare);
                lam++;
            }
            
            tortoise = hare = x0;

            for (int i = 0; i < lam; i++)
            {
                hare = G_n(0, 0, hare);
            }

            while (true)
            {
                var nextTask = AsyncG_n(0, 0, tortoise);
                var next2Task = AsyncG_n(0, 0, hare);
                            
                Task.WaitAll(nextTask, next2Task);
                            
                if (nextTask.Result == next2Task.Result) break;
            
                tortoise = nextTask.Result;
                hare = next2Task.Result;
            }

            return (tortoise, hare);
        }
        
        private (ulong, ulong) Floyd(int[] l, int[] sBox, int[] tau, ulong[] c)
        {
            var x0 = G_n(0, 0, BitConverter.ToUInt64(GetRandomByteArray(8).ToArray(), 0));

            var hare = x0;
            var tortoise = x0;

            while (true)
            {
                hare = G_n(0, 0, G_n(0, 0, hare));
                tortoise = G_n(0, 0, tortoise);
                
                if (hare == tortoise)
                {
                    break;
                }
            }

            tortoise = x0;

            while (true)
            {
                var nextTask = G_n(0, 0, tortoise);
                var next2Task = G_n(0, 0, hare);

                if (nextTask == next2Task) break;
            }

            return (tortoise, hare);
        }

        private static int SplitLeftByte(ulong data, int byteNum)
        {
            int shift = (8 * byteNum);
            var b = (data >> shift) & 0xff;
            return (int)(b >> 4);
        }
        
        private static int SplitRightByte(ulong data, int byteNum)
        {
            int shift = (8 * byteNum);
            var b = (data >> shift) & 0xff;
            return (int)(b & 0xf);
        }
        
        private static ulong JoinBytes(int hi, int low)
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
        
        private static ulong E(ulong k, ulong m)
        {
            ulong state = k ^ m;
            for (int i = 0; i < Constants.C.Length - 1; i++)
            {
                k = KeySchedule(k, Constants.C[i]);
                state = L(P(S(state))) ^ k;
            }
            return state;
        }
        
        private static ulong KeySchedule(ulong k, ulong c)
        {
            return L(P(S(k ^ c)));
        }
        
        public static ulong G_n(ulong N, ulong h, ulong m)
        {
            return E(L(P(S(h ^ N))), m) ^ h ^ m;
        }

        private static ulong Function(ulong N, ulong h, ulong m)
        {
            var n1 = N + 64UL;
            return G_n(n1, G_n(N, h, m), ulong.MaxValue - m + 1);
        }

        #endregion
        
        #region GPU G_n
        private static ulong FunctionGPU(ulong N, ulong h, ulong m)
        {
            var n1 = N + 64UL;
            return G_nGPU(n1, G_nGPU(N, h, m), ulong.MaxValue - m + 1);
        }
        
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
        
        private static ulong EGPU(ulong k, ulong m)
        {
            ulong state = k ^ m;
            for (int i = 0; i < ConstC.Length - 1; i++)
            {
                k = KeyScheduleGPU(k, ConstC[i]);
                state = LGPU(PGPU(SGPU(state))) ^ k;
            }
            return state;
        }
        
        private static ulong KeyScheduleGPU(ulong k, ulong c)
        {
            return LGPU(PGPU(SGPU((k ^ c))));
        }
        

        #endregion

        public (byte[][] messages, ulong h, ulong n) FindCollisions(int t)
        {
            var gpu = Gpu.Default;
            
            gpu.Copy(Constants.C, ConstC);
            gpu.Copy(Constants.L, ConstL);
            gpu.Copy(Constants.SBox, ConstSbox);
            gpu.Copy(Constants.Tau, ConstTau);

            var lst = new List<List<ulong>>();
            var solutions = new List<ulong[]>();

            var h = 0UL;
            var n = 0UL;

            for (int i = 0; i < t; i++)
            {
                var r = MagicPoints(n, h);
                Console.WriteLine($"N: {n}; h: {h}");
                Console.WriteLine($"{r.Item1} : {r.Item2}");
                h = Function(n, h, r.Item1);
                n += 128UL;
                lst.Add(new List<ulong>() {r.Item1, r.Item2});
            }

            lst.Reverse();

            var solution = new ulong[lst.Count];
            Utils.Solve(lst, solutions, solution);
            
            var messages = solutions.Select(seq =>
            {
                IEnumerable<byte> ret = new byte[0];

                ret = seq.Aggregate(ret, (current, value) => 
                    current.Concat(BitConverter.GetBytes(ulong.MaxValue - value + 1).Reverse())
                        .Concat(BitConverter.GetBytes(value).Reverse()));

                return ret.ToArray();
            }).ToArray();

            return (messages, h, n);
        }

        private IEnumerable<byte> GetRandomByteArray(int arraySize)
        {
            byte[] buffer = new byte[arraySize];
            _rng.GetBytes(buffer);
            return buffer;
        }

    }
}