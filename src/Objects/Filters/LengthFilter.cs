using MyAddressExtractor.Objects.Attributes;

namespace MyAddressExtractor.Objects.Filters {
    [AddressFilter(Priority = 1000)]
    public sealed class LengthFilter : AddressFilter.BaseFilter {
        public override string Name => "Check length";

        /// <inheritdoc />
        public override Result ValidateEmailAddress(ref EmailAddress address)
            // Use the match position to validate too long of a length so we don't have to allocate the substring
            => this.Continue(address.Length <= 255);
    }
}
