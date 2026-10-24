using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using TodoApi.Infrastructure.Configuration;
using Microsoft.IdentityModel.Tokens;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Domain.Enums;
using TodoApi.Infrastructure.Services.Authentication;
using TodoApi.Application.Abstractions.Persistence;

namespace TodoApi.API.Authentication;

public static class AuthenticationSetup
{
    public static IServiceCollection AddTodoAuthentication(
        this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, configuredJwt) =>
            {
                JwtOptions jwtSettings = configuredJwt.Value;
                byte[] signingKeyBytes = Encoding.UTF8.GetBytes(jwtSettings.SigningKey);
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
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        string? subject = context.Principal?.FindFirst("sub")?.Value;
                        if (!int.TryParse(subject, out int userId) || userId <= 0)
                        {
                            context.Fail("The account is unavailable.");
                            return;
                        }
                        var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                        if (await users.GetByIdAsync(userId, context.HttpContext.RequestAborted) == null)
                            context.Fail("The account is unavailable.");
                    }
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

        services.AddRateLimiter(options => options.RejectionStatusCode = StatusCodes.Status429TooManyRequests);
        services.AddOptions<RateLimiterOptions>().Configure<IOptions<SecurityOptions>>((options, configuredSecurity) =>
        {
            int authenticationRequestsPerMinute = configuredSecurity.Value.AuthenticationRequestsPerMinute;
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
}
