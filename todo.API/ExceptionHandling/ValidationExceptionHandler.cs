using Microsoft.AspNetCore.Diagnostics;
using TodoApi.API.Models.Responses;
using TodoApi.Application.Common.Results;
using TodoApi.Application.Exceptions;

namespace TodoApi.API.ExceptionHandling;

public sealed class ValidationExceptionHandler(ILogger<ValidationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not RequestValidationException validation) return false;
        logger.LogInformation("Validation failed for {RequestPath}: {InvalidFields}", context.Request.Path, validation.Errors.Keys);
        await ProblemDetailsWriter.WriteAsync(context, ResultHttpMapping.CreateProblem(AppErrors.Validation(validation.Errors)), cancellationToken);
        return true;
    }
}