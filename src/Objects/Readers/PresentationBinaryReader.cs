using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

using HaveIBeenPwned.AddressExtractor.Objects.Attributes;

using OpenMcdf;

namespace HaveIBeenPwned.AddressExtractor.Objects.Readers;

[ExtensionTypes(".ppt")]
internal sealed class PresentationBinaryReader(string path) : ILineReader
{
    private const ushort TextCharsAtom = 4000;
    private const ushort TextBytesAtom = 4008;
    private const ushort CString = 4026;
    private const ushort ClientTextbox = 0xF00D;
    private const ushort ClientData = 0xF011;
    private static readonly Encoding UnicodeEncoding = new UnicodeEncoding(false, false, true);

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    public async IAsyncEnumerable<string?> ReadLineAsync([EnumeratorCancellation] CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        using var storage = RootStorage.OpenRead(path);
        using (var currentUser = storage.OpenStream("Current User"))
        {
            var userHeader = new byte[16];
            await currentUser.ReadExactlyAsync(userHeader, cancellation).ConfigureAwait(false);
            var token = BinaryPrimitives.ReadUInt32LittleEndian(userHeader.AsSpan(12));
            if (token == 0xF3D1C4DF)
            {
                throw new NotSupportedException("Password-protected PowerPoint presentations are not supported.");
            }

            if (BinaryPrimitives.ReadUInt16LittleEndian(userHeader.AsSpan(2)) != 4086 || token != 0xE391C05F)
            {
                throw new InvalidDataException("Invalid PowerPoint Current User record; only PowerPoint 97-2003 binary files are supported.");
            }
        }

        using var stream = storage.OpenStream("PowerPoint Document");
        var boundaries = new Stack<long>();
        boundaries.Push(stream.Length);
        var header = new byte[8];
        while (boundaries.TryPeek(out var boundary))
        {
            cancellation.ThrowIfCancellationRequested();
            if (stream.Position == boundary)
            {
                boundaries.Pop();
                continue;
            }

            if (boundary - stream.Position < header.Length)
            {
                throw new InvalidDataException("Truncated PowerPoint record header.");
            }

            await stream.ReadExactlyAsync(header, cancellation).ConfigureAwait(false);
            var version = BinaryPrimitives.ReadUInt16LittleEndian(header) & 0xF;
            var type = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(2));
            var length = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4));
            var end = stream.Position + length;
            if (end > boundary)
            {
                throw new InvalidDataException("PowerPoint record extends beyond its container.");
            }

            if (type == 12052)
            {
                throw new NotSupportedException("Encrypted PowerPoint records are not supported.");
            }

            if (type is TextCharsAtom or TextBytesAtom or CString)
            {
                if (version != 0 || (type != TextBytesAtom && length % 2 != 0))
                {
                    throw new InvalidDataException("Invalid PowerPoint text record.");
                }

                var text = new byte[checked((int)length)];
                await stream.ReadExactlyAsync(text, cancellation).ConfigureAwait(false);
                // TextBytesAtom stores the low bytes of Unicode characters, not a Windows code page.
                yield return (type == TextBytesAtom ? Encoding.Latin1 : UnicodeEncoding).GetString(text);
            }
            else if (version == 0xF || type is ClientTextbox or ClientData)
            {
                // OfficeArt client records contain PowerPoint records even without the container flag.
                boundaries.Push(end);
            }
            else
            {
                stream.Position = end;
            }
        }
    }
}
