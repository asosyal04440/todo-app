using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TodoApi.API.Models.Responses;
using TodoApi.Application.Common.Results;

namespace todo.API.Tests;

public class ResultHttpMappingTests
{
    [Fact]
    public void MissingAndForbiddenResourcesHaveTheSamePublicProblem()
    {
        ProblemDetails missing = ResultHttpMapping.CreateProblem(AppErrors.ListNotFound);
        ProblemDetails forbidden = ResultHttpMapping.CreateProblem(AppErrors.AccessDenied);
        ProblemDetails todo = ResultHttpMapping.CreateProblem(AppErrors.TodoNotFound);
        foreach (ProblemDetails problem in new[] { forbidden, todo })
        {
            Assert.Equal(404, problem.Status);
            Assert.Equal(missing.Title, problem.Title);
            Assert.Equal(missing.Detail, problem.Detail);
            Assert.Equal(missing.Type, problem.Type);
            Assert.Empty(problem.Extensions);
        }
    }

    [Theory]
    [InlineData(ErrorCode.InvalidCredentials, 401)]
    [InlineData(ErrorCode.InvalidSession, 401)]
    [InlineData(ErrorCode.DuplicateName, 409)]
    [InlineData(ErrorCode.DomainRuleViolation, 400)]
    [InlineData(ErrorCode.ConcurrentWrite, 409)]
    public void ExpectedErrorsKeepTheirHttpContract(ErrorCode code, int status)
    {
        AppError error = code switch
        {
            ErrorCode.InvalidCredentials => AppErrors.InvalidCredentials,
            ErrorCode.InvalidSession => AppErrors.InvalidSession,
            ErrorCode.DuplicateName => AppErrors.DuplicateName("Already used"),
            ErrorCode.DomainRuleViolation => AppErrors.DomainRule("Invalid value"),
            _ => AppErrors.ConcurrentWrite
        };
        ProblemDetails problem = ResultHttpMapping.CreateProblem(error);
        Assert.Equal(status, problem.Status);
        Assert.Equal(error.Message, problem.Detail);
        if (code == ErrorCode.ConcurrentWrite)
            Assert.Equal("urn:todo:problem:concurrency-conflict", problem.Type);
    }

    [Fact]
    public void ValidationKeepsFieldErrorsAndFailureNeverRunsSuccessActions()
    {
        var controller = new ProbeController { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        controller.Request.Path = "/api/todo-lists";
        controller.HttpContext.TraceIdentifier = "trace-123";
        bool callbackRan = false;
        var result = Result<string>.Failure(AppErrors.Validation(new Dictionary<string, string[]> { ["Name"] = ["Required"] }));
        var response = Assert.IsType<ObjectResult>(result.ToActionResult(controller, value =>
        {
            callbackRan = true;
            return controller.Ok(value);
        }));
        var problem = Assert.IsType<ValidationProblemDetails>(response.Value);
        Assert.Equal(400, response.StatusCode);
        Assert.Contains("application/problem+json", response.ContentTypes);
        Assert.Equal("Required", Assert.Single(problem.Errors["Name"]));
        Assert.Equal("/api/todo-lists", problem.Instance);
        Assert.Equal("trace-123", problem.Extensions["traceId"]);
        Assert.False(callbackRan);
    }

    [Fact]
    public void SuccessPreservesTheControllersCustomStatusAndPayload()
    {
        var controller = new ProbeController();
        var response = Assert.IsType<CreatedResult>(Result<string>.Success("saved").ToActionResult(controller,
            value => controller.Created("/api/todo-lists/1", value)));
        Assert.Equal(201, response.StatusCode);
        Assert.Equal("saved", response.Value);
    }

    private sealed class ProbeController : ControllerBase;
}
