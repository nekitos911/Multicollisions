using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Alea;
using Alea.Parallel;
using CommandLine;
using CommandLine.Text;
using MoraHash;
using MoreLinq;
using MoreLinq.Extensions;
using ServiceStack.Logging;

namespace Algorithm
{
    class Program
    {
       static void Main(string[] args)
        {
            var arg1 = Enumerable.Range(0, 10000).ToArray();
            var arg2 = Enumerable.Range(0, 10000).ToArray();
            var result = new int[10000];

//            L3(Enumerable.Repeat((byte) 0xF, 16).ToArray());

            var mora = new HashFunction();
            var m = new byte[] {0x01, 0xd4, 0x44, 0x90, 0x7e, 0xfb, 0x8c, 0xf7};
//            var bytes = new byte[] {14, 7, 9, 12, 9, 5, 14, 12, 15, 11, 11, 12, 10, 8, 3, 12};
//            var bytes2 = new byte[] {0x0, 0x1, 0xd, 0x4, 0x4, 0x4, 0x9, 0x0, 0x7, 0xe, 0xf, 0xb, 0x8, 0xc, 0xf, 0x7};
//            var bytes3 = Enumerable.Repeat((byte) 0xF, 16).ToArray();
//            
//            var old = L3(Enumerable.Repeat((byte) 0xF, 16).ToArray());
//                
//            old = bytes2.Xor(old);
//            old = S(old);
//            old = P(old);
//            old = L3(old);
//            
//
//            var n = mora.S(new byte[8]);
//            n = mora.P(n);
//            n = mora.L(n);

//            n = m.Xor(n);
//            n = mora.S(n);
//            n = mora.P(n);
//            n = mora.L(n);

//            var h = mora.ComputeHash(m);

//            var res = mora.L(Enumerable.Repeat((byte) 0xF, 16).ToArray());
//
//            res = mora.L(mora.P(mora.S(bytes)));

            var test = BitConverter.ToInt64(new byte[] {0xf9, 0xcb, 0xfe, 0xc9, 0xb6, 0x89, 0xab, 0x5b}.Xor(new byte[]
                {0x92, 0x5b, 0x4e, 0xf4, 0x9a, 0x5e, 0x71, 0x74}), 0);

            var test2 = 0xf9cbfec9b689ab5b ^ 0x925b4ef49a5e7174;
            
            var test3 = 0x6b90b03d2cd7da2f;
            var col = new Multicollision();
            col.FindCollisions();
            
            
//            var hash = new HashFunction();
////            var m = new byte[] {0x0, 0x1, 0xd, 0x4, 0x4, 0x4, 0x9, 0x0, 0x7, 0xe, 0xf, 0xb, 0x8, 0xc, 0xf};
//            var m = new byte[] {0x01, 0xd4, 0x44, 0x90, 0x7e, 0xfb, 0x8c, 0xf7};
//
//            hash.ComputeHash(m);
//            var h0 = new byte[16];
//            var N0 = new byte[16];
//            var K = N0.Xor(h0);
//            K = hash.S(K);
//            K = hash.P(K);
//            K = hash.L(K);
//
//            var m1 = m.Xor(K);
//            m1 = hash.S(m1);
//            m1 = hash.P(m1);
//            m1 = hash.L(m1.Reverse().ToArray());
//            
//            
//            int q = 0;
//            
//            //            Console.WriteLine(HelpText.AutoBuild(result, _ => _, _ => _));
//
//            var input = Enumerable.Range(1, 0xFFFF).Select(num => new BitArray(BitConverter.GetBytes(num)).Cast<bool>().Reverse().ToArray()).Select(bits => bits.Skip(16).ToArray()).ToArray();
//            
////            var output = new byte[] {0x2, 0xc, 0x5, 0xb}.ToArray();
////            var output2 = new byte[] {0x8, 0x3, 0x6, 0x2}.ToArray();
//            var output = new byte[] {0xf, 0xa, 0x4, 0xc, 0x0, 0xe, 0x2, 0xf, 0xb, 0x6, 0x3, 0xf, 0x3, 0xe, 0xc, 0xb};
//            var inp = new byte[] {0xb, 0x8, 0x1, 0xf, 0xc, 0xb, 0xc, 0x5, 0x5, 0xe, 0x9, 0x4, 0xf, 0x3, 0xc, 0xf};
//
//            var output2 = L3(inp);
//
//            var b = MoreEnumerable.Batch(output, 4).Select(o => input.FirstOrDefault(t => L(t).SequenceEqual(o)))
//                .Select(
//                    bits => MoreEnumerable.Batch(bits, 4).ToArray()
//                        .Select(c => MoreEnumerable.PadStart(c, 8).ToArray()).SelectMany(k => k).ToArray()
//                ).Select(bits => MoreEnumerable.Batch(bits, 8).Select(s => new BitArray(s.Reverse().ToArray())).ToArray())
//                .SelectMany(k => k).ToArray();
//
//            var res = new byte[16];
//            
//            for (int i = 0; i < b.Length; i++)
//            {
//                b[i].CopyTo(res, i); 
//            }
//
//            
//            var output2_2 = L2(res);
//
//            int a = 0;
            
            
//            var result = Parser.Default.ParseArguments<Options>(args);
//            
//            var errors = new List<CommandLine.Error>();
//            var parserResults = result
//                    .WithNotParsed(x => errors = x.ToList())
//                ;
//
//            if (errors.Any())
//            {
//                errors.ForEach(x => Console.WriteLine(x.ToString()));
//                return;
//            }
//
//            if (result.Errors.Any())
//            {
//                throw new ArgumentException();
//            }
//
//            if (!File.Exists(result.Value.OutputFile))
//            {
//                throw new FileNotFoundException(result.Value.OutputFile);
//            }
//            
//            var hash = new HashFunction();
//
//            var resultHash = hash.ComputeHash(File.ReadAllBytes(result.Value.OutputFile));
//            
//            File.WriteAllText(result.Value.OutputFile, resultHash);
            
            Console.ReadLine();
        }
    }
}