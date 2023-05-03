using System.Text.RegularExpressions;
using MyAddressExtractor.Objects.Attributes;

namespace MyAddressExtractor.Objects.Readers
{
    /// <summary>Open Office XML - ISO 29500</summary>
    [ExtensionTypes(".docx", ".pptx")]
    internal sealed partial class OpenOfficeXmlReader : CompressedXmlReader
    {
        [GeneratedRegex(@"(word/document\.xml|ppt/slides/slide(\d*)\.xml)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex SlideNameRegex();

        public OpenOfficeXmlReader(string zipPath) : base(zipPath)
        {
        }

        public override bool IsMatch(string entry)
            => OpenOfficeXmlReader.SlideNameRegex()
                .IsMatch(entry);
    }
}