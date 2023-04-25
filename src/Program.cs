using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

[assembly:InternalsVisibleTo("AddressExtractorTest")]

namespace MyAddressExtractor
{
    class Program
    {
        enum ErrorCode
        {
            NoError = 0,
            UnspecifiedError = 1,
            InvalidArguments = 2
        }

        static async Task<int> Main(string[] args)
        {
            var inputFilePaths = new List<string>();

            CommandLineProcessor config;
            try
            {
                config = new CommandLineProcessor(args, inputFilePaths);
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

                await using (var monitor = new AddressExtractorMonitor())
                {
                    await monitor.RunAsync(files, CancellationToken.None);

                    // Log one last time out of the Timer loop
                    monitor.Log();
                    await monitor.SaveAsync(config, CancellationToken.None);
                }
                
                Console.WriteLine($"Addresses saved to {config.OutputFilePath}");
                Console.WriteLine($"Report saved to {config.ReportFilePath}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An error occurred: {ex.Message}");
                return (int)ErrorCode.UnspecifiedError;
            }

            return (int)ErrorCode.NoError;
        }
    }
}
