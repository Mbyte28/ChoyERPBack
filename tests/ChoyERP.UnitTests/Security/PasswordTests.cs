using ChoyERP.Application.Common.Security;
using ChoyERP.Infrastructure.Identity;

namespace ChoyERP.UnitTests.Security;

public sealed class PasswordTests
{
    [Theory]
    [InlineData("Choy.Admin#2026")]
    [InlineData("Password123")]
    public void PasswordPolicy_AcceptsValidPasswords(string password) =>
        Assert.True(PasswordPolicy.IsValid(password));

    [Theory]
    [InlineData("")]
    [InlineData("short1")]
    [InlineData("sololetras")]
    [InlineData("1234567890")]
    public void PasswordPolicy_RejectsInvalidPasswords(string password) =>
        Assert.False(PasswordPolicy.IsValid(password));

    [Fact]
    public void BCryptHasher_RoundTripsAndRejectsWrongPassword()
    {
        var hasher = new BCryptPasswordHasher();
        var hash = hasher.Hash("Password123");

        Assert.NotEqual("Password123", hash);
        Assert.True(hasher.Verify("Password123", hash));
        Assert.False(hasher.Verify("OtraClave123", hash));
    }

    [Fact]
    public void BCryptHasher_ReturnsFalseForInvalidHash()
    {
        var hasher = new BCryptPasswordHasher();
        Assert.False(hasher.Verify("Password123", "no-es-un-hash"));
    }

    [Fact]
    public void TokenHasher_IsDeterministicAndDoesNotExposeToken()
    {
        const string token = "token-de-prueba";

        Assert.Equal(TokenHasher.Sha256(token), TokenHasher.Sha256(token));
        Assert.NotEqual(token, TokenHasher.Sha256(token));
        Assert.Equal(64, TokenHasher.Sha256(token).Length);
    }
}
