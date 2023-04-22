using System;
using System.Collections.Generic;
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

        static int Main(string[] args)
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
                var allAddresses = new HashSet<string>();

                foreach (var inputFilePath in inputFilePaths)
                {
                    var addresses = extractor.ExtractAddressesFromFile(inputFilePath);
                    allAddresses.UnionWith(addresses);
                    uniqueAddressesPerFile.Add(inputFilePath, addresses.Count);
                }

                extractor.SaveAddresses(outputFilePath, allAddresses.ToList());
                extractor.SaveReport(reportFilePath, uniqueAddressesPerFile);

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
