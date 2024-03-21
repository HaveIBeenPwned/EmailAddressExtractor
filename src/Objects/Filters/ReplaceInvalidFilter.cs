using System.Text.RegularExpressions;

namespace HaveIBeenPwned.AddressExtractor.Objects.Filters {
    /// <summary>
    /// Checks if the full email starts with specifically illegal characters and trims them until there are no more illegal characters.    /// </summary>
    public partial class ReplaceInvalidFilter : AddressFilter.BaseFilter {
        [GeneratedRegex(@"^['!`\{#\\n\\\\]+(.*)")]
        public static partial Regex StartsWithCharacter();

        /// <inheritdoc />
        public override string Name => "TrimIllegalStartChars";

        /// <inheritdoc />
        public override Result ValidateEmailAddress(ref EmailAddress address) {
            Match match = ReplaceInvalidFilter.StartsWithCharacter()
                .Match(address.Full);
            
            if ( match is not { Length: > 0 } )
                return Result.CONTINUE;
            
            address.Full = match.Groups[1].Value;
            
            // If the email is now empty, it was only consisting of illegal characters
            return address.Length is 0 ? Result.DENY : Result.REVALIDATE;

        }
        
        
    }
}
