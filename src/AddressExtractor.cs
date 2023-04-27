using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using MyAddressExtractor.Objects.Readers;

namespace MyAddressExtractor
{
    public partial class AddressExtractor
    {
        /// <summary>
        /// Email Regex pattern
        /// </summary>
        [GeneratedRegex(@"(\\"")?""?'?[a-z0-9\.\-\*!#$%&'+/=?^_`{|}~""\\]+(?<!\.)@([a-z0-9\-_]+\.)+[a-z0-9]{2,}\b(\\"")?""?'?(?<!\s)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)]
        public static partial Regex EmailRegex();

        /// <summary>
        /// A negative-regex pattern for filtering out bad emails
        /// </summary>
        /// <remarks>
        /// \.\.	Email having consecutive dot
        /// \*	    Email having *
        /// .@	    Email having .@
        /// @-	    Email having @-
        /// </remarks>
        [GeneratedRegex(@"\.\.|\*|\.@|^\.|@-", RegexOptions.Compiled)]
        public static partial Regex InvalidEmailRegex();

        public async IAsyncEnumerable<string> ExtractAddressesAsync(ILineReader reader, [EnumeratorCancellation] CancellationToken cancellation = default)
        {
            await foreach (var line in reader.ReadLineAsync(cancellation))
            {
                foreach (var address in this.ExtractAddresses(line))
                {
                    yield return address;
                }
            }
        }

        public IEnumerable<string> ExtractAddresses(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                yield break;
            }
            var matches = AddressExtractor.EmailRegex()
                .Matches(content);

            foreach (Match match in matches) {
                string email = match.Value;

                // Handle quotes at the start and end of the match
                if (email.StartsWith('\'') && email.EndsWith('\''))
                    email = email[1..^1];
                if (email.StartsWith('"') && email.EndsWith('"'))
                    email = email[1..^1];
                if (email.StartsWith("\\\"") && email.EndsWith("\\\""))
                    email = email[2..^2];

                // The regex cannot enforce length restrictions
                if (email.Length >= 256)
                    continue;

                // Filter out edge case addresses
                if (AddressExtractor.InvalidEmailRegex().IsMatch(email))
                {
                    System.Diagnostics.Debug.WriteLine(email);
                    continue;
                }

                // Is there a single backslash enclosed in quotes? if yes, that's ok. A single backslash without quotes is not
                var username = email[0..email.LastIndexOf('@')];
                // Handle quotes at the start and end of the username
                if (username.StartsWith('\'') && username.EndsWith('\''))
                    username = username[1..^1];
                if (username.StartsWith('"') && username.EndsWith('"'))
                    username = username[1..^1];
                if (username.StartsWith("\\\"") && username.EndsWith("\\\""))
                    username = username[2..^2];

                // Does the username contain any unescaped double quotes?
                var quoteIndex = username.IndexOf('"');
                int unescapedQuoteCount = 0;
                bool passed = true;
                while (passed && quoteIndex != -1)
                {
                    if (quoteIndex == 0 || (quoteIndex > 0 && username[quoteIndex - 1] != '\\'))
                    {
                        unescapedQuoteCount++;
                    }
                    quoteIndex = username.IndexOf('"', quoteIndex + 1);
                }

                // Does it contain mismatched (an odd number) quotes?
                if ((unescapedQuoteCount & 1) == 1)
                    continue;

                var domain = email[email.LastIndexOf('@')..];
                // Handle cases such as: foo@bar.1com, foo@bar.12com
                if (char.IsNumber(domain[domain.LastIndexOf('.')+1]))
                    continue;
                // Handle cases such as: foobar@_.com
                if (domain[1..domain.LastIndexOf('.')] == "_")
                    continue;
                 // Handle cases such as: username@-example-.com and username@example-.com
                if (domain.Contains("-."))
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
