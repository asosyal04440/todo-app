using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Domain.Enums;
using TodoApi.Infrastructure.Services.Authentication;

namespace TodoApi.API.Authentication;

public static class AuthenticationSetup
{
    private const int DefaultAuthenticationRequestsPerMinute = 10;

    public static IServiceCollection AddTodoAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        JwtSettings jwtSettings = JwtSettings.FromConfiguration(configuration);
        int authenticationRequestsPerMinute = ReadAuthenticationRequestsPerMinute(configuration);
        byte[] signingKeyBytes = Encoding.UTF8.GetBytes(jwtSettings.SigningKey);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(signingKeyBytes),
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = JwtAccessTokenGenerator.RoleClaimType
                };
            });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(UserRole.Admin.ToString());
            });
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitingPolicies.Authentication, httpContext =>
            {
                string clientAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(
                    clientAddress,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = authenticationRequestsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        return services;
    }

    private static int ReadAuthenticationRequestsPerMinute(IConfiguration configuration)
    {
        string? configuredValue = configuration["Security:AuthenticationRequestsPerMinute"];

        if (string.IsNullOrWhiteSpace(configuredValue))
        {
            return DefaultAuthenticationRequestsPerMinute;
        }

        bool isNumber = int.TryParse(configuredValue, out int requestsPerMinute);

        if (isNumber == false || requestsPerMinute <= 0)
        {
            throw new InvalidOperationException(
                "The Security:AuthenticationRequestsPerMinute setting must be a positive whole number.");
        }

        return requestsPerMinute;
    }
}
