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
            string addressPattern = @"(?!\.)[a-zA-Z0-9\.\-!#$%&'+-/=?^_`{|}~""\\]+(?<!\.)@([a-zA-Z0-9\-]+\.)+[a-zA-Z0-9]{2,}\b(?<!\s)";
            var matches = Regex.Matches(content, addressPattern);
            var uniqueAddresses = new HashSet<string>();

            foreach (Match match in matches)
            {
                var email = match.Value;
                if (email.Contains('*'))
                    continue;
                if (email.Contains(".."))
                    continue;
                if (email.Contains(".@"))
                    continue;
                if (email.Length >= 256)
                    continue;
                // Handle cases such as: foobar@_.com, oobar@f_b.com
                if (email.Substring(email.LastIndexOf("@")).Contains("_"))
                    continue;
                // Handle cases such as: foo@bar.1com, foo@bar.12com
                if (int.TryParse(email[email.LastIndexOf(".")+1].ToString(), out _))
                    continue;
                
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
