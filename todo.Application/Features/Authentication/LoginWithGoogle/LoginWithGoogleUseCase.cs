using TodoApi.Application.Common.Results;
using TodoApi.Application.Common.Execution;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Exceptions;
using TodoApi.Application.Features.Authentication.Common;
using TodoApi.Domain.Common;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;

namespace TodoApi.Application.Features.Authentication.LoginWithGoogle;

public class LoginWithGoogleUseCase : UseCase<LoginWithGoogleCommand, AuthSession>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly IRoleAssignmentPolicy _roleAssignmentPolicy;
    private readonly SessionIssuer _sessionIssuer;
    private readonly TimeProvider _timeProvider;

    public LoginWithGoogleUseCase(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IGoogleTokenValidator googleTokenValidator,
        IRoleAssignmentPolicy roleAssignmentPolicy,
        SessionIssuer sessionIssuer,
        UseCasePipeline<LoginWithGoogleCommand, AuthSession> pipeline,
        TimeProvider timeProvider) : base(pipeline)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        if (userRepository == null)
        {
            throw new ArgumentNullException(nameof(userRepository));
        }

        if (refreshTokenRepository == null)
        {
            throw new ArgumentNullException(nameof(refreshTokenRepository));
        }

        if (googleTokenValidator == null)
        {
            throw new ArgumentNullException(nameof(googleTokenValidator));
        }

        if (roleAssignmentPolicy == null)
        {
            throw new ArgumentNullException(nameof(roleAssignmentPolicy));
        }

        if (sessionIssuer == null)
        {
            throw new ArgumentNullException(nameof(sessionIssuer));
        }

        if (timeProvider == null)
        {
            throw new ArgumentNullException(nameof(timeProvider));
        }

        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _googleTokenValidator = googleTokenValidator;
        _roleAssignmentPolicy = roleAssignmentPolicy;
        _sessionIssuer = sessionIssuer;
        _timeProvider = timeProvider;
    }

    protected override async Task<Result<AuthSession>> HandleAsync(
        LoginWithGoogleCommand command, CancellationToken cancellationToken)
    {
        GoogleUserInfo? googleUser = await _googleTokenValidator.ValidateAsync(
            command.IdToken,
            cancellationToken);

        if (googleUser == null)
        {
            return Result<AuthSession>.Failure(AppErrors.GoogleTokenInvalid);
        }

        if (googleUser.IsEmailVerified == false)
        {
            return Result<AuthSession>.Failure(AppErrors.GoogleEmailUnverified);
        }

        string normalizedEmail = EmailNormalizer.Normalize(googleUser.Email);
        UserRole configuredRole = _roleAssignmentPolicy.GetRoleForEmail(normalizedEmail);

        User? user = await _userRepository.GetByGoogleSubjectIdAsync(
            googleUser.SubjectId,
            cancellationToken);

        if (user == null)
        {
            user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

            if (user != null)
            {
                bool hadPasswordBeforeLinking = user.HasPassword;

                user.LinkGoogleAccount(googleUser.SubjectId);

                if (hadPasswordBeforeLinking)
                {
                    await RevokeExistingSessionsAsync(user.Id, cancellationToken);
                }
            }
            else
            {
                string displayName = ChooseDisplayName(googleUser, normalizedEmail);

                user = User.RegisterWithGoogle(
                    normalizedEmail,
                    displayName,
                    googleUser.SubjectId,
                    configuredRole);

                await _userRepository.AddAsync(user, cancellationToken);
            }
        }

        if (configuredRole == UserRole.Admin && user.IsAdmin == false)
        {
            user.PromoteToAdmin();
        }

        PendingAuthSession session = await _sessionIssuer.PrepareAsync(user, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthSession>.Success(_sessionIssuer.CreateSession(session));
    }

    private async Task RevokeExistingSessionsAsync(int userId, CancellationToken cancellationToken)
    {
        DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        IReadOnlyList<RefreshToken> activeTokens = await _refreshTokenRepository.GetActiveForUserAsync(
            userId,
            nowUtc,
            cancellationToken);

        foreach (RefreshToken activeToken in activeTokens)
        {
            activeToken.Revoke(nowUtc);
        }
    }

    private static string ChooseDisplayName(GoogleUserInfo googleUser, string normalizedEmail)
    {
        if (string.IsNullOrWhiteSpace(googleUser.Name) == false)
        {
            return googleUser.Name;
        }

        int atSignIndex = normalizedEmail.IndexOf('@');

        return normalizedEmail.Substring(0, atSignIndex);
    }
}
