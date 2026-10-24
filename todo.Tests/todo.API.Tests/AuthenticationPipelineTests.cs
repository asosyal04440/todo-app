using Microsoft.Extensions.Options;
using TodoApi.Infrastructure.Configuration;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.API.Models.Responses;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Infrastructure.Services.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TodoApi.Infrastructure.Persistence;

namespace todo.API.Tests;

public class AuthenticationPipelineTests : IClassFixture<TodoApiFactory>
{
    private readonly TodoApiFactory _factory;

    public AuthenticationPipelineTests(TodoApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/todo-lists")]
    [InlineData("/api/todo-lists/1/todos")]
    [InlineData("/api/auth/me")]
    [InlineData("/api/admin/users")]
    public async Task ProtectedEndpoints_ReturnUnauthorizedWithoutAToken(string url)
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthConfig_IsAvailableWithoutAToken()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/auth/config",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AuthConfigResponse? config = await response.Content.ReadFromJsonAsync<AuthConfigResponse>(
            TestContext.Current.CancellationToken);
        Assert.NotNull(config);
    }

    [Fact]
    public async Task AdminEndpoint_ReturnsForbiddenForANormalUser()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        var options = new DbContextOptionsBuilder<TodoDbContext>().UseSqlite(connection).Options;
        using var factory = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TodoDbContext>();
            services.RemoveAll<DbContextOptions<TodoDbContext>>();
            services.AddSingleton(options);
            services.AddScoped<TodoDbContext>();
        }));
        string token;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
            await db.Database.EnsureCreatedAsync(ct);
            var user = User.RegisterWithPassword("normal@example.com", "Normal", "hash");
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
            token = scope.ServiceProvider.GetRequiredService<IAccessTokenGenerator>().GenerateToken(user).Value;
        }
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        HttpResponseMessage response = await client.GetAsync(
            "/api/admin/users",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_RejectsAnExpiredToken()
    {
        HttpClient client = _factory.CreateClient();
        DateTimeOffset twoHoursAgo = DateTimeOffset.UtcNow.AddHours(-2);
        string expiredToken = CreateToken(UserRole.User, new FixedTimeProvider(twoHoursAgo));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        HttpResponseMessage response = await client.GetAsync(
            "/api/todo-lists",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_RejectsATokenSignedWithAnotherKey()
    {
        HttpClient client = _factory.CreateClient();
        var forgedSettings = Options.Create(new JwtOptions
        { Issuer = "TodoApi", Audience = "TodoWeb", SigningKey = "an-attacker-key-that-is-long-enough-for-hmac", AccessTokenLifetimeMinutes = 60, RefreshTokenLifetimeDays = 7 });
        JwtAccessTokenGenerator forgedGenerator = new JwtAccessTokenGenerator(
            forgedSettings,
            TimeProvider.System);
        User user = User.RegisterWithGoogle("saldirgan@example.com", "Saldırgan", "google-subject-1", UserRole.Admin);
        string forgedToken = forgedGenerator.GenerateToken(user).Value;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forgedToken);

        HttpResponseMessage response = await client.GetAsync(
            "/api/admin/users",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private string CreateToken(UserRole role, TimeProvider timeProvider)
    {
        IOptions<JwtOptions> settings = _factory.Services.GetRequiredService<IOptions<JwtOptions>>();
        JwtAccessTokenGenerator generator = new JwtAccessTokenGenerator(settings, timeProvider);
        User user = User.RegisterWithGoogle("test@example.com", "Test", "google-subject-1", role);

        AccessToken accessToken = generator.GenerateToken(user);

        return accessToken.Value;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _now;
        }
    }
}
