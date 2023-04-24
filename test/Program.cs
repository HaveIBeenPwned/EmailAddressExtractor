using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyAddressExtractor;

[TestClass]
public class MyAddressExtractorTest
{
    [TestMethod]
    public async Task correct_number_of_addresses_are_extracted_from_file()
    {
        // Act
        var result = await this.ExtractAddressesFromFileAsync(@"../../../../TestData/SingleFile/SingleSmallFile.txt");

        // Assert
        Assert.IsTrue(result.Count == 12, "Parsing should pass");
    }  
    
    [TestMethod]
    public void email_addresses_are_not_case_sensitive()
    {
        const string addresses = "test@example.com TEST@EXAMPLE.COM";

        // Act
        var result = this.ExtractAddresses(addresses);

        // Assert
        Assert.IsTrue(result.Count == 1, "Same addresses of different case should be merged");
    }
    
    [TestMethod]
    public void email_addresses_are_converted_to_lowercase()
    {
        // Arrange
        const string input = "TEST@EXAMPLE.COM";
        const string expected = "test@example.com";

        // Act
        var result = this.ExtractAddresses(input);

        result.Add(expected);

        // Assert
        Assert.IsTrue(result.Count == 1, "Address should always be converted to lowercase");
    } 
    
    [TestMethod]
    public void email_addresses_cannot_have_domain_starting_with_hyphen()
    {
        // Arrange
        const string input = "test@-example.com";

        // Act
        var result = this.ExtractAddresses(input);

        // Assert
        Assert.IsFalse(result.Any(), "No results should be returned");
    }    

    [TestMethod]
    public void email_addresses_cannot_have_domain_ending_with_hyphen()
    {
        // Arrange
        const string input = "test@example-.com";

        // Act
        var result = this.ExtractAddresses(input);

        // Assert
        Assert.IsFalse(result.Any(), "No results should be returned");
    }

    [TestMethod]
    public void comma_should_terminate_address()
    {
        // Arrange
        const string input = "email1@example.com,email2@example.com";
        const string expectedFirst = "email1@example.com";
        const string expectedLast = "email2@example.com";

        // Act
        var result = this.ExtractAddresses(input);

        // Assert
        Assert.IsTrue(result.Count == 2, "Two results should be returned");
        Assert.AreEqual(expectedFirst, result.First(), $"First email address must be {expectedFirst}");
        Assert.AreEqual(expectedLast, result.Last(), $"Last email address must be {expectedLast}");
    }

    [TestMethod]
    public void alias_on_emoji_domain_is_found()
    {
      // Arrange
      const string input = "example@i❤️.ws is";

      // Act
      var result = this.ExtractAddresses(input);

      // Assert
      Assert.IsTrue(result.Any(), "A result should be returned");
    }

    [TestMethod]
    public void email_Address_on_idn_domain_name_is_recognised()
    {
      // Arrange
      const string input = "بريد@موقع.شبكة";

      // Act
      var result = this.ExtractAddresses(input);

      // Assert
      Assert.IsTrue(result.Any(), "A result should be returned");
    }

    #region Wrappers
    
    /// <summary>
    /// Extract the addresses from the Address Extractor and wrap it into a set
    /// Wrapping the <see cref="IEnumerable{String}"/> here instead of within the Method
    /// prevents the overhead of creating a Set for every iteration in Production
    /// </summary>
    private HashSet<string> ExtractAddresses(string input)
    {
        var extractor = new AddressExtractor();
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        set.UnionWith(extractor.ExtractAddresses(input));
        return set;
    }

    private async ValueTask<HashSet<string>> ExtractAddressesFromFileAsync(string path, CancellationToken cancellation = default)
    {
        // Arrange
        var extractor = new AddressExtractor();
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await foreach(var address in extractor.ExtractAddressesFromFileAsync(path, cancellation))
            set.Add(address);
        return set;
    }

    #endregion
}