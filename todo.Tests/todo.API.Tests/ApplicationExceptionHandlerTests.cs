using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using TodoApi.API.ExceptionHandling;
using TodoApi.Application.Exceptions;

namespace todo.API.Tests;

public class ApplicationExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ReturnsUnauthorizedForAFailedSignIn()
    {
        ProblemDetails problemDetails = await HandleAsync(
            new AuthenticationFailedException("The email address or password is incorrect."),
            StatusCodes.Status401Unauthorized);

        Assert.Equal("Authentication failed.", problemDetails.Title);
        Assert.Equal("The email address or password is incorrect.", problemDetails.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_ReturnsConflictForADuplicateEmail()
    {
        ProblemDetails problemDetails = await HandleAsync(
            new EmailAlreadyRegisteredException(),
            StatusCodes.Status409Conflict);

        Assert.Equal("The email address is already registered.", problemDetails.Title);
    }

    [Fact]
    public async Task TryHandleAsync_DoesNotHandleAnUnrelatedException()
    {
        ApplicationExceptionHandler handler = new ApplicationExceptionHandler(
            NullLogger<ApplicationExceptionHandler>.Instance);
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        bool wasHandled = await handler.TryHandleAsync(
            httpContext,
            new InvalidOperationException("Something else happened."),
            TestContext.Current.CancellationToken);

        Assert.False(wasHandled);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }

    private static async Task<ProblemDetails> HandleAsync(Exception exception, int expectedStatusCode)
    {
        ApplicationExceptionHandler handler = new ApplicationExceptionHandler(
            NullLogger<ApplicationExceptionHandler>.Instance);
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/auth/login";
        httpContext.Response.Body = new MemoryStream();

        bool wasHandled = await handler.TryHandleAsync(
            httpContext,
            exception,
            TestContext.Current.CancellationToken);

        Assert.True(wasHandled);
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);

        httpContext.Response.Body.Position = 0;
        ProblemDetails? problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            httpContext.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(problemDetails);
        Assert.Equal(expectedStatusCode, problemDetails.Status);

        return problemDetails;
    }
}
