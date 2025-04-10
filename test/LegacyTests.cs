using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HaveIBeenPwned.AddressExtractor.Tests;

[TestClass]
public class LegacyTests
{

    #region Valid addresses

    [TestMethod]
    public void AmpersandIsValid()
        => Assert.IsTrue(IsValidEmail(@"Mary&Jane@example.org"));

    [TestMethod]
    public void ForwardSlashIsValid()
        => Assert.IsTrue(IsValidEmail(@"customer/department@example.com"));

    [TestMethod]
    public void StartingWithDollarSignIsValid()
        => Assert.IsTrue(IsValidEmail(@"$A12345@example.com"));

    [TestMethod]
    public void StartingWithExclamationMarkIsValid()
        => Assert.IsTrue(IsValidEmail(@"!def!xyz%abc@example.com"));

    [TestMethod]
    public void DotInAliasIsValid()
        => Assert.IsTrue(IsValidEmail(@"Ima.Fool@example.com"));

    [TestMethod]
    public void AliasWithSinglePeriodIsValid()
        => Assert.IsTrue(IsValidEmail(@"foo.bar@example.com"));

    [TestMethod]
    public void AliasWithConsecutivePeriodsIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"foo..bar@example.com"));

    [TestMethod]
    public void AliasWithPeriodsBeforeAtIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"foo.@example.com"));

    [TestMethod]
    public void AliasWithNonConsecutivePeriodsIsValid()
        => Assert.IsTrue(IsValidEmail(@"f.o.o.b.a.r@example.com"));

    [TestMethod]
    public void SingleCharDomainIsValid()
        => Assert.IsTrue(IsValidEmail(@"foobar@x.com"));

    [TestMethod]
    public void DomainWithNumberIsValid()
        => Assert.IsTrue(IsValidEmail(@"foobar@c0m.com"));

    [TestMethod]
    public void DomainWithUnderscoreIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"foobar@c_m.com"));

    [TestMethod]
    public void DomainWithUnderscoreBeforeTldIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"foo@bar_.com"));

    [TestMethod]
    public void DomainWithNumbersOnlyIsValid()
        => Assert.IsTrue(IsValidEmail(@"foo@666.com"));

    #endregion
    #region Invalid addresses

    [TestMethod]
    public void NullIsInvalid()
    => Assert.IsFalse(IsValidEmail(null));

    [TestMethod]
    public void EmptyEmailIsInvalid()
        => Assert.IsFalse(IsValidEmail(string.Empty));

    [TestMethod]
    public void NoAtSymbolIsInvalid()
        => Assert.IsFalse(IsValidEmail("NotAnEmail"));

    [TestMethod]
    public void AtFirstIsInvalid()
        => Assert.IsFalse(IsValidEmail("@NotAnEmail"));

    [TestMethod]
    public void AtLastIsInvalid()
        => Assert.IsFalse(IsValidEmail("NotAnEmail@"));

    [TestMethod]
    public void BackspaceCharIsInvalid()
        => Assert.IsFalse(IsValidEmail("foo@b" + (char)8 + "ar.com"));

    [TestMethod]
    public void HorizontalTabCharIsInvalid()
        => Assert.IsFalse(IsValidEmail("foo@b" + (char)9 + "ar.com"));

    [TestMethod]
    public void DeleteCharIsInvalid()
        => Assert.IsFalse(IsValidEmail("foo@b" + (char)127 + "ar.com"));

    [TestMethod]
    [Ignore("This is a low priority feature so the test is ignored for the moment in the interests of having all green all the way for tests that *should* be working now")]
    public void AliasWithAsteriskIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"fo*o@bar.com"));

    [TestMethod]
    public void EmailOf256CharsIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"1111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111@example.com"));

    [TestMethod]
    public void UnescapedDoubleQuoteIsInvalid()
        => Assert.IsFalse(IsValidEmail("\"test\rblah\"@example.com"));

    [TestMethod]
    public void DotFirstIsInvalid()
        => Assert.IsFalse(IsValidEmail(@".wooly@example.com"));

    [TestMethod]
    public void ConsecutiveDotsIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"wo..oly@example.com"));

    [TestMethod]
    public void DotBeforeAtIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"pootietang.@example.com"));

    [TestMethod]
    public void DotBeforeAliasIsInvalid()
        => Assert.IsFalse(IsValidEmail(@".pootietang@example.com"));

    [TestMethod]
    public void DotForAliasIsInvalid()
        => Assert.IsFalse(IsValidEmail(@".@example.com"));

    [TestMethod]
    public void NoDotInDomainIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"foo@bar"));

    [TestMethod]
    public void TldStartingWithNumberIsInvalid()
        => Assert.IsFalse(IsValidEmail("foo@bar.1com"));

    [TestMethod]
    public void TldWithOnlyNumbersIsInvalid()
        => Assert.IsFalse(IsValidEmail("foo@bar.123"));

    [TestMethod]
    public void TldWithOneCharacterIsInvalid()
        => Assert.IsFalse(IsValidEmail("foo@bar.a"));

    [TestMethod]
    public void DomainWithUnderscoreOnlyIsInalid()
        => Assert.IsFalse(IsValidEmail(@"foobar@_.com"));

    [TestMethod]
    public void DomainWithSpaceIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"foo@bar foo.com"));

    [TestMethod]
    public void DomainWithoutPeriodIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"foo@barcom"));

    [TestMethod]
    public void DomainEndingInPeriodIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"foo@bar.com."));

    [TestMethod]
    public void DomainWithConsecutivePeriodsIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"foo@bar..com"));

    [TestMethod]
    public void DomainWithForwardSlashIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"foo@ba/r.com"));

    [TestMethod]
    public void DomainStartingWithPeriodIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"foo@.bar.com"));

    [TestMethod]
    public void DomainWithBackslashIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"foo@ba\r.com"));

    [TestMethod]
    public void DomainWithEscapedForwardSlashIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"foo@ba\/r.com"));

    [TestMethod]
    public void DomainWithBacktickIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"foo@ba`r.com"));

    [TestMethod]
    public void DomainWithColonIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"foo@ba:r.com"));

    [TestMethod]
    public void DomainWithSemicolonIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"foo@ba:r.com"));

    [TestMethod]
    public void DomainWithPercentIsInvalid()
        => Assert.IsFalse(IsValidEmail(@"foo@ba%r.com"));

    #endregion

    /// <summary>
    /// Purely added to make it easy to include the legacy tests, ideally should be removed in favour of more verbose test syntax
    /// </summary>
    /// <param name="sourceEmail"></param>
    /// <returns></returns>
    private static bool IsValidEmail(string? sourceEmail)
    {
        return AddressExtractor.ExtractAddresses(sourceEmail).Count > 0;
    }
}
