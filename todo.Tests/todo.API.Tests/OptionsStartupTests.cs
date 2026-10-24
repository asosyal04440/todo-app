using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using TodoApi.API.Authentication;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Domain.Entities;
using TodoApi.Infrastructure.Configuration;

namespace todo.API.Tests;

public class OptionsStartupTests
{
    [Theory]
    [InlineData("Jwt:SigningKey", "short")]
    [InlineData("Jwt:AccessTokenLifetimeMinutes", "-10")]
    [InlineData("Email:Smtp:Port", "65536")]
    [InlineData("Security:AuthenticationRequestsPerMinute", "0")]
    public void InvalidOptionsPreventTheApiFromStarting(string key, string value)
    {
        using var original = new TodoApiFactory();
        using var factory = original.WithWebHostBuilder(builder => builder.UseSetting(key, value));
        Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
    }

    [Fact]
    public async Task ConfiguredRequestLimitIsUsedByTheAuthenticationMiddleware()
    {
        using var original = new TodoApiFactory();
        using var factory = original.WithWebHostBuilder(builder => builder.UseSetting("Security:AuthenticationRequestsPerMinute", "2"));
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(RequireClientHeaderAttribute.HeaderName, "1");
        for (int attempt = 0; attempt < 3; attempt++)
        {
            using HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login",
                new { email = "", password = "" }, TestContext.Current.CancellationToken);
            Assert.Equal(attempt < 2 ? HttpStatusCode.BadRequest : HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }

    [Fact]
    public async Task TokenGenerationAndBearerValidationUseTheSameConfiguredJwtOptions()
    {
        using var original = new TodoApiFactory();
        using var factory = original.WithWebHostBuilder(builder => builder
            .UseSetting("Jwt:Issuer", "custom-issuer")
            .UseSetting("Jwt:Audience", "custom-audience")
            .UseSetting("Jwt:AccessTokenLifetimeMinutes", "3"));
        using HttpClient client = factory.CreateClient();
        JwtOptions options = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var bearer = factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        AccessToken token = factory.Services.GetRequiredService<IAccessTokenGenerator>().GenerateToken(
            User.RegisterWithPassword("options@example.com", "Options", "test-hash"));
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, bearer.TokenValidationParameters);
        Assert.True(result.IsValid);
        Assert.Equal("custom-issuer", bearer.TokenValidationParameters.ValidIssuer);
        Assert.Equal("custom-audience", bearer.TokenValidationParameters.ValidAudience);
        Assert.Equal(3, options.AccessTokenLifetimeMinutes);
        var jwt = new JsonWebToken(token.Value);
        Assert.Equal(TimeSpan.FromMinutes(3), jwt.ValidTo - jwt.IssuedAt);
    }
}
