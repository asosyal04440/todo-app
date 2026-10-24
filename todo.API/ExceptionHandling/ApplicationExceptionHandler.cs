using Microsoft.AspNetCore.Diagnostics;
using TodoApi.API.Models.Responses;
using TodoApi.Application.Common.Results;
using TodoApi.Application.Exceptions;

namespace TodoApi.API.ExceptionHandling;

public sealed class ApplicationExceptionHandler(ILogger<ApplicationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        AppError? error = exception switch
        {
            AuthenticationFailedException authentication => AppErrors.Authentication(authentication.Message),
            EmailAlreadyRegisteredException => AppErrors.EmailAlreadyRegistered,
            _ => null
        };
        if (error == null) return false;
        logger.LogInformation("Application request rejected {RequestPath}: {ErrorCode}", context.Request.Path, error.Code);
        await ProblemDetailsWriter.WriteAsync(context, ResultHttpMapping.CreateProblem(error), cancellationToken);
        return true;
    }
}