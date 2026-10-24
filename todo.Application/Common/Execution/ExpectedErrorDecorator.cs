using TodoApi.Application.Common.Results;
using TodoApi.Application.Exceptions;
using TodoApi.Domain.Exceptions;

namespace TodoApi.Application.Common.Execution;

public sealed class ExpectedErrorDecorator<TRequest, TResponse>(IUseCase<TRequest, TResponse> next) : IUseCase<TRequest, TResponse>
{
    public async Task<Result<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        try { return await next.ExecuteAsync(request, cancellationToken); }
        catch (RequestValidationException error) { return Result<TResponse>.Failure(AppErrors.Validation(error.Errors)); }
        catch (DuplicateNameException error) { return Result<TResponse>.Failure(AppErrors.DuplicateName(error.Message)); }
        catch (DomainRuleViolationException error) { return Result<TResponse>.Failure(AppErrors.DomainRule(error.Message)); }
        catch (EmailAlreadyRegisteredException) { return Result<TResponse>.Failure(AppErrors.EmailAlreadyRegistered); }
        catch (AuthenticationFailedException error) { return Result<TResponse>.Failure(AppErrors.Authentication(error.Message)); }
        catch (ConcurrencyConflictException) { return Result<TResponse>.Failure(AppErrors.ConcurrentWrite); }
        catch (ConflictException error) { return Result<TResponse>.Failure(AppErrors.DataConflict(error.Message)); }
    }
}
