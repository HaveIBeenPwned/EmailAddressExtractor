using System.Collections.Concurrent;
using System.Diagnostics;

namespace MyAddressExtractor {
    public class AddressExtractorMonitor : IAsyncDisposable {
        private readonly AddressExtractor Extractor = new();
        protected readonly IDictionary<string, Count> Files = new ConcurrentDictionary<string, Count>();
        protected readonly ISet<string> Addresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        protected int Lines { get; private set; }

        protected readonly Stopwatch Stopwatch = Stopwatch.StartNew();
        private readonly Timer Timer;

        public AddressExtractorMonitor(): this(TimeSpan.FromMinutes(1)) {}
        public AddressExtractorMonitor(TimeSpan iterate)
        {
            this.Timer = new Timer(_ => this.Log(), null, iterate, iterate);
        }

        public async ValueTask RunAsync(IEnumerable<string> files, CancellationToken cancellation = default)
        {
            foreach (var inputFilePath in files)
            {
                var count = new Count();
                var addresses = 0;

                this.Files.Add(inputFilePath, count);

                var parser = FileExtensionParsing.GetFromPath(inputFilePath);
                await using (var reader = parser.GetReader(inputFilePath))
                {
                    await foreach(var email in this.Extractor.ExtractAddressesAsync(reader, cancellation))
                    {
                        if (this.Addresses.Add(email))
                            count.Value = addresses++;
                        this.Lines++;
                    }
                }
            }
        }

        public virtual void Log()
        {
            Console.WriteLine($"Extraction time: {this.Stopwatch.ElapsedMilliseconds:n0}ms");
            Console.WriteLine($"Addresses extracted: {this.Addresses.Count:n0}");
            long rate = (long)(this.Lines / (this.Stopwatch.ElapsedMilliseconds / 1000.0));
            Console.WriteLine($"Read lines total: {this.Lines:n0}");
            Console.WriteLine($"Read lines rate: {rate:n0}/s\n");
        }

        internal async ValueTask SaveAsync(CommandLineProcessor cli, CancellationToken cancellation = default)
        {
            string output = cli.OutputFilePath;
            string report = cli.ReportFilePath;
            if (!string.IsNullOrWhiteSpace(output))
            {
                await this.Extractor.SaveAddressesAsync(output, this.Addresses, cancellation);
            }

            if (!string.IsNullOrWhiteSpace(report))
            {
                await this.Extractor.SaveReportAsync(report, this.Files, cancellation);
            }
        }

        public async ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);

            await this.Timer.DisposeAsync();
            this.Stopwatch.Stop();
        }
    }
}
