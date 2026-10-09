using System.Data.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.Exceptions;

namespace TodoApi.API.ExceptionHandling;

public sealed class DatabaseExceptionHandler : IExceptionHandler
{
    public const string ConcurrencyConflictProblemType = "urn:todo:problem:concurrency-conflict";

    private readonly ILogger<DatabaseExceptionHandler> _logger;

    public DatabaseExceptionHandler(ILogger<DatabaseExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ProblemDetails problemDetails;

        if (exception is ConflictException)
        {
            _logger.LogWarning(
                exception,
                "A database uniqueness rule rejected a write for {RequestPath}.",
                httpContext.Request.Path);

            problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "The record conflicts with existing data.",
                Detail = exception.Message
            };
        }
        else if (exception is ConcurrencyConflictException)
        {
            _logger.LogWarning(
                "A concurrent request changed or deleted the record first for {RequestPath}.",
                httpContext.Request.Path);

            problemDetails = new ProblemDetails
            {
                Type = ConcurrencyConflictProblemType,
                Status = StatusCodes.Status409Conflict,
                Title = "The record was changed by another request.",
                Detail = "The record was changed or deleted by another request. Reload and try again."
            };
        }
        else if (exception is DataAccessException || ContainsDatabaseException(exception))
        {
            _logger.LogError(
                exception,
                "A database error occurred for {RequestPath}.",
                httpContext.Request.Path);

            problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "The service is temporarily unavailable.",
                Detail = "Please try again in a moment."
            };
        }
        else
        {
            return false;
        }

        await ProblemDetailsWriter.WriteAsync(httpContext, problemDetails, cancellationToken);

        return true;
    }

    private static bool ContainsDatabaseException(Exception exception)
    {
        Exception? current = exception;

        while (current != null)
        {
            if (current is DbException)
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}
