using TodoApi.Application.Common.Results;

namespace TodoApi.Application.Common.Execution;

public abstract class UseCase<TRequest, TResponse> : IUseCase<TRequest, TResponse>
{
    private readonly IUseCase<TRequest, TResponse> _execution;

    protected UseCase(UseCasePipeline<TRequest, TResponse> pipeline)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        _execution = pipeline.Wrap(new Handler(HandleAsync), GetType().Name);
    }

    public Task<Result<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default) =>
        _execution.ExecuteAsync(request, cancellationToken);

    protected abstract Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken);

    private sealed class Handler(Func<TRequest, CancellationToken, Task<Result<TResponse>>> execute) : IUseCase<TRequest, TResponse>
    {
        public Task<Result<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default) =>
            execute(request, cancellationToken);
    }
}

