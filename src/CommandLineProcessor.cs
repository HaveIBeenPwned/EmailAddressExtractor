using System.Reflection;
using System.Threading.Channels;
using MyAddressExtractor.Objects;

namespace MyAddressExtractor
{
    public class CommandLineProcessor
    {
        public string OutputFilePath { get; private set; } = Defaults.OUTPUT_FILE_PATH;
        public string ReportFilePath { get; private set; } = Defaults.REPORT_FILE_PATH;

        public bool OperateRecursively { get; private set; } = Defaults.OPERATE_RECURSIVELY;
        public bool SkipPrompts { get; private set; } = Defaults.SKIP_PROMPTS;

        public int Channels { get; private set; } = Defaults.CHANNELS;

        public bool Debug { get; private set; } = Defaults.DEBUG;

        internal CommandLineProcessor(IReadOnlyCollection<string> args, out IList<string> inputFilePaths)
        {
            if (args.Count == 0)
            {
                throw new ArgumentException("Please provide at least one input file path.");
            }

            Action<string>? handle = null;
            string previous = string.Empty;

            inputFilePaths = new List<string>();
            foreach (var arg in args)
            {
                if (handle is not null)
                {
                    // Is it an option?
                    if (arg.Length > 0 && arg[0] == '-')
                    {
                        throw new ArgumentException($"Missing value for '{previous}' option");
                    }
                    try {
                        handle(arg);
                        handle = null;
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException($"Error reading option '{previous}' ({e.Message})");
                    }
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
                                case "-recursive":
                                    this.OperateRecursively = true;
                                    break;
                                case "o" or "-output":
                                    handle = value => this.OutputFilePath = value;
                                    break;
                                case "r" or "-report":
                                    handle = value => this.ReportFilePath = value;
                                    break;
                                case "y" or "-yes":
                                    this.SkipPrompts = true;
                                    break;
                                case "v" or "-version":
                                    if (args.Count > 1)
                                    {
                                        throw new ArgumentException($"'{arg}' must be the only argument when it is used");
                                    }
                                    Version();
                                    return;
                                case "?" or "h" or "-help":
                                    if (args.Count > 1)
                                    {
                                        throw new ArgumentException($"'{arg}' must be the only argument when it is used");
                                    }
                                    Usage();
                                    return;
                                case "-debug":
                                    this.Debug = true;
                                    break;
                                case "-channels":
                                    handle = value => this.Channels = this.ParseInt(value, min: 0, max: 1000);
                                    break;
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

                previous = arg;
            }

            // If there are no more arguments but we were expecting a option file path, alert the user
            if (handle is not null)
            {
                throw new ArgumentException($"Missing output file path after {previous} option");
            }
            // Make sure we have at least one input file path
            if (inputFilePaths.Count == 0)
            {
                throw new ArgumentException("No input file paths specified");
            }
        }

        public Channel<Line> CreateChannel()
            => Channel.CreateBounded<Line>(new BoundedChannelOptions(this.Channels) {
                SingleWriter = true, // Only one instance of `ILineReader` is used at a time
                AllowSynchronousContinuations = false // Require Async
            });

        #region Parsers

        private int ParseInt(string value, int? min = null, int? max = null)
        {
            if (!int.TryParse(value, out int i))
                throw new ArgumentException("Value must be a number");

            if (i < min)
                throw new ArgumentException($"Value cannot be less than {min}");

            if (i > max)
                throw new ArgumentException($"Value cannot be more than {max}");

            return i;
        }

        #endregion
        #region CLI Waiters

        internal bool WaitInput(FileCollection files)
        {
            // If silent output don't prompt
            if (this.SkipPrompts)
                return true;

            while (true)
            {
                Console.Write("Press ANY KEY to continue ['Q' to Quit; 'I' for info]: ");
                var read = Console.ReadKey(intercept: true);
                Console.WriteLine();

                // No modifiers (shift/ctrl/alt)
                if (read.Modifiers is 0)
                {
                    switch (read.Key)
                    {
                        case ConsoleKey.Q:
                            Console.WriteLine("Exiting");
                            return false;
                        case ConsoleKey.I:
                            Console.WriteLine($"Reading the following {files.Count} files:");
                            foreach (var file in files)
                            {
                                var info = new FileInfo(file);
                                Console.WriteLine($"- [{ByteExtensions.Format(info.Length).PadRight(10)}] {file}");
                            }
                            Console.WriteLine();
                            continue;
                        default:
                            return true;
                    }
                }
            }
        }

        internal bool WaitContinue()
        {
            // If silent output don't prompt
            if (this.SkipPrompts)
                return true;

            Console.Write("Continue? [y/n]: ");
            while (true)
            {
                var read = Console.ReadKey(intercept: true);

                // No modifiers (shift/ctrl/alt)
                if (read.Modifiers is 0)
                {
                    switch (read.Key)
                    {
                        // Allow continuing
                        case ConsoleKey.Y:
                            Console.WriteLine();
                            return true;

                        // Exit
                        case ConsoleKey.N:
                        case ConsoleKey.Escape:
                            Console.WriteLine();
                            return false;
                    }
                }
            }
        }

        #endregion
        #region Special Output

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
            Console.WriteLine("");
            Console.WriteLine("--recursive          Recursively enters directories provided to search for files");
        }

        static void Version()
        {
            var assembly = Assembly.GetExecutingAssembly();
            Console.WriteLine(assembly.GetName().Version);
        }

        #endregion

        public static class Defaults {
            public const string OUTPUT_FILE_PATH = "addresses_output.txt";
            public const string REPORT_FILE_PATH = "report.txt";

            public const bool OPERATE_RECURSIVELY = false;
            public const bool SKIP_PROMPTS = false;

            public const int CHANNELS = 4;

            public const bool DEBUG = false;
        }
    }
}
