namespace MyAddressExtractor.Objects.Filters {
    public sealed class QuotesFilter : AddressFilter.BaseFilter {
        public override string Name => "Filter quotes";

        /// <inheritdoc />
        public override Result ValidateEmailAddress(EmailAddress address)
        {
            // Is there a single backslash enclosed in quotes? if yes, that's ok. A single backslash without quotes is not
            var username = address.Username;

            // Handle quotes at the start and end of the username
            if (username.StartsWith('\'') && username.EndsWith('\''))
                username = username[1..^1];
            if (username.StartsWith('"') && username.EndsWith('"'))
                username = username[1..^1];
            if (username.StartsWith("\\\"") && username.EndsWith("\\\""))
                username = username[2..^2];

            // Does the username contain any unescaped double quotes?
            var quoteIndex = username.IndexOf('"');
            var unescapedQuoteCount = 0;

            while (quoteIndex != -1)
            {
                if (quoteIndex == 0 || (quoteIndex > 0 && username[quoteIndex - 1] != '\\'))
                {
                    unescapedQuoteCount++;
                }
                quoteIndex = username.IndexOf('"', quoteIndex + 1);
            }

            // Does it contain mismatched (an odd number) quotes?
            return this.Continue((unescapedQuoteCount & 1) != 1);
        }
    }
}
