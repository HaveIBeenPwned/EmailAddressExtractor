using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace MyAddressExtractor
{
    public partial class AddressExtractor
    {
        [GeneratedRegex(@"[a-z0-9\.\-!#$%&'+-/=?^_`{|}~""\\]+(?<!\.)@([a-z0-9\-_]+\.)+[a-z0-9]{2,}\b(?<!\s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)]
        public static partial Regex EmailRegex();
        
        public async IAsyncEnumerable<string> ExtractAddressesFromFileAsync(string inputFilePath, [EnumeratorCancellation] CancellationToken cancellation = default)
        {
            await using (var reader = new FileStream(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, FileOptions.SequentialScan | FileOptions.Asynchronous))
            {
                using (var stream = new StreamReader(reader, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096))
                {
                    while (await stream.ReadLineAsync(cancellation) is {Length: >0} line)
                    {
                        foreach (var address in this.ExtractAddresses(line))
                        {
                            yield return address;
                        }
                    }
                }
            }
        }

        public IEnumerable<string> ExtractAddresses(string content)
        {
            var matches = AddressExtractor.EmailRegex()
                .Matches(content);

            foreach (Match match in matches) {
                string email = match.Value;

                if (email.Contains('*'))
                    continue;
                if (email.Contains(".."))
                    continue;
                if (email.Contains(".@"))
                    continue;
                if (email.Length >= 256)
                    continue;
                // Handle cases such as: foobar@_.com, oobar@f_b.com
                if (email[email.LastIndexOf('@')..].Contains('_'))
                    continue;
                // Handle cases such as: foo@bar.1com, foo@bar.12com
                if (int.TryParse(email[email.LastIndexOf('.')+1].ToString(), out _))
                    continue;
                if (email.StartsWith("."))
                    continue;
                 // Handle cases such as: username@-example-.com , username@-example.com and username@example-.com
                if (email.Contains("@-") || email[email.LastIndexOf('@')..].Contains("-."))
                    continue;
                    
                yield return email;
            }
        }

        public async ValueTask SaveAddressesAsync(string filePath, IEnumerable<string> addresses, CancellationToken cancellation = default)
        {
            await File.WriteAllLinesAsync(
                filePath,
                addresses.Select(address => address.ToLowerInvariant())
                    .OrderBy(address => address, StringComparer.OrdinalIgnoreCase),
                cancellation
            );
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
