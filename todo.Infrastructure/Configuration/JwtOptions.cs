using TodoApi.Application.Features.Authentication.Common;
using TodoApi.Infrastructure.Services.Authentication;

namespace TodoApi.Infrastructure.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; set; }
    public int RefreshTokenLifetimeDays { get; set; }

    public JwtSettings ToSettings() => new(Issuer, Audience, SigningKey, AccessTokenLifetimeMinutes);
    public RefreshTokenSettings ToRefreshTokenSettings() => new(TimeSpan.FromDays(RefreshTokenLifetimeDays));
}
