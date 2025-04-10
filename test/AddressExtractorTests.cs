using HaveIBeenPwned.AddressExtractor.Objects;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HaveIBeenPwned.AddressExtractor.Tests;

[TestClass]
public class AddressExtractorTests
{
    [TestMethod]
    public async Task CorrectNumberOfAddressesAreExtractedFromFileAsync()
    {
        var expected = new List<string> {
              "test1@example.com",
              "test3@example.com",
              "test4@example.com",
              "test-5@example.com",
              "test+6@example.com",
              "test_8@example.com",
              "test.9@example.com",
              "test&12@example.com",
              "13@exmple.com",
              "!test14@example.com",
              "15@example.com" };

        var result = await ExtractAddressesFromFileAsync(@"../../../../TestData/SingleFile/SingleSmallFile.txt").ConfigureAwait(false);

        // Assert
        Assert.AreEqual(11, result.Count, $"11 email addresses should be found, result was missing:\n{string.Join("\n", expected.Except(result))}");
    }

    [TestMethod]
    public async Task AddressAfterBlankLineIsFoundAsync()
    {
        var result = await ExtractAddressesFromFileAsync(@"../../../../TestData/SingleFile/FileWithBlankLine.txt").ConfigureAwait(false);

        // Assert
        Assert.AreEqual(1, result.Count, "One address should be extracted");
    }

    [TestMethod]
    public void EmailAddressesAreNotCaseSensitive()
    {
        const string ADDRESSES = "test@example.com TEST@EXAMPLE.COM";

        // Act
        var result = AddressExtractor.ExtractAddresses(ADDRESSES);

        // Assert
        Assert.AreEqual(1, result.Count, "Same addresses of different case should be merged");
    }

    [TestMethod]
    public void EmailAddressesAreConvertedToLowercase()
    {
        // Arrange
        const string INPUT = "TEST@EXAMPLE.COM";
        const string EXPECTED = "test@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(1, result.Count, "One address should be extracted");
        Assert.AreEqual(EXPECTED, result.First(), "Address should always be converted to lowercase");
    }

    [TestMethod]
    public void EmailAddressesInSingleQuotesIsExtracted()
    {
        // Arrange
        const string INPUT = "'test@example.com'";
        const string EXPECTED = "test@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(1, result.Count, "One address should be extracted");
        Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted from single quotes");
    }

    [TestMethod]
    public void EmailAddressesInSingleQuotesWithTrailingSpaceIsExtracted()
    {
        // Arrange
        const string INPUT = "'test@example.com '";
        const string EXPECTED = "test@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(1, result.Count, "One address should be extracted");
        Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted from single quotes with trailing space");
    }

    [TestMethod]
    public void EmailAddressesInUrlIsExtracted()
    {
        // Arrange
        const string INPUT = "https://example.com/path/test@example.com";
        const string EXPECTED = "test@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(1, result.Count, "One address should be extracted");
        Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted from a URL");
    }

    [TestMethod]
    public void EmailAddressesStartingWithBacktickIsExtracted()
    {
        // Arrange
        const string INPUT = @"`test@example.com'";
        const string EXPECTED = "test@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(1, result.Count, "One address should be extracted");
        Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted when beginning with backslash");
    }

    [TestMethod]
    public void EmailAddressesInDoubleQuotesIsExtracted()
    {
        // Arrange
        const string INPUT = "\"test@example.com\"";
        const string EXPECTED = "test@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(1, result.Count, "One address should be extracted");
        Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted from double quotes");
    }

    [TestMethod]
    public void EmailAddressesWithOnlyOpeningDoubleQuoteIsExtracted()
    {
        // Arrange
        const string INPUT = "\"test@example.com";
        const string EXPECTED = "test@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(1, result.Count, "One address should be extracted");
        Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted from opening double quote");
    }

    [TestMethod]
    public void EmailAddressesWithInDoubleQuotesWithTrailingSpacesIsExtracted()
    {
        // Arrange
        const string INPUT = "\"test@example.com      \"";
        const string EXPECTED = "test@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(1, result.Count, "One address should be extracted");
        Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted from opening double quote");
    }

    [TestMethod]
    public void EmailAddressesInEscapedDoubleQuotesIsExtracted()
    {
        // Arrange
        const string INPUT = "\\\"test@example.com\\\"";
        const string EXPECTED = "test@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(1, result.Count, "One address should be extracted");
        Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted from escaped double quotes");
    }

    [TestMethod]
    public void EmailAddressesInQuotesWithLeadingExclamationMarkIsInvalid()
    {
        // Arrange
        const string INPUT = "'!test@example.com'";
        const string EXPECTED = "test@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(1, result.Count, "One address should be extracted");
        Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted from quotes with trailing space");
    }

    [TestMethod]
    public void EmailAddressesInPipesIsExtracted()
    {
        // Arrange
        const string INPUT = "foo|test@example.com|bar";
        const string EXPECTED = "test@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(1, result.Count, "One address should be extracted");
        Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted from pipes");
    }

    [TestMethod]
    public void EmailAddressesAfterEqualsSignIsExtracted()
    {
        // Arrange
        const string INPUT = "username=test@example.com";
        const string EXPECTED = "test@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(1, result.Count, "One address should be extracted");
        Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted after equals sign");
    }

    [TestMethod]
    public void EmailAddressesStartingWithEscapedCarriageReturnlsSignIsExtracted()
    {
        // Arrange
        const string INPUT = "\\rtest@example.com";
        const string EXPECTED = "test@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(1, result.Count, "One address should be extracted");
        Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted after escaped carriage return");
    }

    [TestMethod]
    public void EmailAddressesStartingWithEscapedNewlinelsSignIsExtracted()
    {
        // Arrange
        const string INPUT = "\\ntest@example.com";
        const string EXPECTED = "test@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(1, result.Count, "One address should be extracted");
        Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted after escaped newline");
    }

    [TestMethod]
    public void EmailAddressesStartingWithEscapedTablsSignIsExtracted()
    {
        // Arrange
        const string INPUT = "\\ttest@example.com";
        const string EXPECTED = "test@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(1, result.Count, "One address should be extracted");
        Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted after escaped tab");
    }

    [TestMethod]
    public void EmailAddressesSurroundedWithTildasIsExtracted()
    {
        // Arrange
        const string INPUT = "~test@example.com~";
        const string EXPECTED = "test@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(1, result.Count, "One address should be extracted");
        Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted from tildas");
    }

    [TestMethod]
    public void EmailAddressInQuotesAreExtracted()
    {
        const string INPUT = """
              "test1@example.com"
              ""test2@example.com""
              'test3@example.com'
              \"test4@example.com\"
              "test5"@example.com
              test6"@example.com
          """;

        var result = AddressExtractor.ExtractAddresses(INPUT);
        Assert.AreEqual(4, result.Count);
    }

    [TestMethod]
    public void LineBreakShouldNotHaltProcessing()
    {
        // Arrange
        const string INPUT = """
              some text
              
              test@example.com
          """;
        const string EXPECTED = "test@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(EXPECTED, result.First(), "Email should be found");
    }

    [TestMethod]
    public void EmailAddressesCannotHaveDomainStartingWithHyphen()
    {
        // Arrange
        const string INPUT = "test@-example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(0, result.Count, "No results should be returned");
    }

    [TestMethod]
    public void EmailAddressesCannotHaveDomainEndingWithHyphen()
    {
        // Arrange
        const string INPUT = "test@example-.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(0, result.Count, "No results should be returned");
    }

    [TestMethod]
    public void EmailAddressesWithKnownFileExtensionsThatAreNotTldsAreIgnored()
    {
        // Arrange
        const string INPUT = "test@example.jpg";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(0, result.Count, "No results should be returned");
    }

    [TestMethod]
    public void CommaShouldTerminateAddress()
    {
        // Arrange
        const string INPUT = "email1@example.com,email2@example.com";
        const string EXPECTED_FIRST = "email1@example.com";
        const string EXPECTED_LAST = "email2@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(2, result.Count, "Two results should be returned");
        Assert.AreEqual(EXPECTED_FIRST, result.First(), $"First email address must be {EXPECTED_FIRST}");
        Assert.AreEqual(EXPECTED_LAST, result.Last(), $"Last email address must be {EXPECTED_LAST}");
    }

    [TestMethod]
    public void AliasOf64CharsIsValid()
    {
        // Arrange
        var ALIAS = new string('a', 64);
        var INPUT = $"{ALIAS}@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(1, result.Count, "One result should be returned");
    }

    [TestMethod]
    public void AliasLongerThan64CharsIsInvalid()
    {
        // Arrange
        var ALIAS = new string('a', 65);
        var INPUT = $"{ALIAS}@example.com";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreEqual(0, result.Count, "No results should be returned");
    }

    [TestMethod]
    public void WeirdAddressTesting()
    {
        const string INPUT = "'@nicedomain.com";

        // OK if this doesn't throw any exceptions!
        var result = AddressExtractor.ExtractAddresses(INPUT);
        Assert.AreEqual(0, result.Count, "No results should be returned");
    }

    [TestMethod]
    [Ignore("This is a low priority feature so the test is ignored for the moment in the interests of having all green all the way for tests that *should* be working now")]
    public void AliasOnEmojiDomainIsFound()
    {
        // Arrange
        const string INPUT = "example@i❤️.ws is";

        // Act
        var result = AddressExtractor.ExtractAddresses(INPUT);

        // Assert
        Assert.AreNotEqual(0, result.Count, "A result should be returned");
    }

    #region Wrappers

    private readonly Runtime _runtime;

    public AddressExtractorTests()
    {
        _runtime = new Runtime();
    }

    private async ValueTask<HashSet<string>> ExtractAddressesFromFileAsync(string path, CancellationToken cancellation = default)
    {
        // Arrange
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parser = _runtime.GetExtensionFromPath(path);

        await using (var reader = parser.GetReader(path))
        {
            await foreach (var address in AddressExtractor.ExtractFileAddressesAsync(reader, cancellation).ConfigureAwait(false))
            {
                set.Add(address);
            }
        }

        return set;
    }

    #endregion
}
