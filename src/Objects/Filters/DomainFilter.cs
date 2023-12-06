using HaveIBeenPwned.AddressExtractor.Objects.Attributes;

namespace HaveIBeenPwned.AddressExtractor.Objects.Filters {
    [AddressFilter(Priority = 900)]
    public sealed class DomainFilter : AddressFilter.BaseFilter {
        public override string Name => "Domain filter";

        /// <inheritdoc />
        public override Result ValidateEmailAddress(ref EmailAddress address)
        {
            var domain = address.Domain;

            // Handle cases such as: foo@bar.1com, foo@bar.12com
            if (char.IsNumber(domain[domain.LastIndexOf('.')+1]))
                return Result.DENY;

            // Handle cases such as: foobar@_.com
            if (domain[1..domain.LastIndexOf('.')] == "_")
                return Result.DENY;

            // Handle cases such as: username@-example-.com and username@example-.com
            if (domain.Contains("-."))
                return Result.DENY;

            return Result.CONTINUE;
        }
    }
}
