using System.ComponentModel;
using System.Reflection;

namespace MyAddressExtractor
{
    internal static class CommandLineProcessor
    {
        public static string OUTPUT_FILE_PATH { get; private set; } = "addresses_output.txt";
        public static string REPORT_FILE_PATH { get; private set; } = "report.txt";
        
        internal static void Process(string[] args, IList<string> inputFilePaths)
        {
            if (args.Length == 0)
            {
                throw new ArgumentException("Please provide at least one input file path.");
            }

            bool expectingOutput = false;
            bool expectingReport = false;

            foreach (var arg in args)
            {
                if (expectingOutput)
                {
                    // Is it an option?
                    if (arg[0] == '-')
                    {
                        throw new ArgumentException("Missing output file path after -o option");
                    }
                    OUTPUT_FILE_PATH = arg;
                    expectingOutput = false;
                }
                else if (expectingReport)
                {
                    // Is it an option?
                    if (arg[0] == '-')
                    {
                        throw new ArgumentException("Missing report file path after -r option");
                    }
                    REPORT_FILE_PATH = arg;
                    expectingReport = false;
                }
                else
                {
                    // Is it an option?
                    if (arg[0] == '-')
                    {
                        if (arg.Length > 1)
                        {
                            var option = arg[1..];
                            switch (option)
                            {
                                case "o":
                                    expectingOutput = true;
                                    break;
                                case "r":
                                    expectingReport = true;
                                    break;
                                case "v":
                                    if (args.Length > 1)
                                    {
                                        throw new ArgumentException($"'{arg}' must be the only argument when it is used");
                                    }
                                    Version();
                                    return;
                                case "?":
                                    if (args.Length > 1)
                                    {
                                        throw new ArgumentException($"'{arg}' must be the only argument when it is used");
                                    }
                                    Usage();
                                    return;
                                default:
                                    throw new ArgumentException($"Unexpected option '{arg}'");
                            }
                        }
                        else
                        {
                            throw new ArgumentException($"Invalid argument '{arg}'");
                        }
                    }
                    else // No option or not expecting an option file path, so assume it to be an input file path
                    {
                        inputFilePaths.Add(arg);
                    }
                }
            }

            // If there are no more arguments but we were expecting a option file path, alert the user
            if (expectingOutput)
            {
                throw new ArgumentException("Missing output file path after -o option");
            }
            else if (expectingReport)
            {
                throw new ArgumentException("Missing report file path after -r option");
            }

            // Make sure we have at least one input file path
            if (inputFilePaths.Count == 0)
            {
                throw new ArgumentException("No input file paths specified");
            }
        }

        internal static bool WaitInput()
        {
            Console.WriteLine("Press ANY KEY to continue. Q to Quit.");
            ConsoleKeyInfo info;
            do {
                info = Console.ReadKey(intercept: true);
            } while(info.Modifiers is not 0); // No modifiers
            return info.Key is not ConsoleKey.Q; // Check for Enter confirmation
        }

        static void Usage()
        {
            var assembly = Assembly.GetExecutingAssembly();
            Console.WriteLine($"Syntax: {assembly.GetName().Name} -?");
            Console.WriteLine($"Syntax: {assembly.GetName().Name} -v");
            Console.WriteLine($"Syntax: {assembly.GetName().Name} <input [input...]> [-o output] [-r report]");
            Console.WriteLine("-?                   Help for the command line arguments");
            Console.WriteLine("-v                   Prints the application version");
            Console.WriteLine("input                One or more input file paths");
            Console.WriteLine("-o output            File path to write output file");
            Console.WriteLine("-r report            File path to write report file");
        }

        static void Version()
        {
            var assembly = Assembly.GetExecutingAssembly();
            Console.WriteLine(assembly.GetName().Version);
        }
    }
}
