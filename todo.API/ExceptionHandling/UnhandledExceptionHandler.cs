using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TodoApi.API.ExceptionHandling;

public sealed class UnhandledExceptionHandler : IExceptionHandler
{
    private readonly ILogger<UnhandledExceptionHandler> _logger;

    public UnhandledExceptionHandler(ILogger<UnhandledExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "An unexpected error occurred for {RequestMethod} {RequestPath}.",
            httpContext.Request.Method,
            httpContext.Request.Path);

        ProblemDetails problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = $"Please contact support with this reference: {ProblemDetailsWriter.GetTraceId(httpContext)}"
        };

        await ProblemDetailsWriter.WriteAsync(httpContext, problemDetails, cancellationToken);

        return true;
    }
}
