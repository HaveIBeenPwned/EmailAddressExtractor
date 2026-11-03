using System.Buffers.Binary;
using System.Text;

using HaveIBeenPwned.AddressExtractor.Objects;
using HaveIBeenPwned.AddressExtractor.Objects.Readers;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenMcdf;

namespace HaveIBeenPwned.AddressExtractor.Tests;

[TestClass]
public class OutlookMessageReaderTests
{
    [TestMethod]
    public async Task ExtractsMessageFieldsBodiesRecipientsAndEmbeddedMessagesAsync()
    {
        var path = TempPath();
        var output = $"{path}.txt";
        try
        {
            using (var root = RootStorage.Create(path))
            {
                WriteProperties(root, 32);
                WriteText(root, "0037001F", "Subject subject@example.com");
                WriteText(root, "0C1F001F", "SENDER@EXAMPLE.COM");
                WriteText(root, "1000001F", "First body@example.com\r\nsender@example.com");
                WriteText(root, "007D001F", "Reply-To: =?utf-8?Q?header=40example.com?=");
                WriteBytes(root, "10130102", Encoding.UTF8.GetBytes(
                    "<p>html&#64;example.com</p><p>rich@<b>example.com</b></p><a href=\"mailto:link@example.com\">Contact</a>"
                    + "<p>not-an-address@</p><p>example.com</p>"));
                var recipient = root.CreateStorage("__recip_version1.0_#00000000");
                WriteProperties(recipient, 8);
                WriteText(recipient, "39FE001F", "recipient@example.com");
                var attachment = root.CreateStorage("__attach_version1.0_#00000000");
                WriteProperties(attachment, 8);
                WriteBytes(attachment, "37010102", "binary-attachment@example.com"u8.ToArray());
                var embedded = attachment.CreateStorage("__substg1.0_3701000D");
                WriteProperties(embedded, 24);
                WriteText(embedded, "1000001F", "embedded@example.com");
            }

            await using var reader = new Runtime().GetExtensionFromPath(path).GetReader(path);
            Assert.IsInstanceOfType<OutlookMessageReader>(reader);
            Assert.AreEqual(0, await Program.Main([path, "-y", "-o", output]).ConfigureAwait(false));
            CollectionAssert.AreEqual(
                new[]
                {
                    "body@example.com", "embedded@example.com", "header@example.com", "html@example.com",
                    "link@example.com", "recipient@example.com", "rich@example.com", "sender@example.com", "subject@example.com"
                },
                await File.ReadAllLinesAsync(output).ConfigureAwait(false));
            StringAssert.Contains(
                await File.ReadAllTextAsync($"{output}{Config.Defaults.REPORT_FILE_SUFFIX}").ConfigureAwait(false),
                $"{path}: 9");
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            DeleteMessage(path);
        }
    }

    [TestMethod]
    public async Task DecodesMessageAndBodyCodePagesAsync()
    {
        var path = TempPath();
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using (var root = RootStorage.Create(path))
            {
                WriteProperties(root, 32, (0x3FFD0003, 1251), (0x3FDE0003, 65001));
                WriteBytes(root, "0037001E", Encoding.GetEncoding(1251).GetBytes("\u041f\u0440\u0438\u0432\u0435\u0442 legacy@example.com\0"));
                WriteBytes(root, "1000001E", Encoding.UTF8.GetBytes("\u4e2d\u6587 body@example.com\0"));
                var recipient = root.CreateStorage("__recip_version1.0_#00000000");
                WriteProperties(recipient, 8);
                WriteBytes(recipient, "3001001E", Encoding.GetEncoding(1251).GetBytes("\u041f\u0440\u0438\u0432\u0435\u0442 recipient@example.com\0"));
            }

            CollectionAssert.AreEquivalent(
                new[] { "\u041f\u0440\u0438\u0432\u0435\u0442 legacy@example.com", "\u4e2d\u6587 body@example.com", "\u041f\u0440\u0438\u0432\u0435\u0442 recipient@example.com" },
                await ReadLinesAsync(path).ConfigureAwait(false));
        }
        finally
        {
            DeleteMessage(path);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task DecodesRtfBodiesAsync(bool compressed)
    {
        var path = TempPath();
        try
        {
            using (var root = RootStorage.Create(path))
            {
                WriteProperties(root, 32);
                WriteBytes(root, "10090102", CreateRtf(@"{\rtf1\ansi rich{\b @}example.com\par escaped\'40example.com}", compressed));
            }

            var lines = await ReadLinesAsync(path).ConfigureAwait(false);
            StringAssert.Contains(string.Join("\n", lines), "rich@example.com");
            StringAssert.Contains(string.Join("\n", lines), "escaped@example.com");
        }
        finally
        {
            DeleteMessage(path);
        }
    }

    [TestMethod]
    public async Task LargeEncodedMessageBypassesRawAtSymbolScanAsync()
    {
        var path = TempPath();
        try
        {
            using (var root = RootStorage.Create(path))
            {
                WriteProperties(root, 32);
                WriteBytes(root, "10130102", "<p>encoded&#64;example.com</p>"u8.ToArray());
            }

            using (var padding = new FileStream(path, FileMode.Open, FileAccess.Write))
            {
                padding.SetLength(Config.Defaults.MINIMUM_FILE_SIZE_FOR_AT_SYMBOL_QUICK_SCAN);
            }

            Assert.IsTrue(new FileInfo(path).Length >= Config.Defaults.MINIMUM_FILE_SIZE_FOR_AT_SYMBOL_QUICK_SCAN);
            var (containsAtSymbol, _) = await AddressExtractorMonitor.QuickScanForAtSymbolAsync(new FileInfo(path)).ConfigureAwait(false);
            Assert.IsFalse(containsAtSymbol);
            Assert.AreEqual(0, await Program.Main([path, "-y", "-o", $"{path}.txt"]).ConfigureAwait(false));
            CollectionAssert.AreEqual(new[] { "encoded@example.com" }, await File.ReadAllLinesAsync($"{path}.txt").ConfigureAwait(false));
        }
        finally
        {
            DeleteMessage(path);
        }
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task CancellationAndEarlyExitReleaseFileAsync(bool cancel)
    {
        var path = TempPath();
        try
        {
            using (var root = RootStorage.Create(path))
            {
                WriteProperties(root, 32);
                WriteText(root, "1000001F", "first@example.com\nsecond@example.com");
            }

            using var cancellation = new CancellationTokenSource();
            await using var reader = new OutlookMessageReader(path);
            await using (var enumerator = reader.ReadLineAsync(cancellation.Token).GetAsyncEnumerator())
            {
                Assert.IsTrue(await enumerator.MoveNextAsync().ConfigureAwait(false));
                if (cancel)
                {
                    await cancellation.CancelAsync().ConfigureAwait(false);
                    await Assert.ThrowsExceptionAsync<OperationCanceledException>(
                        async () => { await enumerator.MoveNextAsync().ConfigureAwait(false); }).ConfigureAwait(false);
                }
            }

            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            DeleteMessage(path);
        }
    }

    [TestMethod]
    public async Task PreCancelledReadDoesNotOpenFileAsync()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync().ConfigureAwait(false);
        await using var reader = new OutlookMessageReader(TempPath());
        await using var enumerator = reader.ReadLineAsync(cancellation.Token).GetAsyncEnumerator();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            async () => { await enumerator.MoveNextAsync().ConfigureAwait(false); }).ConfigureAwait(false);
    }

    [TestMethod]
    [DataRow("properties")]
    [DataRow("rtf-header")]
    [DataRow("rtf-checksum")]
    public async Task MalformedMessageRaisesErrorAndReleasesFileAsync(string corruption)
    {
        var path = TempPath();
        try
        {
            using (var root = RootStorage.Create(path))
            {
                WriteProperties(root, corruption == "properties" ? 31 : 32);
                if (corruption != "properties")
                {
                    var rtf = CreateRtf(@"{\rtf1\ansi text}", true);
                    rtf[corruption == "rtf-header" ? 8 : 12] ^= 0xFF;
                    WriteBytes(root, "10090102", rtf);
                }
            }

            await Assert.ThrowsExceptionAsync<InvalidDataException>(
                async () => { await ReadLinesAsync(path).ConfigureAwait(false); }).ConfigureAwait(false);
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            DeleteMessage(path);
        }
    }

    private static async Task<string?[]> ReadLinesAsync(string path)
    {
        await using var reader = new OutlookMessageReader(path);
        var lines = new List<string?>();
        await foreach (var line in reader.ReadLineAsync().ConfigureAwait(false))
        {
            lines.Add(line);
        }

        return lines.ToArray();
    }

    private static void WriteProperties(Storage storage, int headerSize, params (uint Tag, int Value)[] properties)
    {
        using var stream = storage.CreateStream("__properties_version1.0");
        stream.Write(new byte[headerSize]);
        using var writer = new BinaryWriter(stream);
        foreach (var (tag, value) in properties)
        {
            writer.Write(tag);
            writer.Write(0);
            writer.Write(value);
            writer.Write(0);
        }
    }

    private static void WriteText(Storage storage, string tag, string text)
        => WriteBytes(storage, tag, Encoding.Unicode.GetBytes(text + "\0"));

    private static void WriteBytes(Storage storage, string tag, byte[] bytes)
    {
        using var stream = storage.CreateStream($"__substg1.0_{tag}");
        stream.Write(bytes);
    }

    private static byte[] CreateRtf(string rtf, bool compressed)
    {
        var raw = Encoding.ASCII.GetBytes(rtf);
        using var payload = new MemoryStream();
        if (compressed)
        {
            // Literal-only LZFu packets, followed by a reference to the current dictionary write position.
            var offset = 0;
            while (true)
            {
                var count = Math.Min(8, raw.Length - offset);
                payload.WriteByte(count < 8 ? (byte)(1 << count) : (byte)0);
                payload.Write(raw, offset, count);
                offset += count;
                if (count < 8)
                {
                    var position = (207 + offset) % 4096;
                    payload.WriteByte((byte)(position >> 4));
                    payload.WriteByte((byte)((position & 15) << 4));
                    break;
                }
            }
        }
        else
        {
            payload.Write(raw);
        }

        var bytes = payload.ToArray();
        var crc = 0u;
        if (compressed)
        {
            foreach (var value in bytes)
            {
                crc ^= value;
                for (var bit = 0; bit < 8; bit++)
                {
                    crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xEDB88320u : 0);
                }
            }
        }

        var result = new byte[16 + bytes.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(result, (uint)(result.Length - 4));
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4), (uint)raw.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(8), compressed ? 0x75465A4Cu : 0x414C454Du);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(12), crc);
        bytes.CopyTo(result, 16);
        return result;
    }

    private static string TempPath()
        => Path.Combine(Path.GetTempPath(), $"EmailAddressExtractorTests-{Guid.NewGuid():N}.MSG");

    private static void DeleteMessage(string path)
    {
        File.Delete(path);
        File.Delete($"{path}.txt");
        File.Delete($"{path}.txt{Config.Defaults.REPORT_FILE_SUFFIX}");
    }
}
