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
}