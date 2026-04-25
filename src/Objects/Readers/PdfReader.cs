using System.Runtime.CompilerServices;
using HaveIBeenPwned.AddressExtractor.Objects.Attributes;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace HaveIBeenPwned.AddressExtractor.Objects.Readers;

[ExtensionTypes(".pdf")]
internal sealed class PdfReader(string path) : ILineReader
{
    /// <inheritdoc />
    public async IAsyncEnumerable<string?> ReadLineAsync([EnumeratorCancellation] CancellationToken cancellation = default)
    {
        using var document = PdfDocument.Open(path);
        foreach (var page in document.GetPages())
        {
            cancellation.ThrowIfCancellationRequested();
            var text = ContentOrderTextExtractor.GetText(page);
            await Task.Yield();
            yield return text;
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
