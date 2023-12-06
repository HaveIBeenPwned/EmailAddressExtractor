using System.Text.RegularExpressions;
using HaveIBeenPwned.AddressExtractor.Objects.Attributes;

namespace HaveIBeenPwned.AddressExtractor.Objects.Filters {
    [AddressFilter(Priority = 990)]
    public sealed partial class StrictMatchFilter : AddressFilter.BaseFilter
    {
        /// <summary>
        /// Email Regex pattern with full complex checks
        /// </summary>
        [GeneratedRegex(
            @"(\\"")?""?'?[a-z0-9\.\-\*!#$%&'+/=?^_`{|}~""\\]+(?<!\.)@([a-z0-9\-_]+\.)+[a-z0-9]{2,}\b(\\"")?""?'?(?<!\s)",
            RegexOptions.ExplicitCapture // Require naming captures; implies '(?:)' on groups. We don't make use of the groups
            | RegexOptions.IgnoreCase // Match upper and lower casing
            | RegexOptions.Compiled // Compile the nodes
            | RegexOptions.Singleline // Singleline mode
            | RegexOptions.CultureInvariant // Allow culture invariant character matching
        )]
        public static partial Regex StrictRegex();

        public override string Name => "Strict Match";

        /// <inheritdoc />
        public override Result ValidateEmailAddress(ref EmailAddress address)
            => this.Continue(StrictMatchFilter.StrictRegex()
                .IsMatch(address.Full));
    }
}
