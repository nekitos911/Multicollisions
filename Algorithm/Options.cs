using CommandLine;

namespace Algorithm
{
    public class Options
    {
//        [Option('h', "help", Required = false, Default = false,
//            HelpText = "Help")]
//        public bool Help { get; set; }

        [Option('i', "input", Required = true, Default = "",
            HelpText =
                "Input data file path")]
        public string InputFile { get; set; }
        
        [Option('o', "output", Required = true, Default = "",
            HelpText =
                "Output data file path")]
        public string OutputFile { get; set; }
//
//        [Option('s', "silent", Required = false, Default = false, HelpText = "Disables output ...")]
//        public bool Output { get; set; }
//
//        [Option('p', "path", Required = false, Default = "../some/dir/",
//            HelpText =
//                "Specifies the path ...")]
//        public string StartPath { get; set; }
    }
}