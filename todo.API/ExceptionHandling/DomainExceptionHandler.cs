using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Domain.Exceptions;

namespace TodoApi.API.ExceptionHandling;

public sealed class DomainExceptionHandler : IExceptionHandler
{
    private readonly ILogger<DomainExceptionHandler> _logger;

    public DomainExceptionHandler(ILogger<DomainExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int statusCode;
        string title;

        if (exception is DuplicateNameException)
        {
            statusCode = StatusCodes.Status409Conflict;
            title = "The name is already in use.";
        }
        else if (exception is DomainRuleViolationException)
        {
            statusCode = StatusCodes.Status400BadRequest;
            title = "The request violates a domain rule.";
        }
        else
        {
            return false;
        }

        _logger.LogInformation(
            "Business rule violated for {RequestPath}: {Reason}",
            httpContext.Request.Path,
            exception.Message);

        ProblemDetails problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message
        };

        await ProblemDetailsWriter.WriteAsync(httpContext, problemDetails, cancellationToken);

        return true;
    }
}
