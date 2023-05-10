using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using MyAddressExtractor.Objects;
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
                Output.Exception(ae, trace: false);
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
                Output.Write("Extracting...");

                IPerformanceStack perf = config.Debug
                    ? new DebugPerformanceStack() : IPerformanceStack.DEFAULT;

                var saveOutput = !string.IsNullOrWhiteSpace(config.OutputFilePath);
                var saveReport = !string.IsNullOrWhiteSpace(config.ReportFilePath);

                await using (var monitor = new AddressExtractorMonitor(config, perf))
                {
                    foreach (var file in files)
                    {
                        try {
                            await monitor.RunAsync(file, config.CancellationToken);
                        } catch (OperationCanceledException) {
                            throw;
                        } catch (Exception ex) {
                            if (config.Debug)
                                Output.Exception(new Exception($"An error occurred while reading '{file}':", ex));
                            else
                                Output.Error($"An error occurred while reading '{file}': {ex.Message}");

                            if (ex is not NotImplementedException && !await config.WaitOnExceptionAsync(config.CancellationToken))
                                return (int)ErrorCode.UnspecifiedError;
                        }
                    }

                    // Wait for completion
                    Output.Write("Finished reading files");
                    await monitor.AwaitCompletionAsync();

                    // Log one last time out of the Timer loop
                    monitor.Log();

                    if (saveOutput || saveReport)
                    {
                        Output.Write("Saving to disk..");
                        await monitor.SaveAsync(CancellationToken.None);
                    }
                }

                if (saveOutput)
                    Output.Write($"Addresses saved to {config.OutputFilePath}");

                if (saveReport)
                    Output.Write($"Report saved to {config.ReportFilePath}");
            }
            catch (TaskCanceledException)
            {
                return (int)ErrorCode.UnspecifiedError;
            }
            catch (OperationCanceledException)
            {
                return (int)ErrorCode.UnspecifiedError;
            }
            catch (Exception ex)
            {
                if (config.Debug)
                    Output.Exception(ex);
                else
                    Output.Error($"An error occurred: {ex.Message}");
                return (int)ErrorCode.UnspecifiedError;
            }

            return (int)ErrorCode.NoError;
        }
    }
}
