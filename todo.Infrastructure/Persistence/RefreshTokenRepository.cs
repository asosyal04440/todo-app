using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Domain.Entities;

namespace TodoApi.Infrastructure.Persistence;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly TodoDbContext _todoDbContext;

    public RefreshTokenRepository(TodoDbContext todoDbContext)
    {
        if (todoDbContext == null)
        {
            throw new ArgumentNullException(nameof(todoDbContext));
        }

        _todoDbContext = todoDbContext;
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken)
    {
        RefreshToken? refreshToken = await _todoDbContext.RefreshTokens.FirstOrDefaultAsync(
            existingToken => existingToken.TokenHash == tokenHash,
            cancellationToken);

        return refreshToken;
    }

    public async Task<IReadOnlyList<RefreshToken>> GetActiveForUserAsync(
        int userId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        IQueryable<RefreshToken> refreshTokenQuery = _todoDbContext.RefreshTokens;

        refreshTokenQuery = refreshTokenQuery.Where(refreshToken =>
            refreshToken.UserId == userId &&
            refreshToken.RevokedAtUtc == null &&
            refreshToken.ExpiresAtUtc > nowUtc);

        List<RefreshToken> refreshTokens = await refreshTokenQuery.ToListAsync(cancellationToken);

        IReadOnlyList<RefreshToken> result = refreshTokens;

        return result;
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        if (refreshToken == null)
        {
            throw new ArgumentNullException(nameof(refreshToken));
        }

        await _todoDbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }
}
