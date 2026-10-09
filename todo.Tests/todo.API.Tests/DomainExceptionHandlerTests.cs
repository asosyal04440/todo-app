using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using TodoApi.API.ExceptionHandling;
using TodoApi.Domain.Exceptions;

namespace todo.API.Tests;

public class DomainExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ReturnsAProblemResponseForDomainRuleErrors()
    {
        DomainExceptionHandler handler = new DomainExceptionHandler(
            NullLogger<DomainExceptionHandler>.Instance);
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/todo-lists";
        httpContext.Response.Body = new MemoryStream();
        DomainRuleViolationException exception = new DomainRuleViolationException(
            "A todo list name cannot be empty.");

        bool wasHandled = await handler.TryHandleAsync(
            httpContext,
            exception,
            TestContext.Current.CancellationToken);

        Assert.True(wasHandled);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);

        httpContext.Response.Body.Position = 0;
        ProblemDetails? problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            httpContext.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(problemDetails);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.Equal("The request violates a domain rule.", problemDetails.Title);
        Assert.Equal("A todo list name cannot be empty.", problemDetails.Detail);
        Assert.Equal("/api/todo-lists", problemDetails.Instance);
    }

    [Fact]
    public async Task TryHandleAsync_DoesNotHandleAnUnrelatedException()
    {
        DomainExceptionHandler handler = new DomainExceptionHandler(
            NullLogger<DomainExceptionHandler>.Instance);
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        bool wasHandled = await handler.TryHandleAsync(
            httpContext,
            new InvalidOperationException("Something else happened."),
            TestContext.Current.CancellationToken);

        Assert.False(wasHandled);
        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal(0, httpContext.Response.Body.Length);
    }
}
