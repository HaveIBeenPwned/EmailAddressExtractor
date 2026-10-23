using System.Text.RegularExpressions;

using HaveIBeenPwned.AddressExtractor.Objects.Attributes;

namespace HaveIBeenPwned.AddressExtractor.Objects.Readers;

/// <summary>Open Office XML - ISO 29500</summary>
[ExtensionTypes(".docx")]
internal sealed partial class OpenOfficeXmlReader(string zipPath) : CompressedXmlReader(zipPath)
{
    [GeneratedRegex(@"word/document\.xml", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex DocumentNameRegex();

    public override bool IsMatch(string entry)
        => DocumentNameRegex()
            .IsMatch(entry);
}
