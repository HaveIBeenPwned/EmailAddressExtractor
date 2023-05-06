using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using MyAddressExtractor.Objects;
using MyAddressExtractor.Objects.Performance;

namespace MyAddressExtractor {
    public class AddressExtractorMonitor : IAsyncDisposable {
        private readonly CommandLineProcessor Config;
        private readonly Channel<Line> Channel;
        private ChannelReader<Line> Reader => this.Channel.Reader;
        private ChannelWriter<Line> Writer => this.Channel.Writer;
        private IList<Task> Tasks = new List<Task>();

        private readonly AddressExtractor Extractor = new();
        private readonly IPerformanceStack Stack;

        protected readonly IDictionary<string, Count> Files = new ConcurrentDictionary<string, Count>();
        protected readonly ConcurrentDictionary<string, byte> Addresses = new(StringComparer.OrdinalIgnoreCase);

        // ReadLine count
        protected int Lines => this.LineCounter;
        private int LineCounter;

        protected readonly Stopwatch Stopwatch = Stopwatch.StartNew();
        private readonly Timer Timer;

        public AddressExtractorMonitor(
            CommandLineProcessor config,
            IPerformanceStack stack
        ): this(config, stack, TimeSpan.FromMinutes(1)) {}

        public AddressExtractorMonitor(
            CommandLineProcessor config,
            IPerformanceStack stack,
            TimeSpan iterate
        ) {
            this.Config = config;
            this.Channel = this.Config.CreateChannel();
            this.Stack = stack;
            this.Timer = new Timer(_ => this.Log(), null, iterate, iterate);

            for (int i = 0; i < this.Config.Threads; i++)
                this.Tasks.Add(Task.Run(() => this.ReadAsync(CancellationToken.None)));
        }

        private async Task ReadAsync(CancellationToken cancellation)
        {
            try {
                while (await this.Reader.ReadAsync(cancellation) is var line)
                {
                    await foreach(var email in this.Extractor.ExtractAddressesAsync(this.Stack, line.Value, cancellation))
                    {
                        if (this.Addresses.TryAdd(email, 0))
                            line.Counter.Add();
                        Interlocked.Increment(ref this.LineCounter);
                    }
                }
            } catch (ChannelClosedException) {
            } catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        public async ValueTask RunAsync(string inputFilePath, CancellationToken cancellation = default)
        {
            using (var stack = this.Stack.CreateStack("Read file"))
            {
                var count = new Count();

                this.Files.Add(inputFilePath, count);

                var parser = FileExtensionParsing.GetFromPath(inputFilePath);
                await using (var reader = parser.GetReader(inputFilePath))
                {
                    await foreach(var line in reader.ReadLineAsync(cancellation))
                    {
                        stack.Step("Read line");
                        if (line is not null)
                        {
                            await this.Writer.WriteAsync(new Line {
                                File = inputFilePath,
                                Value = line,
                                Counter = count
                            }, cancellation);
                        }
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

        internal async ValueTask AwaitCompletion()
        {
            this.Writer.Complete();
            await Task.WhenAll(this.Tasks);
            await this.Reader.Completion;
        }

        internal async ValueTask SaveAsync(CancellationToken cancellation = default)
        {
            string output = this.Config.OutputFilePath;
            string report = this.Config.ReportFilePath;
            if (!string.IsNullOrWhiteSpace(output))
            {
                await File.WriteAllLinesAsync(
                    output,
                    this.Addresses.Keys.Select(address => address.ToLowerInvariant())
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
