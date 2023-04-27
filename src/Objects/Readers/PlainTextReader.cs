using System.Runtime.CompilerServices;
using System.Text;

namespace MyAddressExtractor.Objects.Readers {
    internal sealed class PlainTextReader : ILineReader
    {
        private readonly FileStream FileStream;
        private readonly StreamReader StreamReader;
        
        public PlainTextReader(string file)
        {
            this.FileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, FileOptions.SequentialScan | FileOptions.Asynchronous);
            this.StreamReader = new StreamReader(this.FileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096);
        }
        
        /// <inheritdoc />
        public async IAsyncEnumerable<string?> ReadLineAsync([EnumeratorCancellation] CancellationToken cancellation = default)
        {
            while (!this.StreamReader.EndOfStream)
            {
                yield return await this.StreamReader.ReadLineAsync(cancellation);
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            this.StreamReader.Dispose();
            await this.FileStream.DisposeAsync();
        }
    }
}
