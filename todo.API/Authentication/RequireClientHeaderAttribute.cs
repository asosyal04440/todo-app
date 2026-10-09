using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TodoApi.API.Authentication;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireClientHeaderAttribute : Attribute, IActionFilter
{
    public const string HeaderName = "X-Todo-Client";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        string method = context.HttpContext.Request.Method;
        bool isReadOnlyRequest = HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method);

        if (isReadOnlyRequest)
        {
            return;
        }

        bool hasClientHeader = context.HttpContext.Request.Headers.ContainsKey(HeaderName);

        if (hasClientHeader == false)
        {
            ProblemDetails problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "The request is missing a required header.",
                Detail = $"The {HeaderName} header is required for this request."
            };

            context.Result = new BadRequestObjectResult(problemDetails);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
