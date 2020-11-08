using CommandLine;

namespace Algorithm
{
    public class Options
    {
        [Option('i', "input", Required = true, Default = "",
            HelpText =
                "Input data file path")]
        public string InputFile { get; set; }
        
        [Option('o', "output", Required = true, Default = "",
            HelpText =
                "Output data file path")]
        public string OutputFile { get; set; }
    }
}