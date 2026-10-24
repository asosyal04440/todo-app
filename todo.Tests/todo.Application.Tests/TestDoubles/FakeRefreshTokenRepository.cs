using System.Reflection;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Domain.Entities;

namespace todo.Application.Tests.TestDoubles;

public sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly List<RefreshToken> _refreshTokens = new List<RefreshToken>();
    private int _nextId = 1;

    public IReadOnlyList<RefreshToken> RefreshTokens
    {
        get
        {
            return _refreshTokens;
        }
    }

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        RefreshToken? refreshToken = _refreshTokens.Find(
            existingToken => existingToken.TokenHash == tokenHash);

        return Task.FromResult(refreshToken);
    }

    public Task<IReadOnlyList<RefreshToken>> GetActiveForUserAsync(
        int userId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<RefreshToken> results = _refreshTokens
            .Where(refreshToken => refreshToken.UserId == userId && refreshToken.IsActive(nowUtc))
            .ToList();

        return Task.FromResult(results);
    }

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        _refreshTokens.Add(refreshToken);

        return Task.CompletedTask;
    }

    public void ApplyChanges()
    {
        foreach (RefreshToken refreshToken in _refreshTokens)
        {
            if (refreshToken.User != null)
            {
                typeof(RefreshToken).GetProperty(nameof(RefreshToken.UserId))!
                    .GetSetMethod(nonPublic: true)!
                    .Invoke(refreshToken, new object[] { refreshToken.User.Id });
            }

            if (refreshToken.Id == 0)
            {
                PropertyInfo property = typeof(RefreshToken).GetProperty(nameof(RefreshToken.Id))!;
                property.GetSetMethod(nonPublic: true)!.Invoke(refreshToken, new object[] { _nextId });
                _nextId++;
            }
        }

    }
}
