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
            var outputFilePath = "addresses_output.txt";
            var reportFilePath = "report.txt";

            try
            {
                CommandLineProcessor.Process(args, inputFilePaths, ref outputFilePath, ref reportFilePath);
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

            Dictionary<string, int> uniqueAddressesPerFile = new Dictionary<string, int>();

            var extractor = new AddressExtractor();

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var allAddresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var inputFilePath in inputFilePaths)
                {
                    var addresses = await extractor.ExtractAddressesFromFileAsync(inputFilePath, CancellationToken.None);
                    allAddresses.UnionWith(addresses);
                    uniqueAddressesPerFile.Add(inputFilePath, addresses.Count);
                }
                
                stopwatch.Stop();
                Console.WriteLine($"Extraction time: {stopwatch.ElapsedMilliseconds:n0}ms");
                Console.WriteLine($"Addresses extracted: {allAddresses.Count:n0}");
                // Extraction does not currently process per row, so we do not have the row count at this time
                long rate = (long)(allAddresses.Count / (stopwatch.ElapsedMilliseconds / 1000.0));
                Console.WriteLine($"Extraction rate: {rate:n0}/s");

                await extractor.SaveAddressesAsync(outputFilePath, allAddresses, CancellationToken.None);
                await extractor.SaveReportAsync(reportFilePath, uniqueAddressesPerFile, CancellationToken.None);

                Console.WriteLine($"Addresses saved to {outputFilePath}");
                Console.WriteLine($"Report saved to {reportFilePath}");
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
