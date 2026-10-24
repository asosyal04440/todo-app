using TodoApi.Application.Common.Results;
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
    private readonly FakeUnitOfWork _unitOfWork;
    private readonly FakeUserRepository _userRepository = new FakeUserRepository();
    private readonly FakeRefreshTokenRepository _refreshTokenRepository = new FakeRefreshTokenRepository();
    private readonly FakeSecureTokenGenerator _secureTokenGenerator = new FakeSecureTokenGenerator();
    private readonly FakeTimeProvider _timeProvider =
        new FakeTimeProvider(new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero));
    private readonly SessionIssuer _sessionIssuer;
    private readonly User _user;

    public SessionUseCaseTests()
    {
        _unitOfWork = new FakeUnitOfWork(
            _userRepository.ApplyChanges,
            _refreshTokenRepository.ApplyChanges);
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
        AuthSession firstSession = await StartSessionAsync();
        RefreshSessionUseCase useCase = CreateRefreshUseCase();

        AuthSession refreshedSession = TestResults.Success(await useCase.ExecuteAsync(
            firstSession.RefreshToken,
            TestContext.Current.CancellationToken));

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
        AuthSession firstSession = await StartSessionAsync();
        RefreshSessionUseCase useCase = CreateRefreshUseCase();
        AuthSession refreshedSession = TestResults.Success(await useCase.ExecuteAsync(
            firstSession.RefreshToken,
            TestContext.Current.CancellationToken));

        _timeProvider.Advance(TimeSpan.FromHours(1));

        await TestResults.FailureAsync(
            () => useCase.ExecuteAsync(firstSession.RefreshToken, TestContext.Current.CancellationToken), ErrorKind.Unauthorized);

        await TestResults.FailureAsync(
            () => useCase.ExecuteAsync(refreshedSession.RefreshToken, TestContext.Current.CancellationToken), ErrorKind.Unauthorized);

        DateTime nowUtc = _timeProvider.Now.UtcDateTime;
        Assert.All(_refreshTokenRepository.RefreshTokens, token => Assert.False(token.IsActive(nowUtc)));
    }

    [Fact]
    public async Task Refresh_DoesNotRevokeEverythingForAConcurrentRefreshFromAnotherTab()
    {
        AuthSession firstSession = await StartSessionAsync();
        RefreshSessionUseCase useCase = CreateRefreshUseCase();
        AuthSession refreshedSession = TestResults.Success(await useCase.ExecuteAsync(
            firstSession.RefreshToken,
            TestContext.Current.CancellationToken));

        _timeProvider.Advance(TimeSpan.FromSeconds(2));

        await TestResults.FailureAsync(
            () => useCase.ExecuteAsync(firstSession.RefreshToken, TestContext.Current.CancellationToken), ErrorKind.Unauthorized);

        AuthSession stillValid = TestResults.Success(await useCase.ExecuteAsync(
            refreshedSession.RefreshToken,
            TestContext.Current.CancellationToken));
        Assert.NotNull(stillValid);
    }

    [Fact]
    public async Task Refresh_RejectsAnExpiredToken()
    {
        AuthSession session = await StartSessionAsync();
        RefreshSessionUseCase useCase = CreateRefreshUseCase();

        _timeProvider.Advance(TimeSpan.FromDays(8));

        await TestResults.FailureAsync(
            () => useCase.ExecuteAsync(session.RefreshToken, TestContext.Current.CancellationToken), ErrorKind.Unauthorized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("hic-verilmemis-bir-token")]
    public async Task Refresh_RejectsAMissingOrUnknownToken(string? refreshToken)
    {
        RefreshSessionUseCase useCase = CreateRefreshUseCase();

        await TestResults.FailureAsync(
            () => useCase.ExecuteAsync(refreshToken, TestContext.Current.CancellationToken), ErrorKind.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        AuthSession session = await StartSessionAsync();
        LogoutUseCase logoutUseCase = new LogoutUseCase(_unitOfWork,
            _refreshTokenRepository,
            _secureTokenGenerator,
            _timeProvider,
            TestUseCasePipelines.ForLogout());

        await logoutUseCase.ExecuteAsync(session.RefreshToken, TestContext.Current.CancellationToken);

        Assert.True(Assert.Single(_refreshTokenRepository.RefreshTokens).IsRevoked);
        await TestResults.FailureAsync(
            () => CreateRefreshUseCase().ExecuteAsync(
                session.RefreshToken,
                TestContext.Current.CancellationToken), ErrorKind.Unauthorized);
    }

    [Fact]
    public async Task Logout_SucceedsEvenWithoutAToken()
    {
        LogoutUseCase logoutUseCase = new LogoutUseCase(_unitOfWork,
            _refreshTokenRepository,
            _secureTokenGenerator,
            _timeProvider,
            TestUseCasePipelines.ForLogout());

        await logoutUseCase.ExecuteAsync(null, TestContext.Current.CancellationToken);

        Assert.Equal(0, _unitOfWork.SaveChangesCount);
    }

    private async Task<AuthSession> StartSessionAsync()
    {
        PendingAuthSession pending = await _sessionIssuer.PrepareAsync(
            _user,
            TestContext.Current.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        return _sessionIssuer.CreateSession(pending);
    }

    private RefreshSessionUseCase CreateRefreshUseCase()
    {
        return new RefreshSessionUseCase(_unitOfWork,
            _refreshTokenRepository,
            _userRepository,
            _secureTokenGenerator,
            _sessionIssuer,
            _timeProvider,
            TestUseCasePipelines.ForRefreshSession());
    }
}
