using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using MoraHash;

namespace Algorithm
{
    class Program
    {
        static void Main(string[] args)
        {
            //            Console.WriteLine(HelpText.AutoBuild(result, _ => _, _ => _));
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

            if (!File.Exists(result.Value.OutputFile))
            {
                throw new FileNotFoundException(result.Value.OutputFile);
            }
            
            var hash = new HashFunction();

            var resultHash = hash.ComputeHash(File.ReadAllBytes(result.Value.OutputFile));
            
            File.WriteAllText(result.Value.OutputFile, resultHash);
            
            Console.ReadLine();
        }
    }
}