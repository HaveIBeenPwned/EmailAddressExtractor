using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Xml;

namespace MyAddressExtractor.Objects.Readers 
{
    internal sealed class OpenDocumentTextReader : ILineReader
    {
        private readonly string zipPath;

        public OpenDocumentTextReader(string zipPath)
        {
            this.zipPath = zipPath;
        }

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;

        public async IAsyncEnumerable<string?> ReadLineAsync([EnumeratorCancellation] CancellationToken cancellation = default)
        {
            using (ZipArchive archive = ZipFile.OpenRead(this.zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.Equals("content.xml", StringComparison.OrdinalIgnoreCase) ||
                        entry.FullName.Equals("word/document.xml", StringComparison.OrdinalIgnoreCase))
                    {
                        XmlReaderSettings settings = new XmlReaderSettings();
                        settings.Async = true;

                        using(var reader = XmlReader.Create(entry.Open(), settings))
                        {
                            while(await reader.ReadAsync())
                            {
                                if (reader.NodeType == XmlNodeType.Text ||
                                    reader.NodeType == XmlNodeType.CDATA)
                                {
                                    yield return await reader.ReadContentAsStringAsync();
                                }
                            }
                        }

                        yield break;
                    }
                }
            }
            
            throw new Exception($"Unable to load content.xml from '{this.zipPath}'");
        }
    }
}