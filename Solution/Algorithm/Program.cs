using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using MoraHash;
using MoreLinq;

namespace Algorithm
{
    class Program
    {
        static void Main(string[] args)
        {
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

            var collisions = new Multicollision();
            var messages = collisions.FindCollisions(result.Value.T);

            using (var sw = File.CreateText(result.Value.OutputFile))
            {
                sw.WriteLine($"h = {BitConverter.ToString(BitConverter.GetBytes(messages.Item2).Reverse().ToArray()).Replace("-", string.Empty)}");
                messages.Item1.Select(msg => msg.SelectMany(m => BitConverter.GetBytes(m).Reverse())).ForEach(msg =>
                {
                    sw.WriteLine(HashFunction.StringRepresentation(msg.ToArray()));
                });  
            }

            Console.ReadLine();
        }
    }
}