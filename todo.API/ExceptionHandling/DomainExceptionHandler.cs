using Microsoft.AspNetCore.Diagnostics;
using TodoApi.API.Models.Responses;
using TodoApi.Application.Common.Results;
using TodoApi.Domain.Exceptions;

namespace TodoApi.API.ExceptionHandling;

public sealed class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        AppError? error = exception switch
        {
            DuplicateNameException duplicate => AppErrors.DuplicateName(duplicate.Message),
            DomainRuleViolationException rule => AppErrors.DomainRule(rule.Message),
            _ => null
        };
        if (error == null) return false;
        logger.LogInformation("Business rule rejected {RequestPath}: {ErrorCode}", context.Request.Path, error.Code);
        await ProblemDetailsWriter.WriteAsync(context, ResultHttpMapping.CreateProblem(error), cancellationToken);
        return true;
    }
}