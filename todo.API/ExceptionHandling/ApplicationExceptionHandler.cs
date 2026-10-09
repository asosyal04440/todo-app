using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.Exceptions;

namespace TodoApi.API.ExceptionHandling;

public sealed class ApplicationExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApplicationExceptionHandler> _logger;

    public ApplicationExceptionHandler(ILogger<ApplicationExceptionHandler> logger)
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

        if (exception is AuthenticationFailedException)
        {
            statusCode = StatusCodes.Status401Unauthorized;
            title = "Authentication failed.";

            _logger.LogWarning("Authentication failed for {RequestPath}.", httpContext.Request.Path);
        }
        else if (exception is EmailAlreadyRegisteredException)
        {
            statusCode = StatusCodes.Status409Conflict;
            title = "The email address is already registered.";

            _logger.LogInformation("Registration rejected because the email address is already in use.");
        }
        else
        {
            return false;
        }

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
