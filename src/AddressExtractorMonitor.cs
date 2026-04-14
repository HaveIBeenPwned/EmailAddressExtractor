using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading.Channels;

using HaveIBeenPwned.AddressExtractor.Objects;
using HaveIBeenPwned.AddressExtractor.Objects.Performance;

namespace HaveIBeenPwned.AddressExtractor;

public class AddressExtractorMonitor : IAsyncDisposable
{
    private readonly Runtime _runtime;
    private Config Config => _runtime.Config;
    private readonly Channel<Line> _channel;
    private ChannelReader<Line> Reader => _channel.Reader;
    private ChannelWriter<Line> Writer => _channel.Writer;
    private readonly List<Task> _tasks = [];

    private readonly IPerformanceStack _stack;

    protected readonly IDictionary<string, Count> Files = new ConcurrentDictionary<string, Count>();
    protected readonly ConcurrentDictionary<string, byte> Addresses = new(StringComparer.OrdinalIgnoreCase);

    // ReadLine count
    protected long Lines => _lineCounter;
    private long _lineCounter;

    private long _quickScanCheckedFiles;
    private long _quickScanSkippedFiles;
    private long _quickScanBytesRead;
    private long _quickScanSavedBytes;
    private long _quickScanTicks;

    protected readonly Stopwatch Stopwatch = Stopwatch.StartNew();
    private readonly Timer _timer;

    public AddressExtractorMonitor(
        Runtime runtime,
        IPerformanceStack stack
    ) : this(runtime, stack, TimeSpan.FromMinutes(1)) { }

    public AddressExtractorMonitor(
        Runtime runtime,
        IPerformanceStack stack,
        TimeSpan iterate
    )
    {
        _runtime = runtime;
        _channel = Config.CreateChannel();
        _stack = stack;
        _timer = new Timer(_ => Log(), null, iterate, iterate);

        for (var i = 0; i < Config.Threads; i++)
        {
            var task = Task.Run(() => ReadAsync(_runtime.CancellationToken));
            _tasks.Add(task);
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
                    line = await Reader.ReadAsync(cancellation).ConfigureAwait(false);

                    // Check for pauses
                    await _runtime.AwaitContinuationAsync(cancellation).ConfigureAwait(false);

                    // Extract addresses from the line
                    foreach (var email in AddressExtractor.ExtractAddresses(line.Value))
                    {
                        if (Addresses.TryAdd(email, 0))
                        {
                            line.Counter.Increment();
                        }

                        Interlocked.Increment(ref _lineCounter);

                        // Disabled currently, alternating log messages has oddities
                        /*if (!this.Config.Quiet && this.Lines % 25000 is 0)
                            Output.Write($"Checked {this.Lines:n0} lines, found {line.Counter.Value:n0} emails");*/
                    }
                }
            }
            catch (ChannelClosedException)
            {
                break; // Break the Task when Channel closed
            }
            catch (TaskCanceledException)
            {
                break; // Break the Task when Tasks are cancelled
            }
            catch (Exception ex)
            {
                if (Config.Debug)
                {
                    Output.Exception(new FormatException($"An error occurred while parsing '{line.File}'L{line.Number}:", ex));
                }
                else
                {
                    Output.Error($"An error occurred while parsing '{line.File}'L{line.Number}: {ex.Message}");
                }

                if (!await _runtime.WaitOnExceptionAsync(cancellation).ConfigureAwait(false))
                {
                    break;
                }
            }
        }
    }

    public async ValueTask RunAsync(long fileCount, FileInfo file, CancellationToken cancellation = default)
    {
        using var stack = _stack.CreateStack("Read file");

        if (file.Length >= Config.MinimumFileSizeForAtSymbolQuickScan)
        {
            var quickScanStopwatch = Stopwatch.StartNew();
            var quickScan = await QuickScanForAtSymbolAsync(file, cancellation).ConfigureAwait(false);
            quickScanStopwatch.Stop();

            stack.Step("Quick '@' scan");

            Interlocked.Increment(ref _quickScanCheckedFiles);
            Interlocked.Add(ref _quickScanBytesRead, quickScan.BytesRead);
            Interlocked.Add(ref _quickScanTicks, quickScanStopwatch.Elapsed.Ticks);

            if (!quickScan.ContainsAtSymbol)
            {
                Interlocked.Increment(ref _quickScanSkippedFiles);
                Interlocked.Add(ref _quickScanSavedBytes, file.Length);

                Output.FileResult(fileCount, file.FullName, file.Length, "skipped due to no @ symbol");

                return;
            }
        }

        var lines = 0L;
        var count = new Count();

        Files.Add(file.FullName, count);

        var parser = _runtime.GetExtension(file);
        await using var reader = parser.GetReader(file.FullName);
        // Await any 'continue' prompts
        await _runtime.AwaitContinuationAsync(cancellation).ConfigureAwait(false);

        Output.FileResult(fileCount, file.FullName, file.Length);

        await foreach (var line in reader.ReadLineAsync(cancellation).ConfigureAwait(false))
        {
            stack.Step("Read line");
            if (line is not null)
            {
                await Writer.WriteAsync(new Line
                {
                    File = file.FullName,
                    Value = line,
                    Counter = count,
                    Number = ++lines
                }, cancellation).ConfigureAwait(false);

                if (!Config.Quiet && lines % 250000 is 0)
                {
                    Output.WriteTime($"Read {lines:n0} lines from \"{file.Name}\"");
                }
            }
        }
    }

    public virtual void Log()
    {
        Output.Write($"Extraction time: {Stopwatch.Format()}");
        Output.Write($"Files parsed: {Files.Count:n0}");

        var checkedFiles = Interlocked.Read(ref _quickScanCheckedFiles);
        var skippedFiles = Interlocked.Read(ref _quickScanSkippedFiles);
        var bytesRead = Interlocked.Read(ref _quickScanBytesRead);
        var savedBytes = Interlocked.Read(ref _quickScanSavedBytes);
        var quickScanTime = TimeSpan.FromTicks(Interlocked.Read(ref _quickScanTicks));
        var scanRate = quickScanTime > TimeSpan.Zero
            ? (long)(bytesRead / quickScanTime.TotalSeconds)
            : 0L;

        Output.Write($"Quick '@' scan threshold: {ByteExtensions.Format(Config.MinimumFileSizeForAtSymbolQuickScan)}");
        Output.Write($"Quick '@' scans: {checkedFiles:n0} files in {quickScanTime.Format()}");
        Output.Write($"Quick '@' scan bytes read: {ByteExtensions.Format(bytesRead)} ({ByteExtensions.Format(scanRate)}/s)");
        Output.Write($"Quick '@' scan skips: {skippedFiles:n0} files, avoided parsing {ByteExtensions.Format(savedBytes)}");

        Output.Write($"Addresses extracted: {Addresses.Count:n0}");
        var rate = (long)(Lines / (Stopwatch.ElapsedMilliseconds / 1000.0));
        Output.Write($"Read lines total: {Lines:n0}");
        Output.Write($"Read lines rate: {rate:n0}/s\n");

        _stack.Log();
    }

    internal async ValueTask AwaitCompletionAsync()
    {
        Writer.Complete();
        await Task.WhenAll(_tasks).ConfigureAwait(false);
        await Reader.Completion.ConfigureAwait(false);
    }

    internal async ValueTask SaveAsync(CancellationToken cancellation = default)
    {
        var output = Config.OutputFilePath;
        var report = Config.ReportFilePath;
        if (!string.IsNullOrWhiteSpace(output))
        {
            await File.WriteAllLinesAsync(
                output,
                Addresses.Keys.Select(address => address.ToLowerInvariant())
                    .OrderBy(address => address, StringComparer.OrdinalIgnoreCase),
                cancellation
            ).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(report))
        {
            var reportContent = new StringBuilder("Unique addresses per file:\n");

            foreach ((var file, var count) in Files.OrderByDescending(f => f.Value.Value))
            {
                reportContent.AppendLine(CultureInfo.InvariantCulture, $"{file}: {count}");
            }

            await File.WriteAllTextAsync(report, reportContent.ToString(), cancellation).ConfigureAwait(false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        await _timer.DisposeAsync().ConfigureAwait(false);
        Stopwatch.Stop();
    }

    internal static async ValueTask<(bool ContainsAtSymbol, long BytesRead)> QuickScanForAtSymbolAsync(FileInfo file, CancellationToken cancellation = default)
    {
        const int bufferSize = 1024 * 64;

        var bytesRead = 0L;
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            await using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous);
            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, bufferSize), cancellation).ConfigureAwait(false);
                if (read == 0)
                {
                    return (false, bytesRead);
                }

                bytesRead += read;
                if (buffer.AsSpan(0, read).Contains((byte)'@'))
                {
                    return (true, bytesRead);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

}
