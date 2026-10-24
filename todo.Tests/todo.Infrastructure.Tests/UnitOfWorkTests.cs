using Microsoft.Extensions.Options;
using TodoApi.Infrastructure.Configuration;
using TodoApi.Application.Common.Results;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.JsonWebTokens;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Exceptions;
using TodoApi.Application.Features.Authentication.Common;
using TodoApi.Application.Features.Authentication.LoginWithGoogle;
using TodoApi.Application.Features.Authentication.LoginWithPassword;
using TodoApi.Application.Features.Authentication.RefreshSession;
using TodoApi.Application.Features.Authentication.RegisterWithPassword;
using TodoApi.Domain.Entities;
using TodoApi.Infrastructure.Persistence;
using TodoApi.Infrastructure.Services.Authentication;

namespace todo.Infrastructure.Tests;

public class UnitOfWorkTests
{
    [Fact]
    public async Task GoogleLogin_SavesANewUserAndItsSessionWithOneSave()
    {
        using SqliteConnection connection = CreateDatabase();
        SaveCounter counter = new SaveCounter();
        await using TodoDbContext db = CreateContext(connection, counter);
        SecureTokenGenerator tokens = new SecureTokenGenerator();
        LoginWithGoogleUseCase useCase = new LoginWithGoogleUseCase(new EfUnitOfWork(db),
            new UserRepository(db),
            new RefreshTokenRepository(db),
            new VerifiedGoogleTokenValidator(),
            new ConfiguredRoleAssignmentPolicy(Array.Empty<string>()),
            CreateIssuer(db, tokens),
            TestUseCasePipelines.ForLoginWithGoogle(new LoginWithGoogleCommandValidator()),
            TimeProvider.System);

        AuthSession session = TestResults.Success(await useCase.ExecuteAsync(
            new LoginWithGoogleCommand { IdToken = "verified-test-token" },
            TestContext.Current.CancellationToken));

        await using TodoDbContext verification = CreateContext(connection);
        User user = await verification.Users.SingleAsync(TestContext.Current.CancellationToken);
        RefreshToken token = await verification.RefreshTokens.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal(1, counter.Count);
        Assert.True(user.Id > 0);
        Assert.Equal(user.Id, token.UserId);
        Assert.Equal(user.Id, session.Result.User.Id);
        Assert.Equal(user.Id.ToString(), new JsonWebToken(session.Result.AccessToken).Subject);
        Assert.True(user.HasGoogleLogin);
        Assert.False(user.HasPassword);
    }

    [Fact]
    public async Task Register_SavesTheNewUserAndSessionOnceAndUsesTheGeneratedIdInTheJwt()
    {
        using SqliteConnection connection = CreateDatabase();
        SaveCounter counter = new SaveCounter();
        await using TodoDbContext db = CreateContext(connection, counter);
        UserRepository users = new UserRepository(db);
        SessionIssuer issuer = CreateIssuer(db, new SecureTokenGenerator());
        RegisterWithPasswordUseCase useCase = new RegisterWithPasswordUseCase(new EfUnitOfWork(db),
            users,
            new AspNetCorePasswordHasher(),
            issuer,
            TestUseCasePipelines.ForRegisterWithPassword(new RegisterWithPasswordCommandValidator()));

        AuthSession session = TestResults.Success(await useCase.ExecuteAsync(
            Registration("new@example.com"), TestContext.Current.CancellationToken));

        await using TodoDbContext verification = CreateContext(connection);
        User user = await verification.Users.SingleAsync(TestContext.Current.CancellationToken);
        RefreshToken token = await verification.RefreshTokens.SingleAsync(TestContext.Current.CancellationToken);
        JsonWebToken jwt = new JsonWebToken(session.Result.AccessToken);

        Assert.Equal(1, counter.Count);
        Assert.True(user.Id > 0);
        Assert.Equal(user.Id, token.UserId);
        Assert.Equal(user.Id, session.Result.User.Id);
        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal(new SecureTokenGenerator().ComputeHash(session.RefreshToken), token.TokenHash);
    }

    [Fact]
    public async Task Register_RollsBackTheNewUserWhenTheSessionInsertFails()
    {
        using SqliteConnection connection = CreateDatabase();
        FixedSecureTokenGenerator tokens = new FixedSecureTokenGenerator();
        await SeedUserAndTokenAsync(connection, tokens);
        SaveCounter counter = new SaveCounter();
        await using TodoDbContext db = CreateContext(connection, counter);
        RegisterWithPasswordUseCase useCase = new RegisterWithPasswordUseCase(new EfUnitOfWork(db),
            new UserRepository(db),
            new AspNetCorePasswordHasher(),
            CreateIssuer(db, tokens),
            TestUseCasePipelines.ForRegisterWithPassword(new RegisterWithPasswordCommandValidator()));

        await Assert.ThrowsAsync<DataAccessException>(() => useCase.ExecuteAsync(
            Registration("new@example.com"), TestContext.Current.CancellationToken));

        await using TodoDbContext verification = CreateContext(connection);
        Assert.Equal(1, counter.Count);
        Assert.False(await verification.Users.AnyAsync(
            user => user.Email == "new@example.com", TestContext.Current.CancellationToken));
        Assert.Equal(1, await verification.RefreshTokens.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Login_RollsBackThePasswordRehashWhenTheSessionInsertFails()
    {
        using SqliteConnection connection = CreateDatabase();
        FixedSecureTokenGenerator tokens = new FixedSecureTokenGenerator();
        await SeedUserAndTokenAsync(connection, tokens);
        await using TodoDbContext db = CreateContext(connection);
        LoginWithPasswordUseCase useCase = new LoginWithPasswordUseCase(new EfUnitOfWork(db),
            new UserRepository(db),
            new RehashingPasswordHasher(),
            CreateIssuer(db, tokens),
            TestUseCasePipelines.ForLoginWithPassword(new LoginWithPasswordCommandValidator()));

        await Assert.ThrowsAsync<DataAccessException>(() => useCase.ExecuteAsync(
            new LoginWithPasswordCommand { Email = "existing@example.com", Password = "Sifre1234" },
            TestContext.Current.CancellationToken));

        await using TodoDbContext verification = CreateContext(connection);
        Assert.Equal("old-hash", (await verification.Users.SingleAsync(
            TestContext.Current.CancellationToken)).PasswordHash);
        Assert.Equal(1, await verification.RefreshTokens.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GoogleLogin_RollsBackAccountLinkingAndRevocationWhenTheNewSessionInsertFails()
    {
        using SqliteConnection connection = CreateDatabase();
        FixedSecureTokenGenerator tokens = new FixedSecureTokenGenerator();
        await SeedUserAndTokenAsync(connection, tokens);
        await using TodoDbContext db = CreateContext(connection);
        LoginWithGoogleUseCase useCase = new LoginWithGoogleUseCase(new EfUnitOfWork(db),
            new UserRepository(db),
            new RefreshTokenRepository(db),
            new VerifiedGoogleTokenValidator(),
            new ConfiguredRoleAssignmentPolicy(Array.Empty<string>()),
            CreateIssuer(db, tokens),
            TestUseCasePipelines.ForLoginWithGoogle(new LoginWithGoogleCommandValidator()),
            TimeProvider.System);

        await Assert.ThrowsAsync<DataAccessException>(() => useCase.ExecuteAsync(
            new LoginWithGoogleCommand { IdToken = "verified-test-token" },
            TestContext.Current.CancellationToken));

        await using TodoDbContext verification = CreateContext(connection);
        User user = await verification.Users.SingleAsync(TestContext.Current.CancellationToken);
        RefreshToken token = await verification.RefreshTokens.SingleAsync(TestContext.Current.CancellationToken);
        Assert.Null(user.GoogleSubjectId);
        Assert.Equal("old-hash", user.PasswordHash);
        Assert.Null(token.RevokedAtUtc);
    }

    [Fact]
    public async Task Refresh_KeepsTheOldTokenActiveWhenTheReplacementInsertFails()
    {
        using SqliteConnection connection = CreateDatabase();
        FixedSecureTokenGenerator tokens = new FixedSecureTokenGenerator();
        await SeedUserAndTokenAsync(connection, tokens);
        string oldTokenValue = "old-refresh-token";
        string oldTokenHash = tokens.ComputeHash(oldTokenValue);
        await using (TodoDbContext seed = CreateContext(connection))
        {
            User user = await seed.Users.SingleAsync(TestContext.Current.CancellationToken);
            DateTime now = DateTime.UtcNow;
            seed.RefreshTokens.Add(new RefreshToken(user.Id, oldTokenHash, now, now.AddDays(7)));
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using TodoDbContext db = CreateContext(connection);
        RefreshSessionUseCase useCase = CreateRefreshUseCase(db, tokens);
        await Assert.ThrowsAsync<DataAccessException>(() => useCase.ExecuteAsync(
            oldTokenValue, TestContext.Current.CancellationToken));

        await using TodoDbContext verification = CreateContext(connection);
        RefreshToken oldToken = await verification.RefreshTokens.SingleAsync(
            token => token.TokenHash == oldTokenHash, TestContext.Current.CancellationToken);
        Assert.Null(oldToken.RevokedAtUtc);
        Assert.Null(oldToken.ReplacedByTokenHash);
        Assert.Equal(2, await verification.RefreshTokens.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Refresh_PersistsReuseRevocationBeforeRejectingTheRequest()
    {
        using SqliteConnection connection = CreateDatabase();
        FixedSecureTokenGenerator tokens = new FixedSecureTokenGenerator();
        await SeedUserAndTokenAsync(connection, tokens);
        string reusedTokenValue = "reused-refresh-token";
        await using (TodoDbContext seed = CreateContext(connection))
        {
            User user = await seed.Users.SingleAsync(TestContext.Current.CancellationToken);
            DateTime now = DateTime.UtcNow;
            RefreshToken revoked = new RefreshToken(
                user.Id, tokens.ComputeHash(reusedTokenValue), now.AddHours(-1), now.AddDays(7));
            revoked.Revoke(now.AddMinutes(-1), tokens.ComputeHash(tokens.GenerateToken()));
            seed.RefreshTokens.Add(revoked);
            await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        SaveCounter counter = new SaveCounter();
        await using TodoDbContext db = CreateContext(connection, counter);
        await TestResults.FailureAsync(() => CreateRefreshUseCase(db, tokens)
            .ExecuteAsync(reusedTokenValue, TestContext.Current.CancellationToken), ErrorKind.Unauthorized);

        await using TodoDbContext verification = CreateContext(connection);
        List<RefreshToken> savedTokens = await verification.RefreshTokens.ToListAsync(
            TestContext.Current.CancellationToken);
        Assert.Equal(1, counter.Count);
        Assert.All(savedTokens, token => Assert.True(token.IsRevoked));
    }

    private static SqliteConnection CreateDatabase()
    {
        SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using TodoDbContext db = CreateContext(connection);
        db.Database.EnsureCreated();
        return connection;
    }

    private static TodoDbContext CreateContext(SqliteConnection connection, SaveCounter? counter = null)
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>().UseSqlite(connection);
        if (counter != null)
        {
            options.AddInterceptors(counter);
        }

        return new TodoDbContext(options.Options);
    }

    private static async Task SeedUserAndTokenAsync(SqliteConnection connection, ISecureTokenGenerator tokens)
    {
        await using TodoDbContext seed = CreateContext(connection);
        User user = User.RegisterWithPassword("existing@example.com", "Existing user", "old-hash");
        DateTime now = DateTime.UtcNow;
        seed.Users.Add(user);
        seed.RefreshTokens.Add(new RefreshToken(
            user, tokens.ComputeHash(tokens.GenerateToken()), now, now.AddDays(7)));
        await seed.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static SessionIssuer CreateIssuer(TodoDbContext db, ISecureTokenGenerator tokens)
    {
        var settings = Options.Create(new JwtOptions
        { Issuer = "transaction-tests", Audience = "transaction-tests", SigningKey = "unit-of-work-test-signing-key-32-bytes-minimum", AccessTokenLifetimeMinutes = 15, RefreshTokenLifetimeDays = 7 });
        return new SessionIssuer(
            new JwtAccessTokenGenerator(settings, TimeProvider.System), tokens,
            new RefreshTokenRepository(db), new RefreshTokenSettings(TimeSpan.FromDays(7)), TimeProvider.System);
    }

    private static RegisterWithPasswordCommand Registration(string email)
    {
        return new RegisterWithPasswordCommand { Email = email, DisplayName = "New user", Password = "Sifre1234" };
    }

    private static RefreshSessionUseCase CreateRefreshUseCase(TodoDbContext db, ISecureTokenGenerator tokens)
    {
        return new RefreshSessionUseCase(new EfUnitOfWork(db),
            new RefreshTokenRepository(db),
            new UserRepository(db),
            tokens,
            CreateIssuer(db, tokens),
            TimeProvider.System,
            TestUseCasePipelines.ForRefreshSession());
    }

    private sealed class SaveCounter : SaveChangesInterceptor
    {
        public int Count { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Count++;
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    private sealed class FixedSecureTokenGenerator : ISecureTokenGenerator
    {
        private readonly SecureTokenGenerator _hashing = new SecureTokenGenerator();

        public string GenerateToken() => "duplicate-test-refresh-token";

        public string ComputeHash(string token) => _hashing.ComputeHash(token);
    }

    private sealed class RehashingPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => "replacement-hash";

        public PasswordVerificationOutcome VerifyPassword(string passwordHash, string providedPassword)
        {
            return passwordHash == "old-hash" && providedPassword == "Sifre1234"
                ? PasswordVerificationOutcome.SucceededRehashNeeded
                : PasswordVerificationOutcome.Failed;
        }
    }

    private sealed class VerifiedGoogleTokenValidator : IGoogleTokenValidator
    {
        public Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GoogleUserInfo? user = idToken == "verified-test-token"
                ? new GoogleUserInfo("test-google-subject", "existing@example.com", true, "Existing user")
                : null;
            return Task.FromResult(user);
        }
    }
}
