using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Xml;

namespace HaveIBeenPwned.AddressExtractor.Objects.Readers;

internal abstract class CompressedXmlReader(string zipPath) : ILineReader
{
    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    public async IAsyncEnumerable<string?> ReadLineAsync([EnumeratorCancellation] CancellationToken cancellation = default)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            if (IsMatch(entry.FullName))
            {
                var settings = new XmlReaderSettings();
                settings.Async = true;

                using var reader = XmlReader.Create(entry.Open(), settings);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    if (reader.NodeType == XmlNodeType.Text ||
                        reader.NodeType == XmlNodeType.CDATA)
                    {
                        yield return await reader.ReadContentAsStringAsync().ConfigureAwait(false);
                    }
                }
            }
        }
    }

    public abstract bool IsMatch(string entry);
}
