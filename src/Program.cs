using System;
using System.Collections.Generic;
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
                var allAddresses = new HashSet<string>();

                foreach (var inputFilePath in args)
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
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}
