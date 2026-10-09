using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Infrastructure.Services.Authentication;

namespace todo.Infrastructure.Tests;

public class AuthenticationServiceTests
{
    private const string SigningKey = "test-signing-key-that-is-long-enough-for-hmac-sha256";

    [Fact]
    public async Task JwtAccessTokenGenerator_CreatesASignedTokenWithTheUsersClaims()
    {
        DateTimeOffset now = new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
        JwtSettings settings = new JwtSettings("TodoApi", "TodoWeb", SigningKey, 60);
        JwtAccessTokenGenerator generator = new JwtAccessTokenGenerator(settings, new FixedTimeProvider(now));
        User user = User.RegisterWithGoogle("ali@example.com", "Ali", "google-subject-1", UserRole.Admin);

        AccessToken accessToken = generator.GenerateToken(user);

        Assert.Equal(now.UtcDateTime.AddMinutes(60), accessToken.ExpiresAtUtc);

        JsonWebTokenHandler tokenHandler = new JsonWebTokenHandler();
        TokenValidationResult validationResult = await tokenHandler.ValidateTokenAsync(
            accessToken.Value,
            new TokenValidationParameters
            {
                ValidIssuer = "TodoApi",
                ValidAudience = "TodoWeb",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                ValidateLifetime = false
            });

        Assert.True(validationResult.IsValid);
        Assert.Equal("ali@example.com", validationResult.Claims[JwtRegisteredClaimNames.Email]);
        Assert.Equal("Ali", validationResult.Claims[JwtRegisteredClaimNames.Name]);
        Assert.Equal("Admin", validationResult.Claims[JwtAccessTokenGenerator.RoleClaimType]);
    }

    [Fact]
    public async Task JwtAccessTokenGenerator_TokenIsRejectedWhenSignedWithAnotherKey()
    {
        JwtSettings settings = new JwtSettings("TodoApi", "TodoWeb", SigningKey, 60);
        JwtAccessTokenGenerator generator = new JwtAccessTokenGenerator(settings, TimeProvider.System);
        User user = User.RegisterWithPassword("ali@example.com", "Ali", "hash-value");

        AccessToken accessToken = generator.GenerateToken(user);

        JsonWebTokenHandler tokenHandler = new JsonWebTokenHandler();
        TokenValidationResult validationResult = await tokenHandler.ValidateTokenAsync(
            accessToken.Value,
            new TokenValidationParameters
            {
                ValidIssuer = "TodoApi",
                ValidAudience = "TodoWeb",
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes("a-completely-different-key-used-by-an-attacker"))
            });

        Assert.False(validationResult.IsValid);
    }

    [Fact]
    public void JwtSettings_RejectsASigningKeyThatIsTooShort()
    {
        Assert.Throws<InvalidOperationException>(
            () => new JwtSettings("TodoApi", "TodoWeb", "too-short", 60));
    }

    [Fact]
    public void PasswordHasher_VerifiesOnlyTheOriginalPassword()
    {
        AspNetCorePasswordHasher hasher = new AspNetCorePasswordHasher();

        string passwordHash = hasher.HashPassword("Sifre1234");

        Assert.NotEqual("Sifre1234", passwordHash);
        Assert.Equal(PasswordVerificationOutcome.Succeeded, hasher.VerifyPassword(passwordHash, "Sifre1234"));
        Assert.Equal(PasswordVerificationOutcome.Failed, hasher.VerifyPassword(passwordHash, "sifre1234"));
    }

    [Fact]
    public void PasswordHasher_AsksForARehashWhenTheHashUsesFewerIterations()
    {
        PasswordHasher<User> weakerHasher = new PasswordHasher<User>(
            Options.Create(new PasswordHasherOptions { IterationCount = 100_000 }));
        string outdatedHash = weakerHasher.HashPassword(null!, "Sifre1234");
        AspNetCorePasswordHasher hasher = new AspNetCorePasswordHasher();

        PasswordVerificationOutcome outcome = hasher.VerifyPassword(outdatedHash, "Sifre1234");

        Assert.Equal(PasswordVerificationOutcome.SucceededRehashNeeded, outcome);
    }

    [Fact]
    public void PasswordHasher_TreatsACorruptedHashAsAFailedLogin()
    {
        AspNetCorePasswordHasher hasher = new AspNetCorePasswordHasher();

        Assert.Equal(PasswordVerificationOutcome.Failed, hasher.VerifyPassword("bozuk-hash", "Sifre1234"));
    }

    [Fact]
    public void SecureTokenGenerator_ProducesUniqueUrlSafeTokens()
    {
        SecureTokenGenerator generator = new SecureTokenGenerator();

        string firstToken = generator.GenerateToken();
        string secondToken = generator.GenerateToken();

        Assert.NotEqual(firstToken, secondToken);
        Assert.Equal(43, firstToken.Length);
        Assert.Matches("^[A-Za-z0-9_-]+$", firstToken);
    }

    [Fact]
    public void SecureTokenGenerator_HashesTheSameTokenToTheSameValue()
    {
        SecureTokenGenerator generator = new SecureTokenGenerator();
        string token = generator.GenerateToken();

        string firstHash = generator.ComputeHash(token);
        string secondHash = generator.ComputeHash(token);

        Assert.Equal(firstHash, secondHash);
        Assert.Equal(64, firstHash.Length);
        Assert.NotEqual(token, firstHash);
    }

    [Fact]
    public void PasswordHasher_ProducesADifferentHashEachTimeBecauseOfTheSalt()
    {
        AspNetCorePasswordHasher hasher = new AspNetCorePasswordHasher();

        string firstHash = hasher.HashPassword("Sifre1234");
        string secondHash = hasher.HashPassword("Sifre1234");

        Assert.NotEqual(firstHash, secondHash);
    }

    [Fact]
    public void RoleAssignmentPolicy_GivesAdminOnlyToConfiguredEmails()
    {
        ConfiguredRoleAssignmentPolicy policy = new ConfiguredRoleAssignmentPolicy(
            new[] { " Patron@Example.com " });

        Assert.Equal(UserRole.Admin, policy.GetRoleForEmail("patron@example.com"));
        Assert.Equal(UserRole.User, policy.GetRoleForEmail("stajyer@example.com"));
    }

    [Fact]
    public void GoogleSettings_TreatAnEmptyClientIdAsDisabled()
    {
        GoogleAuthenticationSettings settings = new GoogleAuthenticationSettings("   ");

        Assert.False(settings.IsEnabled);
        Assert.Null(settings.ClientId);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }
    }
}
