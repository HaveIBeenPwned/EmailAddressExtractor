using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MyAddressExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide at least one input file path.");
                return;
            }

            var outputFilePath = "addresses_output.txt";
            var reportFilePath = "report.txt";
            Dictionary<string, int> uniqueAddressesPerFile = new Dictionary<string, int>();

            var extractor = new AddressExtractor();

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var allAddresses = new HashSet<string>();

                foreach (var inputFilePath in args)
                {
                    var addresses = extractor.ExtractAddressesFromFile(inputFilePath);
                    allAddresses.UnionWith(addresses);
                    uniqueAddressesPerFile.Add(inputFilePath, addresses.Count);
                }
                
                stopwatch.Stop();
                Console.WriteLine($"Extraction time: {stopwatch.ElapsedMilliseconds}ms");
                Console.WriteLine($"Addresses extracted: {allAddresses.Count}");
                // Extraction does not currently process per row, so we do not have the row count at this time
                long rate = (long)(allAddresses.Count / (stopwatch.ElapsedMilliseconds / 1000.0));
                Console.WriteLine($"Extraction rate: {rate}/s");

                extractor.SaveAddresses(outputFilePath, allAddresses.ToList());
                extractor.SaveReport(reportFilePath, uniqueAddressesPerFile);

                Console.WriteLine($"Addresses saved to {outputFilePath}");
                Console.WriteLine($"Report saved to {reportFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
