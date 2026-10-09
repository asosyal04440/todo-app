using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Domain.Entities;

namespace TodoApi.Infrastructure.Services.Authentication;

public sealed class JwtAccessTokenGenerator : IAccessTokenGenerator
{
    public const string RoleClaimType = "role";

    private readonly JwtSettings _jwtSettings;
    private readonly TimeProvider _timeProvider;

    public JwtAccessTokenGenerator(JwtSettings jwtSettings, TimeProvider timeProvider)
    {
        if (jwtSettings == null)
        {
            throw new ArgumentNullException(nameof(jwtSettings));
        }

        if (timeProvider == null)
        {
            throw new ArgumentNullException(nameof(timeProvider));
        }

        _jwtSettings = jwtSettings;
        _timeProvider = timeProvider;
    }

    public AccessToken GenerateToken(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        DateTime issuedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        DateTime expiresAtUtc = issuedAtUtc.AddMinutes(_jwtSettings.AccessTokenLifetimeMinutes);

        Dictionary<string, object> claims = new Dictionary<string, object>
        {
            { JwtRegisteredClaimNames.Sub, user.Id.ToString() },
            { JwtRegisteredClaimNames.Email, user.Email },
            { JwtRegisteredClaimNames.Name, user.DisplayName },
            { RoleClaimType, user.Role.ToString() },
            { JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString() }
        };

        byte[] signingKeyBytes = Encoding.UTF8.GetBytes(_jwtSettings.SigningKey);
        SymmetricSecurityKey signingKey = new SymmetricSecurityKey(signingKeyBytes);
        SigningCredentials signingCredentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256);

        SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            Claims = claims,
            IssuedAt = issuedAtUtc,
            NotBefore = issuedAtUtc,
            Expires = expiresAtUtc,
            SigningCredentials = signingCredentials
        };

        JsonWebTokenHandler tokenHandler = new JsonWebTokenHandler();
        string tokenValue = tokenHandler.CreateToken(tokenDescriptor);

        AccessToken accessToken = new AccessToken(tokenValue, expiresAtUtc);

        return accessToken;
    }
}
