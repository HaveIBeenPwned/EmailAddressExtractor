using MyAddressExtractor.Objects.Attributes;

namespace MyAddressExtractor.Objects.Filters {
    /// <summary>
    /// Parse out bad emails in the
    /// </summary>
    [AddressFilter(Priority = 1000)]
    public sealed class LengthFilter : AddressFilter.BaseFilter {
        /// <summary>The entire email address (alias + domain) should not be any longer than this</summary>
        public const int TOTAL_LENGTH = 255;

        /// <summary>Email Aliases should not be any longer than this</summary>
        public const int ALIAS_LENGTH = 64;

        public override string Name => "Check length";

        /// <inheritdoc />
        public override Result ValidateEmailAddress(ref EmailAddress address)
            // Use the match position to validate too long of a length so we don't have to allocate the substring
            => this.Continue(
                address.Length <= LengthFilter.TOTAL_LENGTH
                &&
                address.Username.Length <= LengthFilter.ALIAS_LENGTH
            );
    }
}
