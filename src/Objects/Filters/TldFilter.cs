namespace MyAddressExtractor.Objects.Filters {
    public sealed class TldFilter : AddressFilter.BaseFilter {
        public override string Name => "TLD Filter";

        /// <inheritdoc />
        public override Result ValidateEmailAddress(EmailAddress address)
            => Result.CONTINUE; // TODO: Implement a handler to check address.Domain's TLD
    }
}
