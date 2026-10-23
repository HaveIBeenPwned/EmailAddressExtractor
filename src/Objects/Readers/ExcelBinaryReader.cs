using System.Runtime.CompilerServices;
using System.Text;

using HaveIBeenPwned.AddressExtractor.Objects.Attributes;

using ExcelDataReader;

namespace HaveIBeenPwned.AddressExtractor.Objects.Readers;

[ExtensionTypes(".xls")]
internal sealed class ExcelBinaryReader(string path) : ILineReader
{
    static ExcelBinaryReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public ValueTask DisposeAsync()
        => ValueTask.CompletedTask;

    public async IAsyncEnumerable<string?> ReadLineAsync([EnumeratorCancellation] CancellationToken cancellation = default)
    {
        cancellation.ThrowIfCancellationRequested();
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = ExcelReaderFactory.CreateBinaryReader(stream, new ExcelReaderConfiguration
        {
            LeaveOpen = true,
            SinglePassMode = true
        });

        do
        {
            cancellation.ThrowIfCancellationRequested();
            while (reader.Read())
            {
                cancellation.ThrowIfCancellationRequested();
                for (var column = 0; column < reader.FieldCount; column++)
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (reader.GetValue(column) is string text)
                    {
                        yield return text;
                    }
                }

                cancellation.ThrowIfCancellationRequested();
            }

            cancellation.ThrowIfCancellationRequested();
        } while (reader.NextResult());
    }
}
