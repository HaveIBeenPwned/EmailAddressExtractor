using System.Net;

namespace HaveIBeenPwned.AddressExtractor;

public static class EmailValidation
{
    private static readonly Lazy<string[]> _validTlds = new(FetchTlds, true);
    private static readonly char[] _separators = ['\r', '\n'];

    private static string[] FetchTlds()
    {
        var content = new HttpClient().GetStringAsync("http://data.iana.org/TLD/tlds-alpha-by-domain.txt").GetAwaiter().GetResult();
        return content.Split(_separators, StringSplitOptions.RemoveEmptyEntries);
    }

    public static bool IsValidEmail(string email) => IsValidEmail(email.AsSpan());

    public static bool IsValidEmail(ReadOnlySpan<char> email)
    {
        if (email.Count('@') != 1 || email.Length < 3 || email.Length > 255)
        {
            return false;
        }

        Span<char> lowerEmail = stackalloc char[email.Length];
        email.ToLowerInvariant(lowerEmail);
        ReadOnlySpan<char> readOnlyLower = lowerEmail;
        Span<Range> ranges = stackalloc Range[2];
        readOnlyLower.Split(ranges, '@');

        if (!IsAliasValid(readOnlyLower[ranges[0]]) || !IsDomainValid(readOnlyLower[ranges[1]]))
        {
            return false;
        }

        return true;
    }

    public static bool IsDomainValid(ReadOnlySpan<char> domain)
    {
        // Domain must have at least three characters
        if (domain.Length < 3)
        {
            return false;
        }

        // First and last characters must be alphanumeric
        if (!IsCharAlphanumeric(domain[0]) || !IsCharAlphanumeric(domain[^1]))
        {
            return false;
        }

        // Disallow ip address as domain
        if (IPAddress.TryParse(domain, out _))
        {
            return false;
        }

        // Domain must contain at least one dot
        if (domain.IndexOf('.') == -1)
        {
            return false;
        }

        // We've already checked the first and last characters
        for (var i = 1; i < domain.Length - 1; i++)
        {
            // Must be alphanumeric or a valid non-alphanumeric character
            if (!IsCharValidInDomain(domain[i]))
            {
                return false;
            }

            // Can't have two non-alphanumeric characters in a row
            if (IsNonAlphaCharValidInDomain(domain[i]) && IsNonAlphaCharValidInDomain(domain[i + 1]))
            {
                return false;
            }
        }

        // Let's make sure we have a valid hostname
        if (Uri.CheckHostName(domain.ToString()) != UriHostNameType.Dns)
        {
            return false;
        }

        // Let's make sure the TLD is valid
        var tld = domain[(domain.LastIndexOf('.') + 1)..];
        if (!_validTlds.Value.Contains(tld.ToString().ToUpperInvariant()))
        {
            return false;
        }

        return true;
    }

    public static bool IsAliasValid(ReadOnlySpan<char> alias)
    {
        // Alias must be between 1 and 64 characters
        if (alias.Length > 64 || alias.Length < 1)
        {
            return false;
        }

        // First and last characters must be alphanumeric
        if (!IsCharAlphanumeric(alias[0]) || !IsCharAlphanumeric(alias[^1]))
        {
            return false;
        }

        // We've already checked the first and last characters
        for (var i = 1; i < alias.Length - 1; i++)
        {
            // Must be alphanumeric or a valid non-alphanumeric character
            if (!IsCharValidInAlias(alias[i]))
            {
                return false;
            }

            // Can't have two non-alphanumeric characters in a row
            if (IsNonAlphaCharValidInAlias(alias[i]) && IsNonAlphaCharValidInAlias(alias[i + 1]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsCharAlphanumeric(char input)
    {
        return (input >= 'a' && input <= 'z') || (input >= 'A' && input <= 'Z') || (input >= '0' && input <= '9');
    }

    private static bool IsCharValidInDomain(char input)
    {
        return IsCharAlphanumeric(input) || IsNonAlphaCharValidInDomain(input);
    }

    // List of valid non-alphanumeric characters in a domain alias
    private static bool IsNonAlphaCharValidInAlias(char input)
    {
        return input == '.' || input == '-' || input == '_' || input == '+';
    }

    // List of valid non-alphanumeric characters in a domain
    private static bool IsNonAlphaCharValidInDomain(char input)
    {
        return input == '.' || input == '-';
    }

    private static bool IsCharValidInAlias(char input)
    {
        return IsCharAlphanumeric(input) || IsNonAlphaCharValidInAlias(input);
    }
}
