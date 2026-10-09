using System;

namespace TodoApi.Application.Abstractions.Authentication;

public sealed class AccessToken
{
    private readonly string _value;
    private readonly DateTime _expiresAtUtc;

    public AccessToken(string value, DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("An access token cannot be empty.", nameof(value));
        }

        _value = value;
        _expiresAtUtc = expiresAtUtc;
    }

    public string Value
    {
        get
        {
            return _value;
        }
    }

    public DateTime ExpiresAtUtc
    {
        get
        {
            return _expiresAtUtc;
        }
    }
}
