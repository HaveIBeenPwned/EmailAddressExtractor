using HaveIBeenPwned.AddressExtractor.Objects;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HaveIBeenPwned.AddressExtractor.Tests;

[TestClass]
[DoNotParallelize]
public class FileCollectionTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void SummarySeparatesUnknownTypesAndSortsByTotalSizeThenCount(bool processAllExtensions)
    {
        var inputs = new (string Name, int Size)[]
        {
            ("one.smallunknown", 10), ("two.smallunknown", 10),
            ("three.smallunknown", 10), ("four.smallunknown", 10),
            ("one.lesscommon", 30), ("two.lesscommon", 30),
            ("one.morecommon", 20), ("two.MORECOMMON", 20), ("three.morecommon", 20),
            ("one.largeunknown", 90), ("no-extension", 0),
            ("one.txt", 10), ("two.TXT", 10), ("one.zip", 5)
        };

        var (output, files) = Summarize(inputs, processAllExtensions);
        var knownHeading = processAllExtensions
            ? "Known and processed file types: 3 files: 25 Bytes"
            : "Known and processed file types: 2 files: 20 Bytes";
        var ignoredHeading = processAllExtensions
            ? "Known and ignored file types: 0 files: 0 Bytes"
            : "Known and ignored file types: 1 files: 5 Bytes";
        var unknownHeading = $"Unknown and {(processAllExtensions ? "processed" : "ignored")} file types: 11 files: {ByteExtensions.Format(250)}";
        StringAssert.Contains(output, "Found 14 files:");
        StringAssert.Contains(output, knownHeading);
        StringAssert.Contains(output, ignoredHeading);
        StringAssert.Contains(output, unknownHeading);
        AssertOrdered(output, knownHeading, "  .txt", processAllExtensions ? "  .zip" : ignoredHeading,
            processAllExtensions ? ignoredHeading : "  .zip", unknownHeading,
            "  .largeunknown", "  .morecommon", "  .lesscommon", "  .smallunknown", "  (no extension)");
        StringAssert.Contains(output, ".morecommon 3 files: 60 Bytes");
        StringAssert.Contains(output, ".lesscommon 2 files: 60 Bytes");
        Assert.IsFalse(output.Contains("Skipping (Unknown Extension)", StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("Skipping", StringComparison.Ordinal));
        Assert.AreEqual(!processAllExtensions, output.Contains("(Archive files)", StringComparison.Ordinal));
        foreach (var heading in new[] { knownHeading, ignoredHeading, unknownHeading })
        {
            StringAssert.Contains(output, Environment.NewLine + Environment.NewLine + heading);
        }
        Assert.IsFalse(output.Contains('[', StringComparison.Ordinal), "The entire initial summary should omit timestamps.");
        CollectionAssert.AreEquivalent(
            inputs.Where(input => processAllExtensions || input.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                .Select(input => input.Name).ToArray(),
            files);
    }

    [TestMethod]
    [DataRow(".txt", 1, 0, 0)]
    [DataRow(".zip", 0, 1, 0)]
    [DataRow(".unknown", 0, 0, 1)]
    public void SummaryIncludesSeparateTotalsWhenSectionsAreEmpty(string extension, int processedCount, int ignoredCount, int unknownCount)
    {
        var (output, _) = Summarize([("one" + extension, 12)]);

        StringAssert.Contains(output, $"Known and processed file types: {processedCount} files: {processedCount * 12} Bytes");
        StringAssert.Contains(output, $"Known and ignored file types: {ignoredCount} files: {ignoredCount * 12} Bytes");
        StringAssert.Contains(output, $"Unknown and ignored file types: {unknownCount} files: {unknownCount * 12} Bytes");
    }

    [TestMethod]
    public void SummaryHandlesNoFiles()
    {
        var (output, files) = Summarize([]);

        StringAssert.Contains(output, "Found 0 files:");
        StringAssert.Contains(output, "Known and processed file types: 0 files: 0 Bytes");
        StringAssert.Contains(output, "Known and ignored file types: 0 files: 0 Bytes");
        StringAssert.Contains(output, "Unknown and ignored file types: 0 files: 0 Bytes");
        Assert.AreEqual(0, files.Length);
    }

    [TestMethod]
    public void KnownIgnoredTypesSortByTotalSizeThenCount()
    {
        var (output, files) = Summarize(
        [
            ("one.zip", 10), ("two.zip", 10), ("three.zip", 10), ("four.zip", 10),
            ("one.jpg", 30), ("two.jpg", 30),
            ("one.mp3", 20), ("two.mp3", 20), ("three.mp3", 20),
            ("one.exe", 90)
        ]);

        StringAssert.Contains(output, $"Known and ignored file types: 10 files: {ByteExtensions.Format(250)}");
        AssertOrdered(output, "Known and ignored file types", "  .exe", "  .mp3", "  .jpg", "  .zip",
            "Unknown and ignored file types");
        Assert.AreEqual(0, files.Length);
    }

    [TestMethod]
    public void ProcessingResultsRetainTimestamps()
    {
        var originalOutput = Console.Out;
        using var output = new StringWriter();
        try
        {
            Console.SetOut(output);
            Output.Write("Extracting...");
            Output.FileResult(1, "example.txt", 12, "1 address");
        }
        finally
        {
            Console.SetOut(originalOutput);
        }

        var lines = output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.AreEqual(2, lines.Length);
        StringAssert.StartsWith(lines[0], "[");
        StringAssert.EndsWith(lines[0], "] Extracting...");
        StringAssert.Matches(lines[1], new System.Text.RegularExpressions.Regex(
            @"^\[\d{2}:\d{2}:\d{2}\] File number 1 example\.txt \[12 Bytes\] - 1 address$"));
    }

    private static void AssertOrdered(string output, params string[] values)
    {
        var previousIndex = -1;
        foreach (var value in values)
        {
            var index = output.IndexOf(value, StringComparison.Ordinal);
            Assert.IsTrue(index > previousIndex, $"Expected '{value}' after the previous entry in:\n{output}");
            previousIndex = index;
        }
    }

    private static (string Output, string[] Files) Summarize(
        (string Name, int Size)[] inputs, bool processAllExtensions = false)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"AddressExtractorTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var originalOutput = Console.Out;
        using var output = new StringWriter();
        try
        {
            foreach (var (name, size) in inputs)
            {
                File.WriteAllBytes(Path.Combine(directory, name), new byte[size]);
            }

            var args = processAllExtensions
                ? new[] { directory, "--processAllExtensions" }
                : new[] { directory };
            var config = CommandLineProcessor.Parse(args, out var paths);
            Console.SetOut(output);
            var files = new FileCollection(new Runtime(config), paths);
            return (output.ToString(), files.Select(file => file.Name).ToArray());
        }
        finally
        {
            Console.SetOut(originalOutput);
            Directory.Delete(directory, recursive: true);
        }
    }
}
