using System.Buffers;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;

using HaveIBeenPwned.AddressExtractor.Objects.Attributes;

using MimeKit;
using MimeKit.IO;

namespace HaveIBeenPwned.AddressExtractor.Objects.Readers;

[ExtensionTypes(".mbox")]
internal sealed class MboxReader(string path) : ILineReader
{
    static MboxReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    public async IAsyncEnumerable<string?> ReadLineAsync([EnumeratorCancellation] CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length == 0)
        {
            yield break;
        }

        // Persistent parsing keeps MIME content in file-backed substreams, including large attachments.
        var options = new ParserOptions { RespectContentLength = true };
        var parser = new MimeParser(options, stream, MimeFormat.Mbox, persistent: true);
        long? recoveryStart = null;
        while (!parser.IsEndOfStream)
        {
            cancellation.ThrowIfCancellationRequested();
            var messageStart = parser.Position;
            MimeMessage? message = null;
            var resumeAt = stream.Length;
            try
            {
                message = await parser.ParseMessageAsync(cancellation).ConfigureAwait(false);
            }
            catch (FormatException)
            {
                if (recoveryStart is null)
                {
                    recoveryStart = messageStart;
                    Output.Notice($"Cannot parse MIME message at byte {messageStart} in '{path}'; reading damaged section as plain text and attempting to resume MIME parsing.");
                }

                // The parser's marker search is bounded, so scan arbitrarily long damaged sections ourselves.
                var scanStart = Math.Max(messageStart, parser.MboxMarkerOffset) + 1;
                resumeAt = await FindNextMarkerAsync(stream, scanStart, cancellation).ConfigureAwait(false);
            }

            if (message is null)
            {
                if (resumeAt >= stream.Length)
                {
                    break;
                }

                stream.Position = resumeAt;
                parser = new MimeParser(options, stream, MimeFormat.Mbox, persistent: true);
                continue;
            }

            using (message)
            {
                var rawStart = recoveryStart ?? messageStart;
                if (rawStart < parser.MboxMarkerOffset)
                {
                    if (recoveryStart is null)
                    {
                        Output.Notice($"Reading non-message data at byte {rawStart} in '{path}' as plain text.");
                    }

                    await foreach (var line in ReadPlainTextRangeAsync(stream, rawStart, parser.MboxMarkerOffset, cancellation).ConfigureAwait(false))
                    {
                        yield return line;
                    }
                }

                recoveryStart = null;
                yield return parser.MboxMarker;
                await foreach (var line in ReadMessageAsync(message, cancellation).ConfigureAwait(false))
                {
                    yield return line;
                }
            }
        }

        if (recoveryStart is { } start)
        {
            await foreach (var line in ReadPlainTextRangeAsync(stream, start, stream.Length, cancellation).ConfigureAwait(false))
            {
                yield return line;
            }
        }
    }

    private static async ValueTask<long> FindNextMarkerAsync(Stream stream, long start, CancellationToken cancellation)
    {
        const string Marker = "From ";
        var buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
        try
        {
            stream.Position = start;
            var offset = start;
            var matched = start == 0 ? 0 : -1;
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(), cancellation).ConfigureAwait(false)) > 0)
            {
                for (var index = 0; index < read; index++)
                {
                    var value = buffer[index];
                    if (matched >= 0 && value == Marker[matched])
                    {
                        if (++matched == Marker.Length)
                        {
                            return offset + index + 1 - Marker.Length;
                        }
                    }
                    else
                    {
                        matched = value == '\n' ? 0 : -1;
                    }
                }

                offset += read;
            }

            return stream.Length;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async IAsyncEnumerable<string> ReadPlainTextRangeAsync(Stream stream, long start, long end,
        [EnumeratorCancellation] CancellationToken cancellation)
    {
        using var range = new BoundStream(stream, start, end, leaveOpen: true);
        using var reader = new StreamReader(range, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 64 * 1024);
        while (await reader.ReadLineAsync(cancellation).ConfigureAwait(false) is { } line)
        {
            cancellation.ThrowIfCancellationRequested();
            yield return line;
        }
    }

    private async IAsyncEnumerable<string?> ReadMessageAsync(MimeMessage message,
        [EnumeratorCancellation] CancellationToken cancellation)
    {
        foreach (var header in message.Headers)
        {
            cancellation.ThrowIfCancellationRequested();
            yield return header.Value;
        }

        if (message.Body is not null)
        {
            await foreach (var line in ReadEntityAsync(message.Body, cancellation).ConfigureAwait(false))
            {
                yield return line;
            }
        }
    }

    private async IAsyncEnumerable<string?> ReadEntityAsync(MimeEntity entity,
        [EnumeratorCancellation] CancellationToken cancellation)
    {
        foreach (var header in entity.Headers)
        {
            cancellation.ThrowIfCancellationRequested();
            yield return header.Value;
        }

        cancellation.ThrowIfCancellationRequested();
        switch (entity)
        {
            case Multipart multipart:
                foreach (var part in multipart)
                {
                    await foreach (var line in ReadEntityAsync(part, cancellation).ConfigureAwait(false))
                    {
                        yield return line;
                    }
                }
                break;
            case MessagePart { Message: not null } nested:
                await foreach (var line in ReadMessageAsync(nested.Message, cancellation).ConfigureAwait(false))
                {
                    yield return line;
                }
                break;
            case TextPart { Content: not null } text:
                var encoding = text.ContentType.CharsetEncoding;
                if (encoding is null && text.ContentType.Charset is not null)
                {
                    Output.Notice($"Unsupported MIME charset in '{path}'; reading this text part as UTF-8.");
                }

                using (var decoded = text.Content.Open())
                using (var reader = new StreamReader(decoded, encoding ?? Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    while (await reader.ReadLineAsync(cancellation).ConfigureAwait(false) is { } line)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        yield return text.IsHtml ? WebUtility.HtmlDecode(line) : line;
                    }
                }
                break;
        }
    }
}
