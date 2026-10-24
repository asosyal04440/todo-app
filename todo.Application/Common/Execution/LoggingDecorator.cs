using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TodoApi.Application.Common.Results;

namespace TodoApi.Application.Common.Execution;

public sealed class LoggingDecorator<TRequest, TResponse>(
    IUseCase<TRequest, TResponse> next,
    ILogger logger,
    string operationName) : IUseCase<TRequest, TResponse>
{
    public async Task<Result<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        long start = Stopwatch.GetTimestamp();
        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["UseCase"] = operationName,
            ["TraceId"] = Activity.Current?.TraceId.ToString()
        });
        logger.LogInformation("Use case {UseCase} started", operationName);
        try
        {
            Result<TResponse> response = await next.ExecuteAsync(request, cancellationToken);
            logger.LogInformation("Use case {UseCase} finished with {Outcome}, {ErrorKind}, {ErrorCode} in {ElapsedMilliseconds} ms",
                operationName, response.IsSuccess ? "Completed" : "Rejected", response.Error?.Kind,
                response.Error?.Code, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Use case {UseCase} cancelled after {ElapsedMilliseconds} ms",
                operationName, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            throw;
        }
        catch (Exception exception)
        {
            
            logger.LogWarning("Use case {UseCase} failed with {ErrorType} after {ElapsedMilliseconds} ms",
                operationName, exception.GetType().Name, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            throw;
        }
    }
}
