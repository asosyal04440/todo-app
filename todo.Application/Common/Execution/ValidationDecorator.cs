using FluentValidation;
using FluentValidation.Results;
using TodoApi.Application.Common.Results;

namespace TodoApi.Application.Common.Execution;

public sealed class ValidationDecorator<TRequest, TResponse>(
    IUseCase<TRequest, TResponse> next,
    IReadOnlyList<IValidator<TRequest>> validators) : IUseCase<TRequest, TResponse>
{
    public async Task<Result<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (validators.Count > 0 && request is null)
            return Result<TResponse>.Failure(AppErrors.Validation(new Dictionary<string, string[]> { ["Request"] = ["A request is required."] }));

        List<ValidationFailure> failures = [];
        
        foreach (IValidator<TRequest> validator in validators)
        {
            ValidationResult result = await validator.ValidateAsync(request, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count > 0)
            return Result<TResponse>.Failure(AppErrors.Validation(failures.GroupBy(failure => failure.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray())));
        cancellationToken.ThrowIfCancellationRequested();
        return await next.ExecuteAsync(request, cancellationToken);
    }
}
