using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HaveIBeenPwned.AddressExtractor.Tests;

[TestClass]
public class QuickAtSymbolScanTests
{
    [TestMethod]
    public async Task QuickScanFindsAtSymbolAsync()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "before\ntest@example.com\nafter").ConfigureAwait(false);

            var file = new FileInfo(path);
            var (containsAtSymbol, bytesRead) = await AddressExtractorMonitor.QuickScanForAtSymbolAsync(file).ConfigureAwait(false);

            Assert.IsTrue(containsAtSymbol, "Quick scan should find '@' when the file contains one");
            Assert.IsTrue(bytesRead > 0, "Quick scan should read at least part of the file");
            Assert.IsTrue(bytesRead <= file.Length, "Quick scan should not report reading more than the file length");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task QuickScanStopsAtEndWhenNoAtSymbolExistsAsync()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "before\nno email addresses here\nafter").ConfigureAwait(false);

            var file = new FileInfo(path);
            var (containsAtSymbol, bytesRead) = await AddressExtractorMonitor.QuickScanForAtSymbolAsync(file).ConfigureAwait(false);

            Assert.IsFalse(containsAtSymbol, "Quick scan should return false when the file contains no '@'");
            Assert.AreEqual(file.Length, bytesRead, "Quick scan should read the full file when no '@' is found");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
