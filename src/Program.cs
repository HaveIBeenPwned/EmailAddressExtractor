using System.Runtime.CompilerServices;

using HaveIBeenPwned.AddressExtractor.Objects;
using HaveIBeenPwned.AddressExtractor.Objects.Performance;

[assembly: InternalsVisibleTo("AddressExtractorTest")]

namespace HaveIBeenPwned.AddressExtractor;

public class Program
{
    private enum ErrorCode
    {
        NoError = 0,
        UnspecifiedError = 1,
        InvalidArguments = 2
    }

    public static async Task<int> Main(string[] args)
    {
        IList<string> inputFilePaths;

        Config config;
        try
        {
            config = CommandLineProcessor.Parse(args, out inputFilePaths);
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
            var runtime = new Runtime(config);
            var files = new FileCollection(runtime, inputFilePaths);

            if (!runtime.WaitInput(files))
            {
                return (int)ErrorCode.NoError;
            }

            Output.Write("Extracting...");

            var perf = config.Debug
                ? new DebugPerformanceStack() : IPerformanceStack.DEFAULT;

            var saveOutput = !string.IsNullOrWhiteSpace(config.OutputFilePath);
            var saveReport = !string.IsNullOrWhiteSpace(config.ReportFilePath);

            await using (var monitor = new AddressExtractorMonitor(runtime, perf))
            {
                var fileCount = 0L;
                foreach (var file in files.OrderBy(f => f.Length))
                {
                    fileCount++;
                    try
                    {
                        await monitor.RunAsync(fileCount, file, runtime.CancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        if (runtime.ShouldDebug(ex))
                        {
                            Output.Exception(new IOException($"An error occurred while reading '{file}':", ex));
                        }
                        else
                        {
                            Output.Error($"An error occurred while reading '{file}': {ex.Message}");
                        }

                        if (runtime.ShouldDebug(ex) && !await runtime.WaitOnExceptionAsync().ConfigureAwait(false))
                        {
                            return (int)ErrorCode.UnspecifiedError;
                        }
                    }
                }

                // Wait for completion
                Output.Write("Finished reading files");
                await monitor.AwaitCompletionAsync().ConfigureAwait(false);

                // Log one last time out of the Timer loop
                monitor.Log();

                if (saveOutput || saveReport)
                {
                    Output.Write("Saving to disk..");
                    await monitor.SaveAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }

            if (saveOutput)
            {
                Output.Write($"Addresses saved to {config.OutputFilePath}");
            }

            if (saveReport)
            {
                Output.Write($"Report saved to {config.ReportFilePath}");
            }
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
            {
                Output.Exception(ex);
            }
            else
            {
                Output.Error($"An error occurred: {ex.Message}");
            }

            return (int)ErrorCode.UnspecifiedError;
        }

        return (int)ErrorCode.NoError;
    }
}
