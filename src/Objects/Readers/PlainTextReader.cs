using System.Runtime.CompilerServices;
using System.Text;

using HaveIBeenPwned.AddressExtractor.Objects.Attributes;

namespace HaveIBeenPwned.AddressExtractor.Objects.Readers;

[ExtensionTypes(".log", ".json", ".jsonl", ".txt", ".sql", ".xml", ".yaml", ".sample", ".csv", ".tsv")]
internal sealed class PlainTextReader : ILineReader
{
    /// <summary>
    /// 4 KB buffers cost roughly one syscall per 40 lines on a dense address list.
    /// 64 KB amortises that without meaningfully raising the memory floor.
    /// </summary>
    private const int BufferSize = 64 * 1024;

    private readonly FileStream FileStream;
    private readonly StreamReader StreamReader;

    public PlainTextReader(string file)
    {
        FileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: BufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous);
        StreamReader = new StreamReader(FileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: BufferSize);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string?> ReadLineAsync([EnumeratorCancellation] CancellationToken cancellation = default)
    {
        // Testing EndOfStream before each line forces the reader to fault its buffer
        // in separately from the read that follows; reading until null is a single pass.
        while (await StreamReader.ReadLineAsync(cancellation).ConfigureAwait(false) is { } line)
        {
            yield return line;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        StreamReader.Dispose();
        await FileStream.DisposeAsync().ConfigureAwait(false);
    }
}
