using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Domain.Exceptions;

namespace todo.Domain.Tests;

public class RefreshTokenTests
{
    private static readonly DateTime CreatedAtUtc = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void IsActive_IsTrueUntilTheTokenExpires()
    {
        RefreshToken token = new RefreshToken(1, "hash", CreatedAtUtc, CreatedAtUtc.AddDays(7));

        Assert.True(token.IsActive(CreatedAtUtc.AddDays(6)));
        Assert.False(token.IsActive(CreatedAtUtc.AddDays(7)));
    }

    [Fact]
    public void Revoke_DeactivatesTheTokenAndRecordsItsReplacement()
    {
        RefreshToken token = new RefreshToken(1, "hash", CreatedAtUtc, CreatedAtUtc.AddDays(7));

        token.Revoke(CreatedAtUtc.AddHours(1), "new-hash");

        Assert.True(token.IsRevoked);
        Assert.False(token.IsActive(CreatedAtUtc.AddHours(2)));
        Assert.Equal(CreatedAtUtc.AddHours(1), token.RevokedAtUtc);
        Assert.Equal("new-hash", token.ReplacedByTokenHash);
    }

    [Fact]
    public void Revoke_KeepsTheFirstRevocationTime()
    {
        RefreshToken token = new RefreshToken(1, "hash", CreatedAtUtc, CreatedAtUtc.AddDays(7));

        token.Revoke(CreatedAtUtc.AddHours(1), "new-hash");
        token.Revoke(CreatedAtUtc.AddHours(5));

        Assert.Equal(CreatedAtUtc.AddHours(1), token.RevokedAtUtc);
        Assert.Equal("new-hash", token.ReplacedByTokenHash);
    }

    [Fact]
    public void Constructor_RejectsInvalidValues()
    {
        Assert.Throws<DomainRuleViolationException>(
            () => new RefreshToken(0, "hash", CreatedAtUtc, CreatedAtUtc.AddDays(7)));
        Assert.Throws<DomainRuleViolationException>(
            () => new RefreshToken(1, " ", CreatedAtUtc, CreatedAtUtc.AddDays(7)));
        Assert.Throws<DomainRuleViolationException>(
            () => new RefreshToken(1, "hash", CreatedAtUtc, CreatedAtUtc));
    }

    [Fact]
    public void ReplacePasswordHash_IsOnlyAllowedForPasswordAccounts()
    {
        User passwordUser = User.RegisterWithPassword("ali@example.com", "Ali", "old");
        User googleUser = User.RegisterWithGoogle("ayse@gmail.com", "Ayşe", "google-1", UserRole.User);

        passwordUser.ReplacePasswordHash("new");

        Assert.Equal("new", passwordUser.PasswordHash);
        Assert.Throws<DomainRuleViolationException>(() => googleUser.ReplacePasswordHash("new"));
    }
}
