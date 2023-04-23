using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace MyAddressExtractor
{
    public partial class AddressExtractor
    {
        [GeneratedRegex(@"(?!\.)[a-zA-Z0-9\.\-!#$%&'+-/=?^_`{|}~""\\]+(?<!\.)@([a-zA-Z0-9\-_]+\.)+[a-zA-Z0-9]{2,}\b(?<!\s)")]
        public static partial Regex EmailRegex();
        
        public async IAsyncEnumerable<string> ExtractAddressesFromFileAsync(string inputFilePath, [EnumeratorCancellation] CancellationToken cancellation = default)
        {
            await foreach(var line in File.ReadLinesAsync(inputFilePath, cancellation))
            {
                foreach (var address in this.ExtractAddresses(line))
                {
                    yield return address;
                }
            }
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
            await File.WriteAllLinesAsync(filePath, addresses.OrderBy(a => a, StringComparer.OrdinalIgnoreCase), cancellation);
        }

        public async ValueTask SaveReportAsync(string filePath, IDictionary<string, Count> uniqueAddressesPerFile, CancellationToken cancellation = default)
        {
            var reportContent = new StringBuilder("Unique addresses per file:\n");
            
            foreach ((var file, var count) in uniqueAddressesPerFile)
            {
                reportContent.AppendLine($"{file}: {count}");
            }
            
            await File.WriteAllTextAsync(filePath, reportContent.ToString(), cancellation);
        }
    }
}
