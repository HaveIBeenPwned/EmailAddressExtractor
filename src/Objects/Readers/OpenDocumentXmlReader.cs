namespace MyAddressExtractor.Objects.Readers
{
    internal sealed class OpenDocumentXmlReader : CompressedXmlReader
    {
        public OpenDocumentXmlReader(string zipPath) : base(zipPath)
        {
        }

        public override bool IsMatch(string entry) => entry.Equals("content.xml", StringComparison.OrdinalIgnoreCase);
    }
}