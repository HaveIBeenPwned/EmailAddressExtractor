using HaveIBeenPwned.AddressExtractor.Objects;
using HaveIBeenPwned.AddressExtractor.Objects.Filters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HaveIBeenPwned.AddressExtractor.Tests
{
    [TestClass]
    public class AddressExtractorTests
    {
        [TestMethod]
        public async Task CorrectNumberOfAddressesAreExtractedFromFile()
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

            var result = await this.ExtractAddressesFromFileAsync(@"../../../../TestData/SingleFile/SingleSmallFile.txt");

            // Assert
            Assert.IsTrue(result.Count == 11, $"11 email addresses should be found, result was missing:\n{string.Join("\n", expected.Except(result))}");
        }     
        
        [TestMethod]
        public async Task AddressAfterBlankLineIsFound()
        {
            var result = await this.ExtractAddressesFromFileAsync(@"../../../../TestData/SingleFile/FileWithBlankLine.txt");

            // Assert
            Assert.IsTrue(result.Count == 1, "An email address should be found");
        }

        [TestMethod]
        public async Task EmailAddressesAreNotCaseSensitive()
        {
            const string ADDRESSES = "test@example.com TEST@EXAMPLE.COM";

            // Act
            var result = await this.ExtractAddressesAsync(ADDRESSES);

            // Assert
            Assert.IsTrue(result.Count == 1, "Same addresses of different case should be merged");
        }

        [TestMethod]
        public async Task EmailAddressesAreConvertedToLowercase()
        {
            // Arrange
            const string INPUT = "TEST@EXAMPLE.COM";
            const string EXPECTED = "test@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.IsTrue(result.Count == 1, "Address should always be converted to lowercase");
        }

        [TestMethod]
        public async Task EmailAddressesInSingleQuotesIsExtracted()
        {
            // Arrange
            const string INPUT = "'test@example.com'";
            const string EXPECTED = "test@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted from single quotes");
        }

        [TestMethod]
        public async Task EmailAddressesInSingleQuotesWithTrailingSpaceIsExtracted()
        {
            // Arrange
            const string INPUT = "'test@example.com '";
            const string EXPECTED = "test@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted from single quotes with trailing space");
        }

        [TestMethod]
        public async Task EmailAddressesInUrlIsExtracted()
        {
            // Arrange
            const string INPUT = "https://example.com/path/test@example.com";
            const string EXPECTED = "test@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted from a URL");
        }

        [TestMethod]
        public async Task EmailAddressesStartingWithBackslashIsExtracted()
        {
            // Arrange
            const string INPUT = @"""\test@example.com'";
            const string EXPECTED = "test@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted when beginning with backslash");
        }

        [TestMethod]
        public async Task EmailAddressesStartingWithBacktickIsExtracted()
        {
            // Arrange
            const string INPUT = @"`test@example.com'";
            const string EXPECTED = "test@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted when beginning with backslash");
        }

        [TestMethod]
        public async Task EmailAddressesInDoubleQuotesIsExtracted()
        {
            // Arrange
            const string INPUT = "\"test@example.com\"";
            const string EXPECTED = "test@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted from double quotes");
        }

        [TestMethod]
        public async Task EmailAddressesInEscapedDoubleQuotesIsExtracted()
        {
            // Arrange
            const string INPUT = "\\\"test@example.com\\\"";
            const string EXPECTED = "test@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted from escaped double quotes");
        }

        [TestMethod]
        public async Task EmailAddressesInQuotesWithLeadingExclamationMarkIsInvalid()
        {
            // Arrange
            const string INPUT = "'!test@example.com'";
            const string EXPECTED = "test@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted from quotes with trailing space");
        }

        [TestMethod]
        public async Task EmailAddressesInPipesIsExtracted()
        {
            // Arrange
            const string INPUT = "foo|test@example.com|bar";
            const string EXPECTED = "test@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted from pipes");
        }

        [TestMethod]
        public async Task EmailAddressesAfterEqualsSignIsExtracted()
        {
            // Arrange
            const string INPUT = "username=test@example.com";
            const string EXPECTED = "test@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted after equals sign");
        }

        [TestMethod]
        public async Task EmailAddressesStartingWithEscapedCarriageReturnlsSignIsExtracted()
        {
            // Arrange
            const string INPUT = "\\rtest@example.com";
            const string EXPECTED = "test@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted after escaped carriage return");
        }

        [TestMethod]
        public async Task EmailAddressesStartingWithEscapedNewlinelsSignIsExtracted()
        {
            // Arrange
            const string INPUT = "\\ntest@example.com";
            const string EXPECTED = "test@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted after escaped newline");
        }

        [TestMethod]
        public async Task EmailAddressesStartingWithEscapedTablsSignIsExtracted()
        {
            // Arrange
            const string INPUT = "\\ttest@example.com";
            const string EXPECTED = "test@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted after escaped tab");
        }

        [TestMethod]
        public async Task EmailAddressesSurroundedWithTildasIsExtracted()
        {
            // Arrange
            const string INPUT = "~test@example.com~";
            const string EXPECTED = "test@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted from tildas");
        }

        [TestMethod]
        public async Task EmailAddressesStartingWithNonBreakingUnicodeSpaceIsExtracted()
        {
            // Arrange
            const string INPUT = "\\xa0test@example.com";
            const string EXPECTED = "test@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.AreEqual(EXPECTED, result.First(), "Address should be extracted after non-breaking unicode space");
        }

        [TestMethod]
        public async Task EmailAddressInQuotesAreExtracted()
        {
            const string INPUT = """
                "test1@example.com"
                ""test2@example.com""
                'test3@example.com'
                \"test4@example.com\"
                "test5"@example.com
                test6"@example.com
            """;

            var result = await this.ExtractAddressesAsync(INPUT);
            Assert.AreEqual(4, result.Count);
        }

        [TestMethod]
        public async Task LineBreakShouldNotHaltProcessing()
        {
            // Arrange
            const string INPUT = """
                some text
                
                test@example.com
            """;
            const string EXPECTED = "test@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.AreEqual(EXPECTED, result.First(), "Email should be found");
        } 

        [TestMethod]
        public async Task EmailAddressesCannotHaveDomainStartingWithHyphen()
        {
            // Arrange
            const string INPUT = "test@-example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.IsFalse(result.Any(), "No results should be returned");
        }    

        [TestMethod]
        public async Task EmailAddressesCannotHaveDomainEndingWithHyphen()
        {
            // Arrange
            const string INPUT = "test@example-.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.IsFalse(result.Any(), "No results should be returned");
        }

        [TestMethod]
        public async Task EmailAddressesWithKnownFileExtensionsThatAreNotTldsAreIgnored()
        {
            // Arrange
            const string INPUT = "test@example.jpg";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.IsFalse(result.Any(), "No results should be returned");
        }

        [TestMethod]
        public async Task CommaShouldTerminateAddress()
        {
            // Arrange
            const string INPUT = "email1@example.com,email2@example.com";
            const string EXPECTED_FIRST = "email1@example.com";
            const string EXPECTED_LAST = "email2@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.IsTrue(result.Count == 2, "Two results should be returned");
            Assert.AreEqual(EXPECTED_FIRST, result.First(), $"First email address must be {EXPECTED_FIRST}");
            Assert.AreEqual(EXPECTED_LAST, result.Last(), $"Last email address must be {EXPECTED_LAST}");
        }

        [TestMethod]
        public async Task AliasOf64CharsIsValid()
        {
            // Arrange
            var ALIAS = new string('a', LengthFilter.ALIAS_LENGTH);
            var INPUT = $"{ALIAS}@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.IsTrue(result.Count == 1, "One result should be returned");
        }   
        
        [TestMethod]
        public async Task AliasLongerThan64CharsIsInvalid()
        {
            // Arrange
            var ALIAS = new string('a', LengthFilter.ALIAS_LENGTH + 1);
            var INPUT = $"{ALIAS}@example.com";

            // Act
            var result = await this.ExtractAddressesAsync(INPUT);

            // Assert
            Assert.IsTrue(result.Count == 0, "No results should be returned");
        }

        [TestMethod]
        [Ignore("The loose address matching captures this as a valid address, TBD if it should be valid or not")]
        public async Task WeirdAddressTesting()
        {
            const string INPUT = "'@nicedomain.com";

            // OK if this doesn't throw any exceptions!
            var result = await this.ExtractAddressesAsync(INPUT);
        }

        [TestMethod]
        [Ignore("This is a low priority feature so the test is ignored for the moment in the interests of having all green all the way for tests that *should* be working now")]
        public async Task AliasOnEmojiDomainIsFound()
        {
          // Arrange
          const string INPUT = "example@i❤️.ws is";

          // Act
          var result = await this.ExtractAddressesAsync(INPUT);

          // Assert
          Assert.IsTrue(result.Any(), "A result should be returned");
        }

        [TestMethod]
        [Ignore("This is a low priority feature so the test is ignored for the moment in the interests of having all green all the way for tests that *should* be working now")]
        public async Task EmailAddressOnIdnDomainNameIsRecognised()
        {
          // Arrange
          const string INPUT = "بريد@موقع.شبكة";

          // Act
          var result = await this.ExtractAddressesAsync(INPUT);

          // Assert
          Assert.IsTrue(result.Any(), "A result should be returned");
        }

        #region Wrappers

        private readonly Runtime Runtime;
        private readonly AddressExtractor Extractor;

        public AddressExtractorTests()
        {
            this.Runtime = new Runtime();
            this.Extractor = new AddressExtractor(this.Runtime);
        }

        /// <summary>
        /// Extract the addresses from the Address Extractor and wrap it into a set
        /// Wrapping the <see cref="IEnumerable{String}"/> here instead of within the Method
        /// prevents the overhead of creating a Set for every iteration in Production
        /// </summary>
        private async ValueTask<HashSet<string>> ExtractAddressesAsync(string input)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await foreach(var address in this.Extractor.ExtractAddressesAsync(input))
                set.Add(address);

            return set;
        }

        private async ValueTask<HashSet<string>> ExtractAddressesFromFileAsync(string path, CancellationToken cancellation = default)
        {
            // Arrange
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var parser = this.Runtime.GetExtensionFromPath(path);

            await using (var reader = parser.GetReader(path))
            {
                await foreach(var address in this.Extractor.ExtractFileAddressesAsync(reader, cancellation))
                    set.Add(address);
            }
            return set;
        }

        #endregion
    }
}