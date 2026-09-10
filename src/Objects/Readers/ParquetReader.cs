using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

using HaveIBeenPwned.AddressExtractor.Objects.Attributes;

using Parquet.Serialization;

namespace HaveIBeenPwned.AddressExtractor.Objects.Readers;

[ExtensionTypes(".parquet")]
internal sealed class ParquetReader : ILineReader
{
    private const int BufferSize = 64 * 1024;
    private static readonly Encoding TextEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private readonly string _path;

    public ParquetReader(string path)
    {
        _path = path;
    }

    public async IAsyncEnumerable<string?> ReadLineAsync([EnumeratorCancellation] CancellationToken cancellation = default)
    {
        var textPath = $"{_path}.txt";
        await WriteTextFileAsync(textPath, cancellation).ConfigureAwait(false);

        await using var textReader = new PlainTextReader(textPath);
        await foreach (var line in textReader.ReadLineAsync(cancellation).ConfigureAwait(false))
        {
            yield return line;
        }
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    private async Task WriteTextFileAsync(string textPath, CancellationToken cancellation)
    {
        await using var textStream = new FileStream(textPath, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize: BufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous);
        await using var textWriter = new StreamWriter(textStream, TextEncoding, BufferSize);

        var rowGroupCount = await GetRowGroupCountAsync(cancellation).ConfigureAwait(false);
        for (var rowGroupIndex = 0; rowGroupIndex < rowGroupCount; rowGroupIndex++)
        {
            cancellation.ThrowIfCancellationRequested();

            await using var stream = OpenStream();
            var result = await ParquetSerializer.DeserializeUntypedAsync(stream, rowGroupIndex: rowGroupIndex, cancellationToken: cancellation).ConfigureAwait(false);
            foreach (var row in result.Data)
            {
                cancellation.ThrowIfCancellationRequested();

                var line = FlattenRow(row);
                if (!string.IsNullOrWhiteSpace(line))
                {
                    await textWriter.WriteLineAsync(line.AsMemory(), cancellation).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task<int> GetRowGroupCountAsync(CancellationToken cancellation)
    {
        await using var stream = OpenStream();
        await using var reader = await Parquet.ParquetReader.CreateAsync(stream, cancellationToken: cancellation).ConfigureAwait(false);
        return reader.RowGroupCount;
    }

    private FileStream OpenStream()
        => new(_path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: BufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous);

    private static string? FlattenRow(IReadOnlyDictionary<string, object> row)
    {
        var values = new List<string>();
        foreach (var value in row.Values)
        {
            AppendValues(value, values);
        }

        return values.Count == 0 ? null : string.Join('\t', values);
    }

    private static void AppendValues(object? value, List<string> values)
    {
        switch (value)
        {
            case null:
            case DBNull:
                return;
            case string s when string.IsNullOrWhiteSpace(s):
                return;
            case string s:
                values.Add(s);
                return;
            case byte[] bytes when bytes.Length == 0:
                return;
            case byte[] bytes:
                var decoded = Encoding.UTF8.GetString(bytes);
                if (!string.IsNullOrWhiteSpace(decoded))
                {
                    values.Add(decoded);
                }

                return;
            case IDictionary dictionary:
                foreach (DictionaryEntry entry in dictionary)
                {
                    AppendValues(entry.Value, values);
                }

                return;
            case IEnumerable sequence:
                foreach (var item in sequence)
                {
                    AppendValues(item, values);
                }

                return;
            default:
                var formatted = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(formatted))
                {
                    values.Add(formatted);
                }

                return;
        }
    }
}
