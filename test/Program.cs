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