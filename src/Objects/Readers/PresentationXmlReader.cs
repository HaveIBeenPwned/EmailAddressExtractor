using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

using HaveIBeenPwned.AddressExtractor.Objects.Attributes;

namespace HaveIBeenPwned.AddressExtractor.Objects.Readers;

[ExtensionTypes(".pptx")]
internal sealed class PresentationXmlReader(string path) : ILineReader
{
    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    public async IAsyncEnumerable<string?> ReadLineAsync([EnumeratorCancellation] CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        using var archive = ZipFile.OpenRead(path);
        foreach (var entry in archive.Entries)
        {
            cancellation.ThrowIfCancellationRequested();
            if (!IsSlideOrNotes(entry.FullName))
            {
                continue;
            }

            using var stream = entry.Open();
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { Async = true });
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                cancellation.ThrowIfCancellationRequested();
                if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "p"
                    || reader.NamespaceURI is not ("http://schemas.openxmlformats.org/drawingml/2006/main"
                        or "http://purl.oclc.org/ooxml/drawingml/main"))
                {
                    continue;
                }

                using var subtree = reader.ReadSubtree();
                var paragraph = await XElement.LoadAsync(subtree, LoadOptions.PreserveWhitespace, cancellation).ConfigureAwait(false);
                var ns = paragraph.Name.Namespace;
                // Formatting runs belong to one paragraph; explicit breaks must not join addresses.
                yield return string.Concat(paragraph.Descendants()
                    .Where(element => element.Name == ns + "t" || element.Name == ns + "br")
                    .Select(element => element.Name == ns + "br" ? "\n" : element.Value));
            }
        }
    }

    private static bool IsSlideOrNotes(string name)
    {
        var prefix = name.StartsWith("ppt/slides/", StringComparison.OrdinalIgnoreCase) ? "ppt/slides/"
            : name.StartsWith("ppt/notesSlides/", StringComparison.OrdinalIgnoreCase) ? "ppt/notesSlides/"
            : null;
        return prefix is not null && name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
            && !name.AsSpan(prefix.Length).Contains('/');
    }
}
