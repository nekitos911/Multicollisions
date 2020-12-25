using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Alea;
using Alea.CSharp;
using Alea.cuRAND;
using Alea.Parallel;
using Microsoft.VisualBasic;
using MoraHash;
using MoreLinq;
using MoreLinq.Extensions;
using ServiceStack;
using ServiceStack.Logging;

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
            var x0 = input.X0;
            var x = G_n(0, h, x0, l, sBox, tau, c);
            long counter = 0;

            //if first 16 bits are zero, x - magic num
            while (x >> (32 + size) != 0)
            {
                x = G_n(0, h, x, l, sBox, tau, c);
                counter++;
            }
            return new MagicInput() {X = x, X0 = x0, Counter = counter};
        }

        [GpuManaged]
        private MagicInput[] Cycle(ulong h, int[] l, int[] sBox, int[] tau, ulong[] c)
        {
            var size = 100_000;
            var t = (int)Math.Log(size, 2);
            var input = new ulong[size].AsParallel().Select(data => BitConverter.ToUInt64(GetRandomByteArray2(8).ToArray(), 0)).Select(data => new MagicInput() {X0 = data}).ToArray();
            var result = new MagicInput[size];

//            Gpu.Default.For(0, size, i =>
//            {
//                result[i] = GetMagic(input[i], l, sBox, tau, c);
//            });
//            new Thread(() =>
//            {
                            Gpu.Default.For(0, size, i =>
            {
                result[i] = GetMagic(input[i], h, l, sBox, tau, c, t);
            });
//                Parallel.For(0, size, (i) => { result[i] = GetMagic(input[i], l, sBox, tau, c, t); });
//            }).Start();
            

            return result;
        }
        
//        [GpuManaged]
        private (ulong, ulong) MagicPoints(ulong h, int[] l, int[] sBox, int[] tau, ulong[] c)
        {
            Console.WriteLine("Begin");
            (ulong, ulong) retVal = (0, 0);
            var dict = new ConcurrentDictionary<ulong, (ulong, long)>();

            while (true)
            {
                var result = Cycle(h, l, sBox, tau, c);
//                var usedIndexes = new bool[result.Length];
//                while (!usedIndexes.All(i => i))
//                {
//                    var tmpResult = result.Where((res, i) =>
//                    {
//                        if (res.X == 0 && res.X0 == 0 || usedIndexes[i]) return false;
//
//                        usedIndexes[i] = true;
//                        return true;
//                    }).ToArray();
//
//                    if (!tmpResult.Any())
//                    {
//                        Thread.Sleep(5 * 60 * 1000);
//                        continue;
//                    }
//                    
//                    Console.WriteLine($"{usedIndexes.Count(i => i)}");

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

                            retVal.Item1 = x1;
                            retVal.Item2 = x0;
                            state.Break();
                        }
                    });
                
                if (retVal != (0, 0)) break;
            }

//            Console.WriteLine($"{retVal.Item1} : {retVal.Item2}");
            return retVal;
        }
        
        private (ulong, ulong) Brent(int[] l, int[] sBox, int[] tau, ulong[] c)
        {
            var power = 1;
            var lam = 1;
            var x0 = G_n(0, 0, BitConverter.ToUInt64(GetRandomByteArray2(8).ToArray(), 0), l, sBox, tau, c);
  
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
            var x0 = G_n(0, 0, BitConverter.ToUInt64(GetRandomByteArray2(8).ToArray(), 0), l, sBox, tau, c);

            var hare = x0;
            var tortoise = x0;

            while (true)
            {
                hare = G_n(0, 0, G_n(0, 0, hare, l, sBox, tau, c), l, sBox, tau, c);
                tortoise = G_n(0, 0, tortoise, l, sBox, tau, c);

//                Task.WaitAll(hareTask, tortoiseTask);
//                hare = hareTask.Result;
//                tortoise = tortoiseTask.Result;
                
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
                
//                Task.WaitAll(nextTask, next2Task);
                
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
            Y[0] = G_n(0, 0, BitConverter.ToUInt64(GetRandomByteArray2(8).ToArray(), 0), l, sBox, tau, c);
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
        
        private ulong GCD(ulong a, ulong b)
        {
            while (a != 0 && b != 0)
            {
                if (a > b)
                    a %= b;
                else
                    b %= a;
            }

            return a | b;
        }

        private int SplitLeftByte(ulong data, int byteNum)
        {
            int shift = (8 * byteNum);
            var b = (data >> shift) & 0xff;
            return (int)(b >> 4);
        }
        
        private int SplitRightByte(ulong data, int byteNum)
        {
            int shift = (8 * byteNum);
            var b = (data >> shift) & 0xff;
            return (int)(b & 0xf);
        }
        
        private ulong JoinBytes(int hi, int low)
        {
            return (ulong)((hi << 4) | (low & 0xffffffffL));
        }

        public ulong P(ulong state, int[] tau)
        {
            ulong ret = 0;
            for (int i = 0; i < 8; i++)
            {
                int shift = (8 * i);
                var l = tau[i * 2] % 2 == 0 ? SplitLeftByte(state, tau[i * 2] / 2) : SplitRightByte(state, tau[i * 2] / 2);
                var r = tau[i * 2 + 1] % 2 == 0 ? SplitLeftByte(state, tau[i * 2 + 1] / 2) : SplitRightByte(state, tau[i * 2 + 1] / 2);
                ret |= JoinBytes(l, r) << shift;
            }

            return ret;
        }
        
        private ulong S(ulong state, int[] sBox)
        {
            ulong ret = 0;
            for (int i = 0; i < 8; i++)
            {
                int shift = (8 * i);
                ret |= JoinBytes(sBox[SplitLeftByte(state, i)], sBox[SplitRightByte(state, i)]) << shift;
            }

            return ret;
        }
        
        public ulong L(ulong state, int[] l)
        {
            ulong result = 0;

            for (int i = 0; i < 4; i++)
            {
                int t = 0;
                var tmp = (int) ((state >> (i * 2 + 1 << 3)) & 0xFF) << (8 * i + 1 << 3) |
                          (int) ((state >> (i * 2 << 3)) & 0xFF);
                
                for(int k = 0; k < 16; k++){
                    int mask =  1 << k;
                    int masked_n = tmp & mask;
                    int thebit = masked_n >> k;

                    if (k < 8)
                    {
                        if (thebit == 1)
                            t ^= l[8 - k - 1];
                    }
                    else
                    {
                        if (thebit == 1)
                            t ^= l[24 - k - 1];
                    }
                }

                var data = (ulong) t;
                result |= (data >> 8 | ((data & 0xFF) << 8)) << i * 16;
            }

            return result;
        }
        
        private ulong E(ulong k, ulong m, ulong[] c, int[] l, int[] sBox, int[] tau)
        {
            ulong state = k ^ m;
            for (int i = 0; i < c.Length; i++)
            {
                state = L(P(S(state, sBox), tau), l) ^ KeySchedule(k, c[i], l, sBox, tau);
            }
            return state;
        }
        
        private ulong KeySchedule(ulong k, ulong c, int[] l, int[] sBox, int[] tau)
        {
            return L(P(S((k ^ c), sBox), tau), l);
        }
        
        public ulong G_n(ulong N, ulong h, ulong m, int[] l, int[] sBox, int[] tau, ulong[] c)
        {
            return E(L(P(S(h ^ N, sBox), tau), l), m, c, l, sBox, tau) ^ h ^ m;
        }
        


//        [GpuManaged]
        public void FindCollisions()
        {
            var mora = new HashFunction();
            
            ulong[] C =
            {
                0xc0164633575a9699,
                0x925b4ef49a5e7174,
                0x86a89cdcf673be26,
                0x86a89cdcf673be26,
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
                0x3a22, 0x483b, 0x59e5, 0xac52,
                0x8511, 0x248c, 0xbd7b, 0x56b1,
                0x4b99, 0x1246, 0xcfac, 0xb3c9,
                0x2cdd, 0x9123, 0x6e56 ,0xc86d
            };
            int[] SBox =
            {
                15, 9, 1, 7, 13, 12, 2, 8, 6, 5, 14, 3, 0, 11, 4, 10
            };

            var singleCollision = MagicPoints(12601850608120225315, LArr, SBox, Tau, C);

            int a = 0;


        }

        private IEnumerable<byte[]> GetRandomByteArray(int arraySize)
        {
            for (int i = 0; i < 10_000; i++)
            {
                byte[] buffer = new byte[arraySize];
                _rng.GetBytes(buffer);
                yield return buffer;
            }
           
        }
        
        private IEnumerable<byte> GetRandomByteArray2(int arraySize)
        {
            byte[] buffer = new byte[arraySize];
            _rng.GetBytes(buffer);
            return buffer;
        }
        
        private IEnumerable<byte> GetRandomByteArray3(int arraySize)
        {
            deviceptr<byte> buf = new deviceptr<byte>();
            byte[] buffer = new byte[arraySize];
            _rng.GetBytes(buffer);
            return buffer;
        }
    }
}