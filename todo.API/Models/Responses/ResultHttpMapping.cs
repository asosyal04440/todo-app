using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.Common.Results;

namespace TodoApi.API.Models.Responses;

public static class ResultHttpMapping
{
    public const string ConcurrencyConflictProblemType = "urn:todo:problem:concurrency-conflict";

    public static ActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller,
        Func<T, ActionResult>? success = null)
    {
        if (result.IsSuccess) return success?.Invoke(result.Value) ?? controller.Ok(result.Value);
        ProblemDetails problem = CreateProblem(result.Error!);
        if (controller.HttpContext is { } context)
        {
            problem.Instance = context.Request.Path;
            problem.Extensions["traceId"] = TodoApi.API.ExceptionHandling.ProblemDetailsWriter.GetTraceId(context);
        }
        var response = new ObjectResult(problem) { StatusCode = problem.Status };
        response.ContentTypes.Add("application/problem+json");
        return response;
    }

    public static ProblemDetails CreateProblem(AppError error)
    {
        if (error.Kind is ErrorKind.NotFound or ErrorKind.Forbidden)
            return new ProblemDetails { Status = 404, Type = "about:blank", Title = "Not Found",
                Detail = "The requested resource was not found." };

        if (error.Code == ErrorCode.ValidationFailed)
            return new ValidationProblemDetails(error.Errors.ToDictionary(pair => pair.Key, pair => pair.Value))
            { Status = 400, Type = "about:blank", Title = "One or more validation errors occurred.", Detail = error.Message };

        int status = error.Kind switch
        {
            ErrorKind.Validation => 400,
            ErrorKind.Unauthorized => 401,
            ErrorKind.Conflict => 409,
            _ => throw new InvalidOperationException("Unmapped error kind.")
        };
        string title = error.Code switch
        {
            ErrorCode.DuplicateName => "The name is already in use.",
            ErrorCode.EmailAlreadyRegistered => "The email address is already registered.",
            ErrorCode.ConcurrentWrite => "The record was changed by another request.",
            ErrorCode.DomainRuleViolation => "The request violates a domain rule.",
            _ => error.Kind == ErrorKind.Unauthorized ? "Authentication failed." : "The record conflicts with existing data."
        };
        return new ProblemDetails { Status = status, Title = title, Detail = error.Message,
            Type = error.Code == ErrorCode.ConcurrentWrite ? ConcurrencyConflictProblemType : "about:blank" };
    }
}
