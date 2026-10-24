using TodoApi.Application.Common.Results;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Contracts;
using TodoApi.Application.Exceptions;
using TodoApi.Application.Features.Authentication.Common;
using TodoApi.Application.Features.Authentication.GetCurrentUser;
using TodoApi.Application.Features.Authentication.LoginWithGoogle;
using TodoApi.Application.Features.Authentication.LoginWithPassword;
using TodoApi.Application.Features.Authentication.RegisterWithPassword;
using TodoApi.Application.Features.Users.GetUsers;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using todo.Application.Tests.TestDoubles;

namespace todo.Application.Tests;

public class AuthenticationUseCaseTests
{
    private readonly FakeUnitOfWork _unitOfWork;

    public AuthenticationUseCaseTests()
    {
        _unitOfWork = new FakeUnitOfWork(
            _userRepository.ApplyChanges,
            _refreshTokenRepository.ApplyChanges);
    }

    private readonly FakeUserRepository _userRepository = new FakeUserRepository();
    private readonly FakeRefreshTokenRepository _refreshTokenRepository = new FakeRefreshTokenRepository();
    private readonly FakePasswordHasher _passwordHasher = new FakePasswordHasher();
    private readonly FakeGoogleTokenValidator _googleTokenValidator = new FakeGoogleTokenValidator();
    private readonly FakeTimeProvider _timeProvider =
        new FakeTimeProvider(new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Register_SavesTheUserWithAHashedPasswordAndStartsASession()
    {
        RegisterWithPasswordUseCase useCase = CreateRegisterUseCase();

        AuthSession session = TestResults.Success(await useCase.ExecuteAsync(
            new RegisterWithPasswordCommand { Email = "  Ali@Example.com ", DisplayName = "Ali", Password = "Sifre1234" },
            TestContext.Current.CancellationToken));

        User savedUser = Assert.Single(_userRepository.Users);
        Assert.Equal("ali@example.com", savedUser.Email);
        Assert.Equal("hashed:Sifre1234", savedUser.PasswordHash);
        Assert.Equal(UserRole.User, savedUser.Role);
        Assert.Equal("token-for-1-User", session.Result.AccessToken);
        Assert.Equal(FakeAccessTokenGenerator.ExpiresAtUtc, session.Result.ExpiresAtUtc);
        Assert.Equal("ali@example.com", session.Result.User.Email);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task Register_StoresOnlyTheHashOfTheRefreshToken()
    {
        RegisterWithPasswordUseCase useCase = CreateRegisterUseCase();

        AuthSession session = TestResults.Success(await useCase.ExecuteAsync(
            new RegisterWithPasswordCommand { Email = "ali@example.com", DisplayName = "Ali", Password = "Sifre1234" },
            TestContext.Current.CancellationToken));

        RefreshToken storedToken = Assert.Single(_refreshTokenRepository.RefreshTokens);
        Assert.Equal("refresh-1", session.RefreshToken);
        Assert.Equal("hash-of-refresh-1", storedToken.TokenHash);
        Assert.NotEqual(session.RefreshToken, storedToken.TokenHash);
        Assert.Equal(1, storedToken.UserId);
        Assert.Equal(_timeProvider.Now.UtcDateTime.AddDays(7), session.RefreshTokenExpiresAtUtc);
    }

    [Fact]
    public async Task Register_RejectsAnEmailThatIsAlreadyRegistered()
    {
        _userRepository.AddExistingUser(
            User.RegisterWithPassword("ali@example.com", "Ali", "hashed:Sifre1234"));
        RegisterWithPasswordUseCase useCase = CreateRegisterUseCase();

        await TestResults.FailureAsync(
            () => useCase.ExecuteAsync(
                new RegisterWithPasswordCommand { Email = "ALI@example.com", DisplayName = "Başka Ali", Password = "Sifre1234" },
                TestContext.Current.CancellationToken), ErrorKind.Conflict);

        Assert.Single(_userRepository.Users);
        Assert.Empty(_refreshTokenRepository.RefreshTokens);
    }

    [Theory]
    [InlineData("kisa1")]
    [InlineData("sadeceharf")]
    [InlineData("1234567890")]
    public async Task Register_RejectsAWeakPassword(string password)
    {
        RegisterWithPasswordUseCase useCase = CreateRegisterUseCase();

        await TestResults.FailureAsync(
            () => useCase.ExecuteAsync(
                new RegisterWithPasswordCommand { Email = "ali@example.com", DisplayName = "Ali", Password = password },
                TestContext.Current.CancellationToken), ErrorKind.Validation);

        Assert.Empty(_userRepository.Users);
    }

    [Fact]
    public async Task Register_NeverGivesTheAdminRoleBecauseTheEmailIsNotVerified()
    {
        RegisterWithPasswordUseCase useCase = CreateRegisterUseCase();

        AuthSession session = TestResults.Success(await useCase.ExecuteAsync(
            new RegisterWithPasswordCommand { Email = "patron@example.com", DisplayName = "Patron", Password = "Sifre1234" },
            TestContext.Current.CancellationToken));

        Assert.Equal(UserRole.User, session.Result.User.Role);
    }

    [Fact]
    public async Task Login_StartsASessionForTheCorrectPassword()
    {
        _userRepository.AddExistingUser(
            User.RegisterWithPassword("ali@example.com", "Ali", "hashed:Sifre1234"));
        LoginWithPasswordUseCase useCase = CreateLoginUseCase();

        AuthSession session = TestResults.Success(await useCase.ExecuteAsync(
            new LoginWithPasswordCommand { Email = " ALI@example.com ", Password = "Sifre1234" },
            TestContext.Current.CancellationToken));

        Assert.Equal("token-for-1-User", session.Result.AccessToken);
        Assert.Equal("ali@example.com", session.Result.User.Email);
        Assert.Single(_refreshTokenRepository.RefreshTokens);
    }

    [Fact]
    public async Task Login_RejectsAWrongPasswordAndAnUnknownEmailWithTheSameMessage()
    {
        _userRepository.AddExistingUser(
            User.RegisterWithPassword("ali@example.com", "Ali", "hashed:Sifre1234"));
        LoginWithPasswordUseCase useCase = CreateLoginUseCase();

        AppError wrongPassword = await TestResults.FailureAsync(
            () => useCase.ExecuteAsync(
                new LoginWithPasswordCommand { Email = "ali@example.com", Password = "YanlisSifre1" },
                TestContext.Current.CancellationToken), ErrorKind.Unauthorized);
        AppError unknownEmail = await TestResults.FailureAsync(
            () => useCase.ExecuteAsync(
                new LoginWithPasswordCommand { Email = "kimse@example.com", Password = "Sifre1234" },
                TestContext.Current.CancellationToken), ErrorKind.Unauthorized);

        Assert.Equal(wrongPassword.Message, unknownEmail.Message);
        Assert.Empty(_refreshTokenRepository.RefreshTokens);
    }

    [Fact]
    public async Task Login_DoesTheSameHashingWorkForAnUnknownEmail()
    {
        LoginWithPasswordUseCase useCase = CreateLoginUseCase();

        await TestResults.FailureAsync(
            () => useCase.ExecuteAsync(
                new LoginWithPasswordCommand { Email = "kimse@example.com", Password = "Sifre1234" },
                TestContext.Current.CancellationToken), ErrorKind.Unauthorized);

        Assert.Equal(1, _passwordHasher.HashCallCount);
    }

    [Fact]
    public async Task Login_RejectsAPasswordLoginForAGoogleOnlyAccount()
    {
        _userRepository.AddExistingUser(
            User.RegisterWithGoogle("ayse@gmail.com", "Ayşe", "google-subject-1", UserRole.User));
        LoginWithPasswordUseCase useCase = CreateLoginUseCase();

        await TestResults.FailureAsync(
            () => useCase.ExecuteAsync(
                new LoginWithPasswordCommand { Email = "ayse@gmail.com", Password = "Sifre1234" },
                TestContext.Current.CancellationToken), ErrorKind.Unauthorized);
    }

    [Fact]
    public async Task Login_UpgradesAPasswordHashThatWasCreatedWithOlderSettings()
    {
        User existingUser = _userRepository.AddExistingUser(
            User.RegisterWithPassword("ali@example.com", "Ali", "old-hashed:Sifre1234"));
        LoginWithPasswordUseCase useCase = CreateLoginUseCase();

        await useCase.ExecuteAsync(
            new LoginWithPasswordCommand { Email = "ali@example.com", Password = "Sifre1234" },
            TestContext.Current.CancellationToken);

        Assert.Equal("hashed:Sifre1234", existingUser.PasswordHash);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task GoogleLogin_CreatesANewUserOnTheFirstSignIn()
    {
        _googleTokenValidator.AddValidToken(
            "valid-google-token",
            new GoogleUserInfo("google-subject-1", "Ayse@Gmail.com", isEmailVerified: true, "Ayşe Yılmaz"));
        LoginWithGoogleUseCase useCase = CreateGoogleLoginUseCase();

        AuthSession session = TestResults.Success(await useCase.ExecuteAsync(
            new LoginWithGoogleCommand { IdToken = "valid-google-token" },
            TestContext.Current.CancellationToken));

        User savedUser = Assert.Single(_userRepository.Users);
        Assert.Equal("ayse@gmail.com", savedUser.Email);
        Assert.Equal("Ayşe Yılmaz", savedUser.DisplayName);
        Assert.Equal("google-subject-1", savedUser.GoogleSubjectId);
        Assert.False(savedUser.HasPassword);
        Assert.True(session.Result.User.HasGoogleLogin);
        Assert.Equal("token-for-1-User", session.Result.AccessToken);
        Assert.Equal(1, _unitOfWork.SaveChangesCount);
        Assert.Equal(savedUser.Id, Assert.Single(_refreshTokenRepository.RefreshTokens).UserId);
    }

    [Fact]
    public async Task GoogleLogin_ReusesTheSameUserOnTheNextSignIn()
    {
        _userRepository.AddExistingUser(
            User.RegisterWithGoogle("ayse@gmail.com", "Ayşe", "google-subject-1", UserRole.User));
        _googleTokenValidator.AddValidToken(
            "valid-google-token",
            new GoogleUserInfo("google-subject-1", "ayse@gmail.com", isEmailVerified: true, "Ayşe"));
        LoginWithGoogleUseCase useCase = CreateGoogleLoginUseCase();

        AuthSession session = TestResults.Success(await useCase.ExecuteAsync(
            new LoginWithGoogleCommand { IdToken = "valid-google-token" },
            TestContext.Current.CancellationToken));

        Assert.Single(_userRepository.Users);
        Assert.Equal(1, session.Result.User.Id);
    }

    [Fact]
    public async Task GoogleLogin_LinksAnExistingPasswordAccountAndDiscardsItsPassword()
    {
        User existingUser = _userRepository.AddExistingUser(
            User.RegisterWithPassword("ali@gmail.com", "Ali", "hashed:Sifre1234"));
        _googleTokenValidator.AddValidToken(
            "valid-google-token",
            new GoogleUserInfo("google-subject-1", "ali@gmail.com", isEmailVerified: true, "Ali"));
        LoginWithGoogleUseCase useCase = CreateGoogleLoginUseCase();

        AuthSession session = TestResults.Success(await useCase.ExecuteAsync(
            new LoginWithGoogleCommand { IdToken = "valid-google-token" },
            TestContext.Current.CancellationToken));

        Assert.Single(_userRepository.Users);
        Assert.Equal("google-subject-1", existingUser.GoogleSubjectId);
        Assert.False(session.Result.User.HasPassword);
        Assert.True(session.Result.User.HasGoogleLogin);
    }

    [Fact]
    public async Task GoogleLogin_GivesTheAdminRoleToAConfiguredAdminEmail()
    {
        _googleTokenValidator.AddValidToken(
            "valid-google-token",
            new GoogleUserInfo("google-subject-1", "patron@gmail.com", isEmailVerified: true, "Patron"));
        LoginWithGoogleUseCase useCase = CreateGoogleLoginUseCase("patron@gmail.com");

        AuthSession session = TestResults.Success(await useCase.ExecuteAsync(
            new LoginWithGoogleCommand { IdToken = "valid-google-token" },
            TestContext.Current.CancellationToken));

        Assert.Equal(UserRole.Admin, session.Result.User.Role);
    }

    [Fact]
    public async Task GoogleLogin_LocksOutSomeoneWhoRegisteredTheOwnersEmailFirst()
    {
        AuthSession attackerSession = TestResults.Success(await CreateRegisterUseCase().ExecuteAsync(
            new RegisterWithPasswordCommand { Email = "patron@gmail.com", DisplayName = "Saldırgan", Password = "Saldirgan123" },
            TestContext.Current.CancellationToken));
        _googleTokenValidator.AddValidToken(
            "owner-google-token",
            new GoogleUserInfo("google-subject-1", "patron@gmail.com", isEmailVerified: true, "Patron"));

        Assert.Equal(UserRole.User, attackerSession.Result.User.Role);
        RefreshToken attackerToken = Assert.Single(_refreshTokenRepository.RefreshTokens);

        AuthSession ownerSession = TestResults.Success(await CreateGoogleLoginUseCase("patron@gmail.com").ExecuteAsync(
            new LoginWithGoogleCommand { IdToken = "owner-google-token" },
            TestContext.Current.CancellationToken));

        Assert.True(attackerToken.IsRevoked);
        Assert.Equal(UserRole.Admin, ownerSession.Result.User.Role);
        Assert.False(ownerSession.Result.User.HasPassword);

        await TestResults.FailureAsync(
            () => CreateLoginUseCase().ExecuteAsync(
                new LoginWithPasswordCommand { Email = "patron@gmail.com", Password = "Saldirgan123" },
                TestContext.Current.CancellationToken), ErrorKind.Unauthorized);
    }

    [Fact]
    public async Task GoogleLogin_RejectsATokenThatGoogleDidNotIssue()
    {
        LoginWithGoogleUseCase useCase = CreateGoogleLoginUseCase();

        await TestResults.FailureAsync(
            () => useCase.ExecuteAsync(
                new LoginWithGoogleCommand { IdToken = "forged-token" },
                TestContext.Current.CancellationToken), ErrorKind.Unauthorized);

        Assert.Empty(_userRepository.Users);
    }

    [Fact]
    public async Task GoogleLogin_RejectsAnUnverifiedGoogleEmail()
    {
        _googleTokenValidator.AddValidToken(
            "unverified-token",
            new GoogleUserInfo("google-subject-1", "ali@gmail.com", isEmailVerified: false, "Ali"));
        LoginWithGoogleUseCase useCase = CreateGoogleLoginUseCase();

        await TestResults.FailureAsync(
            () => useCase.ExecuteAsync(
                new LoginWithGoogleCommand { IdToken = "unverified-token" },
                TestContext.Current.CancellationToken), ErrorKind.Unauthorized);

        Assert.Empty(_userRepository.Users);
    }

    [Fact]
    public async Task GetCurrentUser_ReturnsTheSignedInUser()
    {
        _userRepository.AddExistingUser(
            User.RegisterWithPassword("ali@example.com", "Ali", "hashed:Sifre1234"));
        GetCurrentUserUseCase useCase = new GetCurrentUserUseCase(_userRepository,
            new FakeCurrentUser(1, isAdmin: false),
            TestUseCasePipelines.ForGetCurrentUser());

        var result = TestResults.Success(await useCase.ExecuteAsync(TestContext.Current.CancellationToken));

        Assert.NotNull(result);
        Assert.Equal("ali@example.com", result.Email);
    }

    [Fact]
    public async Task GetUsers_ReturnsEveryUserWithoutTheirPasswordHash()
    {
        _userRepository.AddExistingUser(
            User.RegisterWithPassword("ali@example.com", "Ali", "hashed:Sifre1234"));
        _userRepository.AddExistingUser(
            User.RegisterWithGoogle("ayse@gmail.com", "Ayşe", "google-subject-1", UserRole.Admin));
        GetUsersUseCase useCase = new GetUsersUseCase(_userRepository,
            TestUseCasePipelines.ForGetUsers());

        var results = TestResults.Success(await useCase.ExecuteAsync(TestContext.Current.CancellationToken));

        Assert.Collection(
            results.Items,
            first => Assert.Equal("ali@example.com", first.Email),
            second => Assert.Equal(UserRole.Admin, second.Role));
        Assert.Null(typeof(UserDto).GetProperty("PasswordHash"));
    }

    private SessionIssuer CreateSessionIssuer()
    {
        return new SessionIssuer(
            new FakeAccessTokenGenerator(),
            new FakeSecureTokenGenerator(),
            _refreshTokenRepository,
            new RefreshTokenSettings(TimeSpan.FromDays(7)),
            _timeProvider);
    }

    private RegisterWithPasswordUseCase CreateRegisterUseCase()
    {
        return new RegisterWithPasswordUseCase(_unitOfWork,
            _userRepository,
            _passwordHasher,
            CreateSessionIssuer(),
            TestUseCasePipelines.ForRegisterWithPassword(new RegisterWithPasswordCommandValidator()));
    }

    private LoginWithPasswordUseCase CreateLoginUseCase()
    {
        return new LoginWithPasswordUseCase(_unitOfWork,
            _userRepository,
            _passwordHasher,
            CreateSessionIssuer(),
            TestUseCasePipelines.ForLoginWithPassword(new LoginWithPasswordCommandValidator()));
    }

    private LoginWithGoogleUseCase CreateGoogleLoginUseCase(params string[] adminEmails)
    {
        return new LoginWithGoogleUseCase(_unitOfWork,
            _userRepository,
            _refreshTokenRepository,
            _googleTokenValidator,
            new FakeRoleAssignmentPolicy(adminEmails),
            CreateSessionIssuer(),
            TestUseCasePipelines.ForLoginWithGoogle(new LoginWithGoogleCommandValidator()),
            _timeProvider);
    }
}
