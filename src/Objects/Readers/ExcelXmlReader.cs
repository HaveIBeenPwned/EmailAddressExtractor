using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

using HaveIBeenPwned.AddressExtractor.Objects.Attributes;

namespace HaveIBeenPwned.AddressExtractor.Objects.Readers;

[ExtensionTypes(".xlsx")]
internal sealed class ExcelXmlReader(string zipPath) : ILineReader
{
    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    public async IAsyncEnumerable<string?> ReadLineAsync([EnumeratorCancellation] CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            cancellation.ThrowIfCancellationRequested();
            var sharedStrings = entry.FullName.Equals("xl/sharedStrings.xml", StringComparison.OrdinalIgnoreCase);
            var worksheet = entry.FullName.StartsWith("xl/worksheets/", StringComparison.OrdinalIgnoreCase)
                && entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                && !entry.FullName["xl/worksheets/".Length..].Contains('/');
            if (!sharedStrings && !worksheet)
            {
                continue;
            }

            using var stream = entry.Open();
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                cancellation.ThrowIfCancellationRequested();
                if (reader.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                var cellType = reader.GetAttribute("t");
                if ((sharedStrings && reader.LocalName == "si")
                    || (worksheet && reader.LocalName == "c" && cellType is "inlineStr" or "str"))
                {
                    using var subtree = reader.ReadSubtree();
                    var element = await XElement.LoadAsync(subtree, LoadOptions.PreserveWhitespace, cancellation).ConfigureAwait(false);
                    var ns = element.Name.Namespace;
                    if (worksheet && cellType == "str")
                    {
                        // Read cached formula results, not the formula expression itself.
                        yield return element.Element(ns + "v")?.Value;
                    }
                    else
                    {
                        // Rich-text runs form one string; phonetic annotations are not cell text.
                        yield return string.Concat(element.Descendants(ns + "t")
                            .Where(text => text.Parent?.Name != ns + "rPh")
                            .Select(text => text.Value));
                    }
                }
            }
        }
    }
}
