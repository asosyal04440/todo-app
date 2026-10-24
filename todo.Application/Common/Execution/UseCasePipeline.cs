using FluentValidation;
using Microsoft.Extensions.Logging;

namespace TodoApi.Application.Common.Execution;

public sealed class UseCasePipeline<TRequest, TResponse>
{
    private readonly IValidator<TRequest>[] _validators;
    private readonly ILoggerFactory _loggerFactory;

    public UseCasePipeline(IEnumerable<IValidator<TRequest>> validators, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(validators);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _validators = validators.ToArray();
        _loggerFactory = loggerFactory;
    }

    public IUseCase<TRequest, TResponse> Wrap(IUseCase<TRequest, TResponse> handler, string operationName)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);
        var validated = new ValidationDecorator<TRequest, TResponse>(handler, _validators);
        var expectedErrors = new ExpectedErrorDecorator<TRequest, TResponse>(validated);
        return new LoggingDecorator<TRequest, TResponse>(expectedErrors,
            _loggerFactory.CreateLogger("TodoApi.Application.UseCases"), operationName);
    }
}
