using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading.Channels;
using MyAddressExtractor.Objects;
using MyAddressExtractor.Objects.Performance;

namespace MyAddressExtractor {
    public class AddressExtractorMonitor : IAsyncDisposable {
        private readonly Config Config;
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
            Config config,
            IPerformanceStack stack
        ): this(config, stack, TimeSpan.FromMinutes(1)) {}

        public AddressExtractorMonitor(
            Config config,
            IPerformanceStack stack,
            TimeSpan iterate
        ) {
            this.Config = config;
            this.Channel = this.Config.CreateChannel();
            this.Stack = stack;
            this.Timer = new Timer(_ => this.Log(), null, iterate, iterate);

            for (int i = 0; i < this.Config.Threads; i++)
            {
                var task = Task.Run(() => this.ReadAsync(config.CancellationToken));
                this.Tasks.Add(task);
            }
        }

        private async Task ReadAsync(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                Line line = default;

                try
                {
                    // Tasks run forever
                    while (!cancellation.IsCancellationRequested)
                    {
                        // Get a line from the Channel
                        line = await this.Reader.ReadAsync(cancellation);

                        // Check for pauses
                        await this.Config.AwaitContinuationAsync(cancellation);

                        // Extract addresses from the line
                        await foreach(var email in this.Extractor.ExtractAddressesAsync(this.Stack, line.Value, cancellation))
                        {
                            if (this.Addresses.TryAdd(email, 0))
                                line.Counter.Increment();
                            Interlocked.Increment(ref this.LineCounter);

                            // Disabled currently, alternating log messages has oddities
                            /*if (!this.Config.Quiet && this.Lines % 25000 is 0)
                                Output.Write($"Checked {this.Lines:n0} lines, found {line.Counter.Value:n0} emails");*/
                        }
                    }
                } catch (ChannelClosedException) {
                    break; // Break the Task when Channel closed
                } catch (TaskCanceledException) {
                    break; // Break the Task when Tasks are cancelled
                } catch (Exception ex) {
                    if (this.Config.Debug)
                        Output.Exception(new Exception($"An error occurred while parsing '{line.File}'L{line.Number}:", ex));
                    else
                        Output.Error($"An error occurred while parsing '{line.File}'L{line.Number}: {ex.Message}");

                    if (!await this.Config.WaitOnExceptionAsync(cancellation))
                        break;
                }
            }
        }

        public async ValueTask RunAsync(FileInfo file, CancellationToken cancellation = default)
        {
            using (var stack = this.Stack.CreateStack("Read file"))
            {
                var lines = 0;
                var count = new Count();

                this.Files.Add(file.FullName, count);

                var parser = FileExtensionParsing.Get(file);
                await using (var reader = parser.GetReader(file.FullName))
                {
                    // Await any 'continue' prompts
                    await this.Config.AwaitContinuationAsync(cancellation);

                    Output.Write($"Reading \"{file.FullName}\" [{ByteExtensions.Format(file.Length)}]");
                    await foreach(var line in reader.ReadLineAsync(cancellation))
                    {
                        stack.Step("Read line");
                        if (line is not null)
                        {
                            await this.Writer.WriteAsync(new Line {
                                File = file.FullName,
                                Value = line,
                                Counter = count,
                                Number = ++lines
                            }, cancellation);

                            if (!this.Config.Quiet && lines % 25000 is 0)
                                Output.Write($"Read {lines:n0} lines from \"{file.Name}\"");
                        }
                    }
                }
            }
        }

        public virtual void Log()
        {
            Output.Write($"Extraction time: {this.Stopwatch.Format()}");
            Output.Write($"Addresses extracted: {this.Addresses.Count:n0}");
            long rate = (long)(this.Lines / (this.Stopwatch.ElapsedMilliseconds / 1000.0));
            Output.Write($"Read lines total: {this.Lines:n0}");
            Output.Write($"Read lines rate: {rate:n0}/s\n");

            this.Stack.Log();
        }

        internal async ValueTask AwaitCompletionAsync()
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
