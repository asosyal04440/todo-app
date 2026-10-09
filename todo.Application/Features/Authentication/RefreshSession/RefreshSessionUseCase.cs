using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Exceptions;
using TodoApi.Application.Features.Authentication.Common;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Authentication.RefreshSession;

public class RefreshSessionUseCase
{
    private const string InvalidSessionMessage = "The session has expired. Please sign in again.";

    private static readonly TimeSpan ConcurrentRefreshGracePeriod = TimeSpan.FromSeconds(30);

    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISecureTokenGenerator _secureTokenGenerator;
    private readonly SessionIssuer _sessionIssuer;
    private readonly TimeProvider _timeProvider;

    public RefreshSessionUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        ISecureTokenGenerator secureTokenGenerator,
        SessionIssuer sessionIssuer,
        TimeProvider timeProvider)
    {
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

        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _secureTokenGenerator = secureTokenGenerator;
        _sessionIssuer = sessionIssuer;
        _timeProvider = timeProvider;
    }

    public async Task<AuthSession> ExecuteAsync(
        string? refreshTokenValue,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenValue))
        {
            throw new AuthenticationFailedException(InvalidSessionMessage);
        }

        string tokenHash = _secureTokenGenerator.ComputeHash(refreshTokenValue);
        RefreshToken? storedToken = await _refreshTokenRepository.GetByTokenHashAsync(
            tokenHash,
            cancellationToken);

        if (storedToken == null)
        {
            throw new AuthenticationFailedException(InvalidSessionMessage);
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
            }

            throw new AuthenticationFailedException(InvalidSessionMessage);
        }

        if (storedToken.IsActive(nowUtc) == false)
        {
            throw new AuthenticationFailedException(InvalidSessionMessage);
        }

        User? user = await _userRepository.GetByIdAsync(storedToken.UserId, cancellationToken);

        if (user == null)
        {
            throw new AuthenticationFailedException(InvalidSessionMessage);
        }

        AuthSession session = await _sessionIssuer.IssueAsync(user, cancellationToken, storedToken);

        return session;
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

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }
}
