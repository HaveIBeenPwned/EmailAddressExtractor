using Microsoft.VisualStudio.TestTools.UnitTesting;
using MyAddressExtractor;

namespace AddressExtractorTest
{
  [TestClass]
  public class LegacyTests
  {
    // Valid addresses

    [TestMethod]
    public void ampersand_is_valid() { Assert.IsTrue(IsValidEmail(@"Mary&Jane@example.org")); }

    [TestMethod]
    public void single_backslash_enclosed_in_quotes_is_valid() { Assert.IsTrue(IsValidEmail(@"""test\""blah""@example.com")); }

    [TestMethod]
    public void forward_slash_is_valid() { Assert.IsTrue(IsValidEmail(@"customer/department@example.com")); }

    [TestMethod]
    public void starting_with_dollar_sign_is_valid() { Assert.IsTrue(IsValidEmail(@"$A12345@example.com")); }

    [TestMethod]
    public void starting_with_exclamation_mark_is_valid() { Assert.IsTrue(IsValidEmail(@"!def!xyz%abc@example.com")); }

    [TestMethod]
    public void starting_with_underscore_is_valid() { Assert.IsTrue(IsValidEmail(@"_Yosemite.Sam@example.com")); }

    [TestMethod]
    public void dot_in_alias_is_valid() { Assert.IsTrue(IsValidEmail(@"Ima.Fool@example.com")); }

    [TestMethod]
    public void single_char_domain_is_valid() { Assert.IsTrue(IsValidEmail(@"foobar@x.com")); }

    [TestMethod]
    public void domain_with_number_is_valid() { Assert.IsTrue(IsValidEmail(@"foobar@c0m.com")); }

    [TestMethod]
    public void domain_with_underscore_is_valid() { Assert.IsTrue(IsValidEmail(@"foobar@c_m.com")); }

    [TestMethod]
    public void domain_with_underscore_before_tld_is_valid() { Assert.IsTrue(IsValidEmail(@"foo@bar_.com")); }

    [TestMethod]
    public void domain_with_numbers_only_is_valid() { Assert.IsTrue(IsValidEmail(@"foo@666.com")); }

    [TestMethod]
    public void email_of_255_chars_is_valid() { Assert.IsTrue(IsValidEmail(@"111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111@example.com")); }

    // Invalid addresses

    [TestMethod]
    public void null_is_invalid() { Assert.IsFalse(IsValidEmail(null)); }

    [TestMethod]
    public void empty_email_is_invalid() { Assert.IsFalse(IsValidEmail(string.Empty)); }

    [TestMethod]
    public void no_at_symbol_is_invalid() { Assert.IsFalse(IsValidEmail("NotAnEmail")); }

    [TestMethod]
    public void at_first_is_invalid() { Assert.IsFalse(IsValidEmail("@NotAnEmail")); }

    [TestMethod]
    public void at_last_is_invalid() { Assert.IsFalse(IsValidEmail("NotAnEmail@")); }

    [TestMethod]
    public void backspace_char_is_invalid() { Assert.IsFalse(IsValidEmail("foo@b" + (char)8 + "ar.com")); }

    [TestMethod]
    public void horizontal_tab_char_is_invalid() { Assert.IsFalse(IsValidEmail("foo@b" + (char)9 + "ar.com")); }

    [TestMethod]
    public void delete_char_is_invalid() { Assert.IsFalse(IsValidEmail("foo@b" + (char)127 + "ar.com")); }

    [TestMethod]
    public void alias_with_asterisk_is_invalid() { Assert.IsFalse(IsValidEmail(@"fo*o@bar.com")); }

    [TestMethod]
    public void email_of_256_chars_is_invalid() { Assert.IsFalse(IsValidEmail(@"1111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111@example.com")); }

    [TestMethod]
    public void unescaped_double_quote_is_invalid() { Assert.IsFalse(IsValidEmail("\"test\rblah\"@example.com")); }

    [TestMethod]
    public void dot_first_is_invalid() { Assert.IsFalse(IsValidEmail(@".wooly@example.com")); }

    [TestMethod]
    public void consecutive_dots_is_invalid() { Assert.IsFalse(IsValidEmail(@"wo..oly@example.com")); }

    [TestMethod]
    public void dot_before_at_is_invalid() { Assert.IsFalse(IsValidEmail(@"pootietang.@example.com")); }

    [TestMethod]
    public void dot_for_alias_is_invalid() { Assert.IsFalse(IsValidEmail(@".@example.com")); }

    [TestMethod]
    public void no_dot_in_domain_is_invalid() { Assert.IsFalse(IsValidEmail(@"foo@bar")); }

    [TestMethod]
    public void tld_starting_with_number_is_not_valid() { Assert.IsFalse(IsValidEmail("foo@bar.1com")); }
    
    [TestMethod]
    public void tld_with_only_numbers_is_not_valid() { Assert.IsFalse(IsValidEmail("foo@bar.123")); }   
    
    [TestMethod]
    public void tld_with_one_character_is_not_valid() { Assert.IsFalse(IsValidEmail("foo@bar.a")); }

    [TestMethod]
    public void domain_with_underscore_only_is_not_valid() { Assert.IsFalse(IsValidEmail(@"foobar@_.com")); }

    /// <summary>
    /// Purely added to make it easy to include the legacy tests, ideally should be removed in favour of more verbose test syntax
    /// </summary>
    /// <param name="sourceEmail"></param>
    /// <returns></returns>
    private bool IsValidEmail(string sourceEmail)
    {
      var sut = new AddressExtractor();
      var result = sut.ExtractAddresses(sourceEmail);
      return result.Any();
    }
  }
}
