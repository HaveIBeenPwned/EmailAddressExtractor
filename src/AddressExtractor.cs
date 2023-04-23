using System.Text;
using System.Text.RegularExpressions;

namespace MyAddressExtractor
{
    public partial class AddressExtractor
    {
        [GeneratedRegex(@"(?!\.)[a-zA-Z0-9\.\-!#$%&'+-/=?^_`{|}~""\\]+(?<!\.)@([a-zA-Z0-9\-_]+\.)+[a-zA-Z0-9]{2,}\b(?<!\s)")]
        public static partial Regex EmailRegex();
        
        public async ValueTask<HashSet<string>> ExtractAddressesFromFileAsync(string inputFilePath, CancellationToken cancellation = default)
        {
            var list = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await foreach(string line in File.ReadLinesAsync(inputFilePath, cancellation))
            {
                foreach (string address in this.ExtractAddresses(line)) {
                    list.Add(address);
                }
            }
            
            return list;
        }

        public IEnumerable<string> ExtractAddresses(string content)
        {

            var matches = AddressExtractor.EmailRegex()
                .Matches(content);

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
                
                yield return match.Value;
            }
        }

        public async ValueTask SaveAddressesAsync(string filePath, IEnumerable<string> addresses, CancellationToken cancellation = default)
        {
            await File.WriteAllLinesAsync(filePath, addresses.OrderBy(a => a), cancellation);
        }

        public async ValueTask SaveReportAsync(string filePath, Dictionary<string, int> uniqueAddressesPerFile, CancellationToken cancellation = default)
        {
            var reportContent = new StringBuilder("Unique addresses per file:\n");
            
            foreach (var entry in uniqueAddressesPerFile)
            {
                reportContent.AppendLine($"{entry.Key}: {entry.Value}");
            }
            
            await File.WriteAllTextAsync(filePath, reportContent.ToString(), cancellation);
        }
    }
}
