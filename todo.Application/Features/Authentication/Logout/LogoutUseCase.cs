using System;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Authentication.Logout;

public class LogoutUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ISecureTokenGenerator _secureTokenGenerator;
    private readonly TimeProvider _timeProvider;

    public LogoutUseCase(
        IRefreshTokenRepository refreshTokenRepository,
        ISecureTokenGenerator secureTokenGenerator,
        TimeProvider timeProvider)
    {
        if (refreshTokenRepository == null)
        {
            throw new ArgumentNullException(nameof(refreshTokenRepository));
        }

        if (secureTokenGenerator == null)
        {
            throw new ArgumentNullException(nameof(secureTokenGenerator));
        }

        if (timeProvider == null)
        {
            throw new ArgumentNullException(nameof(timeProvider));
        }

        _refreshTokenRepository = refreshTokenRepository;
        _secureTokenGenerator = secureTokenGenerator;
        _timeProvider = timeProvider;
    }

    public async Task ExecuteAsync(
        string? refreshTokenValue,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenValue))
        {
            return;
        }

        string tokenHash = _secureTokenGenerator.ComputeHash(refreshTokenValue);
        RefreshToken? storedToken = await _refreshTokenRepository.GetByTokenHashAsync(
            tokenHash,
            cancellationToken);

        if (storedToken == null || storedToken.IsRevoked)
        {
            return;
        }

        storedToken.Revoke(_timeProvider.GetUtcNow().UtcDateTime);

        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
    }
}
