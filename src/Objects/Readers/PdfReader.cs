namespace MyAddressExtractor.Objects.Readers {
    public sealed class PdfReader : ILineReader
    {
        /// <inheritdoc />
        public IAsyncEnumerable<string?> ReadLineAsync(CancellationToken cancellation = default)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;
    }
}
