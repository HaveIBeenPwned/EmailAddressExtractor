using System.Runtime.CompilerServices;
using System.Text;

using HaveIBeenPwned.AddressExtractor.Objects.Attributes;

namespace HaveIBeenPwned.AddressExtractor.Objects.Readers;

[ExtensionTypes(".log", ".json", ".jsonl", ".txt", ".sql", ".xml", ".sample", ".csv", ".tsv")]
internal sealed class PlainTextReader : ILineReader
{
    private readonly FileStream FileStream;
    private readonly StreamReader StreamReader;

    public PlainTextReader(string file)
    {
        FileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, FileOptions.SequentialScan | FileOptions.Asynchronous);
        StreamReader = new StreamReader(FileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string?> ReadLineAsync([EnumeratorCancellation] CancellationToken cancellation = default)
    {
        while (!StreamReader.EndOfStream)
        {
            yield return await StreamReader.ReadLineAsync(cancellation).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        StreamReader.Dispose();
        await FileStream.DisposeAsync().ConfigureAwait(false);
    }
}
