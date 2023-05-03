using MyAddressExtractor.Objects.Attributes;

namespace MyAddressExtractor.Objects.Readers {
    [ExtensionTypes(".doc")]
    public sealed class DocumentReader : ILineReader
    {
        /// <inheritdoc />
        public IAsyncEnumerable<string?> ReadLineAsync(CancellationToken cancellation = default)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
