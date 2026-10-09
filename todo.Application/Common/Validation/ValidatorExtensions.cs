using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using TodoApi.Application.Exceptions;

namespace TodoApi.Application.Common.Validation;

public static class ValidatorExtensions
{
    public static async Task ValidateOrThrowAsync<TCommand>(
        this IValidator<TCommand> validator,
        TCommand command,
        CancellationToken cancellationToken)
    {
        ValidationResult result = await validator.ValidateAsync(command, cancellationToken);

        if (result.IsValid == false)
        {
            throw new RequestValidationException(result.Errors);
        }
    }
}
