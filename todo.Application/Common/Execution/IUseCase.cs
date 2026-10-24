using TodoApi.Application.Common.Results;

namespace TodoApi.Application.Common.Execution;

public interface IUseCase<TRequest, TResponse>
{
    Task<Result<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}

