using System.IO.Compression;

using HaveIBeenPwned.AddressExtractor.Objects;
using HaveIBeenPwned.AddressExtractor.Objects.Readers;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HaveIBeenPwned.AddressExtractor.Tests;

[TestClass]
public class ExcelXmlReaderTests
{
    [TestMethod]
    public async Task ExtractsSharedInlineRichAndCachedTextAcrossWorksheetsAsync()
    {
        var path = CreateWorkbook();
        try
        {
            var runtime = new Runtime();
            await using var reader = runtime.GetExtensionFromPath(path).GetReader(path);
            var addresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await foreach (var address in AddressExtractor.ExtractFileAddressesAsync(reader).ConfigureAwait(false))
            {
                addresses.Add(address);
            }

            CollectionAssert.AreEquivalent(
                new[]
                {
                    "shared@example.com", "rich-shared@example.com", "inline@example.com",
                    "rich-inline@example.com", "cached@example.com", "second@example.com"
                },
                addresses.ToArray());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task ReadsWorkbookWithoutSharedStringsAsync()
    {
        var path = CreateWorkbook(includeSharedStrings: false);
        try
        {
            await using var reader = new ExcelXmlReader(path);
            var lines = new List<string?>();
            await foreach (var line in reader.ReadLineAsync().ConfigureAwait(false))
            {
                lines.Add(line);
            }

            CollectionAssert.AreEquivalent(
                new[] { "INLINE@EXAMPLE.COM", "rich-inline@example.com", "cached@example.com", "second@example.com" },
                lines.ToArray());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task CancellationReleasesWorkbookAsync()
    {
        var path = CreateWorkbook();
        try
        {
            using var cancellation = new CancellationTokenSource();
            await using var reader = new ExcelXmlReader(path);
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
    public async Task MalformedWorksheetRaisesXmlErrorAsync()
    {
        var path = CreateWorkbook();
        try
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                WriteEntry(archive, "xl/worksheets/broken.xml", "<worksheet>");
            }

            await using var reader = new ExcelXmlReader(path);
            await Assert.ThrowsExceptionAsync<System.Xml.XmlException>(async () =>
            {
                await foreach (var line in reader.ReadLineAsync().ConfigureAwait(false))
                {
                }
            }).ConfigureAwait(false);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task LargeWorkbookIsNotSkippedByRawAtSymbolScanAsync()
    {
        var path = CreateWorkbook(includeSharedStrings: false, includePadding: true);
        var output = $"{path}.txt";
        try
        {
            Assert.IsTrue(new FileInfo(path).Length >= Config.Defaults.MINIMUM_FILE_SIZE_FOR_AT_SYMBOL_QUICK_SCAN);
            var (containsAtSymbol, _) = await AddressExtractorMonitor.QuickScanForAtSymbolAsync(new FileInfo(path)).ConfigureAwait(false);
            Assert.IsFalse(containsAtSymbol, "The fixture must exercise a workbook whose raw archive bytes contain no '@'");
            var exitCode = await Program.Main([path, "-y", "-o", output]).ConfigureAwait(false);
            Assert.AreEqual(0, exitCode);
            CollectionAssert.AreEquivalent(
                new[] { "encoded@example.com" },
                await File.ReadAllLinesAsync(output).ConfigureAwait(false));
        }
        finally
        {
            File.Delete(path);
            File.Delete(output);
            File.Delete($"{output}{Config.Defaults.REPORT_FILE_SUFFIX}");
        }
    }

    private static string CreateWorkbook(bool includeSharedStrings = true, bool includePadding = false)
    {
        var path = Path.Combine(Path.GetTempPath(), $"EmailAddressExtractorTests-{Guid.NewGuid():N}.xlsx");
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        if (includePadding)
        {
            // Stored XML with an entity guarantees that the cell's '@' is not in the raw text.
            WriteEntry(archive, "xl/worksheets/sheet1.xml", """
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <sheetData><row r="1"><c r="A1" t="inlineStr"><is><t>encoded&#64;example.com</t></is></c></row></sheetData>
                </worksheet>
                """, CompressionLevel.NoCompression);
            var entry = archive.CreateEntry("padding.bin", CompressionLevel.NoCompression);
            entry.LastWriteTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            using var stream = entry.Open();
            var buffer = new byte[1000];
            for (var i = 0; i < Config.Defaults.MINIMUM_FILE_SIZE_FOR_AT_SYMBOL_QUICK_SCAN / buffer.Length; i++)
            {
                stream.Write(buffer);
            }

            return path;
        }

        if (includeSharedStrings)
        {
            WriteEntry(archive, "xl/sharedStrings.xml", """
                <sst xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <si><t>SHARED@EXAMPLE.COM</t></si>
                  <si><r><t>rich-shared@</t></r><r><t>example.com</t></r><rPh sb="0" eb="1"><t>phonetic@example.com</t></rPh></si>
                </sst>
                """);
        }

        WriteEntry(archive, "xl/worksheets/sheet1.xml", """
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <sheetData><row r="1">
                <c r="A1" t="s"><v>0</v></c>
                <c r="B1" t="s"><v>1</v></c>
                <c r="C1" t="inlineStr"><is><t>INLINE@EXAMPLE.COM</t></is></c>
                <c r="D1" t="inlineStr"><is><r><t>rich-inline@</t></r><r><t>example.com</t></r></is></c>
                <c r="E1" t="str"><f>"formula@example.com"</f><v>cached@example.com</v></c>
                <c r="F1" t="n"><v>123</v></c>
              </row></sheetData>
            </worksheet>
            """);
        WriteEntry(archive, "xl/worksheets/sheet2.xml", """
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <sheetData><row r="1"><c r="A1" t="inlineStr"><is><t>second@example.com</t></is></c></row></sheetData>
            </worksheet>
            """);
        WriteEntry(archive, "docProps/core.xml", "<metadata>metadata@example.com</metadata>");
        WriteEntry(archive, "xl/worksheets/_rels/sheet1.xml.rels", "<relationships>relationship@example.com</relationships>");

        return path;
    }

    private static void WriteEntry(ZipArchive archive, string name, string content, CompressionLevel compression = CompressionLevel.Optimal)
    {
        var entry = archive.CreateEntry(name, compression);
        entry.LastWriteTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }
}
