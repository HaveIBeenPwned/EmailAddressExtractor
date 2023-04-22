using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyAddressExtractor;

[TestClass]
public class MyAddressExtractorTest
{
    [TestMethod]
    public void correct_number_of_addresses_are_extracted_from_file()
    {
        // Arrange
        var sut = new AddressExtractor();

        // Act
        var result = sut.ExtractAddressesFromFile(@"../../../../TestData/SingleFile/SingleSmallFile.txt");

        // Assert
        Assert.IsTrue(result.Count == 12, "Parsing should pass");
    }  
    
    [TestMethod]
    public void email_addresses_are_not_case_sensitive()
    {
        // Arrange
        var sut = new AddressExtractor();
        const string addresses = "test@example.com TEST@EXAMPLE.COM";

        // Act
        var result = sut.ExtractAddresses(addresses);

        // Assert
        Assert.IsTrue(result.Count == 1, "Same addresses of different case should be merged");
    }   
    
    [TestMethod]
    public void email_addresses_are_converted_to_lowercase()
    {
        // Arrange
        var sut = new AddressExtractor();
        const string input = "TEST@EXAMPLE.COM";
        const string expected = "test@example.com";

        // Act
        var result = sut.ExtractAddresses(input).First();

        // Assert
        Assert.AreEqual(expected, result, "Address should always be converted to lowercase");
    }
}