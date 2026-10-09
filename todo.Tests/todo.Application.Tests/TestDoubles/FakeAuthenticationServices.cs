using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;

namespace todo.Application.Tests.TestDoubles;

public sealed class FakePasswordHasher : IPasswordHasher
{
    public const string CurrentPrefix = "hashed:";
    public const string OutdatedPrefix = "old-hashed:";

    public int HashCallCount { get; private set; }

    public string HashPassword(string password)
    {
        HashCallCount++;

        return CurrentPrefix + password;
    }

    public PasswordVerificationOutcome VerifyPassword(string passwordHash, string providedPassword)
    {
        if (passwordHash == CurrentPrefix + providedPassword)
        {
            return PasswordVerificationOutcome.Succeeded;
        }

        if (passwordHash == OutdatedPrefix + providedPassword)
        {
            return PasswordVerificationOutcome.SucceededRehashNeeded;
        }

        return PasswordVerificationOutcome.Failed;
    }
}

public sealed class FakeAccessTokenGenerator : IAccessTokenGenerator
{
    public static readonly DateTime ExpiresAtUtc = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public AccessToken GenerateToken(User user)
    {
        return new AccessToken($"token-for-{user.Id}-{user.Role}", ExpiresAtUtc);
    }
}

public sealed class FakeSecureTokenGenerator : ISecureTokenGenerator
{
    private int _nextTokenNumber = 1;

    public string GenerateToken()
    {
        string token = $"refresh-{_nextTokenNumber}";
        _nextTokenNumber++;

        return token;
    }

    public string ComputeHash(string token)
    {
        return "hash-of-" + token;
    }
}

public sealed class FakeTimeProvider : TimeProvider
{
    public FakeTimeProvider(DateTimeOffset now)
    {
        Now = now;
    }

    public static FakeTimeProvider AtTestStart()
    {
        return new FakeTimeProvider(new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero));
    }

    public DateTimeOffset Now { get; set; }

    public override DateTimeOffset GetUtcNow()
    {
        return Now;
    }

    public void Advance(TimeSpan duration)
    {
        Now = Now.Add(duration);
    }
}

public sealed class FakeGoogleTokenValidator : IGoogleTokenValidator
{
    private readonly Dictionary<string, GoogleUserInfo> _validTokens =
        new Dictionary<string, GoogleUserInfo>();

    public void AddValidToken(string idToken, GoogleUserInfo googleUser)
    {
        _validTokens[idToken] = googleUser;
    }

    public Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        bool isKnownToken = _validTokens.TryGetValue(idToken, out GoogleUserInfo? googleUser);

        if (isKnownToken == false)
        {
            return Task.FromResult<GoogleUserInfo?>(null);
        }

        return Task.FromResult<GoogleUserInfo?>(googleUser);
    }
}

public sealed class FakeRoleAssignmentPolicy : IRoleAssignmentPolicy
{
    private readonly HashSet<string> _adminEmails;

    public FakeRoleAssignmentPolicy(params string[] adminEmails)
    {
        _adminEmails = new HashSet<string>(adminEmails, StringComparer.OrdinalIgnoreCase);
    }

    public UserRole GetRoleForEmail(string email)
    {
        if (_adminEmails.Contains(email))
        {
            return UserRole.Admin;
        }

        return UserRole.User;
    }
}
