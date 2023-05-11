using System.Reflection;
using System.Threading.Channels;

namespace MyAddressExtractor.Objects {
    public sealed class Config {
        #region Runtime

        private readonly UserPromptLock PromptLock;
        private readonly CancellationTokenSource ProgramCancellation;
        public CancellationToken CancellationToken => this.ProgramCancellation.Token;

        #endregion
        #region Config Options

        public string OutputFilePath { get; private set; } = Defaults.OUTPUT_FILE_PATH;
        public string ReportFilePath { get; private set; } = Defaults.REPORT_FILE_PATH;

        public bool OperateRecursively { get; private set; } = Defaults.OPERATE_RECURSIVELY;
        public bool SkipPrompts { get; private set; } = Defaults.SKIP_PROMPTS;

        public int Threads { get; private set; } = Defaults.CHANNELS;

        public bool Debug { get; private set; } = Defaults.DEBUG;
        public bool Quiet { get; private set; } = Defaults.QUIET;

        #endregion

        public Config()
        {
            this.ProgramCancellation = new CancellationTokenSource();
            this.PromptLock = new UserPromptLock(this.ProgramCancellation);
        }

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
                            Output.Write("Exiting");
                            return false;
                        case ConsoleKey.I:
                            Output.Write($"Reading the following {files.Count} files:");
                            foreach (var file in files)
                            {
                                Output.Write($"  [{ByteExtensions.Format(file.Length).PadRight(10)}] {file.FullName}");
                            }
                            Console.WriteLine();
                            continue;
                        default:
                            return true;
                    }
                }
            }
        }

        internal async ValueTask<bool> WaitOnExceptionAsync(CancellationToken cancellation = default)
            => this.SkipPrompts // If silent output don't prompt
               || await this.PromptLock.PromptAsync(cancellation);

        internal Task AwaitContinuationAsync(CancellationToken cancellation = default)
            => this.PromptLock.WaitAsync(cancellation);

        #endregion
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
        #region Setters

        internal Writer? Set(IReadOnlyCollection<string> args, string input) => input switch {
            "--recursive"
                => this.SetRecursion(),
            "-o" or "--output"
                => this.SetOutputPath(),
            "-r" or "--report"
                => this.SetReportPath(),
            "-y" or "--yes"
                => this.SetSkipPrompts(),
            "-v" or "--version"
                => args.Count > 1 ? throw new ArgumentException($"'{input}' must be the only argument when it is used") : this.ShowVersion(),
            "-?" or "-h" or "--help"
                => args.Count > 0 ? throw new ArgumentException($"'{input}' must be the only argument when it is used") : this.ShowUsage(),
            "--debug"
                => this.SetDebug(),
            "-q" or "--quiet"
                => this.SetQuiet(),
            "--threads"
                => this.SetThreads(),
            _ => throw new ArgumentException($"Unexpected option '{input}'")
        };

        private Writer? SetRecursion()
        {
            this.OperateRecursively = true;
            return null;
        }

        private Writer? SetOutputPath()
        {
            return value => this.OutputFilePath = value;
        }

        private Writer? SetReportPath()
        {
            return value => this.ReportFilePath = value;
        }

        private Writer? SetSkipPrompts()
        {
            this.SkipPrompts = true;
            return null;
        }

        private Writer? SetDebug()
        {
            this.Debug = true;
            return null;
        }

        private Writer? SetQuiet()
        {
            this.Quiet = true;
            return null;
        }

        private Writer? SetThreads()
        {
            return value => this.Threads = this.ParseInt(value, min: 1, max: 1000);
        }

        private Writer? ShowUsage()
        {
            var assembly = Assembly.GetExecutingAssembly();
            Console.WriteLine($"""
                Syntax: {assembly.GetName().Name} -?
                Syntax: {assembly.GetName().Name} -v
                Syntax: {assembly.GetName().Name} <input [input...]> [-o output] [-r report]
                
                --help, -h, -?       Help for the command line arguments
                -v                   Prints the application version
                
                input                One or more input file paths
                -o output            File path to write output file
                -r report            File path to write report file
                
                --debug              Enables debug mode, prints timings and exceptions
                --threads #          Specifies the number of Tasks to use for parsing Regex
                --recursive          Recursively enters directories provided to search for files
                --yes, -y            Skips any prompts asking to continue
                --quiet, -q          Runs in quiet mode, not as verbose
            """);

            return null;
        }

        private Writer? ShowVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            Console.WriteLine(assembly.GetName().Version);

            return null;
        }

        #endregion

        public Channel<Line> CreateChannel()
            => Channel.CreateBounded<Line>(new BoundedChannelOptions(this.Threads * 3) {
                SingleWriter = true, // Only one instance of `ILineReader` is used at a time
                SingleReader = false,
                AllowSynchronousContinuations = false // Require Async
            });

        public static class Defaults {
            public const string OUTPUT_FILE_PATH = "addresses_output.txt";
            public const string REPORT_FILE_PATH = "report.txt";

            public const bool OPERATE_RECURSIVELY = false;
            public const bool SKIP_PROMPTS = false;

            public const int CHANNELS = 4;

            public const bool DEBUG = false;
            public const bool QUIET = false;
        }

        internal delegate void Writer(string value);
    }
}
