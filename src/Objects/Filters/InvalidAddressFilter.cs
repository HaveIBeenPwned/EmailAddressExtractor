using System.Text.RegularExpressions;
using MyAddressExtractor.Objects.Attributes;

namespace MyAddressExtractor.Objects.Filters {
    /// <summary>
    /// A negative-regex pattern for filtering out bad emails
    /// </summary>
    [AddressFilter(Priority = 800)]
    public partial class InvalidAddressFilter : AddressFilter.BaseFilter {
        /// <summary>
        /// \.\.	Email having consecutive dot
        /// \*	    Email having *
        /// .@	    Email having .@
        /// @-	    Email having @-
        /// </summary>
        [GeneratedRegex(
            @"\.\.|\*|\.@|^\.|@-",
            RegexOptions.ExplicitCapture // Require naming captures; implies '(?:)' on groups. We don't make use of the groups
            | RegexOptions.IgnoreCase // Match upper and lower casing
            | RegexOptions.Compiled // Compile the nodes
            | RegexOptions.Singleline // Singleline mode
        )]
        public static partial Regex InvalidEmailRegex();

        public override string Name => "Filter invalids";

        /// <inheritdoc />
        public override Result ValidateEmailAddress(ref EmailAddress address)
            => this.Continue(!InvalidAddressFilter.InvalidEmailRegex()
                .IsMatch(address.Full));
    }
}
