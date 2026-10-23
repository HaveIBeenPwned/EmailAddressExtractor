using System.Text;

using HaveIBeenPwned.AddressExtractor.Objects;
using HaveIBeenPwned.AddressExtractor.Objects.Performance;
using HaveIBeenPwned.AddressExtractor.Objects.Readers;

using ExcelDataReader.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HaveIBeenPwned.AddressExtractor.Tests;

[TestClass]
public class ExcelBinaryReaderTests
{
    [TestMethod]
    public async Task ExtractsTextRichTextAndCachedResultsAcrossWorksheetsAsync()
    {
        var path = CopyWorkbook();
        var output = $"{path}.txt";
        try
        {
            var runtime = new Runtime();
            await using var reader = runtime.GetExtensionFromPath(path).GetReader(path);
            Assert.IsInstanceOfType<ExcelBinaryReader>(reader);
            var lines = new List<string?>();
            await foreach (var line in reader.ReadLineAsync().ConfigureAwait(false))
            {
                lines.Add(line);
            }

            CollectionAssert.AreEqual(
                new[]
                {
                    "FIRST@EXAMPLE.COM", "rich@example.com", "cached@example.com",
                    "Contact \u4e2d\u6587 unicode@example.com", "second@example.com", "first@example.com"
                },
                lines.ToArray());

            var exitCode = await Program.Main([path, "-y", "-o", output]).ConfigureAwait(false);
            Assert.AreEqual(0, exitCode);
            CollectionAssert.AreEqual(
                new[] { "cached@example.com", "first@example.com", "rich@example.com", "second@example.com", "unicode@example.com" },
                await File.ReadAllLinesAsync(output).ConfigureAwait(false));
            StringAssert.Contains(
                await File.ReadAllTextAsync($"{output}{Config.Defaults.REPORT_FILE_SUFFIX}").ConfigureAwait(false),
                $"{path}: 5");
            using var exclusiveAccess = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(path);
            File.Delete(output);
            File.Delete($"{output}{Config.Defaults.REPORT_FILE_SUFFIX}");
        }
    }

    [TestMethod]
    public async Task ReadsLegacyCodePageTextAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"EmailAddressExtractorTests-{Guid.NewGuid():N}.xls");
        try
        {
            // A raw BIFF2 worksheet with a Windows-1252 LABEL and no CODEPAGE record.
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write((ushort)0x0009);
                writer.Write((ushort)4);
                writer.Write((ushort)0x0002);
                writer.Write((ushort)0x0010);
                var text = Encoding.Latin1.GetBytes("Andr\u00e9 legacy@example.com");
                writer.Write((ushort)0x0004);
                writer.Write((ushort)(8 + text.Length));
                writer.Write((ushort)0);
                writer.Write((ushort)0);
                writer.Write(new byte[3]);
                writer.Write((byte)text.Length);
                writer.Write(text);
                writer.Write((ushort)0x000A);
                writer.Write((ushort)0);
            }

            await using var reader = new ExcelBinaryReader(path);
            var lines = new List<string?>();
            await foreach (var line in reader.ReadLineAsync().ConfigureAwait(false))
            {
                lines.Add(line);
            }

            CollectionAssert.AreEqual(new[] { "Andr\u00e9 legacy@example.com" }, lines.ToArray());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task CancellationReleasesWorkbookAsync()
    {
        var path = CopyWorkbook();
        try
        {
            using var cancellation = new CancellationTokenSource();
            await using var reader = new ExcelBinaryReader(path);
            await using (var enumerator = reader.ReadLineAsync(cancellation.Token).GetAsyncEnumerator())
            {
                Assert.IsTrue(await enumerator.MoveNextAsync().ConfigureAwait(false));
                await cancellation.CancelAsync().ConfigureAwait(false);
                await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                    async () => { await enumerator.MoveNextAsync().ConfigureAwait(false); }).ConfigureAwait(false);
            }

            using var exclusiveAccess = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task PreCancelledReadDoesNotOpenFileAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"EmailAddressExtractorTests-{Guid.NewGuid():N}.xls");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
        await using var reader = new ExcelBinaryReader(path);
        await using var enumerator = reader.ReadLineAsync(cancellation.Token).GetAsyncEnumerator();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            async () => { await enumerator.MoveNextAsync().ConfigureAwait(false); }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task EarlyExitReleasesWorkbookAsync()
    {
        var path = CopyWorkbook();
        try
        {
            await using var reader = new ExcelBinaryReader(path);
            await foreach (var line in reader.ReadLineAsync().ConfigureAwait(false))
            {
                break;
            }

            using var exclusiveAccess = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task LargeInvalidWorkbookRaisesErrorInsteadOfBeingSkippedAsync()
    {
        var path = Path.Combine(Path.GetTempPath(), $"EmailAddressExtractorTests-{Guid.NewGuid():N}.xls");
        try
        {
            using (var stream = File.Create(path))
            {
                stream.SetLength(Config.Defaults.MINIMUM_FILE_SIZE_FOR_AT_SYMBOL_QUICK_SCAN);
            }

            var (containsAtSymbol, _) = await AddressExtractorMonitor.QuickScanForAtSymbolAsync(new FileInfo(path)).ConfigureAwait(false);
            Assert.IsFalse(containsAtSymbol);
            var runtime = new Runtime(CommandLineProcessor.Parse([path, "-y"], out _));
            await using (var monitor = new AddressExtractorMonitor(runtime, IPerformanceStack.DEFAULT))
            {
                await Assert.ThrowsExceptionAsync<HeaderException>(
                    async () => await monitor.RunAsync(1, new FileInfo(path)).ConfigureAwait(false)).ConfigureAwait(false);
            }

            using var exclusiveAccess = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CopyWorkbook()
    {
        var path = Path.Combine(Path.GetTempPath(), $"EmailAddressExtractorTests-{Guid.NewGuid():N}.XLS");
        // Synthetic BIFF8 fixture: empty/sparse sheets, rich/Unicode text, a cached formula and metadata.
        File.Copy(Path.Combine(AppContext.BaseDirectory, "Fixtures", "BinaryWorkbook.xls"), path);
        return path;
    }
}
