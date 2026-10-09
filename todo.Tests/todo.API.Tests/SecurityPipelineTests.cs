using System.Net;
using TodoApi.API.Authentication;

namespace todo.API.Tests;

public class SecurityPipelineTests : IClassFixture<TodoApiFactory>
{
    private readonly TodoApiFactory _factory;

    public SecurityPipelineTests(TodoApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Responses_IncludeTheSecurityHeaders()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/auth/config",
            TestContext.Current.CancellationToken);

        Assert.Equal("nosniff", GetHeader(response, "X-Content-Type-Options"));
        Assert.Equal("DENY", GetHeader(response, "X-Frame-Options"));
        Assert.Equal("default-src 'none'; frame-ancestors 'none'", GetHeader(response, "Content-Security-Policy"));
        Assert.Equal("no-referrer", GetHeader(response, "Referrer-Policy"));
        Assert.Equal("no-store", GetHeader(response, "Cache-Control"));
        Assert.False(response.Headers.Contains("Server"));
    }

    [Fact]
    public async Task UnauthorizedResponses_AlsoIncludeTheSecurityHeaders()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            "/api/todo-lists",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("nosniff", GetHeader(response, "X-Content-Type-Options"));
    }

    [Fact]
    public async Task Refresh_IsRejectedWithoutTheClientHeader()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(
            "/api/auth/refresh",
            content: null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithoutACookieReturnsUnauthorizedAndClearsTheCookie()
    {
        HttpClient client = _factory.CreateClient();
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add(RequireClientHeaderAttribute.HeaderName, "1");

        HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(GetSetCookieHeaders(response), cookie => cookie.StartsWith("todo_refresh=;"));
    }

    [Fact]
    public async Task Logout_DeletesTheRefreshCookieWithSecureAttributes()
    {
        HttpClient client = _factory.CreateClient();
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Add(RequireClientHeaderAttribute.HeaderName, "1");

        HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        string refreshCookie = Assert.Single(
            GetSetCookieHeaders(response),
            cookie => cookie.StartsWith("todo_refresh="));
        string normalizedCookie = refreshCookie.ToLowerInvariant();

        Assert.Contains("httponly", normalizedCookie);
        Assert.Contains("secure", normalizedCookie);
        Assert.Contains("samesite=strict", normalizedCookie);
        Assert.Contains("path=/api/auth", normalizedCookie);
    }

    [Fact]
    public async Task Cors_AllowsCredentialsOnlyForTheConfiguredOrigin()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage allowed = await SendPreflightAsync(client, TodoApiFactory.AllowedOrigin);
        HttpResponseMessage rejected = await SendPreflightAsync(client, "https://evil.example.com");

        Assert.Equal(TodoApiFactory.AllowedOrigin, GetHeader(allowed, "Access-Control-Allow-Origin"));
        Assert.Equal("true", GetHeader(allowed, "Access-Control-Allow-Credentials"));
        Assert.Null(GetHeader(rejected, "Access-Control-Allow-Origin"));
    }

    private static async Task<HttpResponseMessage> SendPreflightAsync(HttpClient client, string origin)
    {
        HttpRequestMessage preflight = new HttpRequestMessage(HttpMethod.Options, "/api/auth/refresh");
        preflight.Headers.Add("Origin", origin);
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", RequireClientHeaderAttribute.HeaderName);

        return await client.SendAsync(preflight, TestContext.Current.CancellationToken);
    }

    private static string? GetHeader(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out IEnumerable<string>? values))
        {
            return string.Join(", ", values);
        }

        if (response.Content.Headers.TryGetValues(name, out IEnumerable<string>? contentValues))
        {
            return string.Join(", ", contentValues);
        }

        return null;
    }

    private static IEnumerable<string> GetSetCookieHeaders(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? values))
        {
            return values;
        }

        return Array.Empty<string>();
    }
}
