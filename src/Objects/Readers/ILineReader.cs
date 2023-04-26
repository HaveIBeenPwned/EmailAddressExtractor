namespace MyAddressExtractor.Objects.Readers {
    public interface ILineReader : IAsyncDisposable {
        /// <summary>Read and return string segments to be checked for email addresses</summary>
        IAsyncEnumerable<string?> ReadLineAsync(CancellationToken cancellation = default);
    }
}
