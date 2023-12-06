using HaveIBeenPwned.AddressExtractor.Objects.Attributes;

namespace HaveIBeenPwned.AddressExtractor.Objects.Readers
{
    /// <summary>Open Document - ISO 26300</summary>
    [ExtensionTypes(".odt")]
    internal sealed class OpenDocumentXmlReader : CompressedXmlReader
    {
        public OpenDocumentXmlReader(string zipPath) : base(zipPath)
        {
        }

        public override bool IsMatch(string entry) => entry.Equals("content.xml", StringComparison.OrdinalIgnoreCase);
    }
}