using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Xml;

using HaveIBeenPwned.AddressExtractor.Objects;
using HaveIBeenPwned.AddressExtractor.Objects.Readers;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenMcdf;

namespace HaveIBeenPwned.AddressExtractor.Tests;

[TestClass]
public class PresentationReaderTests
{
    [TestMethod]
    [DataRow(".PPT")]
    [DataRow(".PPTX")]
    public async Task ExtractsSlidesNotesAndTablesAsync(string extension)
    {
        var path = TempPath(extension);
        var output = $"{path}.txt";
        try
        {
            CreatePresentation(path);
            var runtime = new Runtime();
            await using var reader = runtime.GetExtensionFromPath(path).GetReader(path);
            Assert.AreEqual(
                extension == ".PPT" ? typeof(PresentationBinaryReader) : typeof(PresentationXmlReader),
                reader.GetType());

            var exitCode = await Program.Main([path, "-y", "-o", output]).ConfigureAwait(false);
            Assert.AreEqual(0, exitCode);
            CollectionAssert.AreEqual(
                new[] { "field@example.com", "first@example.com", "notes@example.com", "rich@example.com", "second@example.com", "table@example.com" },
                await File.ReadAllLinesAsync(output).ConfigureAwait(false));
            StringAssert.Contains(
                await File.ReadAllTextAsync($"{output}{Config.Defaults.REPORT_FILE_SUFFIX}").ConfigureAwait(false),
                $"{path}: 6");
            using var exclusiveAccess = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            DeletePresentation(path);
        }
    }

    [TestMethod]
    public async Task WordDocumentsKeepTheirExistingReaderAsync()
    {
        var path = TempPath(".docx");
        try
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                WriteXml(archive, "word/document.xml", """
                    <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
                      <w:body><w:p><w:r><w:t>document@example.com</w:t></w:r></w:p></w:body>
                    </w:document>
                    """);
            }

            await using var reader = new Runtime().GetExtensionFromPath(path).GetReader(path);
            Assert.IsInstanceOfType<OpenOfficeXmlReader>(reader);
            CollectionAssert.AreEqual(new[] { "document@example.com" }, await ReadLinesAsync(path).ConfigureAwait(false));
        }
        finally
        {
            DeletePresentation(path);
        }
    }

    [TestMethod]
    [DataRow("http://schemas.openxmlformats.org/drawingml/2006/main")]
    [DataRow("http://purl.oclc.org/ooxml/drawingml/main")]
    public async Task XmlJoinsRunsButPreservesParagraphAndLineBreaksAsync(string drawingNamespace)
    {
        var path = TempPath(".pptx");
        try
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                WriteXml(archive, "ppt/slides/slide1.xml", $$"""
                    <root xmlns:a="{{drawingNamespace}}">
                      <a:p><a:r><a:t>rich@</a:t></a:r><a:r><a:t>example.com</a:t></a:r></a:p>
                      <a:p><a:r><a:t>paragraph@</a:t></a:r></a:p>
                      <a:p><a:r><a:t>example.com</a:t></a:r></a:p>
                      <a:p><a:r><a:t>line@</a:t></a:r><a:br/><a:r><a:t>example.com</a:t></a:r></a:p>
                      <a:p><a:r><a:t xml:space="preserve"> first@example.com </a:t></a:r><a:fld><a:t>field@example.com</a:t></a:fld></a:p>
                    </root>
                    """);
            }

            var lines = await ReadLinesAsync(path).ConfigureAwait(false);
            CollectionAssert.AreEqual(
                new[] { "rich@example.com", "paragraph@", "example.com", "line@\nexample.com", " first@example.com field@example.com" },
                lines);
        }
        finally
        {
            DeletePresentation(path);
        }
    }

    [TestMethod]
    public async Task BinaryDecodesTextAndSkipsNonTextRecordsAsync()
    {
        var path = TempPath(".ppt");
        try
        {
            CreateBinary(path, Record(1000, 15,
                Record(4008, 0, [0x41, 0x80, 0xE9]),
                Record(4000, 0, Encoding.Unicode.GetBytes("\u4e2d\u6587 unicode@example.com")),
                Record(4026, 0, Encoding.Unicode.GetBytes("footer@example.com")),
                Record(9999, 0, Record(4008, 0, "not-text@example.com"u8.ToArray()))));

            CollectionAssert.AreEqual(
                new[] { "A\u0080\u00e9", "\u4e2d\u6587 unicode@example.com", "footer@example.com" },
                await ReadLinesAsync(path).ConfigureAwait(false));
        }
        finally
        {
            DeletePresentation(path);
        }
    }

    [TestMethod]
    [DataRow(".ppt")]
    [DataRow(".pptx")]
    public async Task CancellationReleasesPresentationAsync(string extension)
    {
        var path = TempPath(extension);
        try
        {
            CreatePresentation(path);
            using var cancellation = new CancellationTokenSource();
            await using var reader = new Runtime().GetExtensionFromPath(path).GetReader(path);
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
            DeletePresentation(path);
        }
    }

    [TestMethod]
    [DataRow(".ppt")]
    [DataRow(".pptx")]
    public async Task EarlyExitReleasesPresentationAsync(string extension)
    {
        var path = TempPath(extension);
        try
        {
            CreatePresentation(path);
            await using var reader = new Runtime().GetExtensionFromPath(path).GetReader(path);
            await foreach (var line in reader.ReadLineAsync().ConfigureAwait(false))
            {
                break;
            }

            using var exclusiveAccess = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            DeletePresentation(path);
        }
    }

    [TestMethod]
    [DataRow(".ppt")]
    [DataRow(".pptx")]
    public async Task PreCancelledReadDoesNotOpenFileAsync(string extension)
    {
        var path = TempPath(extension);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
        await using var reader = new Runtime().GetExtensionFromPath(path).GetReader(path);
        await using var enumerator = reader.ReadLineAsync(cancellation.Token).GetAsyncEnumerator();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            async () => { await enumerator.MoveNextAsync().ConfigureAwait(false); }).ConfigureAwait(false);
    }

    [TestMethod]
    [DataRow("header")]
    [DataRow("length")]
    [DataRow("container")]
    [DataRow("unicode")]
    public async Task MalformedBinaryRecordsRaiseErrorsAndReleaseFileAsync(string corruption)
    {
        var path = TempPath(".ppt");
        try
        {
            var record = corruption switch
            {
                "header" => new byte[7],
                "length" => Record(4008, 0, "text"u8.ToArray()),
                "container" => Record(1000, 15, Record(4008, 0, "text"u8.ToArray())),
                _ => Record(4000, 0, [65])
            };
            if (corruption == "length")
            {
                BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(4), uint.MaxValue);
            }
            else if (corruption == "container")
            {
                BinaryPrimitives.WriteUInt32LittleEndian(record.AsSpan(4), 8);
            }

            CreateBinary(path, record);
            await Assert.ThrowsExceptionAsync<InvalidDataException>(
                async () => { await ReadLinesAsync(path).ConfigureAwait(false); }).ConfigureAwait(false);
            using var exclusiveAccess = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            DeletePresentation(path);
        }
    }

    [TestMethod]
    public async Task EncryptedBinaryPresentationRaisesExplicitErrorAsync()
    {
        var path = TempPath(".ppt");
        try
        {
            CreateBinary(path, Record(1000, 15), encrypted: true);
            await Assert.ThrowsExceptionAsync<NotSupportedException>(
                async () => { await ReadLinesAsync(path).ConfigureAwait(false); }).ConfigureAwait(false);
            using var exclusiveAccess = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            DeletePresentation(path);
        }
    }

    [TestMethod]
    public async Task MalformedXmlRaisesErrorAndReleasesFileAsync()
    {
        var path = TempPath(".pptx");
        try
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                WriteXml(archive, "ppt/slides/slide1.xml", "<broken>");
            }

            await Assert.ThrowsExceptionAsync<XmlException>(
                async () => { await ReadLinesAsync(path).ConfigureAwait(false); }).ConfigureAwait(false);
            using var exclusiveAccess = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            DeletePresentation(path);
        }
    }

    [TestMethod]
    public async Task LargeXmlPresentationIsNotSkippedByRawAtSymbolScanAsync()
    {
        var path = TempPath(".PPTX");
        var output = $"{path}.txt";
        try
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                WriteXml(archive, "ppt/slides/slide1.xml", """
                    <root xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main">
                      <a:p><a:r><a:t>encoded&#64;example.com</a:t></a:r></a:p>
                    </root>
                    """);
                var entry = archive.CreateEntry("padding.bin", CompressionLevel.NoCompression);
                entry.LastWriteTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
                using var stream = entry.Open();
                var buffer = new byte[1000];
                for (var i = 0; i < Config.Defaults.MINIMUM_FILE_SIZE_FOR_AT_SYMBOL_QUICK_SCAN / buffer.Length; i++)
                {
                    stream.Write(buffer);
                }
            }

            Assert.IsTrue(new FileInfo(path).Length >= Config.Defaults.MINIMUM_FILE_SIZE_FOR_AT_SYMBOL_QUICK_SCAN);
            var (containsAtSymbol, _) = await AddressExtractorMonitor.QuickScanForAtSymbolAsync(new FileInfo(path)).ConfigureAwait(false);
            Assert.IsFalse(containsAtSymbol);
            Assert.AreEqual(0, await Program.Main([path, "-y", "-o", output]).ConfigureAwait(false));
            CollectionAssert.AreEqual(new[] { "encoded@example.com" }, await File.ReadAllLinesAsync(output).ConfigureAwait(false));
        }
        finally
        {
            DeletePresentation(path);
        }
    }

    private static async Task<string?[]> ReadLinesAsync(string path)
    {
        await using var reader = new Runtime().GetExtensionFromPath(path).GetReader(path);
        var lines = new List<string?>();
        await foreach (var line in reader.ReadLineAsync().ConfigureAwait(false))
        {
            lines.Add(line);
        }

        return lines.ToArray();
    }

    private static void CreatePresentation(string path)
    {
        if (Path.GetExtension(path).Equals(".ppt", StringComparison.OrdinalIgnoreCase))
        {
            CreateBinary(path, Record(1000, 15,
                Record(4080, 15, Record(4008, 0, "FIRST@EXAMPLE.COM"u8.ToArray())),
                Record(1006, 15, Record(1036, 15, Record(0xF002, 15,
                    Record(0xF00D, 0, Record(4000, 0, Encoding.Unicode.GetBytes("rich@example.com"))),
                    Record(0xF00D, 0, Record(4008, 0, "table@example.com"u8.ToArray()))))),
                Record(1008, 15, Record(0xF011, 0, Record(4008, 0, "notes@example.com"u8.ToArray()))),
                Record(1006, 15, Record(4008, 0, "second@example.com\rfirst@example.com\rfield@example.com"u8.ToArray()))));
            return;
        }

        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        WriteXml(archive, "ppt/slides/slide1.xml", """
            <p:sld xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main"
                   xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main">
              <p:cSld><p:spTree><p:sp><p:txBody>
                <a:p><a:r><a:t>FIRST@EXAMPLE.COM</a:t></a:r></a:p>
                <a:p><a:r><a:t>rich@</a:t></a:r><a:r><a:t>example.com</a:t></a:r></a:p>
                <a:p><a:fld><a:t>field@example.com</a:t></a:fld></a:p>
              </p:txBody></p:sp>
              <p:graphicFrame><a:graphic><a:graphicData><a:tbl><a:tr><a:tc><a:txBody>
                <a:p><a:r><a:t>table@example.com</a:t></a:r></a:p>
              </a:txBody></a:tc></a:tr></a:tbl></a:graphicData></a:graphic></p:graphicFrame>
              </p:spTree></p:cSld>
            </p:sld>
            """);
        WriteXml(archive, "ppt/slides/custom-slide.xml", """
            <root xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main">
              <a:p><a:r><a:t>second@example.com</a:t></a:r></a:p>
              <a:p><a:r><a:t>first@example.com</a:t></a:r></a:p>
            </root>
            """);
        WriteXml(archive, "ppt/notesSlides/notesSlide1.xml", """
            <root xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main">
              <a:p><a:r><a:t>notes@example.com</a:t></a:r></a:p>
            </root>
            """);
        const string ignored = """
            <root xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main">
              <a:p><a:r><a:t>ignored@example.com</a:t></a:r></a:p>
            </root>
            """;
        foreach (var name in new[]
        {
            "docProps/core.xml", "ppt/slideMasters/slideMaster1.xml", "ppt/slides/_rels/slide1.xml.rels",
            "ppt/slides/slide1.xml/extra.xml", "other/ppt/slides/slide1.xml", "ppt/slides/slide1.xml.bak"
        })
        {
            WriteXml(archive, name, ignored);
        }
    }

    private static void CreateBinary(string path, byte[] records, bool encrypted = false)
    {
        using var storage = RootStorage.Create(path);
        using (var document = storage.CreateStream("PowerPoint Document"))
        {
            document.Write(records);
        }

        using (var currentUser = storage.CreateStream("Current User"))
        {
            var user = new byte[24];
            BinaryPrimitives.WriteUInt32LittleEndian(user, 20);
            BinaryPrimitives.WriteUInt32LittleEndian(user.AsSpan(4), encrypted ? 0xF3D1C4DF : 0xE391C05F);
            BinaryPrimitives.WriteUInt16LittleEndian(user.AsSpan(14), 0x03F4);
            user[16] = 3;
            BinaryPrimitives.WriteUInt32LittleEndian(user.AsSpan(20), 8);
            currentUser.Write(Record(4086, 0, user));
        }

        using var metadata = storage.CreateStream("\u0005SummaryInformation");
        metadata.Write("metadata@example.com"u8);
    }

    private static byte[] Record(ushort type, ushort version, params byte[][] content)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(version);
        writer.Write(type);
        writer.Write(content.Sum(bytes => bytes.Length));
        foreach (var bytes in content)
        {
            writer.Write(bytes);
        }

        return stream.ToArray();
    }

    private static void WriteXml(ZipArchive archive, string name, string xml)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.NoCompression);
        entry.LastWriteTime = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(xml);
    }

    private static string TempPath(string extension)
        => Path.Combine(Path.GetTempPath(), $"EmailAddressExtractorTests-{Guid.NewGuid():N}{extension}");

    private static void DeletePresentation(string path)
    {
        File.Delete(path);
        File.Delete($"{path}.txt");
        File.Delete($"{path}.txt{Config.Defaults.REPORT_FILE_SUFFIX}");
    }
}
