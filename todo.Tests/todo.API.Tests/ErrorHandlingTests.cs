using System.Data.Common;
using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using TodoApi.API.ExceptionHandling;
using TodoApi.Application.Exceptions;
using TodoApi.Domain.Exceptions;

namespace todo.API.Tests;

public class ErrorHandlingTests
{
    [Fact]
    public async Task Validation_ReturnsBadRequestWithTheInvalidFields()
    {
        RequestValidationException exception = new RequestValidationException(new[]
        {
            new ValidationFailure("Name", "'Liste adı' boş olmamalı.")
        });

        JsonElement body = await HandleAsync(
            new ValidationExceptionHandler(NullLogger<ValidationExceptionHandler>.Instance),
            exception,
            StatusCodes.Status400BadRequest);

        Assert.Equal("'Liste adı' boş olmamalı.", body.GetProperty("errors").GetProperty("Name")[0].GetString());
        Assert.Equal("'Liste adı' boş olmamalı.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Business_ReturnsConflictForADuplicateName()
    {
        JsonElement body = await HandleAsync(
            new DomainExceptionHandler(NullLogger<DomainExceptionHandler>.Instance),
            new DuplicateNameException("A todo with the same title already exists in this list."),
            StatusCodes.Status409Conflict);

        Assert.Equal("The name is already in use.", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Database_ReturnsConflictForAUniqueRuleViolation()
    {
        ConflictException exception = new ConflictException(
            "The record conflicts with an existing record.",
            new InvalidOperationException());

        await HandleAsync(
            new DatabaseExceptionHandler(NullLogger<DatabaseExceptionHandler>.Instance),
            exception,
            StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task Database_ReturnsConflictWhenAnotherRequestChangedTheRecordFirst()
    {
        ConcurrencyConflictException exception = new ConcurrencyConflictException(
            "The record was changed or deleted by another request.",
            new InvalidOperationException());

        JsonElement body = await HandleAsync(
            new DatabaseExceptionHandler(NullLogger<DatabaseExceptionHandler>.Instance),
            exception,
            StatusCodes.Status409Conflict);

        Assert.Equal("The record was changed by another request.", body.GetProperty("title").GetString());
        Assert.Equal(DatabaseExceptionHandler.ConcurrencyConflictProblemType, body.GetProperty("type").GetString());
    }

    [Fact]
    public async Task Database_HidesTheDetailsOfAnUnavailableDatabase()
    {
        DataAccessException exception = new DataAccessException(
            "Cannot open database TodoApiDb on server SQLEXPRESS",
            new FakeDbException("Login failed for user sa"));

        JsonElement body = await HandleAsync(
            new DatabaseExceptionHandler(NullLogger<DatabaseExceptionHandler>.Instance),
            exception,
            StatusCodes.Status503ServiceUnavailable);

        string responseText = body.GetRawText();
        Assert.DoesNotContain("SQLEXPRESS", responseText);
        Assert.DoesNotContain("Login failed", responseText);
    }

    [Fact]
    public async Task Database_AlsoRecognizesAnUntranslatedDriverError()
    {
        await HandleAsync(
            new DatabaseExceptionHandler(NullLogger<DatabaseExceptionHandler>.Instance),
            new InvalidOperationException("wrapper", new FakeDbException("timeout")),
            StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task Unexpected_ReturnsAGenericErrorWithATraceId()
    {
        JsonElement body = await HandleAsync(
            new UnhandledExceptionHandler(NullLogger<UnhandledExceptionHandler>.Instance),
            new NullReferenceException("secret internal detail"),
            StatusCodes.Status500InternalServerError);

        Assert.DoesNotContain("secret internal detail", body.GetRawText());
        Assert.Contains(body.GetProperty("traceId").GetString()!, body.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData(typeof(ValidationExceptionHandler))]
    [InlineData(typeof(DomainExceptionHandler))]
    [InlineData(typeof(DatabaseExceptionHandler))]
    public async Task SpecificHandlers_IgnoreExceptionsThatAreNotTheirs(Type handlerType)
    {
        IExceptionHandler handler = CreateHandler(handlerType);
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        bool wasHandled = await handler.TryHandleAsync(
            httpContext,
            new NullReferenceException(),
            TestContext.Current.CancellationToken);

        Assert.False(wasHandled);
    }

    private static IExceptionHandler CreateHandler(Type handlerType)
    {
        if (handlerType == typeof(ValidationExceptionHandler))
        {
            return new ValidationExceptionHandler(NullLogger<ValidationExceptionHandler>.Instance);
        }

        if (handlerType == typeof(DomainExceptionHandler))
        {
            return new DomainExceptionHandler(NullLogger<DomainExceptionHandler>.Instance);
        }

        return new DatabaseExceptionHandler(NullLogger<DatabaseExceptionHandler>.Instance);
    }

    private static async Task<JsonElement> HandleAsync(
        IExceptionHandler handler,
        Exception exception,
        int expectedStatusCode)
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/api/todo-lists";
        httpContext.Response.Body = new MemoryStream();

        bool wasHandled = await handler.TryHandleAsync(
            httpContext,
            exception,
            TestContext.Current.CancellationToken);

        Assert.True(wasHandled);
        Assert.Equal(expectedStatusCode, httpContext.Response.StatusCode);
        Assert.Equal("application/problem+json", httpContext.Response.ContentType);

        httpContext.Response.Body.Position = 0;
        using JsonDocument document = await JsonDocument.ParseAsync(
            httpContext.Response.Body,
            cancellationToken: TestContext.Current.CancellationToken);
        JsonElement body = document.RootElement.Clone();

        Assert.Equal(expectedStatusCode, body.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("traceId").GetString()));

        return body;
    }

    private sealed class FakeDbException : DbException
    {
        public FakeDbException(string message) : base(message)
        {
        }
    }
}
