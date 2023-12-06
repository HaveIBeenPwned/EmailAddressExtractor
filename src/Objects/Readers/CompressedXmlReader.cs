using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Xml;

namespace HaveIBeenPwned.AddressExtractor.Objects.Readers
{
    internal abstract class CompressedXmlReader : ILineReader
    {
        private readonly string zipPath;

        public CompressedXmlReader(string zipPath)
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
                    if (this.IsMatch(entry.FullName))
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
                    }
                }
            }
        }

        public abstract bool IsMatch(string entry);
    }
}