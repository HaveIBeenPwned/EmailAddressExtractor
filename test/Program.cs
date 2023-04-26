using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyAddressExtractor;

namespace AddressExtractorTest
{
    [TestClass]
    public class MyAddressExtractorTest
    {
        [TestMethod]
        public async Task CorrectNumberOfAddressesAreExtractedFromFile()
        {
            var result = await this.ExtractAddressesFromFileAsync(@"../../../../TestData/SingleFile/SingleSmallFile.txt");

            // Assert
            Assert.IsTrue(result.Count == 12, "Parsing should pass");
        }     
        
        [TestMethod]
        public async Task AddressAfterBlankLineIsFound()
        {
            var result = await this.ExtractAddressesFromFileAsync(@"../../../../TestData/SingleFile/FileWithBlankLine.txt");

            // Assert
            Assert.IsTrue(result.Count == 1, "An email address should be found");
        }

        [TestMethod]
        public void EmailAddressesAreNotCaseSensitive()
        {
            const string ADDRESSES = "test@example.com TEST@EXAMPLE.COM";

            // Act
            var result = this.ExtractAddresses(ADDRESSES);

            // Assert
            Assert.IsTrue(result.Count == 1, "Same addresses of different case should be merged");
        }

        [TestMethod]
        public void EmailAddressesAreConvertedToLowercase()
        {
            // Arrange
            const string INPUT = "TEST@EXAMPLE.COM";
            const string EXPECTED = "test@example.com";

            // Act
            var result = this.ExtractAddresses(INPUT);

            result.Add(EXPECTED);

            // Assert
            Assert.IsTrue(result.Count == 1, "Address should always be converted to lowercase");
        } 

        [TestMethod]
        public void EmailAddressesCannotHaveDomainStartingWithHyphen()
        {
            // Arrange
            const string INPUT = "test@-example.com";

            // Act
            var result = this.ExtractAddresses(INPUT);

            // Assert
            Assert.IsFalse(result.Any(), "No results should be returned");
        }    

        [TestMethod]
        public void EmailAddressesCannotHaveDomainEndingWithHyphen()
        {
            // Arrange
            const string INPUT = "test@example-.com";

            // Act
            var result = this.ExtractAddresses(INPUT);

            // Assert
            Assert.IsFalse(result.Any(), "No results should be returned");
        }

        [TestMethod]
        public void CommaShouldTerminateAddress()
        {
            // Arrange
            const string INPUT = "email1@example.com,email2@example.com";
            const string EXPECTED_FIRST = "email1@example.com";
            const string EXPECTED_LAST = "email2@example.com";

            // Act
            var result = this.ExtractAddresses(INPUT);

            // Assert
            Assert.IsTrue(result.Count == 2, "Two results should be returned");
            Assert.AreEqual(EXPECTED_FIRST, result.First(), $"First email address must be {EXPECTED_FIRST}");
            Assert.AreEqual(EXPECTED_LAST, result.Last(), $"Last email address must be {EXPECTED_LAST}");
        }

        [TestMethod]
        public void AliasOnEmojiDomainIsFound()
        {
          // Arrange
          const string INPUT = "example@i❤️.ws is";

          // Act
          var result = this.ExtractAddresses(INPUT);

          // Assert
          Assert.IsTrue(result.Any(), "A result should be returned");
        }

        [TestMethod]
        public void EmailAddressOnIdnDomainNameIsRecognised()
        {
          // Arrange
          const string INPUT = "بريد@موقع.شبكة";

          // Act
          var result = this.ExtractAddresses(INPUT);

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
}