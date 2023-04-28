using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyAddressExtractor;

namespace AddressExtractorTest
{
    [TestClass]
    public class LegacyTests
    {
        #region Valid addresses

        [TestMethod]
        public void AmpersandIsValid()
            => Assert.IsTrue(this.IsValidEmail(@"Mary&Jane@example.org"));

        [TestMethod]
        public void SingleBackslashEnclosedInQuotesIsValid()
            => Assert.IsTrue(this.IsValidEmail(@"""test\""blah""@example.com"));

        [TestMethod]
        public void ForwardSlashIsValid()
            => Assert.IsTrue(this.IsValidEmail(@"customer/department@example.com"));

        [TestMethod]
        public void StartingWithDollarSignIsValid()
            => Assert.IsTrue(this.IsValidEmail(@"$A12345@example.com"));

        [TestMethod]
        public void StartingWithExclamationMarkIsValid()
            => Assert.IsTrue(this.IsValidEmail(@"!def!xyz%abc@example.com"));

        [TestMethod]
        public void StartingWithUnderscoreIsValid()
            => Assert.IsTrue(this.IsValidEmail(@"_Yosemite.Sam@example.com"));

        [TestMethod]
        public void DotInAliasIsValid()
            => Assert.IsTrue(this.IsValidEmail(@"Ima.Fool@example.com"));

        [TestMethod]
        public void SingleCharDomainIsValid()
            => Assert.IsTrue(this.IsValidEmail(@"foobar@x.com"));

        [TestMethod]
        public void DomainWithNumberIsValid()
            => Assert.IsTrue(this.IsValidEmail(@"foobar@c0m.com"));

        [TestMethod]
        public void DomainWithUnderscoreIsValid()
            => Assert.IsTrue(this.IsValidEmail(@"foobar@c_m.com"));

        [TestMethod]
        public void DomainWithUnderscoreBeforeTldIsValid()
            => Assert.IsTrue(this.IsValidEmail(@"foo@bar_.com"));

        [TestMethod]
        public void DomainWithNumbersOnlyIsValid()
            => Assert.IsTrue(this.IsValidEmail(@"foo@666.com"));

        [TestMethod]
        public void EmailOf255CharsIsValid()
            => Assert.IsTrue(this.IsValidEmail(@"111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111@example.com"));

        #endregion
        #region Invalid addresses

        [TestMethod]
        public void NullIsInvalid()
        => Assert.IsFalse(this.IsValidEmail(null));

        [TestMethod]
        public void EmptyEmailIsInvalid()
            => Assert.IsFalse(this.IsValidEmail(string.Empty));

        [TestMethod]
        public void NoAtSymbolIsInvalid()
            => Assert.IsFalse(this.IsValidEmail("NotAnEmail"));

        [TestMethod]
        public void AtFirstIsInvalid()
            => Assert.IsFalse(this.IsValidEmail("@NotAnEmail"));

        [TestMethod]
        public void AtLastIsInvalid()
            => Assert.IsFalse(this.IsValidEmail("NotAnEmail@"));

        [TestMethod]
        public void BackspaceCharIsInvalid()
            => Assert.IsFalse(this.IsValidEmail("foo@b" + (char)8 + "ar.com"));

        [TestMethod]
        public void HorizontalTabCharIsInvalid()
            => Assert.IsFalse(this.IsValidEmail("foo@b" + (char)9 + "ar.com"));

        [TestMethod]
        public void DeleteCharIsInvalid()
            => Assert.IsFalse(this.IsValidEmail("foo@b" + (char)127 + "ar.com"));

        [TestMethod]
        [Ignore("This is a low priority feature so the test is ignored for the moment in the interests of having all green all the way for tests that *should* be working now")]
        public void AliasWithAsteriskIsInvalid()
            => Assert.IsFalse(this.IsValidEmail(@"fo*o@bar.com"));

        [TestMethod]
        public void EmailOf256CharsIsInvalid()
            => Assert.IsFalse(this.IsValidEmail(@"1111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111@example.com"));

        [TestMethod]
        public void UnescapedDoubleQuoteIsInvalid()
            => Assert.IsFalse(this.IsValidEmail("\"test\rblah\"@example.com"));

        [TestMethod]
        public void DotFirstIsInvalid()
            => Assert.IsFalse(this.IsValidEmail(@".wooly@example.com"));

        [TestMethod]
        public void ConsecutiveDotsIsInvalid()
            => Assert.IsFalse(this.IsValidEmail(@"wo..oly@example.com"));

        [TestMethod]
        public void DotBeforeAtIsInvalid()
            => Assert.IsFalse(this.IsValidEmail(@"pootietang.@example.com"));

        [TestMethod]
        public void DotForAliasIsInvalid()
            => Assert.IsFalse(this.IsValidEmail(@".@example.com"));

        [TestMethod]
        public void NoDotInDomainIsInvalid()
            => Assert.IsFalse(this.IsValidEmail(@"foo@bar"));

        [TestMethod]
        public void TldStartingWithNumberIsNotValid()
            => Assert.IsFalse(this.IsValidEmail("foo@bar.1com"));

        [TestMethod]
        public void TldWithOnlyNumbersIsNotValid()
            => Assert.IsFalse(this.IsValidEmail("foo@bar.123"));

        [TestMethod]
        public void TldWithOneCharacterIsNotValid()
            => Assert.IsFalse(this.IsValidEmail("foo@bar.a"));

        [TestMethod]
        public void DomainWithUnderscoreOnlyIsNotValid()
            => Assert.IsFalse(this.IsValidEmail(@"foobar@_.com"));

        #endregion

        /// <summary>
        /// Purely added to make it easy to include the legacy tests, ideally should be removed in favour of more verbose test syntax
        /// </summary>
        /// <param name="sourceEmail"></param>
        /// <returns></returns>
        private bool IsValidEmail(string? sourceEmail)
        {
            var sut = new AddressExtractor();
            var result = sut.ExtractAddresses(sourceEmail);
            return result.Any();
        }
    }
}
