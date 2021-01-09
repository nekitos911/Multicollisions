﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Alea;
using Alea.Parallel;
using MoraHash;
using MoreLinq;
using MoreLinq.Extensions;
using static Algorithm.Utils;

namespace Algorithm
{
    public class MeaningfulCollisions
    {
        [GpuManaged]
        private MagicInput[] MagicCycleGPU(MagicInput[] input, int maxSize, ulong n, ulong h, ulong h1)
        {
            var t = (int)Math.Log(maxSize, 2);
            var result = new MagicInput[input.Length];
             
            Gpu.Default.For(0, result.Length, i =>
            {
                result[i] = GetMagicGPU(input[i], n, h, h1, t);
            });
            
            return result;
        }
        
        private MagicInput[] MagicCycle(MagicInput[] input, int maxSize, ulong n, ulong h, ulong h1)
        {
            var t = (int)Math.Log(maxSize, 2);
            var result = new MagicInput[input.Length];
             
            Parallel.For(0, result.Length, i =>
            {
                result[i] = GetMagic(input[i], n, h, h1, t);
            });
            
            return result;
        }

        private async Task<MagicInput[]> AsyncMagicCycleGPU(MagicInput[] input, int maxSize, ulong n, ulong h, ulong h1)
        {
            return await Task.Run(() => MagicCycleGPU(input, maxSize, n, h, h1));
        }

        private async Task<MagicInput[]> AsyncMagicCycle(MagicInput[] input, int maxSize, ulong n, ulong h, ulong h1)
        {
            return await Task.Run(() => MagicCycle(input, maxSize, n, h, h1));
        }
        
        private MagicInput GetMagicGPU(MagicInput input, ulong n, ulong h, ulong h1, int size)
        {
            var x0 = input.X0;
            var x = G_nGPU(n, h, x0);
            var x1 = G_nGPU(n, h1, x0);
            var counter = 0;
            var counter1 = 0;
            //if first 32 - size bits are zero, x - magic num
            while (x >> (32 + size) != 0)
            {
                x = G_nGPU(n, h, x);
                counter++;
            }
            
            while (x1 >> (32 + size) != 0)
            {
                x1 = G_nGPU(n, h1, x1);
                counter1++;
            }

            return new MagicInput() {X = x, X1 = x1, X0 = x0, Counter = counter, Counter1 = counter1};
        }
        
        private MagicInput GetMagic(MagicInput input, ulong n, ulong h, ulong h1, int size)
        {
            var x0 = input.X0;
            var x = G_n(n, h, x0);
            var x1 = G_n(n, h1, x0);
            var counter = 0;
            var counter1 = 0;
            //if first 32 - size bits are zero, x - magic num
            while (x >> (32 + size) != 0)
            {
                x = G_n(n, h, x);
                counter++;
            }
            
            while (x1 >> (32 + size) != 0)
            {
                x1 = G_n(n, h1, x1);
                counter1++;
            }

            return new MagicInput() {X = x, X1 = x1, X0 = x0, Counter = counter, Counter1 = counter1};
        }
        
        private (ulong, ulong) MagicPoints(ulong n, ulong h1, ulong h2, int stepCount = 50_000)
        {
            (ulong, ulong) retVal = (0, 0);
            var dict1 = new ConcurrentDictionary<ulong, (ulong, long)>();
            var dict2 = new ConcurrentDictionary<ulong, (ulong, long)>();
            
            var maxSize = 1_050_000;
            var step = 100;

            while (true)
            {
                Console.WriteLine("Begin");
                var input = new ulong[maxSize].AsParallel().Select(data => BitConverter.ToUInt64(GetRandomByteArray(8).ToArray(), 0)).Select(data => new MagicInput() {X0 = data}).ToArray();
                for (int i = 0; i < maxSize; i+= step)
                {
                    var st = new Stopwatch();
                    st.Start();
//                    var result = MagicCycle(input.Skip(i).Take(step).ToArray(), maxSize, n, h1, h2);

                    var result = MagicCycle(input.Skip(i).Take(step).ToArray(), maxSize, n, h1, h2);
//                    result = AppendExtension.Append(result, new MagicInput()  {X = result[0].X1}).ToArray();
//                    var result = MagicCycle(new MagicInput[] {new MagicInput() {X0 = 4130961177024394}}, maxSize, n, h1, h2);
                    st.Stop();
                    var time = st.ElapsedMilliseconds;
                    Console.Write($"hash: {time} ");

                    st.Start();

                    Parallel.ForEach(result, (res, state) =>
                    {
                        var x = res.X;
                        var x1 = res.X1;
                        var x0 = res.X0;
                        var count = res.Counter;
                        var count1 = res.Counter1;

                        dict1[x] = (x0, count);
                        dict2[x1] = (x0, count1);
                    });
                    
                    var same = from k1 in dict1.Keys
                        join k2 in dict2.Keys
                            on k1
                            equals k2
                        select new
                        {
                            k = k1
                        };
                    
                    if (same.Any())
                    {
                        var x = same.First().k;
                        var x0 = dict1[x].Item1;
                        var x1 = dict2[x].Item1;
                        var count = dict1[x].Item2;
                        var count1 = dict2[x].Item2;

                        Console.WriteLine($"x0: {x0}, x: {x}, x1: {x1} count: {count}, count1: {count1}");

                        while (count1 > count)
                        {
                            x1 = G_n(n, h2, x1);
                            count1--;
                        }

                        while (count > count1)
                        {
                            x0 = G_n(n, h1, x0);
                            count--;
                        }

                        while (true)
                        {
                            var next = G_n(n, h2, x1);
                            var next2 = G_n(n, h1, x0);

                            if (next == next2) break;

                            x1 = next;
                            x0 = next2;
                        }

                        if (x1 != 0 && x0 != 0)
                        {
                            retVal.Item1 = x1;
                            retVal.Item2 = x0;
//                            state.Break();
                        }
                    }
                    
//                    Parallel.ForEach(result, (res, state) =>
//                    {
//                        var x = res.X;
//                        var x1 = res.X1;
//                        var x0 = res.X0;
//                        var count = res.Counter;
//                        var count1 = res.Counter1;
//
//                        dict1[x] = (x0, count);
//                        dict2[x1] = (x0, count1);
//
//                        var same = from k1 in dict1.Keys
//                            join k2 in dict2.Keys
//                                on k1
//                                equals k2
//                            select new
//                            {
//                                k = k1
//                            };
//
//
//                        if (same.Any())
//                        {
//                            x = same.First().k;
//                            x0 = dict1[x].Item1;
//                            x1 = dict2[x].Item1;
//                            count = dict1[x].Item2;
//                            count1 = dict2[x].Item2;
//
//                            Console.WriteLine($"x0: {x0}, x: {x}, x1: {x1} count: {count}, count1: {count1}");
//
//                            while (count1 > count)
//                            {
//                                x1 = G_n(n, h2, x1);
//                                count1--;
//                            }
//
//                            while (count > count1)
//                            {
//                                x0 = G_n(n, h1, x0);
//                                count--;
//                            }
//
//                            while (true)
//                            {
//                                var next = G_n(n, h2, x1);
//                                var next2 = G_n(n, h1, x0);
//
//                                if (next == next2) break;
//
//                                x1 = next;
//                                x0 = next2;
//                            }
//
//                            if (x1 != 0 && x0 != 0)
//                            {
//                                retVal.Item1 = x1;
//                                retVal.Item2 = x0;
//                                state.Break();
//                            }
//                        }
//                    });
                    st.Stop();
                    time = st.ElapsedMilliseconds;

                    Console.WriteLine($"all: {time} ");
                    if (retVal != (0, 0)) break;
                }
                if (retVal != (0, 0)) break;
            }

//
//                    Parallel.ForEach(result, (res) =>
//                    {
//                        var x = res.X;
//                        var x0 = res.X0;
//                        var count = res.Counter;
//                        dict1[x] = (x0, count);
//                    });
//                    
//                    Parallel.ForEach(result2.Result, (res) =>
//                    {
//                        var x = res.X;
//                        var x0 = res.X0;
//                        var count = res.Counter;
//                        dict2[x] = (x0, count);
//                    });
//
//                    var same = dict1.Where(k => dict2.ContainsKey(k.Key));
//
//                    if (same.Any())
//                    {
//                        var key = same.First().Key;
//                        var x0 = dict1[key].Item1;
//                        var count = dict1[key].Item2;
//                        
//                        var x1 = dict2[key].Item1;
//                        var count1 = dict2[key].Item2;
//                        
//                        while (count1 > count)
//                        {
//                            x1 = G_n(n, h2, x1);
//                            count1--;
//                        }
//
//                        while (count > count1)
//                        {
//                            x0 = G_n(n, h1, x0);
//                            count--;
//                        }
//
//                        while (true)
//                        {
//                            var next = G_n(n, h2, x1);
//                            var next2 = G_n(n, h1, x0);
//
//                            if (next == next2) break;
//
//                            x1 = next;
//                            x0 = next2;
//                        }
//
//                        if (x1 != 0 && x0 != 0)
//                        {
//                            retVal.Item1 = x1;
//                            retVal.Item2 = x0;
//                        }
//                    }
//
//                    if (retVal != (0, 0)) break;
//                }
//                if (retVal != (0, 0)) break;
//            }
//            
            return retVal;
        }
        
        public void MeaningfulCollision(byte[] p1, byte[] p2, int stepCount)
        {
            
            var gpu = Gpu.Default;
            
            gpu.Copy(Constants.C, ConstC);
            gpu.Copy(Constants.L, ConstL);
            gpu.Copy(Constants.SBox, ConstSbox);
            gpu.Copy(Constants.Tau, ConstTau);
            
            var h1 = 0UL;
            var h2 = 0UL;
            var n = 0UL;
            byte[] pad = p1.Length % 8 != 0 ? Enumerable.Repeat(1, (8 - p1.Length % 8) % 8).Select(x => (byte) x).ToArray() : new byte[0];
            
            IEnumerable<IEnumerable<byte>> blocks = MoreEnumerable.Batch(pad.Concat(p1), 8).ToArray();
            IEnumerable<IEnumerable<byte>> blocks2 = MoreEnumerable.Batch(pad.Concat(p2), 8).ToArray();

            MoreEnumerable.ForEach(blocks.Zip(blocks2, (k, v) => (k, v)).Where(block => block.v.Count() >= 8).Reverse(), block =>
            {
                h1 = G_n(n, h1, BitConverter.ToUInt64(block.k.ToArray(), 0));
                h2 = G_n(n, h2, BitConverter.ToUInt64(block.v.ToArray(), 0));
                n += 64UL;
            });


            for (int i = 0; i < 10; i++)
            {
                var r = MagicPoints(n, h1, h2, stepCount);
                Console.WriteLine($"N: {n}; h1: {h1}; h2: {h2}");
                Console.WriteLine($"{r.Item1} : {r.Item2}");
                h1 = G_n(n, h1, r.Item1);
                h2 = G_n(n, h2, r.Item1);
                n += 64UL;
            }
        }
    }
}