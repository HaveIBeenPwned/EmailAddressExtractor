using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using MyAddressExtractor.Objects;
using MyAddressExtractor.Objects.Performance;
using MyAddressExtractor.Objects.Readers;

namespace MyAddressExtractor
{
    public partial class AddressExtractor
    {
        /// <summary>
        /// Email Regex pattern with simple checks and no backtrack
        /// </summary>
        [GeneratedRegex(
            @"(\\"")?""?'?[a-z0-9\.\-\*!#$%&'+/=?^_`{|}~""\\]+@([a-z0-9\-_]+\.)+[a-z0-9]{2,}\b(\\"")?""?'?",
            RegexOptions.ExplicitCapture // Require naming captures; implies '(?:)' on groups. We don't make use of the groups
            | RegexOptions.IgnoreCase // Match upper and lower casing
            | RegexOptions.Compiled // Compile the nodes
            | RegexOptions.Singleline // Singleline mode
            | RegexOptions.NonBacktracking //  guarantees linear-time processing in the length of the input.
            | RegexOptions.CultureInvariant // Allow culture invariant character matching
        )]
        public static partial Regex LooseMatch();

        #region File Extraction

        public IAsyncEnumerable<string> ExtractFileAddressesAsync(ILineReader reader, CancellationToken cancellation = default)
            => this.ExtractFileAddressesAsync(IPerformanceStack.DEFAULT, reader, cancellation);

        public async IAsyncEnumerable<string> ExtractFileAddressesAsync(IPerformanceStack stack, ILineReader reader, [EnumeratorCancellation] CancellationToken cancellation = default)
        {
            await foreach (var line in reader.ReadLineAsync(cancellation))
            {
                stack.Step("Read line");
                await foreach (var address in this.ExtractAddressesAsync(stack, line, cancellation))
                {
                    yield return address;
                }
            }
        }

        #endregion
        #region String Extraction

        public IAsyncEnumerable<string> ExtractAddressesAsync(string? content, CancellationToken cancellation = default)
            => this.ExtractAddressesAsync(IPerformanceStack.DEFAULT, content, cancellation);

        private async IAsyncEnumerable<string> ExtractAddressesAsync(IPerformanceStack stack, string? content, [EnumeratorCancellation] CancellationToken cancellation = default)
        {
            // If line is NULL or any time of whitespace, don't waste computation time searching any empty string
            if (string.IsNullOrWhiteSpace(content))
                yield break;

            var matches = AddressExtractor.LooseMatch()
                .Matches(content);

            using (var debug = stack.CreateStack("Run regex"))
            {
                foreach (Match match in matches)
                {
                    var address = new EmailAddress(match);
                    debug.Step("Generate capture");

                    var valid = Result.ALLOW;
                    while (true)
                    {
                        // Run each filter
                        foreach (var filter in AddressFilter.Filters)
                        {
                            valid = await filter.ValidateEmailAddressAsync(ref address, cancellation);
                            debug.Step(filter.Name);

                            if (valid is not Result.CONTINUE)
                                break;
                        }

                        // Only break if a result has been formed
                        if (valid is not Result.REVALIDATE)
                            break;
                    }

                    if (valid is Result.DENY)
                        continue;

                    yield return address.Full;
                }
            }
        }

        #endregion
    }
}
