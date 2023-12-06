using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using HaveIBeenPwned.AddressExtractor.Objects.Attributes;

namespace HaveIBeenPwned.AddressExtractor.Objects {
    public sealed class Config {
        #region Options

        /// <summary>If the program should recursively enter folders</summary>
        [CommandLineOption("recursive", Description = "Recursively enters directories provided to search for files")]
        public bool OperateRecursively { get; private set; } = Defaults.OPERATE_RECURSIVELY;

        /// <summary>The path to write collected <see cref="EmailAddress"/>es to</summary>
        [CommandLineOption("o", "output", Description = "File path to write output file", Expects = "path")]
        public string OutputFilePath { get; private set; } = Defaults.OUTPUT_FILE_PATH;

        /// <summary>The path to write the summary report to</summary>
        [CommandLineOption("r", "report", Description = "File path to write report file", Expects = "path")]
        public string ReportFilePath { get; private set; } = Defaults.REPORT_FILE_PATH;

        /// <summary>If the prompt to Continue should be skipped</summary>
        [CommandLineOption("y", "yes", Description = "Skips any normal continue prompts")]
        public bool SkipPrompts { get; private set; } = Defaults.SKIP_PROMPTS;

        /// <summary>If the prompt to Continue on Exceptions should be skipped</summary>
        [CommandLineOption("skip-exceptions", Description = "Skips any continue prompts on exceptions")]
        public bool SkipExceptions { get; private set; } = Defaults.SKIP_EXCEPTIONS;

        /// <summary>If verbosity for development is enabled</summary>
        [CommandLineOption("debug", Description = "Enables debug mode, prints timings and exceptions")]
        public bool Debug { get; private set; } = Defaults.DEBUG;

        /// <summary>If generic verbose messages should be omitted</summary>
        [CommandLineOption("q", "quiet", Description = "Runs in quiet mode, not as verbose")]
        public bool Quiet { get; private set; } = Defaults.QUIET;

        /// <summary>The number of <see cref="Task"/>s to use for parsing <see cref="Regex"/></summary>
        [CommandLineOption("threads", Description = "Specifies the number of Tasks to use for parsing Regex")]
        [Range(1, 1000)]
        public int Threads { get; private set; } = Defaults.CHANNELS;

        [CommandLineOption("?", "h", "help", Description = "Help for the command line arguments", Exclusive = true)]
        private void ShowUsage()
        {
            var options = new List<CommandLineOption>(CommandLineProcessor.CLI_OPTIONS);
            options.Insert(0, new CommandLineOption("input", "One or more input file paths"));

            var assembly = Assembly.GetExecutingAssembly();
            var output = new StringBuilder($"""
            Syntax: {assembly.GetName().Name} -?
            Syntax: {assembly.GetName().Name} -v
            Syntax: {assembly.GetName().Name} <input [input...]> [-o output] [-r report]
            """);

            var pairs = options.ToDictionary(key => key.JoinArgs(), val => val.Description);
            var pad = pairs.Keys.Max(key => key.Length) + 3;

            // Spacing
            output.AppendLine()
                .AppendLine();

            foreach ((string key, string value) in pairs)
                output.AppendLine($"  {key.PadRight(pad)}{value}");

            Console.WriteLine(output);
        }

        [CommandLineOption("v", "version", Description = "Prints the application version", Exclusive = true)]
        private void ShowVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            Console.WriteLine(assembly.GetName().Version);
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
            public const bool SKIP_EXCEPTIONS = false;

            public const int CHANNELS = 4;

            public const bool DEBUG = false;
            public const bool QUIET = false;
        }

        internal delegate void Writer(string value);
    }
}
