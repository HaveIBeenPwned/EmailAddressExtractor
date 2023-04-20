using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyAddressExtractor;

[TestClass]
public class MyAddressExtractorTest
{
    [TestMethod]
    public void Test0()
    {
        // Arrange
        var sut = new AddressExtractor();

        // Act
        var result = sut.ExtractAddressesFromFile(@"../../../../TestData/SingleFile/SingleSmallFile.txt");

        // Assert
        Assert.IsTrue(result.Count == 12, "Parsing should pass");
    }
}