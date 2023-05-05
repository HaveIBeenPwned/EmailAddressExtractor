using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using MyAddressExtractor.Objects.Performance;

[assembly:InternalsVisibleTo("AddressExtractorTest")]

namespace MyAddressExtractor
{
    public class Program
    {
        private enum ErrorCode
        {
            NoError = 0,
            UnspecifiedError = 1,
            InvalidArguments = 2
        }

        static async Task<int> Main(string[] args)
        {
            IList<string> inputFilePaths;

            CommandLineProcessor config;
            try
            {
                config = new CommandLineProcessor(args, out inputFilePaths);
            }
            catch (ArgumentException ae)
            {
                Console.Error.WriteLine(ae.Message);
                return (int)ErrorCode.InvalidArguments;
            }
            // If no input paths were listed, the usage was printed, so we should exit cleanly
            if (inputFilePaths.Count == 0)
            {
                return (int)ErrorCode.NoError;
            }

            try
            {
                var files = new FileCollection(config, inputFilePaths);

                if (!config.WaitInput(files))
                    return (int)ErrorCode.NoError;
                Console.WriteLine("Extracting...");

                IPerformanceStack perf = config.Debug
                    ? new DebugPerformanceStack() : IPerformanceStack.DEFAULT;

                var saveOutput = !string.IsNullOrWhiteSpace(config.OutputFilePath);
                var saveReport = !string.IsNullOrWhiteSpace(config.ReportFilePath);

                await using (var monitor = new AddressExtractorMonitor(config, perf))
                {
                    foreach (var file in files)
                    {
                        try {
                            await monitor.RunAsync(file, CancellationToken.None);

                        } catch (Exception ex) {
                            Console.Error.WriteLine($"An error occurred while reading '{file}': {ex.Message}");
                            if (ex is not NotImplementedException && !config.WaitContinue())
                                return (int)ErrorCode.UnspecifiedError;
                        }
                    }

                    // Wait for completion
                    Console.WriteLine("Finished reading files");
                    await monitor.AwaitCompletion();

                    // Log one last time out of the Timer loop
                    monitor.Log();

                    if (saveOutput || saveReport)
                    {
                        Console.WriteLine("Saving to disk..");
                        await monitor.SaveAsync(CancellationToken.None);
                    }
                }

                if (saveOutput)
                    Console.WriteLine($"Addresses saved to {config.OutputFilePath}");

                if (saveReport)
                    Console.WriteLine($"Report saved to {config.ReportFilePath}");
            }
            catch (Exception ex)
            {
                if (config.Debug)
                    Console.WriteLine(ex);
                else
                    Console.Error.WriteLine($"An error occurred: {ex.Message}");
                return (int)ErrorCode.UnspecifiedError;
            }

            return (int)ErrorCode.NoError;
        }
    }
}
