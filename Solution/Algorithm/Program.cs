using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using MoraHash;
using MoreLinq;

namespace Algorithm
{
    class Program
    {
        static void Main(string[] args)
        {
//            var step = args.Length > 0 ? int.Parse(args[0]) : 150_000;
//            new MeaningfulCollisions().MeaningfulCollision(Encoding.UTF8.GetBytes("qwertyui"), Encoding.UTF8.GetBytes("qwertyup"), step);
            var result = Parser.Default.ParseArguments<Options>(args);
            
            var errors = new List<CommandLine.Error>();
            var parserResults = result
                    .WithNotParsed(x => errors = x.ToList())
                ;

            if (errors.Any())
            {
                errors.ForEach(x => Console.WriteLine(x.ToString()));
                return;
            }

            if (result.Errors.Any())
            {
                throw new ArgumentException();
            }

//            if (!File.Exists(result.Value.OutputFile))
//            {
//                throw new FileNotFoundException(result.Value.OutputFile);
//            }
//            
//            var hash = new HashFunction();

//            var resultHash = hash.ComputeHash(File.ReadAllBytes(result.Value.OutputFile));

            var collisions = new MultiCollisions();
            var (messages, h, n) = collisions.FindCollisions(result.Value.T);

            using (var sw = File.CreateText(result.Value.OutputFile))
            {
                sw.WriteLine($"h = {HashFunction.StringRepresentation(BitConverter.GetBytes(h).Reverse().ToArray())}");
                sw.WriteLine($"n = {HashFunction.StringRepresentation(BitConverter.GetBytes(n).Reverse().ToArray())}");
                messages.ForEach(msg =>
                {
                    sw.WriteLine(HashFunction.StringRepresentation(msg.ToArray()));
                });  
            }

            Console.ReadLine();
        }
    }
}