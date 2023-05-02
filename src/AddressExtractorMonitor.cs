using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using MyAddressExtractor.Objects.Performance;

namespace MyAddressExtractor {
    public class AddressExtractorMonitor : IAsyncDisposable {
        private readonly AddressExtractor Extractor = new();
        private readonly IPerformanceStack Stack;

        protected readonly IDictionary<string, Count> Files = new ConcurrentDictionary<string, Count>();
        protected readonly ISet<string> Addresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        protected int Lines { get; private set; }

        protected readonly Stopwatch Stopwatch = Stopwatch.StartNew();
        private readonly Timer Timer;

        public AddressExtractorMonitor(IPerformanceStack stack): this(stack, TimeSpan.FromMinutes(1)) {}
        public AddressExtractorMonitor(IPerformanceStack stack, TimeSpan iterate)
        {
            this.Stack = stack;
            this.Timer = new Timer(_ => this.Log(), null, iterate, iterate);
        }

        public async ValueTask RunAsync(string inputFilePath, CancellationToken cancellation = default)
        {
            using (var stack = this.Stack.CreateStack("Read file"))
            {
                var count = new Count();
                var addresses = 0;

                this.Files.Add(inputFilePath, count);

                var parser = FileExtensionParsing.GetFromPath(inputFilePath);
                await using (var reader = parser.GetReader(inputFilePath))
                {
                    await foreach(var email in this.Extractor.ExtractFileAddressesAsync(stack, reader, cancellation))
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
            Console.WriteLine($"Extraction time: {this.Stopwatch.Format()}");
            Console.WriteLine($"Addresses extracted: {this.Addresses.Count:n0}");
            long rate = (long)(this.Lines / (this.Stopwatch.ElapsedMilliseconds / 1000.0));
            Console.WriteLine($"Read lines total: {this.Lines:n0}");
            Console.WriteLine($"Read lines rate: {rate:n0}/s\n");

            this.Stack.Log();
        }

        internal async ValueTask SaveAsync(CommandLineProcessor cli, CancellationToken cancellation = default)
        {
            string output = cli.OutputFilePath;
            string report = cli.ReportFilePath;
            if (!string.IsNullOrWhiteSpace(output))
            {
                await File.WriteAllLinesAsync(
                    output,
                    this.Addresses.Select(address => address.ToLowerInvariant())
                        .OrderBy(address => address, StringComparer.OrdinalIgnoreCase),
                    cancellation
                );
            }

            if (!string.IsNullOrWhiteSpace(report))
            {
                var reportContent = new StringBuilder("Unique addresses per file:\n");

                foreach ((var file, var count) in this.Files)
                {
                    reportContent.AppendLine($"{file}: {count}");
                }

                await File.WriteAllTextAsync(report, reportContent.ToString(), cancellation);
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
