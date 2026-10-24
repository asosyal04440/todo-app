using System.Data.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.Exceptions;
using TodoApi.Application.Common.Results;
using TodoApi.API.Models.Responses;

namespace TodoApi.API.ExceptionHandling;

public sealed class DatabaseExceptionHandler : IExceptionHandler
{
    public const string ConcurrencyConflictProblemType = ResultHttpMapping.ConcurrencyConflictProblemType;

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

            problemDetails = ResultHttpMapping.CreateProblem(AppErrors.DataConflict(exception.Message));
        }
        else if (exception is ConcurrencyConflictException)
        {
            _logger.LogWarning(
                "A concurrent request changed or deleted the record first for {RequestPath}.",
                httpContext.Request.Path);

            problemDetails = ResultHttpMapping.CreateProblem(AppErrors.ConcurrentWrite);
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
