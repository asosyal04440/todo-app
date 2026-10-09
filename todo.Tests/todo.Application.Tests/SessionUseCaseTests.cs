using TodoApi.Application.Exceptions;
using TodoApi.Application.Features.Authentication.Common;
using TodoApi.Application.Features.Authentication.Logout;
using TodoApi.Application.Features.Authentication.RefreshSession;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using todo.Application.Tests.TestDoubles;

namespace todo.Application.Tests;

public class SessionUseCaseTests
{
    private readonly FakeUserRepository _userRepository = new FakeUserRepository();
    private readonly FakeRefreshTokenRepository _refreshTokenRepository = new FakeRefreshTokenRepository();
    private readonly FakeSecureTokenGenerator _secureTokenGenerator = new FakeSecureTokenGenerator();
    private readonly FakeTimeProvider _timeProvider =
        new FakeTimeProvider(new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero));
    private readonly SessionIssuer _sessionIssuer;
    private readonly User _user;

    public SessionUseCaseTests()
    {
        _sessionIssuer = new SessionIssuer(
            new FakeAccessTokenGenerator(),
            _secureTokenGenerator,
            _refreshTokenRepository,
            new RefreshTokenSettings(TimeSpan.FromDays(7)),
            _timeProvider);
        _user = _userRepository.AddExistingUser(
            User.RegisterWithPassword("ali@example.com", "Ali", "hashed:Sifre1234"));
    }

    [Fact]
    public async Task Refresh_RotatesTheRefreshToken()
    {
        AuthSession firstSession = await _sessionIssuer.IssueAsync(_user, TestContext.Current.CancellationToken);
        RefreshSessionUseCase useCase = CreateRefreshUseCase();

        AuthSession refreshedSession = await useCase.ExecuteAsync(
            firstSession.RefreshToken,
            TestContext.Current.CancellationToken);

        RefreshToken oldToken = _refreshTokenRepository.RefreshTokens[0];
        RefreshToken newToken = _refreshTokenRepository.RefreshTokens[1];
        DateTime nowUtc = _timeProvider.Now.UtcDateTime;

        Assert.NotEqual(firstSession.RefreshToken, refreshedSession.RefreshToken);
        Assert.False(oldToken.IsActive(nowUtc));
        Assert.Equal(newToken.TokenHash, oldToken.ReplacedByTokenHash);
        Assert.True(newToken.IsActive(nowUtc));
        Assert.Equal("ali@example.com", refreshedSession.Result.User.Email);
    }

    [Fact]
    public async Task Refresh_RevokesEverySessionWhenAnOldTokenIsReused()
    {
        AuthSession firstSession = await _sessionIssuer.IssueAsync(_user, TestContext.Current.CancellationToken);
        RefreshSessionUseCase useCase = CreateRefreshUseCase();
        AuthSession refreshedSession = await useCase.ExecuteAsync(
            firstSession.RefreshToken,
            TestContext.Current.CancellationToken);

        _timeProvider.Advance(TimeSpan.FromHours(1));

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => useCase.ExecuteAsync(firstSession.RefreshToken, TestContext.Current.CancellationToken));

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => useCase.ExecuteAsync(refreshedSession.RefreshToken, TestContext.Current.CancellationToken));

        DateTime nowUtc = _timeProvider.Now.UtcDateTime;
        Assert.All(_refreshTokenRepository.RefreshTokens, token => Assert.False(token.IsActive(nowUtc)));
    }

    [Fact]
    public async Task Refresh_DoesNotRevokeEverythingForAConcurrentRefreshFromAnotherTab()
    {
        AuthSession firstSession = await _sessionIssuer.IssueAsync(_user, TestContext.Current.CancellationToken);
        RefreshSessionUseCase useCase = CreateRefreshUseCase();
        AuthSession refreshedSession = await useCase.ExecuteAsync(
            firstSession.RefreshToken,
            TestContext.Current.CancellationToken);

        _timeProvider.Advance(TimeSpan.FromSeconds(2));

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => useCase.ExecuteAsync(firstSession.RefreshToken, TestContext.Current.CancellationToken));

        AuthSession stillValid = await useCase.ExecuteAsync(
            refreshedSession.RefreshToken,
            TestContext.Current.CancellationToken);
        Assert.NotNull(stillValid);
    }

    [Fact]
    public async Task Refresh_RejectsAnExpiredToken()
    {
        AuthSession session = await _sessionIssuer.IssueAsync(_user, TestContext.Current.CancellationToken);
        RefreshSessionUseCase useCase = CreateRefreshUseCase();

        _timeProvider.Advance(TimeSpan.FromDays(8));

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => useCase.ExecuteAsync(session.RefreshToken, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hic-verilmemis-bir-token")]
    public async Task Refresh_RejectsAMissingOrUnknownToken(string? refreshToken)
    {
        RefreshSessionUseCase useCase = CreateRefreshUseCase();

        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => useCase.ExecuteAsync(refreshToken, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        AuthSession session = await _sessionIssuer.IssueAsync(_user, TestContext.Current.CancellationToken);
        LogoutUseCase logoutUseCase = new LogoutUseCase(
            _refreshTokenRepository,
            _secureTokenGenerator,
            _timeProvider);

        await logoutUseCase.ExecuteAsync(session.RefreshToken, TestContext.Current.CancellationToken);

        Assert.True(Assert.Single(_refreshTokenRepository.RefreshTokens).IsRevoked);
        await Assert.ThrowsAsync<AuthenticationFailedException>(
            () => CreateRefreshUseCase().ExecuteAsync(
                session.RefreshToken,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Logout_SucceedsEvenWithoutAToken()
    {
        LogoutUseCase logoutUseCase = new LogoutUseCase(
            _refreshTokenRepository,
            _secureTokenGenerator,
            _timeProvider);

        await logoutUseCase.ExecuteAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal(0, _refreshTokenRepository.SaveChangesCount);
    }

    private RefreshSessionUseCase CreateRefreshUseCase()
    {
        return new RefreshSessionUseCase(
            _refreshTokenRepository,
            _userRepository,
            _secureTokenGenerator,
            _sessionIssuer,
            _timeProvider);
    }
}
