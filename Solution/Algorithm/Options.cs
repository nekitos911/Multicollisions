using CommandLine;

namespace Algorithm
{
    public class Options
    {
        [Option('t', Required = false, Default = 5,
            HelpText =
                "Operations count (2^t messages)")]
        public int T { get; set; }
        
//        [Option('i', "input", Required = true, Default = "",
//            HelpText =
//                "Input data file path")]
//        public string InputFile { get; set; }
//        
        [Option('o', "output", Required = true, Default = "",
            HelpText =
                "Output data file path")]
        public string OutputFile { get; set; }
    }
}