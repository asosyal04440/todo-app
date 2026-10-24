using System;
using System.Text;

namespace TodoApi.Infrastructure.Services.Authentication;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    private const int MinimumSigningKeyByteCount = 32;

    private readonly string _issuer;
    private readonly string _audience;
    private readonly string _signingKey;
    private readonly int _accessTokenLifetimeMinutes;

    public JwtSettings(
        string issuer,
        string audience,
        string signingKey,
        int accessTokenLifetimeMinutes)
    {
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("The Jwt:Issuer setting is missing.");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("The Jwt:Audience setting is missing.");
        }

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "The Jwt:SigningKey setting is missing. In development, store it with " +
                "'dotnet user-secrets set Jwt:SigningKey <value> --project todo.API'. " +
                "On a server, provide it through the Jwt__SigningKey environment variable or a secret store.");
        }

        if (Encoding.UTF8.GetByteCount(signingKey) < MinimumSigningKeyByteCount)
        {
            throw new InvalidOperationException(
                $"The Jwt:SigningKey setting must be at least {MinimumSigningKeyByteCount} bytes long.");
        }

        if (accessTokenLifetimeMinutes <= 0)
        {
            throw new InvalidOperationException(
                "The Jwt:AccessTokenLifetimeMinutes setting must be a positive number.");
        }

        _issuer = issuer;
        _audience = audience;
        _signingKey = signingKey;
        _accessTokenLifetimeMinutes = accessTokenLifetimeMinutes;
    }

    public string Issuer
    {
        get
        {
            return _issuer;
        }
    }

    public string Audience
    {
        get
        {
            return _audience;
        }
    }

    public string SigningKey
    {
        get
        {
            return _signingKey;
        }
    }

    public int AccessTokenLifetimeMinutes
    {
        get
        {
            return _accessTokenLifetimeMinutes;
        }
    }
}
