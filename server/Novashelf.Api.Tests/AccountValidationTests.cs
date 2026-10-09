using Novashelf.Api.Accounts;

namespace Novashelf.Api.Tests;

public class AccountValidationTests
{
    private static RegisterRequest Valid() => new("ada@example.com", "correct horse", "Ada", AgeConfirmed: true);

    [Fact]
    public void A_complete_request_is_valid() =>
        Assert.Empty(AccountValidation.Validate(Valid()));

    [Theory]
    [InlineData("ada@example.com", true)]
    [InlineData("  ada@example.com  ", true)]
    [InlineData("ada.lovelace+news@mail.example.org", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("ada", false)]
    [InlineData("ada@localhost", false)]
    [InlineData("ada@example.", false)]
    [InlineData("Ada <ada@example.com>", false)]
    [InlineData("ada@@example.com", false)]
    public void Emails_must_be_bare_addresses_with_a_real_domain(string? email, bool valid) =>
        Assert.Equal(valid, AccountValidation.IsValidEmail(email));

    [Fact]
    public void Overlong_emails_are_rejected() =>
        Assert.False(AccountValidation.IsValidEmail(new string('a', 250) + "@x.org"));

    [Theory]
    [InlineData("1234567", false)]
    [InlineData("12345678", true)]
    [InlineData("all lowercase words", true)]
    public void Passwords_need_eight_characters_of_any_kind(string password, bool valid) =>
        Assert.Equal(valid, !AccountValidation.Validate(Valid() with { Password = password }).ContainsKey("password"));

    [Fact]
    public void Overlong_passwords_are_rejected() =>
        Assert.True(AccountValidation.Validate(Valid() with { Password = new string('x', 129) }).ContainsKey("password"));

    [Fact]
    public void Age_must_be_confirmed() =>
        Assert.True(AccountValidation.Validate(Valid() with { AgeConfirmed = false }).ContainsKey("ageConfirmed"));

    [Theory]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    [InlineData("  Ada  ", "Ada")]
    [InlineData("Ada   King\\tLovelace", "Ada King Lovelace")]
    public void First_names_are_tidied(string? input, string? expected) =>
        Assert.Equal(expected, AccountValidation.NormalizeFirstName(input?.Replace("\\t", "\t")));

    [Fact]
    public void Long_first_names_are_rejected() =>
        Assert.True(AccountValidation.Validate(Valid() with { FirstName = new string('a', 61) }).ContainsKey("firstName"));
}
