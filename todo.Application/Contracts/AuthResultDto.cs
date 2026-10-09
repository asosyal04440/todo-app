using System;
using TodoApi.Application.Abstractions.Authentication;

namespace TodoApi.Application.Contracts;

public class AuthResultDto
{
    private readonly string _accessToken;
    private readonly DateTime _expiresAtUtc;
    private readonly UserDto _user;

    public AuthResultDto(AccessToken accessToken, UserDto user)
    {
        if (accessToken == null)
        {
            throw new ArgumentNullException(nameof(accessToken));
        }

        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        _accessToken = accessToken.Value;
        _expiresAtUtc = accessToken.ExpiresAtUtc;
        _user = user;
    }

    public string AccessToken
    {
        get
        {
            return _accessToken;
        }
    }

    public DateTime ExpiresAtUtc
    {
        get
        {
            return _expiresAtUtc;
        }
    }

    public UserDto User
    {
        get
        {
            return _user;
        }
    }
}
