using System;
using TodoApi.Application.Contracts;

namespace TodoApi.Application.Features.Authentication.Common;

public sealed class AuthSession
{
    private readonly AuthResultDto _result;
    private readonly string _refreshToken;
    private readonly DateTime _refreshTokenExpiresAtUtc;

    public AuthSession(AuthResultDto result, string refreshToken, DateTime refreshTokenExpiresAtUtc)
    {
        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException("A refresh token cannot be empty.", nameof(refreshToken));
        }

        _result = result;
        _refreshToken = refreshToken;
        _refreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc;
    }

    public AuthResultDto Result
    {
        get
        {
            return _result;
        }
    }

    public string RefreshToken
    {
        get
        {
            return _refreshToken;
        }
    }

    public DateTime RefreshTokenExpiresAtUtc
    {
        get
        {
            return _refreshTokenExpiresAtUtc;
        }
    }
}
