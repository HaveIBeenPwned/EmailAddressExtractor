using System.Text.RegularExpressions;

namespace MyAddressExtractor.Objects.Readers
{
    internal sealed class OpenOfficeXmlReader : CompressedXmlReader
    {
        private Regex slideNameRegex = new Regex(@"(word/document\.xml|ppt/slides/slide(\d*)\.xml)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public OpenOfficeXmlReader(string zipPath) : base(zipPath)
        {
        }

        public override bool IsMatch(string entry) => slideNameRegex.IsMatch(entry);
    }
}