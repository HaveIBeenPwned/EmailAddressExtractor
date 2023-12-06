using HaveIBeenPwned.AddressExtractor.Objects.Attributes;

namespace HaveIBeenPwned.AddressExtractor.Objects.Readers {
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
