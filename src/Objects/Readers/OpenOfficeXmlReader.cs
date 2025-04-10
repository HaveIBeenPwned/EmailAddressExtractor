using System.Text.RegularExpressions;

using HaveIBeenPwned.AddressExtractor.Objects.Attributes;

namespace HaveIBeenPwned.AddressExtractor.Objects.Readers;

/// <summary>Open Office XML - ISO 29500</summary>
[ExtensionTypes(".docx", ".pptx")]
internal sealed partial class OpenOfficeXmlReader(string zipPath) : CompressedXmlReader(zipPath)
{
    [GeneratedRegex(@"(word/document\.xml|ppt/slides/slide(\d*)\.xml)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex SlideNameRegex();

    public override bool IsMatch(string entry)
        => SlideNameRegex()
            .IsMatch(entry);
}
