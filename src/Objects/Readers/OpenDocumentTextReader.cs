
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;

namespace MyAddressExtractor.Objects.Readers {
    internal sealed class OpenDocumentTextReader : ILineReader
    {
        private readonly FileStream? FileStream;
        private readonly StreamReader? StreamReader;
        private bool initialised = false;
        

        public OpenDocumentTextReader(string zipPath)
        {
            using (ZipArchive archive = ZipFile.OpenRead(zipPath))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.Equals("content.xml", StringComparison.OrdinalIgnoreCase))
                    {
                        var extractPath = Path.GetTempPath();
                        
                        string destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));

                        if (destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
                        {
                            entry.ExtractToFile(destinationPath);

                            this.FileStream = new FileStream(destinationPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, FileOptions.SequentialScan | FileOptions.Asynchronous);
                            this.StreamReader = new StreamReader(this.FileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096);
                            this.initialised = true;
                        }
                    }
                }
            }

            if(!this.initialised)
            {
                throw new Exception($"Unable to load content.xml from '{zipPath}'");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if(this.initialised)
            {
                this.StreamReader?.Dispose();
                await this.FileStream!.DisposeAsync();
            }
        }

        public async IAsyncEnumerable<string?> ReadLineAsync([EnumeratorCancellation] CancellationToken cancellation = default)
        {
            while (!this.StreamReader!.EndOfStream)
            {
                yield return await this.StreamReader!.ReadLineAsync(cancellation);
            }
        }
    }
}