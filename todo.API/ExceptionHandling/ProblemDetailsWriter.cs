using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace TodoApi.API.ExceptionHandling;

public static class ProblemDetailsWriter
{
    private static readonly JsonSerializerOptions JsonOptions =
        new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public static string GetTraceId(HttpContext httpContext)
    {
        return Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;
    }

    public static async Task WriteAsync(
        HttpContext httpContext,
        ProblemDetails problemDetails,
        CancellationToken cancellationToken)
    {
        int statusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        problemDetails.Instance ??= httpContext.Request.Path.ToString();
        problemDetails.Extensions["traceId"] = GetTraceId(httpContext);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            problemDetails,
            problemDetails.GetType(),
            JsonOptions,
            cancellationToken);
    }
}
