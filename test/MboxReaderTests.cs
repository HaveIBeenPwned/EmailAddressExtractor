using System.Text;

using HaveIBeenPwned.AddressExtractor.Objects;
using HaveIBeenPwned.AddressExtractor.Objects.Readers;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HaveIBeenPwned.AddressExtractor.Tests;

[TestClass]
public class MboxReaderTests
{
    [DataTestMethod]
    [DataRow("\n")]
    [DataRow("\r\n")]
    public async Task ExtractsMultipleMessagesHeadersAndEscapedFromLinesAsync(string newline)
    {
        var path = await WriteMailboxAsync("""
            From envelope@example.com Sat Oct 24 12:00:00 2026
            From: SENDER@EXAMPLE.COM
            To: first@example.com,
             second@example.com
            Cc: copy@example.com
            Bcc: hidden@example.com
            Reply-To: reply@example.com
            Return-Path: <return@example.com>
            Subject: =?utf-8?Q?subject=40example.com?=
            X-Contact: =?utf-8?B?ZW5jb2RlZC1oZWFkZXJAZXhhbXBsZS5jb20=?=

            BODY@EXAMPLE.COM
            >From quoted@example.com Sat Oct 24 12:00:00 2026
            >>From twice-quoted@example.com Sat Oct 24 12:00:00 2026
            after-quoted@example.com

            From local Sat Oct 24 12:01:00 2026
            From: other@example.com

            final@example.com
            """.ReplaceLineEndings(newline)).ConfigureAwait(false);
        try
        {
            var addresses = await ExtractAsync(path).ConfigureAwait(false);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    "envelope@example.com", "sender@example.com", "first@example.com", "second@example.com",
                    "copy@example.com", "hidden@example.com", "reply@example.com", "return@example.com",
                    "subject@example.com", "encoded-header@example.com", "body@example.com",
                    "quoted@example.com", "twice-quoted@example.com", "after-quoted@example.com",
                    "other@example.com", "final@example.com"
                }, addresses);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task DecodesMultipartTextAndNestedMessagesButNotBinaryAttachmentsAsync()
    {
        var path = await WriteMailboxAsync($"""
            From local Sat Oct 24 12:00:00 2026
            MIME-Version: 1.0
            Content-Type: multipart/mixed; boundary="outer"

            --outer
            Content-Type: multipart/alternative; boundary="alternative"

            --alternative
            Content-Type: text/plain; charset=utf-8
            Content-Transfer-Encoding: quoted-printable

            quoted=40exam=
            ple.com
            --alternative
            Content-Type: text/html; charset=utf-8
            Content-Transfer-Encoding: base64

            {Convert.ToBase64String(Encoding.UTF8.GetBytes("<p>HTML&#64;example.com</p>"), Base64FormattingOptions.InsertLineBreaks)}
            --alternative--
            --outer
            Content-Type: text/plain; charset=windows-1252
            Content-Disposition: attachment; filename="contacts.txt"
            Content-Transfer-Encoding: quoted-printable

            =93attachment@example.com=94
            --outer
            Content-Type: message/rfc822
            Content-Disposition: attachment; filename="forwarded.eml"

            From: nested-sender@example.com
            Subject: =?utf-8?Q?nested-subject=40example.com?=
            Content-Type: text/plain
            Content-Transfer-Encoding: base64

            {Convert.ToBase64String(Encoding.UTF8.GetBytes("nested-body@example.com"))}
            --outer
            Content-Type: application/octet-stream
            Content-Transfer-Encoding: base64

            {Convert.ToBase64String(Encoding.UTF8.GetBytes("binary@example.com"))}
            --outer
            Content-Type: application/octet-stream

            raw-binary@example.com
            --outer--
            """).ConfigureAwait(false);
        try
        {
            CollectionAssert.AreEquivalent(
                new[]
                {
                    "quoted@example.com", "html@example.com", "attachment@example.com",
                    "nested-sender@example.com", "nested-subject@example.com", "nested-body@example.com"
                },
                await ExtractAsync(path).ConfigureAwait(false));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [DataTestMethod]
    [DataRow("utf-16")]
    [DataRow("iso-8859-1")]
    [DataRow("windows-1252")]
    public async Task ReadsDecodedTextLineByLineUsingDeclaredCharsetAsync(string charset)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        const string text = "first@example.com\nsecond@example.com\nthird@example.com";
        var encoded = Convert.ToBase64String(Encoding.GetEncoding(charset).GetBytes(text), Base64FormattingOptions.InsertLineBreaks);
        var path = await WriteMailboxAsync($"""
            From local Sat Oct 24 12:00:00 2026
            Content-Type: text/plain; charset={charset}
            Content-Transfer-Encoding: base64

            {encoded}
            """).ConfigureAwait(false);
        try
        {
            await using var reader = new MboxReader(path);
            var lines = new List<string?>();
            await foreach (var line in reader.ReadLineAsync().ConfigureAwait(false))
            {
                lines.Add(line);
            }

            CollectionAssert.AreEqual(text.Split('\n'), lines.TakeLast(3).ToArray());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task RespectsContentLengthWhenBodyContainsUnescapedFromLineAsync()
    {
        const string body = "before@example.com\nFrom local Sat Oct 24 12:00:00 2026\nX-Contact: inside@example.com\n\n";
        var path = await WriteMailboxAsync(
            $"From local Sat Oct 24 12:00:00 2026\nContent-Length: {Encoding.UTF8.GetByteCount(body)}\n\n{body}\n"
            + "From local Sat Oct 24 12:01:00 2026\nSubject: next\n\nlast@example.com").ConfigureAwait(false);
        try
        {
            CollectionAssert.AreEquivalent(
                new[] { "before@example.com", "inside@example.com", "last@example.com" },
                await ExtractAsync(path).ConfigureAwait(false));

            await using var reader = new MboxReader(path);
            var lines = new List<string?>();
            await foreach (var line in reader.ReadLineAsync().ConfigureAwait(false))
            {
                lines.Add(line);
            }

            CollectionAssert.Contains(lines, "X-Contact: inside@example.com",
                "A header-like body line must not be parsed as a new message header.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task EmptyMailboxProducesNoLinesAsync()
    {
        var path = await WriteMailboxAsync(string.Empty).ConfigureAwait(false);
        try
        {
            await using var reader = new MboxReader(path);
            await foreach (var line in reader.ReadLineAsync().ConfigureAwait(false))
            {
                Assert.Fail("An empty mailbox must not produce any lines.");
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task CancellationReleasesMailboxAsync(bool cancelBeforeReading)
    {
        var path = await WriteMailboxAsync(
            "From local Sat Oct 24 12:00:00 2026\nSubject: contacts\n\none@example.com\ntwo@example.com").ConfigureAwait(false);
        try
        {
            using var cancellation = new CancellationTokenSource();
            await using var reader = new MboxReader(path);
            await using (var enumerator = reader.ReadLineAsync(cancellation.Token).GetAsyncEnumerator())
            {
                if (!cancelBeforeReading)
                {
                    while (await enumerator.MoveNextAsync().ConfigureAwait(false)
                        && enumerator.Current != "one@example.com")
                    {
                    }

                    Assert.AreEqual("one@example.com", enumerator.Current);
                }

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
    public async Task EarlyEnumerationExitReleasesMailboxAsync()
    {
        var path = await WriteMailboxAsync(
            "From local Sat Oct 24 12:00:00 2026\nSubject: contacts\n\none@example.com\ntwo@example.com").ConfigureAwait(false);
        try
        {
            await using var reader = new MboxReader(path);
            await foreach (var line in reader.ReadLineAsync().ConfigureAwait(false))
            {
                if (line == "one@example.com")
                {
                    break;
                }
            }

            using var exclusiveAccess = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task MissingMboxSeparatorFallsBackToPlainTextAndReleasesFileAsync()
    {
        var path = await WriteMailboxAsync("This is not a mailbox.\ncontact@example.com").ConfigureAwait(false);
        try
        {
            CollectionAssert.AreEquivalent(new[] { "contact@example.com" }, await ExtractAsync(path).ConfigureAwait(false));
            using var exclusiveAccess = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task UnsupportedCharsetFallsBackWithoutDroppingTextOrLaterMessagesAsync()
    {
        var path = await WriteMailboxAsync(
            "From local Sat Oct 24 12:00:00 2026\nContent-Type: text/plain; charset=not-a-real-charset\n"
            + "Content-Transfer-Encoding: quoted-printable\n\ncontact=40example.com\n"
            + "From local Sat Oct 24 12:01:00 2026\nSubject: next\n\nlater@example.com").ConfigureAwait(false);
        try
        {
            CollectionAssert.AreEquivalent(new[] { "contact@example.com", "later@example.com" }, await ExtractAsync(path).ConfigureAwait(false));
            using var exclusiveAccess = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [DataTestMethod]
    [DataRow("\n")]
    [DataRow("\r\n")]
    public async Task RecoversUnescapedFromLinesAndResumesMimeDecodingAsync(string newline)
    {
        var path = await WriteMailboxAsync("""
            From local Sat Oct 24 12:00:00 2026
            Subject: first message

            before@example.com
            From not a real message separator
             continuation without a header
            <div>recovered@example.com</div>
            From another false separator
             more body content
            middle@example.com

            From local Sat Oct 24 12:01:00 2026
            Subject: valid message after damaged text
            Content-Type: text/plain
            Content-Transfer-Encoding: base64

            ZW5jb2RlZEBleGFtcGxlLmNvbQ==
            From final false separator
             this is body text, not a header
            tail@example.com
            """.ReplaceLineEndings(newline)).ConfigureAwait(false);
        var originalOutput = Console.Out;
        using var notices = new StringWriter();
        try
        {
            Console.SetOut(notices);
            CollectionAssert.AreEquivalent(
                new[] { "before@example.com", "recovered@example.com", "middle@example.com", "encoded@example.com", "tail@example.com" },
                await ExtractAsync(path).ConfigureAwait(false));
            StringAssert.Contains(notices.ToString(), "[NOTICE]");
            StringAssert.Contains(notices.ToString(), "plain text");
            using var exclusiveAccess = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            Console.SetOut(originalOutput);
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task ReadsLeadingFragmentBeforeFirstMessageAsync()
    {
        var path = await WriteMailboxAsync(
            "Leading fragment \u00e9\nfragment@example.com\n"
            + "From local Sat Oct 24 12:00:00 2026\nContent-Transfer-Encoding: quoted-printable\n\nbody=40example.com").ConfigureAwait(false);
        try
        {
            CollectionAssert.AreEquivalent(
                new[] { "fragment@example.com", "body@example.com" },
                await ExtractAsync(path).ConfigureAwait(false));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task CancellationDuringPlainTextRecoveryReleasesMailboxAsync()
    {
        var path = await WriteMailboxAsync("fragment@example.com\nsecond@example.com\nthird@example.com").ConfigureAwait(false);
        try
        {
            using var cancellation = new CancellationTokenSource();
            await using var reader = new MboxReader(path);
            await using (var enumerator = reader.ReadLineAsync(cancellation.Token).GetAsyncEnumerator())
            {
                Assert.IsTrue(await enumerator.MoveNextAsync().ConfigureAwait(false));
                Assert.AreEqual("fragment@example.com", enumerator.Current);
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
    public async Task CliRecoversLargeDamagedSectionWithoutLosingLaterEncodedAddressesAsync()
    {
        var padding = string.Concat(Enumerable.Repeat(new string('x', 999) + "\n", 100));
        var path = await WriteMailboxAsync(
            "From local Sat Oct 24 12:00:00 2026\nSubject: first\n\nbefore@example.com\n"
            + "From false separator\n continuation without a header\n"
            + padding + "\u00e9 recovered@example.com\n"
            + "From local Sat Oct 24 12:01:00 2026\nContent-Transfer-Encoding: base64\n\n"
            + Convert.ToBase64String(Encoding.UTF8.GetBytes(padding + "encoded@example.com"), Base64FormattingOptions.InsertLineBreaks)).ConfigureAwait(false);
        var output = $"{path}.txt";
        try
        {
            var exitCode = await Program.Main([path, "-y", "-q", "-o", output]).ConfigureAwait(false);

            Assert.AreEqual(0, exitCode);
            CollectionAssert.AreEqual(
                new[] { "before@example.com", "encoded@example.com", "recovered@example.com" },
                await File.ReadAllLinesAsync(output).ConfigureAwait(false));
            StringAssert.Contains(
                await File.ReadAllTextAsync($"{output}{Config.Defaults.REPORT_FILE_SUFFIX}").ConfigureAwait(false),
                $"{path}: 3");
        }
        finally
        {
            File.Delete(path);
            File.Delete(output);
            File.Delete($"{output}{Config.Defaults.REPORT_FILE_SUFFIX}");
        }
    }

    [DataTestMethod]
    [DataRow("From ")]
    [DataRow("From \n")]
    [DataRow("From invalid\n continuation\nvisible@example.com")]
    public async Task TruncatedFinalMarkerDoesNotLoopOrDropTextAsync(string content)
    {
        var path = await WriteMailboxAsync(content).ConfigureAwait(false);
        try
        {
            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await using var reader = new MboxReader(path);
            var lines = new List<string?>();
            await foreach (var line in reader.ReadLineAsync(cancellation.Token).ConfigureAwait(false))
            {
                lines.Add(line);
            }

            CollectionAssert.AreEqual(content.TrimEnd('\n').Split('\n'), lines.ToArray());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [DataTestMethod]
    [DataRow(65534)]
    [DataRow(65535)]
    [DataRow(65536)]
    [DataRow(65537)]
    public async Task RecoveryFindsMarkersAcrossScanBufferBoundariesAsync(int markerOffset)
    {
        const string fragment = "fragment@example.com\n";
        var path = await WriteMailboxAsync(
            fragment + new string('x', markerOffset - fragment.Length - 1) + "\n"
            + "From local Sat Oct 24 12:00:00 2026\nContent-Transfer-Encoding: base64\n\n"
            + "ZW5jb2RlZEBleGFtcGxlLmNvbQ==").ConfigureAwait(false);
        try
        {
            CollectionAssert.AreEquivalent(
                new[] { "fragment@example.com", "encoded@example.com" },
                await ExtractAsync(path).ConfigureAwait(false));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [DataTestMethod]
    [DataRow(".mbox", "base64", "ZW5jb2RlZEBleGFtcGxlLmNvbQ==")]
    [DataRow(".MBOX", "quoted-printable", "encoded=40example.com")]
    public async Task LargeEncodedMailboxIsNotSkippedByRawAtSymbolScanAsync(string extension, string transferEncoding, string body)
    {
        var path = await WriteMailboxAsync(
            "From local Sat Oct 24 12:00:00 2026\nSubject: padding\n\n", extension).ConfigureAwait(false);
        var output = $"{path}.txt";
        try
        {
            await using (var writer = new StreamWriter(path, append: true))
            {
                var padding = new string('x', 999) + "\n";
                for (var i = 0; i < Config.Defaults.MINIMUM_FILE_SIZE_FOR_AT_SYMBOL_QUICK_SCAN / padding.Length; i++)
                {
                    await writer.WriteAsync(padding).ConfigureAwait(false);
                }

                await writer.WriteAsync(
                    $"\nFrom local Sat Oct 24 12:01:00 2026\nContent-Transfer-Encoding: {transferEncoding}\n\n{body}\n").ConfigureAwait(false);
            }

            Assert.IsTrue(new FileInfo(path).Length >= Config.Defaults.MINIMUM_FILE_SIZE_FOR_AT_SYMBOL_QUICK_SCAN);
            var (containsAtSymbol, _) = await AddressExtractorMonitor.QuickScanForAtSymbolAsync(new FileInfo(path)).ConfigureAwait(false);
            Assert.IsFalse(containsAtSymbol, "The fixture must contain no literal '@'.");

            var exitCode = await Program.Main([path, "-y", "-q", "-o", output]).ConfigureAwait(false);

            Assert.AreEqual(0, exitCode);
            CollectionAssert.AreEqual(new[] { "encoded@example.com" }, await File.ReadAllLinesAsync(output).ConfigureAwait(false));
            StringAssert.Contains(await File.ReadAllTextAsync($"{output}{Config.Defaults.REPORT_FILE_SUFFIX}").ConfigureAwait(false), $"{path}: 1");
        }
        finally
        {
            File.Delete(path);
            File.Delete(output);
            File.Delete($"{output}{Config.Defaults.REPORT_FILE_SUFFIX}");
        }
    }

    [TestMethod]
    public async Task CliNormalizesDeduplicatesSortsAndReportsCountsPerMailboxAsync()
    {
        var first = await WriteMailboxAsync(
            "From ZED@example.com Sat Oct 24 12:00:00 2026\nTo: ALPHA@example.com\n\nzed@example.com\ninvalid@not-a-real-tld").ConfigureAwait(false);
        var second = await WriteMailboxAsync(
            "From local Sat Oct 24 12:00:00 2026\nTo: alpha@example.com\n\nmiddle@example.com").ConfigureAwait(false);
        var output = $"{first}.txt";
        try
        {
            var exitCode = await Program.Main([first, second, "-y", "-q", "-o", output]).ConfigureAwait(false);

            Assert.AreEqual(0, exitCode);
            CollectionAssert.AreEqual(
                new[] { "alpha@example.com", "middle@example.com", "zed@example.com" },
                await File.ReadAllLinesAsync(output).ConfigureAwait(false));
            var report = await File.ReadAllTextAsync($"{output}{Config.Defaults.REPORT_FILE_SUFFIX}").ConfigureAwait(false);
            StringAssert.Contains(report, $"{first}: 2");
            StringAssert.Contains(report, $"{second}: 2");
        }
        finally
        {
            File.Delete(first);
            File.Delete(second);
            File.Delete(output);
            File.Delete($"{output}{Config.Defaults.REPORT_FILE_SUFFIX}");
        }
    }

    private static async Task<string> WriteMailboxAsync(string content, string extension = ".mbox")
    {
        var path = Path.Combine(Path.GetTempPath(), $"EmailAddressExtractorTests-{Guid.NewGuid():N}{extension}");
        await File.WriteAllTextAsync(path, content).ConfigureAwait(false);
        return path;
    }

    private static async Task<string[]> ExtractAsync(string path)
    {
        var runtime = new Runtime();
        Assert.IsTrue(runtime.IsKnownExtension(Path.GetExtension(path)));
        await using var reader = runtime.GetExtensionFromPath(path).GetReader(path);
        var addresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await foreach (var address in AddressExtractor.ExtractFileAddressesAsync(reader).ConfigureAwait(false))
        {
            addresses.Add(address);
        }

        return addresses.ToArray();
    }
}
