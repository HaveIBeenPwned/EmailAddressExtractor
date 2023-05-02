namespace MyAddressExtractor.Objects.Filters {
    public sealed class QuotesFilter : AddressFilter.BaseFilter {
        private static readonly object[] JUNK_CHARS = { '"', '\'', "\\\"" };

        public override string Name => "Filter quotes";

        /// <inheritdoc />
        public override Result ValidateEmailAddress(ref EmailAddress address)
        {
            // Clean up the address
            if (this.Trim(ref address))
                return Result.REVALIDATE;

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

        /// <summary>Remove matching junk opening and closing characters caught by the initial Regex</summary>
        /// <returns>If TRUE will re-run previous filters with the changed value</returns>
        private bool Trim(ref EmailAddress address)
        {
            bool modified = false;
            foreach (object junk in QuotesFilter.JUNK_CHARS) {
                int? len;
                do {
                    len = null;
                    if (junk is string str)
                    {
                        if (address.Full.StartsWith(str) && address.Full.EndsWith(str))
                            len = str.Length;
                    }
                    else if (junk is char c)
                    {
                        if (address.Full.StartsWith(c) && address.Full.EndsWith(c))
                            len = 1;
                    }

                    if (len is not null)
                    {
                        address.Full = address.Full[(Range)(len..^len)];
                        modified = true;
                    }
                } while (len is not null);
            }

            return modified;
        }
    }
}
