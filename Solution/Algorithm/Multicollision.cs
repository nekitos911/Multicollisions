using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Alea;
using Alea.Parallel;
using ServiceStack;

namespace Algorithm
{
    public class Multicollision
    {
        private static RNGCryptoServiceProvider _rng = new RNGCryptoServiceProvider();

        private async Task<ulong> AsyncG_n(ulong N, ulong h, ulong m, int[] l, int[] sBox, int[] tau, ulong[] c)
        {
            return await Task.Run(() => G_n(N, h, m, l, sBox, tau, c));
        }
        
        private MagicInput GetMagic(MagicInput input, ulong h, int[] l, int[] sBox, int[] tau, ulong[] c, int size)
        {
            var x = input.X0;
            var x0 = input.X0;
            var counter = 0;
            //if first 32 - size bits are zero, x - magic num
            while (x >> (32 + size) != 0)
            {
                x = G_n(0, h, x, l, sBox, tau, c);
                counter++;
            }

            return new MagicInput() {X = x, X0 = x0, Counter = counter};
        }

//        [GpuManaged]
        private MagicInput[] Cycle(MagicInput[] input, int maxSize, ulong h, int[] l, int[] sBox, int[] tau, ulong[] c)
        {
            var t = (int)Math.Log(maxSize, 2);
            var result = new MagicInput[input.Length];

            Parallel.For(0, result.Length, (i) =>
            {
                result[i] = GetMagic(input[i], h, l, sBox, tau, c, t);
//                    Console.WriteLine($"{result[i].X} : {result[i].X0} : {result[i].Counter}");
            });
            return result;
        }
        
        
        [GpuManaged]
         private MagicInput[] Cycle3(MagicInput[] input, int maxSize, ulong h, int[] l, int[] sBox, int[] tau, ulong[] c)
        {
            var t = (int)Math.Log(maxSize, 2);
            var result = new MagicInput[input.Length];
            
            Gpu.Default.For(0, result.Length, i =>
            {
                result[i] = GetMagic(input[i], h, l, sBox, tau, c, t); 
            });
            
            return result;
        }
         
        
//        [GpuManaged]
        private (ulong, ulong) MagicPoints(ulong h, int[] l, int[] sBox, int[] tau, ulong[] c)
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
                    var result = Cycle3(input.Skip(i).Take(step).ToArray(), maxSize, h, l, sBox, tau, c);
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
                                x1 = G_n(0, h, x1, l, sBox, tau, c);
                                count1--;
                            }

                            while (count > count1)
                            {
                                x0 = G_n(0, h, x0, l, sBox, tau, c);
                                count--;
                            }

                            while (true)
                            {
                                var next = G_n(0, h, x1, l, sBox, tau, c);
                                var next2 = G_n(0, h, x0, l, sBox, tau, c);

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
            var x0 = G_n(0, 0, BitConverter.ToUInt64(GetRandomByteArray(8).ToArray(), 0), l, sBox, tau, c);
  
            var tortoise = x0;
            var hare = G_n(0, 0, x0, l, sBox, tau, c);

            while (tortoise != hare)
            {
                if (power == lam)
                {
                    tortoise = hare;
                    power *= 2;
                    lam = 0;
                }
                hare = G_n(0, 0, hare, l, sBox, tau, c);
                lam++;
            }
            
            tortoise = hare = x0;

            for (int i = 0; i < lam; i++)
            {
                hare = G_n(0, 0, hare, l, sBox, tau, c);
            }

            while (true)
            {
                var nextTask = AsyncG_n(0, 0, tortoise, l, sBox, tau, c);
                var next2Task = AsyncG_n(0, 0, hare, l, sBox, tau, c);
                            
                Task.WaitAll(nextTask, next2Task);
                            
                if (nextTask.Result == next2Task.Result) break;
            
                tortoise = nextTask.Result;
                hare = next2Task.Result;
            }

            return (tortoise, hare);
        }
        
        private (ulong, ulong) Floyd(int[] l, int[] sBox, int[] tau, ulong[] c)
        {
            var x0 = G_n(0, 0, BitConverter.ToUInt64(GetRandomByteArray(8).ToArray(), 0), l, sBox, tau, c);

            var hare = x0;
            var tortoise = x0;

            while (true)
            {
                hare = G_n(0, 0, G_n(0, 0, hare, l, sBox, tau, c), l, sBox, tau, c);
                tortoise = G_n(0, 0, tortoise, l, sBox, tau, c);
                
                if (hare == tortoise)
                {
                    break;
                }
            }

            tortoise = x0;

            while (true)
            {
                var nextTask = G_n(0, 0, tortoise, l, sBox, tau, c);
                var next2Task = G_n(0, 0, hare, l, sBox, tau, c);

                if (nextTask == next2Task) break;
            }

            return (tortoise, hare);
        } 

        private (ulong, ulong) Algo(int N, int[] l, int[] sBox, int[] tau, ulong[] c)
        {
            var size = 64 / N;
            var Y = new ulong[size];
            var u = new ulong[size];
            var v = new ulong[size];
            Y[0] = G_n(0, 0, BitConverter.ToUInt64(GetRandomByteArray(8).ToArray(), 0), l, sBox, tau, c);
            var m = 0UL;
            var n = 0UL;
            var w = Y[0];

            for (int i = 1; i <= size; i++)
            {
                for (int j = (i - 1) * N + 1; j <= i * N - 1; j++)
                {
                    Y[j] = G_n(0, 0, Y[j - 1], l, sBox, tau, c);
                    if (Y[j] < w)
                    {
                        w = Y[j];
                        n = (ulong)j;
                    } else if (Y[j] == w)
                    {
                        m = (ulong)j;

                        return (m, n);
                    }
                }

                for (int k = 1; k <= i - 1; k++)
                {
                    if (u[k] == w)
                    {
                        m = v[k];
                        return (m, n);
                    }
                }

                u[i] = w;
                v[i] = n;
                w = G_n(0, 0, Y[i * N - 1], l, sBox, tau, c);
                n = (ulong)(i * N);
            }

            return (0, 0);
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

        public static ulong P(ulong state, int[] tau)
        {
            ulong ret = 0;
            for (int i = 0; i < 8; i++)
            {
                int shift = (64 - 8 - 8 * i);
                var l = tau[i * 2] % 2 == 0 ? SplitLeftByte(state, 7 - tau[i * 2] / 2) : SplitRightByte(state, 7 - tau[i * 2] / 2);
                var r = tau[i * 2 + 1] % 2 == 0 ? SplitLeftByte(state, 7 - tau[i * 2 + 1] / 2) : SplitRightByte(state, 7 - tau[i * 2 + 1] / 2);
                ret |= JoinBytes(l, r) << shift;
            }

            return ret;
        }
        
        public static ulong S(ulong state, int[] sBox)
        {
            ulong ret = 0;
            for (int i = 0; i < 8; i++)
            {
                int shift = (8 * i);
                ret |= JoinBytes(sBox[SplitLeftByte(state, i)], sBox[SplitRightByte(state, i)]) << shift;
            }

            return ret;
        }
        
        public static ulong L(ulong state, int[] lArr)
        {
            ulong result = 0;

            for (int i = 0; i < 4; i++)
            {
                int t = 0;
                for(int k = 0; k < 16; k++) {
                    if ((state & (1UL << (k + i * 16))) != 0)
                    {
                        t ^= lArr[16 - k - 1];
                    }
                }

                var data = (ulong) t;
                result |= data << (i * 16);
            }

            return result;
        }
        
        private static ulong E(ulong k, ulong m, ulong[] c, int[] l, int[] sBox, int[] tau)
        {
            ulong state = k ^ m;
            for (int i = 0; i < c.Length - 1; i++)
            {
                k = KeySchedule(k, c[i], l, sBox, tau);
                state = L(P(S(state, sBox), tau), l) ^ k;
            }
            return state;
        }
        
        private static ulong KeySchedule(ulong k, ulong c, int[] l, int[] sBox, int[] tau)
        {
            return L(P(S((k ^ c), sBox), tau), l);
        }
        
        public static ulong G_n(ulong N, ulong h, ulong m, int[] l, int[] sBox, int[] tau, ulong[] c)
        {
            return E(L(P(S(h ^ N, sBox), tau), l), m, c, l, sBox, tau) ^ h ^ m;
        }
        
        public (List<ulong[]>, ulong) FindCollisions(int t)
        {
            ulong[] C =
            {
                0xc0164633575a9699,
                0x925b4ef49a5e7174,
                0x86a89cdcf673be26,
                0x1885558f0eaca3f1,
                0xdcfc5b89e35e8439,
                0x54b9edc789464d23,
                0xf80d49afde044bf9,
                0x8cbbdf71ccaa43f1,
                0xcb43af722cb520b9
            };
            int[] Tau =
            {
                0, 4, 8, 12, 1, 5, 9, 13, 2, 6, 10, 14, 3, 7, 11, 15
            };
            int[] LArr =
            {
                0x3a22, 0x8511, 0x4b99, 0x2cdd,
                0x483b, 0x248c, 0x1246, 0x9123,
                0x59e5, 0xbd7b, 0xcfac, 0x6e56,
                0xac52, 0x56b1, 0xb3c9, 0xc86d
            };
            int[] SBox =
            {
                15, 9, 1, 7, 13, 12, 2, 8, 6, 5, 14, 3, 0, 11, 4, 10
            };


//            var inp = BitConverter.GetBytes(8675695359248302480).Reverse().ToArray();
//            var r2q = BitConverter.GetBytes(G_n(0, 0, 8675695359248302480, LArr, SBox, Tau, C)).Reverse().ToArray();
//            //BitConverter.GetBytes(G_n(0, 0, 8675695359248302480, LArr, SBox, Tau, C));
//            var r1q = mora.G_n(new byte[8], new byte[8], inp);//mora.G_n(new byte[8], new byte[8], BitConverter.GetBytes(8675695359248302480).Reverse());//new HashFunction().G_n(new byte[8], new byte[8], BitConverter.GetBytes(8675695359248302480));

//            var size = (int) Math.Log(1_060_000, 2);

//            var m1 = GetMagic(new MagicInput() {X0 = 8675695359248302480}, 0, LArr, SBox, Tau, C, size);
//            var m2 = GetMagic(new MagicInput() {X0 = 15975905551334951251}, 0, LArr, SBox, Tau, C, size);

//            ulong g1 = 11580036220586509192;

//            for (int i = 0; i < 7862; i++)
//            {
//                g1 = G_n(0, 0, g1, LArr, SBox, Tau, C);
//            }
            
//            ulong g2 = 16100290859875211879;

//            for (int i = 0; i <= 9202; i++)
//            {
//                g2 = G_n(0, 0, g2, LArr, SBox, Tau, C);
//            }


//            var t3 = MapUlongToLong(C[0]);

//            var t2 = BitConverter.GetBytes(0xa36818bac98812b9 ^ 0x1010101010101036);
            
//            var input = new byte[]{0xa3, 0x68, 0x18, 0xba, 0xc9, 0x88, 0x12, 0xb9};

//            var r1 = mora.G_n(new byte[8], new byte[8], input);
//            var r2 = BitConverter.GetBytes(G_n(0, 0, BitConverter.ToUInt64(input, 0), LArr, SBox, Tau, C));

//            var rs = mora.L(input);
            
            
//            var g1 = G_n(0, 12601850608120225315, 9869738958639348918, LArr, SBox, Tau, C);
//            var g1B = BitConverter.GetBytes(g1).ToArray();
//            var g2 = mora.G_n(new byte[8], BitConverter.GetBytes(12601850608120225315), BitConverter.GetBytes(9869738958639348918).ToArray());

//            var singleCollision = MagicPoints(12601850608120225315, LArr, SBox, Tau, C);

//            
//            // f (B1, F(0, B0)
//            var test2 = G_n(0, G_n(0, 0, 8002912720257400307, LArr, SBox, Tau, C), 1218609768163530423, LArr, SBox, Tau,
//                C);
//            
//            // f (B1, F(0, B0')
//            var test3 = G_n(0, G_n(0, 0, 11860533414150574761, LArr, SBox, Tau, C), 1218609768163530423, LArr, SBox, Tau,
//                C);
//            
//            // f (B1', F(0, B0)
//            var test4 = G_n(0, G_n(0, 0, 8002912720257400307, LArr, SBox, Tau, C), 7217993951919736131, LArr, SBox, Tau,
//                C);
//            
//            // f (B1', F(0, B0')
//            var test5 = G_n(0, G_n(0, 0, 11860533414150574761, LArr, SBox, Tau, C), 7217993951919736131, LArr, SBox, Tau,
//                C);

//            ulong x1 = 13128198456312556595;
//            var count1 = 4281;
//            ulong x0 = 9341893395587175866;
//            var count = 2165;
//            
//            
//            while (count1 > count)
//            {
//                x1 = G_n(0, 0, x1, LArr, SBox, Tau, C);
//                count1--;
//            }
//
//            while (count > count1)
//            {
//                x0 = G_n(0, 0, x0, LArr, SBox, Tau, C);
//                count--;
//            }
//
//            var iterator = 0;
//            while (true)
//            {
//                var next = G_n(0, 0, x1, LArr, SBox, Tau, C);
//                var next2 = G_n(0, 0, x0, LArr, SBox, Tau, C);
//                iterator++;
//
//                if (next == next2) break;
//
//                x1 = next;
//                x0 = next2;
//            }
//
//            var test = BitConverter.GetBytes(G_n(0, 0, 1974221557608115638, LArr, SBox, Tau, C)).Reverse().ToArray();
//            var test1 = mora.G_n(new byte[8], new byte[8], BitConverter.GetBytes(1974221557608115638).Reverse());
//            var test2 = BitConverter.GetBytes(G_n(0, 0, 8419055756211858106, LArr, SBox, Tau, C)).Reverse().ToArray();
//            var test21 = mora.G_n(new byte[8], new byte[8], BitConverter.GetBytes(8419055756211858106).Reverse());
//            var test3 = BitConverter.GetBytes(G_n(0, 0, BitConverter.ToUInt64(new byte[] {0x01, 0xd4, 0x44, 0x90, 0x7e, 0xfb, 0x8c, 0xf7}.Reverse().ToArray(), 0), LArr, SBox, Tau, C)).Reverse().ToArray();
//            var test31 = mora.G_n(new byte[8], new byte[8], new byte[] {0x01, 0xd4, 0x44, 0x90, 0x7e, 0xfb, 0x8c, 0xf7});

//            844909074791976
//            10270856436754831767 : 6920
//            2087758179587162062 : 2392
//            1974221557608115638 : 8419055756211858106
//            1974221557608115638 : 8419055756211858106

//h0
//            1366367376498725
//            12214781975665660759 : 4492
//            1352692565877422794 : 5209
//            15099911268225057261 : 12827465560520173572
//            15099911268225057261 : 12827465560520173572

//h1
//            3039673566387514
//            6490368572603814171 : 2282
//            15057135322267701395 : 7363
//            13787692588855149020 : 3806373757163530076
//            13787692588855149020 : 3806373757163530076

            var lst = new List<List<ulong>>();
            List<ulong[]> solutions = new List<ulong[]>();

            var h = 0UL;
            for (int i = 0; i < t; i++)
            {
                var r = MagicPoints(h, LArr, SBox, Tau, C);
                Console.WriteLine($"{r.Item1} : {r.Item2}");
                h = G_n(0, h, r.Item1, LArr, SBox, Tau, C);
                lst.Add(new List<ulong>() {r.Item1, r.Item2});
            }

            ulong[] solution = new ulong[lst.Count];
            Solve(lst, solutions, solution);

            return (solutions, h);
        }
        
        private static void Solve(List<List<ulong>> list, List<ulong[]> solutions, ulong[] solution)
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

        private IEnumerable<byte> GetRandomByteArray(int arraySize)
        {
            byte[] buffer = new byte[arraySize];
            _rng.GetBytes(buffer);
            return buffer;
        }

    }
}