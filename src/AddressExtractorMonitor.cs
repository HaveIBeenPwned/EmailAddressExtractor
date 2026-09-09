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
    private readonly Channel<LineBatch> _channel;
    private ChannelReader<LineBatch> Reader => _channel.Reader;
    private ChannelWriter<LineBatch> Writer => _channel.Writer;
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
        // Owned by this task rather than shared with every other one
        var matcher = AddressExtractor.CreateMatcher();

        while (!cancellation.IsCancellationRequested)
        {
            LineBatch batch = default;
            var index = 0;

            try
            {
                // Tasks run forever
                while (!cancellation.IsCancellationRequested)
                {
                    // Get a batch of lines from the Channel
                    batch = await Reader.ReadAsync(cancellation).ConfigureAwait(false);

                    // Check for pauses. Once per batch rather than once per line
                    await _runtime.AwaitContinuationAsync(cancellation).ConfigureAwait(false);

                    var lines = batch.Lines;
                    for (index = 0; index < batch.Count; index++)
                    {
                        // Extract addresses from the line
                        foreach (var email in AddressExtractor.ExtractAddresses(matcher, lines[index]))
                        {
                            batch.Counter.TryAdd(email);

                            Addresses.TryAdd(email, 0);
                        }
                    }

                    // One contended write per batch rather than one per address
                    Interlocked.Add(ref _lineCounter, batch.Count);
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
                var number = batch.StartNumber + index;
                if (Config.Debug)
                {
                    Output.Exception(new FormatException($"An error occurred while parsing '{batch.File}'L{number}:", ex));
                }
                else
                {
                    Output.Error($"An error occurred while parsing '{batch.File}'L{number}: {ex.Message}");
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

        var batchSize = Config.BatchSize;
        var buffer = new string[batchSize];
        var buffered = 0;
        var batchStart = 1L;

        await foreach (var line in reader.ReadLineAsync(cancellation).ConfigureAwait(false))
        {
            stack.Step("Read line");
            if (line is not null)
            {
                buffer[buffered++] = line;
                lines++;

                if (buffered == batchSize)
                {
                    await Writer.WriteAsync(new LineBatch
                    {
                        File = file.FullName,
                        Lines = buffer,
                        Count = buffered,
                        Counter = count,
                        StartNumber = batchStart
                    }, cancellation).ConfigureAwait(false);

                    // The published buffer stays with the reader, so start a fresh one
                    buffer = new string[batchSize];
                    batchStart = lines + 1;
                    buffered = 0;
                }

                if (!Config.Quiet && lines % 250000 is 0)
                {
                    Output.WriteTime($"Read {lines:n0} lines from \"{file.Name}\"");
                }
            }
        }

        // Flush whatever did not fill a whole batch
        if (buffered > 0)
        {
            await Writer.WriteAsync(new LineBatch
            {
                File = file.FullName,
                Lines = buffer,
                Count = buffered,
                Counter = count,
                StartNumber = batchStart
            }, cancellation).ConfigureAwait(false);
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
            var sorted = Addresses.Keys.ToArray();

            if (sorted.Length >= ParallelSortThreshold)
            {
                sorted = sorted.AsParallel()
                    .OrderBy(address => address, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            else
            {
                Array.Sort(sorted, StringComparer.OrdinalIgnoreCase);
            }

            // File.WriteAllLinesAsync buffers 4 KB at a time, which is a lot of flushes across a result set this size.
            var stream = new FileStream(output, FileMode.Create, FileAccess.Write, FileShare.None, WriteBufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using (var writer = new StreamWriter(stream, OutputEncoding, WriteBufferSize))
            {
                foreach (var address in sorted)
                {
                    cancellation.ThrowIfCancellationRequested();
                    await writer.WriteLineAsync(address).ConfigureAwait(false);
                }
            }
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

    /// <summary>Result-set size past which ordering is worth parallelising</summary>
    private const int ParallelSortThreshold = 250_000;

    /// <summary>Buffer used when writing the collected addresses back out</summary>
    private const int WriteBufferSize = 256 * 1024;

    /// <summary>
    /// UTF-8 without a byte order mark, matching what <see cref="File.WriteAllLinesAsync(string, IEnumerable{string}, CancellationToken)"/>
    /// emits. <see cref="Encoding.UTF8"/> would prepend a BOM and change the output.
    /// </summary>
    private static readonly Encoding OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

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
