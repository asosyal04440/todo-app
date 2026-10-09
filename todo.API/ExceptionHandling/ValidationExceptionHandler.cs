using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.Exceptions;

namespace TodoApi.API.ExceptionHandling;

public sealed class ValidationExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ValidationExceptionHandler> _logger;

    public ValidationExceptionHandler(ILogger<ValidationExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not RequestValidationException validationException)
        {
            return false;
        }

        _logger.LogInformation(
            "Validation failed for {RequestPath}: {InvalidFields}",
            httpContext.Request.Path,
            validationException.Errors.Keys);

        Dictionary<string, string[]> errors = new Dictionary<string, string[]>(validationException.Errors);

        ValidationProblemDetails problemDetails = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",

            Detail = errors.Values.SelectMany(messages => messages).FirstOrDefault()
        };

        await ProblemDetailsWriter.WriteAsync(httpContext, problemDetails, cancellationToken);

        return true;
    }
}
