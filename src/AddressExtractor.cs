using System.Text;
using System.Text.RegularExpressions;

namespace MyAddressExtractor
{
    public class AddressExtractor
    {
        public List<string> ExtractAddressesFromFile(string inputFilePath)
        {
            string fileContent = File.ReadAllText(inputFilePath);
            return ExtractAddresses(fileContent);
        }

        public List<string> ExtractAddresses(string content)
        {
            string addressPattern = @"\b[^.\s][\w\.\-!#$%&'*+-/=?^_`{|}~]+@([\w\-]+\.)+[\w]{2,}\b";
            var matches = Regex.Matches(content, addressPattern);
            var uniqueAddresses = new HashSet<string>();

            foreach (Match match in matches)
            {
                uniqueAddresses.Add(match.Value.ToLower());
            }

            return uniqueAddresses.OrderBy(a => a).ToList();
        }

        public void SaveAddresses(string filePath, List<string> addresses)
        {
            File.WriteAllLines(filePath, addresses);
        }

        public void SaveReport(string filePath, Dictionary<string, int> uniqueAddressesPerFile)
        {
            var reportContent = new StringBuilder("Unique addresses per file:\n");

            foreach (var entry in uniqueAddressesPerFile)
            {
                reportContent.AppendLine($"{entry.Key}: {entry.Value}");
            }

            File.WriteAllText(filePath, reportContent.ToString());
        }
    }
}
