using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyAddressExtractor;
using MyAddressExtractor.Objects;

namespace AddressExtractorTest
{
    [TestClass]
    public class LegacyTests
    {
        #region Valid addresses

        [TestMethod]
        public async Task AmpersandIsValid()
            => Assert.IsTrue(await this.IsValidEmailAsync(@"Mary&Jane@example.org"));

        [TestMethod]
        public async Task SingleBackslashEnclosedInQuotesIsValid()
            => Assert.IsTrue(await this.IsValidEmailAsync("""
                                                          "test\"blah"@example.com
                                                          """));

        [TestMethod]
        public async Task ForwardSlashIsValid()
            => Assert.IsTrue(await this.IsValidEmailAsync(@"customer/department@example.com"));

        [TestMethod]
        public async Task StartingWithDollarSignIsValid()
            => Assert.IsTrue(await this.IsValidEmailAsync(@"$A12345@example.com"));

        [TestMethod]
        public async Task StartingWithExclamationMarkIsValid()
            => Assert.IsTrue(await this.IsValidEmailAsync(@"!def!xyz%abc@example.com"));

        [TestMethod]
        public async Task StartingWithUnderscoreIsValid()
            => Assert.IsTrue(await this.IsValidEmailAsync(@"_Yosemite.Sam@example.com"));

        [TestMethod]
        public async Task DotInAliasIsValid()
            => Assert.IsTrue(await this.IsValidEmailAsync(@"Ima.Fool@example.com"));

        [TestMethod]
        public async Task SingleCharDomainIsValid()
            => Assert.IsTrue(await this.IsValidEmailAsync(@"foobar@x.com"));

        [TestMethod]
        public async Task DomainWithNumberIsValid()
            => Assert.IsTrue(await this.IsValidEmailAsync(@"foobar@c0m.com"));

        [TestMethod]
        public async Task DomainWithUnderscoreIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@"foobar@c_m.com"));

        [TestMethod]
        public async Task DomainWithUnderscoreBeforeTldIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@"foo@bar_.com"));

        [TestMethod]
        public async Task DomainWithNumbersOnlyIsValid()
            => Assert.IsTrue(await this.IsValidEmailAsync(@"foo@666.com"));

        [TestMethod]
        public async Task EmailOf255CharsIsValid()
            => Assert.IsTrue(await this.IsValidEmailAsync(@"111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111@example.com"));

        #endregion
        #region Invalid addresses

        [TestMethod]
        public async Task NullIsInvalid()
        => Assert.IsFalse(await this.IsValidEmailAsync(null));

        [TestMethod]
        public async Task EmptyEmailIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(string.Empty));

        [TestMethod]
        public async Task NoAtSymbolIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync("NotAnEmail"));

        [TestMethod]
        public async Task AtFirstIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync("@NotAnEmail"));

        [TestMethod]
        public async Task AtLastIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync("NotAnEmail@"));

        [TestMethod]
        public async Task BackspaceCharIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync("foo@b" + (char)8 + "ar.com"));

        [TestMethod]
        public async Task HorizontalTabCharIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync("foo@b" + (char)9 + "ar.com"));

        [TestMethod]
        public async Task DeleteCharIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync("foo@b" + (char)127 + "ar.com"));

        [TestMethod]
        [Ignore("This is a low priority feature so the test is ignored for the moment in the interests of having all green all the way for tests that *should* be working now")]
        public async Task AliasWithAsteriskIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@"fo*o@bar.com"));

        [TestMethod]
        public async Task EmailOf256CharsIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@"1111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111@example.com"));

        [TestMethod]
        public async Task UnescapedDoubleQuoteIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync("\"test\rblah\"@example.com"));

        [TestMethod]
        public async Task DotFirstIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@".wooly@example.com"));

        [TestMethod]
        public async Task ConsecutiveDotsIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@"wo..oly@example.com"));

        [TestMethod]
        public async Task DotBeforeAtIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@"pootietang.@example.com"));

        [TestMethod]
        public async Task DotForAliasIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@".@example.com"));

        [TestMethod]
        public async Task NoDotInDomainIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@"foo@bar"));

        [TestMethod]
        public async Task TldStartingWithNumberIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync("foo@bar.1com"));

        [TestMethod]
        public async Task TldWithOnlyNumbersIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync("foo@bar.123"));

        [TestMethod]
        public async Task TldWithOneCharacterIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync("foo@bar.a"));

        [TestMethod]
        public async Task DomainWithUnderscoreOnlyIsInalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@"foobar@_.com"));

        [TestMethod]
        public async Task DomainWithSpaceIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@"foo@bar foo.com"));

        [TestMethod]
        public async Task DomainWithoutPeriodIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@"foo@barcom"));

        [TestMethod]
        public async Task DomainWithConsecutivePeriodsIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@"foo@bar..com"));

        [TestMethod]
        public async Task DomainWithForwardSlashIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@"foo@ba/r.com"));

        [TestMethod]
        public async Task DomainWithBackSlashIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@"foo@ba\r.com"));

        [TestMethod]
        public async Task DomainWithEscapedForwardSlashIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@"foo@ba\/r.com"));

        [TestMethod]
        public async Task DomainWithBacktickIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@"foo@ba`r.com"));

        [TestMethod]
        public async Task DomainWithColonIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@"foo@ba:r.com"));

        [TestMethod]
        public async Task DomainWithSemicolonIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@"foo@ba:r.com"));

        [TestMethod]
        public async Task DomainWithPercentIsInvalid()
            => Assert.IsFalse(await this.IsValidEmailAsync(@"foo@ba%r.com"));

        #endregion

        /// <summary>
        /// Purely added to make it easy to include the legacy tests, ideally should be removed in favour of more verbose test syntax
        /// </summary>
        /// <param name="sourceEmail"></param>
        /// <returns></returns>
        private async ValueTask<bool> IsValidEmailAsync(string? sourceEmail)
        {
            var sut = new AddressExtractor(new Runtime());
            await foreach (var _ in sut.ExtractAddressesAsync(sourceEmail))
                return true;
            return false;
        }
    }
}
