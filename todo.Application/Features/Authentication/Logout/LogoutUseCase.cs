using TodoApi.Application.Common.Results;
using TodoApi.Application.Common.Execution;
using System;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Authentication.Logout;

public class LogoutUseCase : UseCase<string?, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ISecureTokenGenerator _secureTokenGenerator;
    private readonly TimeProvider _timeProvider;

    public LogoutUseCase(
        IUnitOfWork unitOfWork,
        IRefreshTokenRepository refreshTokenRepository,
        ISecureTokenGenerator secureTokenGenerator,
        TimeProvider timeProvider,
        UseCasePipeline<string?, Unit> pipeline) : base(pipeline)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

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

        _unitOfWork = unitOfWork;
        _refreshTokenRepository = refreshTokenRepository;
        _secureTokenGenerator = secureTokenGenerator;
        _timeProvider = timeProvider;
    }

    protected override async Task<Result<Unit>> HandleAsync(
        string? refreshTokenValue, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenValue))
        {
            return Result<Unit>.Success(default);
        }

        string tokenHash = _secureTokenGenerator.ComputeHash(refreshTokenValue);
        RefreshToken? storedToken = await _refreshTokenRepository.GetByTokenHashAsync(
            tokenHash,
            cancellationToken);

        if (storedToken == null || storedToken.IsRevoked)
        {
            return Result<Unit>.Success(default);
        }

        storedToken.Revoke(_timeProvider.GetUtcNow().UtcDateTime);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Unit>.Success(default);
    }
}
