using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using HaveIBeenPwned.AddressExtractor.Objects.Readers;

namespace HaveIBeenPwned.AddressExtractor;

public partial class AddressExtractor
{
    /// <summary>
    /// Email Regex pattern with simple checks and no backtrack
    /// </summary>
    [GeneratedRegex(
        """(\\[trn])?[a-z0-9\.\-+_]+@[a-z0-9\-\.]{3,}""",
        RegexOptions.ExplicitCapture // Require naming captures; implies '(?:)' on groups. We don't make use of the groups
        | RegexOptions.IgnoreCase // Match upper and lower casing
        | RegexOptions.Compiled // Compile the nodes
        | RegexOptions.Singleline // Singleline mode
        | RegexOptions.NonBacktracking //  guarantees linear-time processing in the length of the input.
        | RegexOptions.CultureInvariant // Allow culture invariant character matching
    )]
    public static partial Regex LooseMatch();

    #region File Extraction
    public static async IAsyncEnumerable<string> ExtractFileAddressesAsync(ILineReader reader, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        await foreach (var line in reader.ReadLineAsync(cancellation).ConfigureAwait(false))
        {
            cancellation.ThrowIfCancellationRequested();
            foreach (var address in ExtractAddresses(line))
            {
                yield return address;
            }
        }
    }

    #endregion
    #region String Extraction
    public static HashSet<string> ExtractAddresses(ReadOnlySpan<char> content)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var enumerator = LooseMatch().EnumerateMatches(content);
        while (enumerator.MoveNext())
        {
            if (TryValidateEmail(content.Slice(enumerator.Current.Index, enumerator.Current.Length), out var result) && !set.Contains(result))
            {
                set.Add(result);
            }
        }

        return set;
    }

    private static bool TryValidateEmail(ReadOnlySpan<char> match, [NotNullWhen(true)] out string? result)
    {
        if (match[0] == '\\')
        {
            // Skip the first two characters
            match = match[2..];
        }

        if (EmailValidation.IsValidEmail(match))
        {
            result = new string(match).ToLowerInvariant();
            return true;
        }

        result = null;
        return false;
    }

    #endregion
}
