using System;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Contracts;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Authentication.Common;

public class SessionIssuer
{
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly ISecureTokenGenerator _secureTokenGenerator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly RefreshTokenSettings _refreshTokenSettings;
    private readonly TimeProvider _timeProvider;

    public SessionIssuer(
        IAccessTokenGenerator accessTokenGenerator,
        ISecureTokenGenerator secureTokenGenerator,
        IRefreshTokenRepository refreshTokenRepository,
        RefreshTokenSettings refreshTokenSettings,
        TimeProvider timeProvider)
    {
        if (accessTokenGenerator == null)
        {
            throw new ArgumentNullException(nameof(accessTokenGenerator));
        }

        if (secureTokenGenerator == null)
        {
            throw new ArgumentNullException(nameof(secureTokenGenerator));
        }

        if (refreshTokenRepository == null)
        {
            throw new ArgumentNullException(nameof(refreshTokenRepository));
        }

        if (refreshTokenSettings == null)
        {
            throw new ArgumentNullException(nameof(refreshTokenSettings));
        }

        if (timeProvider == null)
        {
            throw new ArgumentNullException(nameof(timeProvider));
        }

        _accessTokenGenerator = accessTokenGenerator;
        _secureTokenGenerator = secureTokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenSettings = refreshTokenSettings;
        _timeProvider = timeProvider;
    }

    public async Task<AuthSession> IssueAsync(
        User user,
        CancellationToken cancellationToken,
        RefreshToken? replacedToken = null)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        DateTime refreshTokenExpiresAtUtc = nowUtc.Add(_refreshTokenSettings.Lifetime);

        string refreshTokenValue = _secureTokenGenerator.GenerateToken();
        string refreshTokenHash = _secureTokenGenerator.ComputeHash(refreshTokenValue);

        RefreshToken refreshToken = new RefreshToken(
            user.Id,
            refreshTokenHash,
            nowUtc,
            refreshTokenExpiresAtUtc);

        if (replacedToken != null)
        {
            replacedToken.Revoke(nowUtc, refreshTokenHash);
        }

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        AccessToken accessToken = _accessTokenGenerator.GenerateToken(user);
        AuthResultDto result = new AuthResultDto(accessToken, new UserDto(user));

        AuthSession session = new AuthSession(result, refreshTokenValue, refreshTokenExpiresAtUtc);

        return session;
    }
}
