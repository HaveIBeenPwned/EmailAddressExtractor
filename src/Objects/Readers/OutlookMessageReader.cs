using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

using HaveIBeenPwned.AddressExtractor.Objects.Attributes;

using MimeKit.IO;
using MimeKit.Text;
using MimeKit.Tnef;
using MimeKit.Utils;
using OpenMcdf;
using RtfPipe;

namespace HaveIBeenPwned.AddressExtractor.Objects.Readers;

[ExtensionTypes(".msg")]
internal sealed class OutlookMessageReader(string path) : ILineReader
{
    private const string PropertiesStream = "__properties_version1.0";
    private const string PropertyPrefix = "__substg1.0_";
    private static readonly Encoding UnicodeEncoding = new UnicodeEncoding(false, false, true);

    static OutlookMessageReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    public async IAsyncEnumerable<string?> ReadLineAsync([EnumeratorCancellation] CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        using var root = RootStorage.OpenRead(path);
        var pending = new Stack<(Storage Storage, int HeaderSize, Encoding Encoding)>();
        pending.Push((root, 32, Encoding.GetEncoding(1252)));
        while (pending.TryPop(out var item))
        {
            cancellation.ThrowIfCancellationRequested();
            var (encoding, bodyEncoding) = await ReadEncodingsAsync(item.Storage, item.HeaderSize, item.Encoding, cancellation).ConfigureAwait(false);
            foreach (var entry in item.Storage.EnumerateEntries())
            {
                cancellation.ThrowIfCancellationRequested();
                if (entry.Type == EntryType.Storage)
                {
                    if (entry.Name.StartsWith("__recip_version1.0_", StringComparison.OrdinalIgnoreCase)
                        || entry.Name.StartsWith("__attach_version1.0_", StringComparison.OrdinalIgnoreCase))
                    {
                        pending.Push((item.Storage.OpenStorage(entry.Name), 8, encoding));
                    }
                    else if (entry.Name.Equals("__substg1.0_3701000D", StringComparison.OrdinalIgnoreCase))
                    {
                        pending.Push((item.Storage.OpenStorage(entry.Name), 24, encoding));
                    }

                    continue;
                }

                if (entry.Name.Length != PropertyPrefix.Length + 8
                    || !entry.Name.StartsWith(PropertyPrefix, StringComparison.OrdinalIgnoreCase)
                    || !uint.TryParse(entry.Name.AsSpan(PropertyPrefix.Length), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var tag))
                {
                    continue;
                }

                var type = tag & 0xFFFF;
                if (type is not (0x001E or 0x001F) && tag is not (0x10130102 or 0x10090102))
                {
                    continue;
                }

                using var stream = item.Storage.OpenStream(entry.Name);
                if (tag == 0x10090102)
                {
                    var header = new byte[16];
                    await stream.ReadExactlyAsync(header, cancellation).ConfigureAwait(false);
                    var compressedSize = BinaryPrimitives.ReadUInt32LittleEndian(header);
                    var rawSize = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4));
                    var magic = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(8));
                    if (compressedSize != stream.Length - 4 || compressedSize > int.MaxValue || rawSize > int.MaxValue
                        || magic is not (0x75465A4C or 0x414C454D)
                        || (magic == 0x414C454D && rawSize != stream.Length - 16)
                        || rawSize > (stream.Length - 16) * 17)
                    {
                        throw new InvalidDataException("Invalid compressed RTF header in Outlook message.");
                    }

                    stream.Position = 0;
                    using var filtered = new FilteredStream(stream);
                    var filter = new RtfCompressedToRtf();
                    filtered.Add(filter);
                    using var rtfReader = new StreamReader(filtered, Encoding.Latin1);
                    using var cancellable = new CancellableReader(rtfReader, cancellation);
                    var html = Rtf.ToHtml(cancellable);
                    // Ensure the decompressor sees the end of the stream before checking its checksum.
                    var remaining = new char[1024];
                    while (await rtfReader.ReadAsync(remaining, cancellation).ConfigureAwait(false) > 0)
                    {
                        cancellation.ThrowIfCancellationRequested();
                    }
                    if (magic == 0x75465A4C && !filter.IsValidCrc32)
                    {
                        throw new InvalidDataException("Invalid compressed RTF body in Outlook message.");
                    }

                    using var htmlReader = new StringReader(html);
                    foreach (var line in ReadHtml(htmlReader, cancellation))
                    {
                        yield return line;
                    }
                }
                else
                {
                    var textEncoding = type == 0x001F ? UnicodeEncoding
                        : tag >> 16 is 0x1000 or 0x1013 ? bodyEncoding : encoding;
                    using var reader = new StreamReader(stream, textEncoding, detectEncodingFromByteOrderMarks: true);
                    if (tag >> 16 == 0x1013)
                    {
                        foreach (var line in ReadHtml(reader, cancellation))
                        {
                            yield return line;
                        }
                    }
                    else
                    {
                        while (true)
                        {
                            cancellation.ThrowIfCancellationRequested();
                            var line = await reader.ReadLineAsync(cancellation).ConfigureAwait(false);
                            if (line is null)
                            {
                                break;
                            }

                            line = line.TrimEnd('\0');
                            yield return tag >> 16 == 0x007D ? Rfc2047.DecodeText(Encoding.UTF8.GetBytes(line)) : line;
                        }
                    }
                }
            }
        }
    }

    private static async ValueTask<(Encoding Message, Encoding Body)> ReadEncodingsAsync(Storage storage, int headerSize,
        Encoding inherited, CancellationToken cancellation)
    {
        using var properties = storage.OpenStream(PropertiesStream);
        if (properties.Length < headerSize || (properties.Length - headerSize) % 16 != 0)
        {
            throw new InvalidDataException("Invalid Outlook message properties stream.");
        }

        properties.Position = headerSize;
        var record = new byte[16];
        var messageEncoding = inherited;
        Encoding? bodyEncoding = null;
        while (properties.Position < properties.Length)
        {
            await properties.ReadExactlyAsync(record, cancellation).ConfigureAwait(false);
            var tag = BinaryPrimitives.ReadUInt32LittleEndian(record);
            if (tag is 0x3FFD0003 or 0x3FDE0003)
            {
                var codePage = BinaryPrimitives.ReadInt32LittleEndian(record.AsSpan(8));
                if (codePage == 0)
                {
                    continue;
                }

                var encoding = Encoding.GetEncoding(codePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
                if (tag == 0x3FFD0003)
                {
                    messageEncoding = encoding;
                }
                else
                {
                    bodyEncoding = encoding;
                }
            }
        }

        return (messageEncoding, bodyEncoding ?? messageEncoding);
    }

    private static IEnumerable<string> ReadHtml(TextReader reader, CancellationToken cancellation)
    {
        using var cancellable = new CancellableReader(reader, cancellation);
        var tokenizer = new HtmlTokenizer(cancellable) { DecodeCharacterReferences = true };
        var paragraph = new StringBuilder();
        while (tokenizer.ReadNextToken(out var token))
        {
            cancellation.ThrowIfCancellationRequested();
            if (token is HtmlDataToken data)
            {
                paragraph.Append(data.Data);
            }
            else if (token is HtmlTagToken tag)
            {
                foreach (var attribute in tag.Attributes)
                {
                    if (attribute.Name.Equals("href", StringComparison.OrdinalIgnoreCase) && attribute.Value is { } value)
                    {
                        yield return WebUtility.HtmlDecode(value);
                    }
                }

                if (tag.Name.ToLowerInvariant() is "p" or "div" or "br" or "hr" or "li" or "tr" or "td" or "th"
                    or "h1" or "h2" or "h3" or "h4" or "h5" or "h6" or "blockquote" or "pre")
                {
                    yield return paragraph.ToString();
                    paragraph.Clear();
                }
            }
        }

        if (paragraph.Length > 0)
        {
            yield return paragraph.ToString();
        }
    }

    private sealed class CancellableReader(TextReader reader, CancellationToken cancellation) : TextReader
    {
        public override int Peek()
        {
            cancellation.ThrowIfCancellationRequested();
            return reader.Peek();
        }

        public override int Read()
        {
            cancellation.ThrowIfCancellationRequested();
            return reader.Read();
        }

        public override int Read(char[] buffer, int index, int count)
        {
            cancellation.ThrowIfCancellationRequested();
            return reader.Read(buffer, index, count);
        }
    }
}
