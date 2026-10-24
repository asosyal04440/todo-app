using TodoApi.Application.Common.Results;
using TodoApi.Application.Common.Execution;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Features.Authentication.Common;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Authentication.RefreshSession;

public class RefreshSessionUseCase : UseCase<string?, AuthSession>
{
    private readonly IUnitOfWork _unitOfWork;

    private static readonly TimeSpan ConcurrentRefreshGracePeriod = TimeSpan.FromSeconds(30);

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISecureTokenGenerator _secureTokenGenerator;
    private readonly SessionIssuer _sessionIssuer;
    private readonly TimeProvider _timeProvider;

    public RefreshSessionUseCase(
        IUnitOfWork unitOfWork,
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        ISecureTokenGenerator secureTokenGenerator,
        SessionIssuer sessionIssuer,
        TimeProvider timeProvider,
        UseCasePipeline<string?, AuthSession> pipeline) : base(pipeline)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        if (refreshTokenRepository == null)
        {
            throw new ArgumentNullException(nameof(refreshTokenRepository));
        }

        if (userRepository == null)
        {
            throw new ArgumentNullException(nameof(userRepository));
        }

        if (secureTokenGenerator == null)
        {
            throw new ArgumentNullException(nameof(secureTokenGenerator));
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
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _secureTokenGenerator = secureTokenGenerator;
        _sessionIssuer = sessionIssuer;
        _timeProvider = timeProvider;
    }

    protected override async Task<Result<AuthSession>> HandleAsync(
        string? refreshTokenValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenValue))
        {
            return Result<AuthSession>.Failure(AppErrors.InvalidSession);
        }

        string tokenHash = _secureTokenGenerator.ComputeHash(refreshTokenValue);
        RefreshToken? storedToken = await _refreshTokenRepository.GetByTokenHashAsync(
            tokenHash,
            cancellationToken);

        if (storedToken == null)
        {
            return Result<AuthSession>.Failure(AppErrors.InvalidSession);
        }

        DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        if (storedToken.IsRevoked)
        {
            bool wasJustRotated =
                storedToken.ReplacedByTokenHash != null &&
                storedToken.RevokedAtUtc.HasValue &&
                nowUtc - storedToken.RevokedAtUtc.Value <= ConcurrentRefreshGracePeriod;

            if (wasJustRotated == false)
            {
                await RevokeAllSessionsAsync(storedToken.UserId, nowUtc, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result<AuthSession>.Failure(AppErrors.InvalidSession);
        }

        if (storedToken.IsActive(nowUtc) == false)
        {
            return Result<AuthSession>.Failure(AppErrors.InvalidSession);
        }

        User? user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);

        if (user == null)
        {
            return Result<AuthSession>.Failure(AppErrors.InvalidSession);
        }

        PendingAuthSession session = await _sessionIssuer.PrepareAsync(user, cancellationToken, storedToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<AuthSession>.Success(_sessionIssuer.CreateSession(session));
    }

    private async Task RevokeAllSessionsAsync(
        int userId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<RefreshToken> activeTokens = await _refreshTokenRepository.GetActiveForUserAsync(
            userId,
            nowUtc,
            cancellationToken);

        foreach (RefreshToken activeToken in activeTokens)
        {
            activeToken.Revoke(nowUtc);
        }
    }
}
